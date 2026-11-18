using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Embeddings;

namespace VectorJobSearch.Infrastructure.Embedding;

using EmbeddingVector = Microsoft.Extensions.AI.Embedding<float>;

/// <summary>
/// Thrown for empty/whitespace-only input (AC-2.2/AC-3.2): the error identifies the
/// invalid input position(s); no request was constructed, so no network call happened.
/// </summary>
public sealed class EmbeddingInputRejectedException(int batchSize, IReadOnlyList<int> positions)
    : Exception(
        $"embedding input rejected: input at "
        + (positions.Count == 1 ? $"position {positions[0]} is" : $"position(s) {string.Join(", ", positions)} are")
        + $" empty/whitespace-only (of {batchSize} input(s)); no request was sent.")
{
    /// <summary>Zero-based input positions that were empty/whitespace-only.</summary>
    public IReadOnlyList<int> Positions { get; } = positions;
}

/// <summary>
/// The first concrete provider (spec 002, B4): an OpenAI-compatible hosted embeddings
/// API (config: <c>openai-compat</c>, model class <c>text-embedding-3-small</c>).
/// Implements the pipeline-facing abstraction <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>
/// (via <c>VectorJobSearch.Application.Embedding</c> usage surface). All input/result
/// guards run ahead of any request construction (plan.md R-4). Bounded explicit retry
/// on transient 429/5xx; the SDK's internal retry stays disabled (plan.md R-2). This
/// type may be referenced only by Infrastructure and the composition root (AC-1.1).
/// </summary>
public sealed class OpenAiCompatibleEmbeddingGenerator : IEmbeddingGenerator<string, EmbeddingVector>
{
    private readonly EmbeddingProviderSettings _settings;
    private readonly EmbeddingRetryPolicy _retry;
    private readonly EmbeddingClient _embeddingClient;

    public OpenAiCompatibleEmbeddingGenerator(
        EmbeddingProviderSettings settings,
        HttpClient? transportClient = null,
        Func<int, TimeSpan>? backoffProvider = null)
    {
        settings.Validate(); // eager: guards run ahead of any request construction
        _settings = settings;
        _retry = backoffProvider is null
            ? new EmbeddingRetryPolicy(EmbeddingRetryPolicy.DefaultMaxAttempts)
            : new EmbeddingRetryPolicy(EmbeddingRetryPolicy.DefaultMaxAttempts, backoffProvider);
        _embeddingClient = new OpenAIClient(
            new ApiKeyCredential(settings.ApiKey),
            settings.ToOpenAIClientOptions(transportClient))
            .GetEmbeddingClient(settings.Model);
    }

    /// <inheritdoc />
    public async Task<GeneratedEmbeddings<EmbeddingVector>> GenerateAsync(
        IEnumerable<string> values,
        Microsoft.Extensions.AI.EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var texts = values as IReadOnlyList<string> ?? values.ToList();

        // Input guards (AC-2.2, AC-3.2) — ahead of any request construction, so no
        // network call is ever made for invalid input.
        if (texts.Count == 0)
        {
            // AC-3.3: empty batch → zero results, no network call.
            return new GeneratedEmbeddings<EmbeddingVector>();
        }
        var invalidPositions = Enumerable.Range(0, texts.Count)
            .Where(i => string.IsNullOrWhiteSpace(texts[i]))
            .ToList();
        if (invalidPositions.Count > 0)
        {
            throw new EmbeddingInputRejectedException(texts.Count, invalidPositions);
        }

        // Bounded explicit retry (AC-4.3/AC-4.4): transient = HTTP 429 or 5xx.
        var attempt = 0;
        while (true)
        {
            attempt++;
            if (attempt > 1)
            {
                await Task.Delay(_retry.Backoff(attempt), cancellationToken);
            }

            ClientResult<OpenAIEmbeddingCollection> result;
            try
            {
                result = await _embeddingClient.GenerateEmbeddingsAsync(texts, null, cancellationToken);
            }
            catch (ClientResultException ex) when (ex.Status is int status && _retry.IsTransient(status))
            {
                if (attempt < _retry.MaxAttempts)
                {
                    continue; // bounded retry of a transient failure (429/5xx)
                }
                throw new EmbeddingRequestFailedException(
                    $"The embedding provider kept failing transiently (HTTP {status}) after {attempt} attempt(s).", status);
            }
            catch (ClientResultException ex) when (ex.Status is int status)
            {
                // Non-transient 4xx (other than 429): immediate error including the
                // remote status code, with no retry (AC-4.4).
                throw new EmbeddingRequestFailedException(
                    "The embedding provider rejected the request.", status);
            }
            catch (Exception ex) when (ex is HttpRequestException or IOException or ObjectDisposedException
                                     or OperationCanceledException or AggregateException)
            {
                if (ex is AggregateException { InnerExceptions: { } } aggregate)
                {
                    foreach (var inner in aggregate.InnerExceptions)
                    {
                        if (inner is OperationCanceledException canceled && cancellationToken.IsCancellationRequested)
                        {
                            throw; // caller-initiated cancellation, not an endpoint failure
                        }
                    }
                }
                else if (ex is OperationCanceledException && cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                // Connection failure/timeout: error identifying the endpoint (AC-4.5).
                throw new EmbeddingEndpointUnreachableException(_settings.BaseUrl, ex);
            }

            return MapResponse(result, texts.Count);
        }
    }

    private GeneratedEmbeddings<EmbeddingVector> MapResponse(
        OpenAIEmbeddingCollection collection, int expectedCount)
    {
        var results = new GeneratedEmbeddings<EmbeddingVector>();

        // Map by response "index" back to input order (AC-3.1): the remote payload may
        // shuffle its data array (the stub does, deliberately).
        var byIndex = new EmbeddingVector?[expectedCount];
        foreach (var item in collection)
        {
            if (item.Index < 0 || item.Index >= expectedCount)
            {
                throw new EmbeddingResponseInvalidException(
                    $"data index {item.Index} is out of range for a batch of {expectedCount} input(s).");
            }
            byIndex[item.Index] = MapOne(item);
        }

        for (var i = 0; i < expectedCount; i++)
        {
            if (byIndex[i] is not { } embedding)
            {
                throw new EmbeddingResponseInvalidException(
                    $"data is missing an entry for input index {i}.");
            }
            results.Add(embedding);
        }
        return results;
    }

    private EmbeddingVector MapOne(OpenAIEmbedding item)
    {
        var floats = item.ToFloats().ToArray();

        // Result shape (AC-1.3): an ordered sequence of finite floating-point numbers —
        // non-finite values (NaN/±Infinity) are provider errors, never returned.
        if (floats.Any(f => !float.IsFinite(f)))
        {
            throw new EmbeddingResponseInvalidException(
                $"the vector at data index {item.Index} contains non-finite values (NaN/Infinity).");
        }

        // Dimensionality must match the configured model output; mismatch is surfaced
        // as a provider error, not silently returned (AC-1.3).
        if (floats.Length != _settings.Dimensions)
        {
            throw new EmbeddingResponseInvalidException(
                $"the vector at data index {item.Index} has dimensionality {floats.Length}, expected {_settings.Dimensions} for model '{_settings.Model}'.");
        }

        return new EmbeddingVector(floats)
        {
            // ModelId: the provider/model identifier that produced the vector, from
            // configuration (the remote response carries no reliable model echo).
            ModelId = _settings.Model,
        };
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType == typeof(EmbeddingProviderSettings) ? _settings : null;

    /// <summary>No unmanaged resources: the OpenAI SDK client owns its pipeline.</summary>
    void IDisposable.Dispose() { }
}