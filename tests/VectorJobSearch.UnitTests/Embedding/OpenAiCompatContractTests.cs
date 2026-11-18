using System.Text.Json;
using VectorJobSearch.Application.Embedding;
using VectorJobSearch.Infrastructure.Embedding;

namespace VectorJobSearch.UnitTests.Embedding;

// Spec 002 — Contract (stub HTTP) suite. Every test runs against the in-process
// EmbeddingStubServer (zero external HTTP, no API key — mission.md Cost constraint).
//   T-2.1 → AC-2.1  SingleTextEmbedding
//   T-2.2 → AC-2.2  EmptyTextRejected
//   T-3.1 → AC-3.1  BatchOrderPreserved
//   T-3.2 → AC-3.2  BatchWithEmptyTextRejected
//   T-3.3 → AC-3.3  EmptyBatchNoCall
//   T-4.1 → AC-4.1  OpenAiCompatRequestShape
//   T-4.2 → AC-4.2  OpenAiCompatResponseMapped
//   T-4.3 → AC-4.3  TransientFailureRetried
//   T-4.4 → AC-4.4  NonTransientFailureNoRetry
//   T-4.5 → AC-4.5  UnreachableEndpointSurfacesError

public class OpenAiCompatContractTests
{
    private static OpenAiCompatibleEmbeddingGenerator NewGenerator(
        EmbeddingStubServer stub, Func<int, TimeSpan>? backoffProvider = null)
        => new(EmbeddingTestConfig.ValidSettings(), stub.CreateClient(), backoffProvider: backoffProvider);

    [Fact] // Test ID: T-2.1 — AC-2.1
    public async Task SingleTextEmbedding()
    {
        using var stub = new EmbeddingStubServer();
        using var generator = NewGenerator(stub);

        var result = await generator.GenerateEmbeddingAsync("senior backend engineer, .NET");

        // Given non-empty text and a working provider, the vector's dimensionality matches
        // the provider's configured model output.
        Assert.Equal(EmbeddingTestConfig.ValidSettings().Dimensions, result.Vector.Length);
        Assert.All(result.Vector.ToArray(), value => Assert.True(float.IsFinite(value)));
    }

    [Fact] // Test ID: T-2.2 — AC-2.2
    public async Task EmptyTextRejected()
    {
        using var stub = new EmbeddingStubServer();
        using var generator = NewGenerator(stub);

        var empty = await Assert.ThrowsAsync<EmbeddingInputRejectedException>(
            () => generator.GenerateEmbeddingAsync(""));
        Assert.Contains("position 0", empty.Message); // error identifies the invalid input

        var whitespace = await Assert.ThrowsAsync<EmbeddingInputRejectedException>(
            () => generator.GenerateEmbeddingAsync("   \t "));
        Assert.Contains("position 0", whitespace.Message);

        // No network call to the provider is made for rejected input.
        Assert.Equal(0, stub.RequestCount);
    }

    [Fact] // Test ID: T-3.1 — AC-3.1
    public async Task BatchOrderPreserved()
    {
        using var stub = new EmbeddingStubServer();
        using var generator = NewGenerator(stub);

        var results = await generator.GenerateAsync(["alpha", "beta", "gamma"]);

        // N ≥ 1 non-empty texts → N results in the same order as the inputs. The stub
        // deliberately shuffles its data array by response "index" (origin 2, 1, 0) so the
        // index→input-order mapping is exercised.
        Assert.Equal(3, results.Count);
        Assert.Equal(new float[] { 1, 2, 3 }, results[0].Vector.ToArray());
        Assert.Equal(new float[] { 4, 5, 6 }, results[1].Vector.ToArray());
        Assert.Equal(new float[] { 7, 8, 9 }, results[2].Vector.ToArray());
        Assert.All(results, r => Assert.Equal(EmbeddingTestConfig.Model, r.ModelId));
    }

    [Fact] // Test ID: T-3.2 — AC-3.2
    public async Task BatchWithEmptyTextRejected()
    {
        using var stub = new EmbeddingStubServer();
        using var generator = NewGenerator(stub);

        // One bad element among good ones → the entire batch is rejected atomically.
        var ex = await Assert.ThrowsAsync<EmbeddingInputRejectedException>(
            () => generator.GenerateAsync(["good", "", "also good", " "]));

        // The error identifies the offending positions (1-based display of 0-based positions is
        // not required; the contract is that the positions are named).
        Assert.Contains("position(s) 1, 3", ex.Message);

        // No partial API call: nothing left the process.
        Assert.Equal(0, stub.RequestCount);
    }

    [Fact] // Test ID: T-3.3 — AC-3.3
    public async Task EmptyBatchNoCall()
    {
        using var stub = new EmbeddingStubServer();
        using var generator = NewGenerator(stub);

        var results = await generator.GenerateAsync(Array.Empty<string>());

        // Zero texts → zero results, and no network call.
        Assert.Empty(results);
        Assert.Equal(0, stub.RequestCount);
    }

    [Fact] // Test ID: T-4.1 — AC-4.1
    public async Task OpenAiCompatRequestShape()
    {
        using var stub = new EmbeddingStubServer();
        using var generator = NewGenerator(stub);

        await generator.GenerateAsync(["first", "second"]);

        Assert.Equal(1, stub.RequestCount);
        var request = stub.Requests[0];
        // Wire contract (plan.md §5.3): POST {BaseUrl}/embeddings
        Assert.Equal("POST", request.Method);
        Assert.Equal("/v1/embeddings", request.Path);
        // Authenticating with the API key from configuration.
        Assert.Equal($"Bearer {EmbeddingTestConfig.ApiKey}", request.Authorization);
        using var body = JsonDocument.Parse(request.Body);
        // Model name from configuration, input array order-preserving.
        Assert.Equal(EmbeddingTestConfig.Model, body.RootElement.GetProperty("model").GetString());
        Assert.Equal(
            new[] { "first", "second" },
            body.RootElement.GetProperty("input").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact] // Test ID: T-4.2 — AC-4.2
    public async Task OpenAiCompatResponseMapped()
    {
        using var stub = new EmbeddingStubServer();
        using var generator = NewGenerator(stub);

        var results = await generator.GenerateAsync(["one"]);

        var result = Assert.Single(results);
        // The remote response is mapped onto the abstraction's result type:
        // vector, dimensions, provider/model id.
        Assert.Equal(new float[] { 1, 2, 3 }, result.Vector.ToArray());
        Assert.Equal(3, result.Dimensions);
        Assert.Equal(EmbeddingTestConfig.Model, result.ModelId);
    }

    [Fact] // Test ID: T-4.3 — AC-4.3
    public async Task TransientFailureRetried()
    {
        using var stub = new EmbeddingStubServer();
        // Injected zero-delay backoff keeps the test fast; defaults are max 3 attempts,
        // exponential backoff starting at 500 ms (spec 002 Open Question → implementation).
        using var generator = NewGenerator(stub, backoffProvider: _ => TimeSpan.Zero);

        stub.ModeQueue.Enqueue(EmbeddingStubServer.ResponseMode.TooManyRequests429);
        stub.ModeQueue.Enqueue(EmbeddingStubServer.ResponseMode.ServerError5xx);
        // Third response falls back to Mode = Success.

        var results = await generator.GenerateAsync(["retry me"]);

        // 429 then 5xx then success → the provider succeeds after bounded retries.
        Assert.Single(results);
        // Bounded attempts: exactly 3 requests recorded (≤ configured max of 3).
        Assert.Equal(3, stub.RequestCount);
    }

    [Fact] // Test ID: T-4.4 — AC-4.4
    public async Task NonTransientFailureNoRetry()
    {
        using var stub = new EmbeddingStubServer();
        stub.Mode = EmbeddingStubServer.ResponseMode.NonTransient4xx;
        using var generator = NewGenerator(stub);

        var ex = await Assert.ThrowsAsync<EmbeddingRequestFailedException>(
            () => generator.GenerateAsync(["bad"]));

        // Immediate error including the remote status code…
        Assert.Equal(400, ex.StatusCode);
        Assert.Contains("400", ex.Message);
        // …with no retry: the stub records exactly one request.
        Assert.Equal(1, stub.RequestCount);
    }

    [Fact] // Test ID: T-4.5 — AC-4.5
    public async Task UnreachableEndpointSurfacesError()
    {
        using var stub = new EmbeddingStubServer();
        stub.Mode = EmbeddingStubServer.ResponseMode.ConnectionDrop;
        using var generator = NewGenerator(stub);

        // Short test timeout: the application must not hang or crash uncontrolled.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var ex = await Assert.ThrowsAsync<EmbeddingEndpointUnreachableException>(
            () => generator.GenerateAsync(["any"], null, cts.Token));

        // The error identifies the endpoint.
        Assert.Contains(EmbeddingTestConfig.BaseUrl, ex.Message);
        Assert.Equal(EmbeddingTestConfig.BaseUrl, ex.Endpoint);
    }
}