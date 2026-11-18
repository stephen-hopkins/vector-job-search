using Microsoft.Extensions.Configuration;
using OpenAI;

namespace VectorJobSearch.Infrastructure.Embedding;

/// <summary>
/// Configuration POCO bound from the <c>embedding_provider</c> section (spec 002, B5;
/// plan.md §4). The API key arrives via the <c>EmbeddingProvider__ApiKey</c>
/// environment variable or user-secrets and is never committed (AC-5.1).
/// </summary>
public sealed class EmbeddingProviderSettings
{
    /// <summary>Config key prefix under which every field binds.</summary>
    public const string SectionName = "embedding_provider";

    /// <summary>Known provider names accepted by <see cref="Validate"/> (the registry).</summary>
    public static readonly string[] KnownProviders = ["openai-compat"];

    /// <summary>Config key: <c>embedding_provider:provider</c>.</summary>
    public const string ProviderKey = SectionName + ":provider";

    /// <summary>Config key: <c>embedding_provider:base_url</c>.</summary>
    public const string BaseUrlKey = SectionName + ":base_url";

    /// <summary>Config key: <c>embedding_provider:api_key</c> (bound from env <c>EmbeddingProvider__ApiKey</c>).</summary>
    public const string ApiKeyKey = SectionName + ":api_key";

    /// <summary>Config key: <c>embedding_provider:model</c>.</summary>
    public const string ModelKey = SectionName + ":model";

    /// <summary>Default dimensionality of <c>text-embedding-3-small</c> output (spec default target model).</summary>
    public const int DefaultDimensions = 3;

    /// <summary>Provider name (e.g. <c>openai-compat</c>). Required, non-blank, in <see cref="KnownProviders"/>.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Provider base URL (e.g. <c>https://api.nano-gpt.example/v1</c>). Required, absolute, parseable.</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>API key, from env <c>EmbeddingProvider__ApiKey</c> / user-secrets. Required, non-empty; never committed.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Model identifier (default target: <c>text-embedding-3-small</c>). Required, non-blank.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Expected output dimensionality of <see cref="Model"/>. Defaults to
    /// <see cref="DefaultDimensions"/> (the stub contract for <c>text-embedding-3-small</c>
    /// in this spec's tests); mismatched remote responses are surfaced as provider errors (AC-1.3).
    /// </summary>
    public int Dimensions { get; set; } = DefaultDimensions;

    /// <summary>Uri built from <see cref="BaseUrl"/>; null when unparseable (validation names the setting).</summary>
    public Uri? Endpoint => Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) ? uri : null;

    /// <summary>Bind from configuration (appsettings + env + user-secrets merge; keys case-insensitive).</summary>
    public static EmbeddingProviderSettings Bind(IConfiguration configuration)
        => new()
        {
            // configuration["embedding_provider:api_key"] picks up the appsettings key AND the
            // user-secret "embedding_provider:api_key"; the env var name EmbeddingProvider__ApiKey
            // binds under the section name "EmbeddingProvider" (double underscore → colon), so both
            // spellings are read, preferring whichever is non-empty.
            Provider = configuration[ProviderKey] ?? string.Empty,
            BaseUrl = configuration[BaseUrlKey] ?? string.Empty,
            ApiKey = FirstNonEmpty(configuration[ApiKeyKey], configuration["EmbeddingProvider:ApiKey"]) ?? string.Empty,
            Model = configuration[ModelKey] ?? string.Empty,
        };

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrEmpty(v));

    /// <summary>
    /// Eager startup validation (AC-5.2): throws <see cref="EmbeddingProviderConfigurationException"/>
    /// naming the offending setting — plus accepted values for an unknown provider name —
    /// before any network call. The composition root converts the throw into exit code 1.
    /// </summary>
    public void Validate()
    {
        // Provider: required, non-blank, ∈ known providers { openai-compat }.
        if (string.IsNullOrWhiteSpace(Provider))
        {
            throw new EmbeddingProviderConfigurationException(
                $"'{SectionName}:provider' is required and must not be blank (accepted values: {string.Join(", ", KnownProviders)}).");
        }
        if (Array.TrueForAll(KnownProviders, p => !string.Equals(p, Provider, StringComparison.Ordinal)))
        {
            throw new EmbeddingProviderConfigurationException(
                $"'{SectionName}:provider' has value '{Provider}' which is not a known provider; accepted values: {string.Join(", ", KnownProviders)}.");
        }

        // BaseUrl: required, absolute, parseable.
        if (string.IsNullOrWhiteSpace(BaseUrl) || !Uri.TryCreate(BaseUrl, UriKind.Absolute, out var _) || !OpenAIClientOptionsEndpointParses)
        {
            throw new EmbeddingProviderConfigurationException(
                $"'{SectionName}:base_url' is required and must be an absolute, parseable URL (found: '{BaseUrl}').");
        }

        // ApiKey: required, non-empty — from env EmbeddingProvider__ApiKey / user-secrets, never committed.
        if (string.IsNullOrEmpty(ApiKey))
        {
            throw new EmbeddingProviderConfigurationException(
                $"'{SectionName}:api_key' is required (set the environment variable 'EmbeddingProvider__ApiKey' or a user-secret; it is never committed).");
        }

        // Model: required, non-blank.
        if (string.IsNullOrWhiteSpace(Model))
        {
            throw new EmbeddingProviderConfigurationException(
                $"'{SectionName}:model' is required and must not be blank (default target: 'text-embedding-3-small').");
        }

        if (Dimensions < 1)
        {
            throw new EmbeddingProviderConfigurationException(
                $"'{SectionName}:dimensions' must be a positive integer (found: {Dimensions}).");
        }
    }

    private bool OpenAIClientOptionsEndpointParses => Endpoint is not null;

    internal OpenAIClientOptions ToOpenAIClientOptions(HttpClient? transportClient)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = Endpoint ?? new Uri("http://localhost"),
        };
        if (transportClient is not null)
        {
            options.Transport = new System.ClientModel.Primitives.HttpClientPipelineTransport(transportClient);
        }
        // SDK internal retry stays disabled: retry is owned by OpenAiCompatibleEmbeddingGenerator
        // (plan.md R-2) — otherwise the SDK would double-retry and skew the stub request counts.
        options.RetryPolicy = new System.ClientModel.Primitives.ClientRetryPolicy(maxRetries: 0);
        return options;
    }
}
