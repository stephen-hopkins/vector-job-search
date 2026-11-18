using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;

namespace VectorJobSearch.UnitTests.Embedding;

/// <summary>
/// In-process stub for an OpenAI-compatible embeddings endpoint (plan.md R-5).
/// Records every request (method, path, Authorization header, JSON body incl. model
/// name and input array order) and is scriptable with responses: success payload
/// (data shuffled by "index" so order-mapping is exercised), HTTP 429, HTTP 5xx,
/// non-429 4xx, connection drop, and vectors containing non-finite values.
/// Zero external HTTP — the provider's HttpClient is TestServer.CreateClient().
/// </summary>
public sealed class EmbeddingStubServer : IDisposable
{
    /// <summary>A recorded embeddings request.</summary>
    public sealed record RecordedRequest(string Method, string Path, string? Authorization, string Body);

    /// <summary>A stub response mode.</summary>
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

    /// <summary>Response mode to use for the next (or every) request. Default: Success.</summary>
    public ResponseMode Mode { get; set; } = ResponseMode.Success;

    /// <summary>Response mode applied only once, then resets to <see cref="Mode"/> (used by T-4.3 retry sequencing).</summary>
    public Queue<ResponseMode> ModeQueue { get; } = new();

    /// <summary>The number of embedding vectors the stub returns per request.</summary>
    public int Dimensions { get; set; } = 3;

    /// <summary>All recorded requests, in order.</summary>
    public IReadOnlyList<RecordedRequest> Requests => _requests;

    /// <summary>Number of requests recorded so far.</summary>
    public int RequestCount => _requests.Count;

    private ResponseMode NextMode()
    {
        if (ModeQueue.Count > 0)
        {
            return ModeQueue.Dequeue();
        }
        return Mode;
    }

    /// <summary>A success payload: {"model": "...", "data": [{"embedding": [floats], "index": n}]} in shuffled order.</summary>
    private string SuccessPayload()
    {
        var sb = new StringBuilder();
        sb.Append("{\"model\":\"text-embedding-3-small\",\"data\":[");
        // Deliberately shuffled index order so response-index→input-order mapping is exercised.
        int[] shuffled = new[] { 2, 0, 1 };
        bool first = true;
        for (var i = 0; i < shuffled.Length; i++)
        {
            if (!first)
            {
                sb.Append(',');
            }
            first = false;
            sb.Append("{\"embedding\":[");
            for (var d = 0; d < Dimensions; d++)
            {
                if (d > 0)
                {
                    sb.Append(',');
                }
                sb.Append((shuffled[i] * Dimensions + d).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture));
            }
            sb.Append("],\"index\":");
            sb.Append(shuffled[i]);
            sb.Append('}');
        }
        sb.Append("]}");
        return sb.ToString();
    }

    /// <summary>A success payload with NaN / +Infinity / -Infinity in the vectors (spec Edge Cases).</summary>
    private string NonFinitePayload()
        => "{\"model\":\"text-embedding-3-small\",\"data\":[{\"embedding\":[NaN,Infinity,-Infinity,1],\"index\":0}]}";

    /// <summary>Creates and starts the TestServer; returns an HttpClient wired to it.</summary>
    public HttpClient CreateClient()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();

        app.Map("/embeddings", async ctx =>
        {
            ctx.Request.EnableBuffering();
            using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            _requests.Add(new RecordedRequest(
                ctx.Request.Method,
                ctx.Request.Path.ToString(),
                ctx.Request.Headers.Authorization.ToString(),
                body));

            var mode = NextMode();
            switch (mode)
            {
                case ResponseMode.Success:
                    ctx.Response.StatusCode = StatusCodes.Status200OK;
                    ctx.Response.ContentType = "application/json";
                    await ctx.Response.WriteAsync(SuccessPayload());
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
                    await ctx.Response.WriteAsync("{\"error\":\"bad request\"}");
                    break;
                case ResponseMode.ConnectionDrop:
                    // Aborts the response mid-flight: client sees a connection failure.
                    ctx.Abort();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        });

        _ = app.RunAsync();
        return app.GetTestClient();
    }

    public void Dispose()
    {
    }
}