using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VectorJobSearch.Application.Embedding;
using VectorJobSearch.Infrastructure.Embedding;

namespace VectorJobSearch.UnitTests.Embedding;

// Spec 002 — Unit (abstraction) suite.
//   T-1.1 → AC-1.1  PipelineResolvesConfiguredProvider
//   T-1.3 → AC-1.3  EmbeddingResultShape
//   T-2.3 → AC-2.3  SingleDelegatesToBatch
// Run filters (spec Test ID Registry):
//   dotnet test --filter FullyQualifiedName~PipelineResolvesConfiguredProvider
//   dotnet test --filter FullyQualifiedName~EmbeddingResultShape
//   dotnet test --filter FullyQualifiedName~SingleDelegatesToBatch

public class EmbeddingAbstractionTests
{
    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(EmbeddingTestConfig.ValidConfigValues())
            .Build();
        return new ServiceCollection()
            .AddEmbeddingProvider(configuration)
            .BuildServiceProvider();
    }

    [Fact] // Test ID: T-1.1 — AC-1.1
    public void PipelineResolvesConfiguredProvider()
    {
        // Given any valid embedding_provider configuration, the DI container resolves
        // exactly one provider instance; the resolved runtime type is the configured
        // provider's. (Test code may name the concrete type; pipeline code may not —
        // enforced separately by the AC-1.1 mechanical scan.)
        using var provider = BuildProvider();

        var generator = provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        Assert.IsType<OpenAiCompatibleEmbeddingGenerator>(generator);

        // Exactly one instance for the configured setting (singleton across resolutions and scopes).
        var second = provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        Assert.Same(generator, second);
        using var scope = provider.CreateScope();
        Assert.Same(generator, scope.ServiceProvider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>());
    }

    [Fact] // Test ID: T-1.3 — AC-1.3
    public async Task EmbeddingResultShape()
    {
        using var stub = new EmbeddingStubServer();
        var settings = EmbeddingTestConfig.ValidSettings(); // Dimensions = configured model output (3)
        using var generator = new OpenAiCompatibleEmbeddingGenerator(settings, stub.CreateClient());

        var result = await generator.GenerateEmbeddingAsync("ideal job text");

        // An embedding result carries, at minimum: the vector (an ordered sequence of
        // finite floating-point numbers), its dimensionality, and the provider/model
        // identifier that produced it.
        Assert.All(result.Vector.ToArray(), value => Assert.True(float.IsFinite(value)));
        Assert.Equal(settings.Dimensions, result.Dimensions);
        Assert.Equal(result.Vector.Length, result.Dimensions);
        Assert.Equal(EmbeddingTestConfig.Model, result.ModelId);

        // Edge case: non-finite values (NaN / ±Infinity) in a remote response violate the
        // result shape → rejected as a provider error, never returned to the caller.
        stub.Mode = EmbeddingStubServer.ResponseMode.NonFiniteVector;
        await Assert.ThrowsAsync<EmbeddingResponseInvalidException>(
            () => generator.GenerateEmbeddingAsync("any text"));

        // Edge case: dimensionality not matching the configured model output → surfaced
        // error, not silently returned.
        stub.Mode = EmbeddingStubServer.ResponseMode.Success;
        var strictSettings = EmbeddingTestConfig.ValidSettings();
        strictSettings.Dimensions = settings.Dimensions + 1; // expects 4, stub serves 3
        using var strictGenerator = new OpenAiCompatibleEmbeddingGenerator(strictSettings, stub.CreateClient());
        await Assert.ThrowsAsync<EmbeddingResponseInvalidException>(
            () => strictGenerator.GenerateEmbeddingAsync("any text"));
    }

    [Fact] // Test ID: T-2.3 — AC-2.3
    public async Task SingleDelegatesToBatch()
    {
        using var stub = new EmbeddingStubServer();
        using var generator = new OpenAiCompatibleEmbeddingGenerator(
            EmbeddingTestConfig.ValidSettings(), stub.CreateClient());

        await generator.GenerateEmbeddingAsync("just one");

        // The single-text method delegates to the batch method: exactly one stub request
        // carrying a one-element input array.
        Assert.Equal(1, stub.RequestCount);
        using var body = JsonDocument.Parse(stub.Requests[0].Body);
        var input = body.RootElement.GetProperty("input");
        Assert.Equal(1, input.GetArrayLength());
        Assert.Equal("just one", input[0].GetString());
    }
}