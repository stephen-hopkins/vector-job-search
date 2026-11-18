using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;

namespace VectorJobSearch.Infrastructure.Embedding;

using EmbeddingVector = Microsoft.Extensions.AI.Embedding<float>;

/// <summary>
/// Composition-root registration for the embedding provider (spec 002, B1/B5).
/// <see cref="AddEmbeddingProvider"/> hosts the registry of known provider names,
/// binds + validates <see cref="EmbeddingProviderSettings"/> from the
/// <c>embedding_provider</c> configuration section, and registers exactly one
/// <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> for the configured provider
/// (AC-1.1). Validation failures throw <see cref="EmbeddingProviderConfigurationException"/>
/// naming the offending setting so the composition root can convert them into a
/// non-zero exit before any network call (AC-5.2).
/// </summary>
public static class EmbeddingProviderServiceCollectionExtensions
{
    /// <summary>Register the embedding generator configured under <c>embedding_provider</c>.</summary>
    public static IServiceCollection AddEmbeddingProvider(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = EmbeddingProviderSettings.Bind(configuration);
        settings.Validate(); // eager startup validation — throws EmbeddingProviderConfigurationException

        // Exactly one provider instance for the configured setting (singleton across
        // scopes — AC-1.1's "resolves exactly one").
        services.AddSingleton(settings);
        services.AddSingleton<IEmbeddingGenerator<string, EmbeddingVector>>(
            _ => new OpenAiCompatibleEmbeddingGenerator(settings));
        return services;
    }
}