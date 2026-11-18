using Microsoft.Extensions.AI;

namespace VectorJobSearch.Application.Embedding;

/// <summary>
/// Single-text convenience helpers over the batch-first embedding seam (spec 002,
/// AC-2.3: the single-text method delegates to the batch method with a one-element
/// batch — so empty-input rejection and request construction behave identically on
/// both paths).
/// </summary>
public static class EmbeddingGeneratorExtensions
{
    /// <summary>
    /// Generates one embedding for <paramref name="text"/> by delegating to the batch
    /// method with a one-element batch (AC-2.3).
    /// </summary>
    public static async Task<Embedding<float>> GenerateEmbeddingAsync(
        this IEmbeddingGenerator<string, Embedding<float>> generator,
        string text,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = await generator.GenerateAsync([text], options, cancellationToken);
        return results[0];
    }
}