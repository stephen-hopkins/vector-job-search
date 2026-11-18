using Microsoft.Extensions.Configuration;
using VectorJobSearch.Infrastructure.Embedding;

namespace VectorJobSearch.UnitTests.Embedding;

// Spec 002 — Unit (config) suite.
//   T-5.1 → AC-5.1  EmbeddingProviderConfigLoading
//   T-5.2 → AC-5.2  StartupValidationFailsFast

public class EmbeddingProviderConfigTests
{
    private static EmbeddingProviderSettings BindFrom(Dictionary<string, string?> values)
        => EmbeddingProviderSettings.Bind(
            new ConfigurationBuilder().AddInMemoryCollection(values).Build());

    private static EmbeddingProviderConfigurationException Reject(Dictionary<string, string?> values)
    {
        var settings = BindFrom(values);
        // Validation is eager startup validation: it throws before any network call (AC-5.2);
        // the composition root converts this exception into a non-zero exit (Program.cs).
        return Assert.Throws<EmbeddingProviderConfigurationException>(settings.Validate);
    }

    private static Dictionary<string, string?> ValidExcept(params string[] remove)
    {
        var values = EmbeddingTestConfig.ValidConfigValues();
        foreach (var key in remove)
        {
            values.Remove(key);
        }
        return values;
    }

    [Fact] // Test ID: T-5.1 — AC-5.1
    public void EmbeddingProviderConfigLoading()
    {
        // The committed Api appsettings.json ships the embedding_provider section with
        // non-secret defaults (provider / base_url / model — never api_key). The API key
        // arrives via the environment variable EmbeddingProvider__ApiKey (or user-secrets).
        var repoRoot = FindRepoRoot()
            ?? throw new InvalidOperationException("repository root not found");
        // The in-memory collection merges over the JSON file (case-insensitive keys):
        // the env var name EmbeddingProvider__ApiKey binds to embedding_provider:api_key.
        var configuration = new ConfigurationBuilder()
            .SetBasePath(repoRoot)
            .AddJsonFile(Path.Combine("src", "VectorJobSearch.Api", "appsettings.json"))
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["embedding_provider:api_key"] = "env-key-123",
            })
            .Build();

        var settings = EmbeddingProviderSettings.Bind(configuration);
        settings.Validate(); // appsettings + env key is a valid configuration — must not throw

        // Provider name, base URL, and model from appsettings…
        Assert.Equal("openai-compat", settings.Provider);
        Assert.NotNull(settings.BaseUrl);
        Assert.Contains("api.nano-gpt.example", settings.BaseUrl); // committed placeholder
        Assert.Equal("text-embedding-3-small", settings.Model);
        // …and api_key from the EmbeddingProvider__ApiKey environment variable, bound into
        // the embedding_provider:api_key setting (case-insensitive config key; the env
        // var name maps to the section via the __ separator).
        Assert.Equal("env-key-123", settings.ApiKey);

        static string? FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null && !dir.GetDirectories(".git").Any())
            {
                dir = dir.Parent!;
            }
            return dir?.FullName;
        }
    }

    [Fact] // Test ID: T-5.2 — AC-5.2 — all permutations enumerated
    public void StartupValidationFailsFast()
    {
        // (a1) Unset provider name → error naming embedding_provider:provider.
        var missingProvider = Reject(ValidExcept("embedding_provider:provider"));
        Assert.Contains("embedding_provider:provider", missingProvider.Message);

        // (a2) Blank provider name → same named setting.
        var blank = ValidExcept("embedding_provider:provider");
        blank["embedding_provider:provider"] = "   ";
        var blankProvider = Reject(blank);
        Assert.Contains("embedding_provider:provider", blankProvider.Message);

        // (b) Provider name not in the set of known providers → names the offending setting
        // AND the accepted values.
        var unknown = ValidExcept("embedding_provider:provider");
        unknown["embedding_provider:provider"] = "acme-embeddings";
        var unknownProvider = Reject(unknown);
        Assert.Contains("embedding_provider:provider", unknownProvider.Message);
        Assert.Contains("acme-embeddings", unknownProvider.Message);
        Assert.Contains("openai-compat", unknownProvider.Message); // accepted values listed

        // (c) Missing API key → error naming embedding_provider:api_key.
        var noKey = Reject(ValidExcept("embedding_provider:api_key"));
        Assert.Contains("embedding_provider:api_key", noKey.Message);

        // (d) Missing base URL → error naming embedding_provider:base_url.
        var noBaseUrl = Reject(ValidExcept("embedding_provider:base_url"));
        Assert.Contains("embedding_provider:base_url", noBaseUrl.Message);

        // (e) Unparseable base URL → same named setting.
        var badBase = ValidExcept("embedding_provider:base_url");
        badBase["embedding_provider:base_url"] = "not a uri at all";
        var badBaseUrl = Reject(badBase);
        Assert.Contains("embedding_provider:base_url", badBaseUrl.Message);

        // Every rejection above is a startup validation error raised before any network
        // call: validation only inspects configuration (no provider request is constructed).
    }
}