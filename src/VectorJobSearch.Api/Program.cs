using VectorJobSearch.Infrastructure.Embedding;

var builder = WebApplication.CreateBuilder(args);

// Composition root: the only place allowed to name the concrete provider wiring
// (AC-1.1 mechanical scan). Startup validation of the embedding_provider section
// runs BEFORE app.Run(): on failure the error names the offending setting (an
// unknown provider name also lists the accepted values), the process logs it and
// exits with code 1 — before any network call (AC-5.2). Test hosts
// (WebApplicationFactory) supply a valid dummy embedding_provider, so this normal
// path is taken by the health suite too.
var configuration = builder.Configuration;
try
{
    builder.Services.AddEmbeddingProvider(configuration);
}
catch (EmbeddingProviderConfigurationException ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
    return; // exit before building/running the host — no network call happens
}

var app = builder.Build();

app.MapGet("/health", () => Results.Json(new { status = "healthy" }));

app.Run();

public partial class Program;
