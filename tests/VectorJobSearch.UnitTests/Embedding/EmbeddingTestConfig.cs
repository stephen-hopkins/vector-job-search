using VectorJobSearch.Infrastructure.Embedding;

namespace VectorJobSearch.UnitTests.Embedding;

/// <summary>Shared valid dummy values for spec-002 tests (plan.md R-3 verbatim).
/// No real API key ever appears here — contract tests run against <see cref="EmbeddingStubServer"/> only.</summary>
internal static class EmbeddingTestConfig
{
    public const string Provider = "openai-compat";
    public const string BaseUrl = "http://localhost:0/v1";
    public const string ApiKey = "test-api-key";
    public const string Model = "text-embedding-3-small";

    /// <summary>In-memory proxy for the committed appsettings <c>embedding_provider</c> section
    /// (provider / base_url / model) plus the api_key, which in production arrives via env
    /// <c>EmbeddingProvider__ApiKey</c> or user-secrets and is never committed (AC-5.1).</summary>
    public static Dictionary<string, string?> ValidConfigValues(bool includeApiKey = true)
    {
        var values = new Dictionary<string, string?>
        {
            ["embedding_provider:provider"] = Provider,
            ["embedding_provider:base_url"] = BaseUrl,
            ["embedding_provider:model"] = Model,
        };
        if (includeApiKey)
        {
            values["embedding_provider:api_key"] = ApiKey;
        }
        return values;
    }

    /// <summary>Valid <see cref="EmbeddingProviderSettings"/> for direct generator construction.</summary>
    public static EmbeddingProviderSettings ValidSettings() => new()
    {
        Provider = Provider,
        BaseUrl = BaseUrl,
        ApiKey = ApiKey,
        Model = Model,
    };
}