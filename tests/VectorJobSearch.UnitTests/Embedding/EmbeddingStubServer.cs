using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace VectorJobSearch.UnitTests.Embedding;

/// <summary>
/// In-process stub for an OpenAI-compatible embeddings endpoint (plan.md R-5).
/// Serves POST {BaseUrl}/embeddings (tests use base URL http://localhost:0/v1).
/// Records every request (method, path, Authorization header, raw JSON body incl.
/// model name and input array) and is scriptable per request: success payload
/// (data shuffled by "index" so order mapping is exercised), HTTP 429, HTTP 5xx,
/// non-429 4xx, connection drop, and a vector containing non-finite values
/// (NaN / ±Infinity). Zero external HTTP — the provider's HttpClient is
/// TestServer.CreateClient().
/// </summary>
public sealed class EmbeddingStubServer : IDisposable
{
    /// <summary>A recorded embeddings request.</summary>
    public sealed record RecordedRequest(string Method, string Path, string? Authorization, string Body);

    /// <summary>Scriptable response modes.</summary>
    public enum ResponseMode
    {
        Success,
        TooManyRequests429,
        ServerError5xx,
        NonTransient4xx,
        ConnectionDrop,
        NonFiniteVector,
    }

    private readonly List<RecordedRequest> _requests = new();
    private WebApplication? _app;
    private HttpClient? _client;

    /// <summary>Persistent response mode (used once <see cref="ModeQueue"/> is empty). Default: Success.</summary>
    public ResponseMode Mode { get; set; } = ResponseMode.Success;

    /// <summary>Per-request response modes, consumed first (T-4.3 retry sequencing: 429 then 5xx then success).</summary>
    public Queue<ResponseMode> ModeQueue { get; } = new();

    /// <summary>Dimensionality of each vector the stub returns.</summary>
    public int Dimensions { get; set; } = 3;

    /// <summary>All recorded requests, in order.</summary>
    public IReadOnlyList<RecordedRequest> Requests => _requests;

    /// <summary>Number of requests recorded so far (asserted == 0 for "no network call" ACs).</summary>
    public int RequestCount => _requests.Count;

    private ResponseMode NextMode() => ModeQueue.Count > 0 ? ModeQueue.Dequeue() : Mode;

    /// <summary>Success payload with <paramref name="count"/> data entries, deliberately shuffled
    /// by "index" so response-index → input-order mapping is exercised. Vector values encode
    /// their origin index: value(origin, d) = origin * Dimensions + d + 1.</summary>
    private static string SuccessPayload(int count, int dimensions)
    {
        var sb = new StringBuilder("{\"data\":[");
        for (var slot = 0; slot < count; slot++)
        {
            // Reverse (shuffled) origin-index order.
            var origin = count - 1 - slot;
            if (slot > 0)
            {
                sb.Append(',');
            }
            sb.Append("{\"embedding\":[");
            for (var d = 0; d < dimensions; d++)
            {
                if (d > 0)
                {
                    sb.Append(',');
                }
                sb.Append((origin * dimensions + d + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            sb.Append("],\"index\":");
            sb.Append(origin);
            sb.Append('}');
        }
        sb.Append("]}");
        return sb.ToString();
    }

    /// <summary>A payload with NaN / +Infinity / -Infinity in the vectors (spec Edge Cases).</summary>
    private static string NonFinitePayload()
        => "{\"data\":[{\"embedding\":[NaN,Infinity,-Infinity,1],\"index\":0}]}";

    /// <summary>Creates (once) the in-process TestServer and returns an HttpClient wired to it.</summary>
    public HttpClient CreateClient()
    {
        if (_client is not null)
        {
            return _client;
        }

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();

        app.Map("/v1/embeddings", async ctx =>
        {
            string body;
            using (var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync();
            }

            _requests.Add(new RecordedRequest(
                ctx.Request.Method,
                ctx.Request.Path.ToString(),
                ctx.Request.Headers.Authorization.ToString(),
                body));

            var inputCount = 1;
            try
            {
                using var doc = JsonDocument.Parse(body);
                inputCount = doc.RootElement.GetProperty("input").GetArrayLength();
            }
            catch (JsonException)
            {
                // Non-JSON body: respond as if a single input.
            }

            switch (NextMode())
            {
                case ResponseMode.Success:
                    ctx.Response.StatusCode = StatusCodes.Status200OK;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.WriteAsync(SuccessPayload(inputCount, Dimensions));
                    break;
                case ResponseMode.NonFiniteVector:
                    ctx.Response.StatusCode = StatusCodes.Status200OK;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.WriteAsync(NonFinitePayload());
                    break;
                case ResponseMode.TooManyRequests429:
                    ctx.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await ctx.Response.WriteAsync("{\"error\":\"rate limited\"}");
                    break;
                case ResponseMode.ServerError5xx:
                    ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await ctx.Response.WriteAsync("{\"error\":\"upstream exploded\"}");
                    break;
                case ResponseMode.NonTransient4xx:
                    ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await ctx.Response.WriteAsync("{\"error\":{\"message\":\"bad request\"}}");
                    break;
                case ResponseMode.ConnectionDrop:
                    // Abort mid-flight: the provider observes a connection failure (AC-4.5).
                    ctx.Abort();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        });

        _app = app;
        _ = app.RunAsync();
        _client = app.GetTestClient();
        return _client;
    }

    public void Dispose()
    {
        _client?.Dispose();
        _app?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}