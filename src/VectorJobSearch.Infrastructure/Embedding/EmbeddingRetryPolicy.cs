using Microsoft.Extensions.AI;

namespace VectorJobSearch.Infrastructure.Embedding;

/// <summary>
/// Bounded explicit retry for the embedding provider (plan.md R-2): transient =
/// HTTP 429 or 5xx; defaults max 3 attempts, exponential backoff starting 500 ms.
/// The backoff curve is injectable so tests stay fast (zero-delay).
/// </summary>
public sealed class EmbeddingRetryPolicy(int maxAttempts = 3)
{
    public const int DefaultMaxAttempts = 3;
    public static readonly TimeSpan DefaultInitialBackoff = TimeSpan.FromMilliseconds(500);

    private readonly TimeSpan _initialBackoff = DefaultInitialBackoff;
    private readonly Func<int, TimeSpan>? _backoffProvider;

    public EmbeddingRetryPolicy(int maxAttempts, TimeSpan initialBackoff) : this(maxAttempts) => _initialBackoff = initialBackoff;

    public EmbeddingRetryPolicy(int maxAttempts, Func<int, TimeSpan> backoffProvider) : this(maxAttempts) => _backoffProvider = backoffProvider;

    /// <summary>Maximum total attempts for a single GenerateAsync call (1 try + retries).</summary>
    public int MaxAttempts { get; } = maxAttempts < 1 ? 1 : maxAttempts;

    internal bool IsTransient(int statusCode) => statusCode == 429 || statusCode >= 500;

    /// <summary>Backoff before attempt <paramref name="attempt"/> (1-based attempt number ≥ 2).</summary>
    internal TimeSpan Backoff(int attempt)
    {
        if (attempt <= 1)
        {
            return TimeSpan.Zero;
        }
        if (_backoffProvider is not null)
        {
            return _backoffProvider(attempt);
        }
        // Exponential: initial * 2^(attempt - 2) → 500 ms, 1 s, 2 s …
        var exponent = attempt - 2;
        var milliseconds = _initialBackoff.TotalMilliseconds * Math.Pow(2, exponent);
        return TimeSpan.FromMilliseconds(Math.Min(milliseconds, int.MaxValue));
    }
}
