using Microsoft.Extensions.DependencyInjection;

namespace VectorJobSearch.Infrastructure.Embedding;

/// <summary>
/// Non-transient remote failure (HTTP 4xx other than 429): surfaced immediately,
/// including the remote status code, with no retry (AC-4.4).
/// </summary>
public sealed class EmbeddingRequestFailedException(string message, int statusCode)
    : Exception($"{message} (HTTP {statusCode})")
{
    /// <summary>The remote HTTP status code.</summary>
    public int StatusCode { get; } = statusCode;
}

/// <summary>
/// Connection failure/timeout reaching the provider endpoint (AC-4.5): the error
/// identifies the endpoint; no uncontrolled hang.
/// </summary>
public sealed class EmbeddingEndpointUnreachableException(string endpoint, Exception inner)
    : Exception($"The embedding provider endpoint '{endpoint}' is unreachable.", inner)
{
    /// <summary>The configured provider base URL.</summary>
    public string Endpoint { get; } = endpoint;
}

/// <summary>
/// The remote response violates the embedding result shape (AC-1.3): non-finite
/// (NaN/±Infinity) values or a dimensionality that does not match the configured
/// model output. Surfaced as a provider error, never silently returned.
/// </summary>
public sealed class EmbeddingResponseInvalidException(string message)
    : Exception($"The embedding provider returned an invalid response: {message}");