// Health endpoint contract tests (RED phase) — spec 001, B4.
//
// Test ID -> acceptance condition mapping (see tests/TEST-PLAN.md):
//   P0-HEALTH-GET          AC-9   GET /health -> 200 + status JSON validating
//                                 tests/schemas/health-response.schema.json
//   P0-HEALTH-STANDALONE   AC-10  health returns 200 with no external services
//                                 configured or reachable
//   P0-HEALTH-METHOD-405   E-2    POST /health -> 405
//   P0-HEALTH-UNKNOWN-404  E-3    unknown route -> 404
//
// All tests use the TestSuite=P0-HEALTH trait so `dotnet test --filter
// "TestSuite=P0-HEALTH"` runs exactly this group.
//
// The implementation must expose a test-visible factory named
// VectorJobSearch.IntegrationTests.AppFactory (idempotent) creating a WebApplication
// client for the API project with all external-service environment variables
// removed.

using System.Net;
using System.Text.Json;
using Json.Schema;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace VectorJobSearch.IntegrationTests;

[Trait("TestSuite", "P0-HEALTH")]
public class HealthEndpointTests
{
    [Fact] // Test ID: P0-HEALTH-GET — AC-9
    public async Task Get_Health_Returns200_WithStatusJson_ValidatingSchema()
    {
        using var client = AppFactory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());
        var schema = JsonSchema.FromFile(RepoPaths.HealthResponseSchema);
        var result = schema.Evaluate(document.RootElement, new EvaluationOptions
        {
            OutputFormat = OutputFormat.List,
        });
        var errors = result.Details
            .Where(d => d.Errors is { Count: > 0 })
            .SelectMany(d => d.Errors!)
            .Select(e => e.Value)
            .ToList();
        Assert.True(errors.Count == 0,
            $"health response failed schema validation: {string.Join("; ", errors)}");
    }

    [Fact] // Test ID: P0-HEALTH-STANDALONE — AC-10
    public async Task Get_Health_Returns200_WithNoExternalServicesConfiguredOrReachable()
    {
        // AppFactory strips all external-service environment variables before
        // building the host, proving the skeleton starts standalone.
        using var client = AppFactory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("status", body);
    }

    [Fact] // Test ID: P0-HEALTH-METHOD-405 — E-2
    public async Task Post_Health_IsRejectedWith405()
    {
        using var client = AppFactory.CreateClient();

        var response = await client.PostAsync("/health", null);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("status", body); // no health payload on non-GET
    }

    [Fact] // Test ID: P0-HEALTH-UNKNOWN-404 — E-3
    public async Task Get_UnknownRoute_Returns404_Not500()
    {
        using var client = AppFactory.CreateClient();

        var response = await client.GetAsync("/no-such-route");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

/// <summary>
/// Creates an in-memory client for the API application with all
/// external-service environment variables removed (AC-10). Idempotent:
/// repeat calls reuse the already-built host.
/// </summary>
public static class AppFactory
{
    private static readonly string[] ExternalServicePrefixes =
    [
        "VJS_", "OPENAI_", "AZURE_", "ADZUNA_", "REED_", "CONNECTIONSTRING_",
    ];

    private static WebApplicationFactory<Program>? _sharedFactory;

    public static HttpClient CreateClient()
    {
        if (_sharedFactory is null)
        {
            foreach (var prefix in ExternalServicePrefixes)
            {
                var names = Environment.GetEnvironmentVariables().Keys
                    .Cast<string>()
                    .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                foreach (var name in names)
                    Environment.SetEnvironmentVariable(name, null);
            }

            _sharedFactory = new WebApplicationFactory<Program>();
        }

        // Reuse the host, but hand each caller its own client: callers dispose
        // their client (`using var`), which must not kill the shared host.
        return _sharedFactory.CreateClient();
    }
}

public static class RepoPaths
{
    /// <summary>Path to the spec contract schema, specs/001-project-scaffold/contracts/health-response.schema.json.</summary>
    public static string HealthResponseSchema { get; } =
        FindRepoRoot() is { } root
            ? Path.Combine(root, "specs", "001-project-scaffold", "contracts", "health-response.schema.json")
            : throw new InvalidOperationException("repository root not found");

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.GetFiles(".git").Any() && !dir.GetDirectories(".git").Any())
            dir = dir.Parent;
        return dir?.FullName;
    }
}
