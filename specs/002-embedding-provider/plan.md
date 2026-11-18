# Design Plan — Spec 002: Embedding Provider Abstraction + First Provider

**Status:** Draft for review
**Working branch (per repo convention):** `feat/002-embedding-provider`

---

## 1. Technical context

**Languages/runtimes** (installed, per spec 001 plan):

- **Backend:** C# 14 on .NET 10 (SDK pinned via `global.json` at repo root).
- **Backend tests:** xUnit; two existing test projects under `/tests` (`VectorJobSearch.UnitTests`, `VectorJobSearch.IntegrationTests`), both `net10.0`.
- **CI:** GitHub Actions (`.github/workflows/ci.yml`); no provider secrets in CI — constraint: CI must not call a metered API.

**Existing structure this feature touches** (onion; `Api → Infrastructure → Application → Domain`):

```
src/VectorJobSearch.Application/     ← currently empty; the seam for pipeline-facing code
src/VectorJobSearch.Infrastructure/  ← currently empty; home of the concrete provider
src/VectorJobSearch.Api/Program.cs   ← composition root: DI registration + startup validation
src/VectorJobSearch.Api/appsettings.json  ← gains the embedding_provider section (no secret values)
tests/VectorJobSearch.UnitTests/          ← unit + contract (stub HTTP) suites
tests/VectorJobSearch.IntegrationTests/   ← health suite; needs a test-only embedding_provider config (ripple, §3 R-3)
```

**Key packages (new):**

| Package | Project | Version |
|---|---|---|
| `Microsoft.Extensions.AI.Abstractions` | Application | latest 9.x stable — resolve at implementation (spec pins no minor) |
| `Microsoft.Extensions.AI.OpenAI` | Infrastructure | latest 9.x stable — resolve at implementation (brings `OpenAI` SDK) |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | Infrastructure | 10.x (for the registration extension) |
| `Microsoft.AspNetCore.TestHost` | UnitTests | 10.x (in-process stub HTTP server for contract tests) |

**Abstraction (constitution-mandated, tech-stack.md):** `Microsoft.Extensions.AI`'s `IEmbeddingGenerator<string, Embedding<float>>`. Batch method: `GenerateAsync(IReadOnlyList<string>, ...)`. Single method: the standard `GenerateEmbeddingAsync(string)` extension, which delegates to the batch path with a one-element list (AC-2.3 by construction). The concrete provider implements the interface in Infrastructure; pipeline code (from spec 003 onward) sees only the interface.

**NEEDS CLARIFICATION items:** none remaining — resolved in §3.

## 2. Constitution check

| Principle (quoted) | Verdict |
|---|---|
| mission.md — "Running costs must be kept as low as possible… free/low tiers are the default target." | **Complies.** All automated tests run against an in-process stub (TestHost); no live/metered call exists in any test, lint, or CI step. |
| spec Cost constraint — "No automated test… may call a paid or metered live embedding API." | **Complies.** Contract tests point the provider's `HttpClient` at `TestServer.CreateClient()`; CI has no API key. |
| tech-stack — "Microsoft.Extensions.AI — provider-agnostic embedding abstraction (`IEmbeddingGenerator`); the OpenAI-compatible client plugs in behind it." | **Complies.** The design uses exactly that interface as the seam. |
| tech-stack — "Keep the provider behind the abstraction — never call an embedding API directly from pipeline code." | **Complies.** Only `OpenAiCompatibleEmbeddingGenerator` (Infrastructure) touches the API; it is registered in Program.cs (composition root), satisfying AC-1.1's scan. |
| tech-stack — "Keep sources swappable via configuration alone" (same pattern for providers) | **Complies.** Base URL, API key (env/user-secrets), model, provider name all come from the `embedding_provider` config section. |
| mission.md — "growth headroom… no unbounded in-process work" | **Complies.** Batch semantics in the abstraction; retry is bounded (no unbounded retry loops). |
| spec Scale-headroom constraint — "must not bake in single-use assumptions (batch semantics exposed)" | **Complies.** `GenerateAsync` batch is the primary path; single text is a thin wrapper (AC-2.3). |
| process.md — "No silent spec drift" / task closure rules | **Complies (process note).** tasks.md `[X]` flips are commit-atomic with evidence; the plan adds no un-spec'd behavior (see §7 proposals column — empty). |

No exceptions required.

## 3. Research — resolutions

**R-1: Abstraction type — `Microsoft.Extensions.AI` vs hand-rolled interface.**
- **Decision:** use `IEmbeddingGenerator<string, Embedding<float>>` (Application references `Microsoft.Extensions.AI.Abstractions`; Infrastructure implements it with the OpenAI-compatible client).
- **Rationale:** tech-stack.md names it as the constitution-level choice; it already has the single→batch delegation (AC-2.3); later phases (ranking pipeline) consume a standard, testable seam.
- **Alternatives:** a bespoke `IEmbeddingProvider` — rejected: duplicates what M.E.AI provides and contradicts the named stack choice.

**R-2: Retry ownership — SDK default vs explicit.**
- **Decision:** disable the OpenAI SDK's internal retry (`OpenAIClientOptions.MaxRetryCount = 0`) and implement bounded retry inside `OpenAiCompatibleEmbeddingGenerator` (transient = HTTP 429 or 5xx; defaults: max 3 attempts, exponential backoff starting 500 ms — exact numbers are the spec's declared implementation-time tuning, T-4.3).
- **Rationale:** AC-4.3/AC-4.4 are observable behaviors counted by the stub ("exactly one request" on non-transient; bounded attempts on transient). Explicit retry makes both mechanically assertable and deterministic in tests.
- **Alternatives:** relying on SDK retry — rejected: not observable/assertable from the stub.

**R-3: Ripple — Api now requires `embedding_provider` to start (AC-5.2), which affects the existing health suite.**
- **Decision:** Program.cs validates `embedding_provider` at startup **before `app.Run()`**; invalid/missing → log an error naming the offending setting (unknown provider name also lists accepted values) and `Environment.ExitCode = 1`. The existing health integration tests keep passing by supplying a test-only configuration (valid dummy values: provider `openai-compat`, base URL `http://localhost:0/v1`, dummy key, model `text-embedding-3-small`) — no network is ever made to it.
- **Rationale:** AC-5.2 makes startup validation part of the application, not just a library; the health contract is unchanged; CI still needs no secrets.
- **Alternatives:** lazy validation on first embed — rejected: spec interview record says "fail fast at startup (Recommended)".

**R-4: Where input/result validation lives (AC-2.2, AC-3.2, AC-3.3, T-1.3 non-finite rejection).**
- **Decision:** all guards live in `OpenAiCompatibleEmbeddingGenerator`, ahead of any request construction: whitespace-only text → error identifying position(s); empty batch → return zero results (no call); response mapping rejects non-finite (NaN/±∞) values and a dimensionality that does not match the configured model output. Result shape: `Embedding<float>` with `ModelId` populated from the configured model (the remote response carries no model echo).
- **Rationale:** keeps the seam one class; every "no network call" assertion is a stub request count.
- **Alternatives:** a validation decorator over a raw generator — rejected: extra type, no AC demands it.

**R-5: Contract-test stub mechanics.**
- **Decision:** an in-process stub HTTP server via `Microsoft.AspNetCore.TestHost` in the UnitTests project (`EmbeddingStubServer` test helper: records requests, scriptable responses — success payload, 429, 5xx, 4xx, connection drop, non-finite vector). `OpenAIClient` is constructed with the stub's `HttpClient` (`TestServer.CreateClient()`) and `Endpoint` from config — so base-URL routing, auth header, model name, and request path (AC-4.1) are all asserted at the stub.
- **Rationale:** zero external network; deterministic; runs in CI with no secrets.
- **Alternatives:** WireMock.Net — rejected (extra dependency for what TestHost already provides); a real socket listener — rejected (non-hermetic ports).

No blocking questions.

## 4. Data model

No persisted entities (storage is Phase 2/3 — spec Non-Goals). The structured artifacts are:

**`EmbeddingProviderSettings`** (config POCO, bound from the `embedding_provider` section — spec B5, quoted validation rules):

| Field | Type | Validation (AC-5.1/AC-5.2) |
|---|---|---|
| `Provider` | string | required, non-blank, ∈ known providers { `openai-compat` } — else startup error naming accepted values |
| `BaseUrl` | Uri | required, absolute, parseable — else startup error naming `embedding_provider:base_url` |
| `ApiKey` | string | required, non-empty — sourced from env/user-secrets (`embedding_provider:api_key`), never committed |
| `Model` | string | required, non-blank (default target: `text-embedding-3-small`) |

**Embedding result** (AC-1.3, "at minimum"): `Embedding<float>` —

- `Vector`: ordered sequence of finite `float` values (non-finite → provider error, T-1.3);
- dimensionality = `Vector.Count`, must equal the model's configured output dimensionality (mismatch → surfaced error, not returned);
- `ModelId`: provider/model identifier that produced it (from config).

**State transitions:** none — the provider is stateless per call; the only app-level state change is startup → validation-pass or validation-fail(exit 1) before any network call.

## 5. Contracts

### 5.1 Configuration contract (appsettings.json + environment/user-secrets)

```jsonc
"embedding_provider": {
  "provider": "openai-compat",
  "base_url": "https://api.nano-gpt.example/v1",   // placeholder resolved per environment
  "model": "text-embedding-3-small"
  // api_key NEVER in committed config — env: EmbeddingProvider__ApiKey (or user-secrets)
}
```

Committed `appsettings.json` ships the section with non-secret defaults only; `api_key` comes from env/user-secrets at every layer (AC-5.1, T-5.1 asserts binding incl. the env path).

### 5.2 Application-level contract (code interface — the seam)

```csharp
// Application consumers (pipeline code, later specs) see only:
IEmbeddingGenerator<string, Embedding<float>>
    Task<Embedding<float>> GenerateEmbeddingAsync(string text)          // AC-2.1/2.2/2.3 — delegates to batch
    Task<IList<Embedding<float>>> GenerateAsync(IReadOnlyList<string>) // AC-3.1/3.2/3.3
```

Input/output behavior per AC — errors are exception types defined by the provider (identifying offending input/position; remote status code; endpoint for connection failures).

### 5.3 Wire contract (OpenAI-compatible, asserted at the stub — AC-4.1/4.2)

- Request: `POST {BaseUrl}/embeddings`, header `Authorization: Bearer {ApiKey}`, body `{"model": <configured model>, "input": [texts…]}` (order-preserving array).
- Response: `{"data": [{"embedding": [floats…], "index": n}], "model": … }` — mapped by response `index` back to input order (AC-3.1); vector + `ModelId` (config) onto the result type.

### 5.4 Startup contract (AC-5.2)

Valid config → DI registration of `IEmbeddingGenerator<string, Embedding<float>>`; invalid (unset/blank provider, unknown provider name, missing key, missing/unparseable base URL) → startup error naming the offending setting (unknown provider: + accepted values) → **exit code 1 before any network call**.

### 5.5 Test-name contract

Every Test ID (T-1.1 … T-5.2) appears verbatim in the test names/comments of the new suites; contract suites run against `EmbeddingStubServer` with zero external requests.

## 6. Quickstart

Reviewer validation, from repo root:

```bash
# 0. Existing gates still green (no regressions)
dotnet build
dotnet test                                   # all 15 new Test IDs + existing health suite pass
dotnet format --verify-no-changes

# 1. Spec-002 suites specifically (expect: 15 passed)
dotnet test --filter FullyQualifiedName~PipelineResolvesConfiguredProvider   # T-1.1
dotnet test --filter FullyQualifiedName~EmbeddingResultShape                 # T-1.3
dotnet test --filter FullyQualifiedName~SingleTextEmbedding                  # T-2.1
dotnet test --filter FullyQualifiedName~EmptyTextRejected                    # T-2.2
dotnet test --filter FullyQualifiedName~SingleDelegatesToBatch               # T-2.3
dotnet test --filter FullyQualifiedName~BatchOrderPreserved                  # T-3.1
dotnet test --filter FullyQualifiedName~BatchWithEmptyTextRejected           # T-3.2
dotnet test --filter FullyQualifiedName~EmptyBatchNoCall                     # T-3.3
dotnet test --filter FullyQualifiedName~OpenAiCompatRequestShape              # T-4.1
dotnet test --filter FullyQualifiedName~OpenAiCompatResponseMapped           # T-4.2
dotnet test --filter FullyQualifiedName~TransientFailureRetried              # T-4.3
dotnet test --filter FullyQualifiedName~NonTransientFailureNoRetry            # T-4.4
dotnet test --filter FullyQualifiedName~UnreachableEndpointSurfacesError      # T-4.5
dotnet test --filter FullyQualifiedName~EmbeddingProviderConfigLoading        # T-5.1
dotnet test --filter FullyQualifiedName~StartupValidationFailsFast           # T-5.2

# 2. Startup fail-fast, observable (AC-5.2): run Api with NO api key configured
env -u EmbeddingProvider__ApiKey dotnet run --project src/VectorJobSearch.Api
# expect: non-zero exit + error naming embedding_provider:api_key, no HTTP call

# 3. Mechanical scan (AC-1.1): pipeline projects must not name the concrete type
grep -rn "OpenAiCompatibleEmbeddingGenerator" src/VectorJobSearch.Api \
  src/VectorJobSearch.Application  # expect: matches only in composition-root wiring/comments

# 4. CI green on push (no secrets present — Cost constraint holds)
gh run list    # expect: conclusion 'success'
```

**Expected observable outcomes:** all commands exit 0; the Api run exits non-zero with the named setting; CI passes with no provider key configured.

## 7. Artifact list

| Artifact | Action |
|---|---|
| `src/VectorJobSearch.Application/VectorJobSearch.Application.csproj` | add `Microsoft.Extensions.AI.Abstractions` package |
| `src/VectorJobSearch.Application/Embedding/` | create: pipeline-facing usage surface (abstraction aliasing/doc; no concrete types) |
| `src/VectorJobSearch.Infrastructure/VectorJobSearch.Infrastructure.csproj` | add `Microsoft.Extensions.AI.OpenAI`, `Microsoft.Extensions.DependencyInjection.Abstractions` |
| `src/VectorJobSearch.Infrastructure/Embedding/EmbeddingProviderSettings.cs` | create (config POCO + validator — §4) |
| `src/VectorJobSearch.Infrastructure/Embedding/OpenAiCompatibleEmbeddingGenerator.cs` | create (guards, request/response mapping, bounded retry, ModelId, dimensionality + non-finite checks) |
| `src/VectorJobSearch.Infrastructure/Embedding/EmbeddingProviderServiceCollectionExtensions.cs` | create (`AddEmbeddingProvider(config)` — resolves one instance from config; registry of known provider names) |
| `src/VectorJobSearch.Api/Program.cs` | amend: startup validation → exit 1 with named setting; DI registration (composition root only) |
| `src/VectorJobSearch.Api/appsettings.json` | amend: add `embedding_provider` section (no secret values) |
| `tests/VectorJobSearch.UnitTests/Embedding/EmbeddingStubServer.cs` | create (TestHost-based recording/scriptable stub — R-5) |
| `tests/VectorJobSearch.UnitTests/Embedding/EmbeddingAbstractionTests.cs` | create (T-1.1, T-1.3, T-2.3) |
| `tests/VectorJobSearch.UnitTests/Embedding/OpenAiCompatContractTests.cs` | create (T-2.1, T-2.2, T-3.1, T-3.2, T-3.3, T-4.1…T-4.5) |
| `tests/VectorJobSearch.UnitTests/Embedding/EmbeddingProviderConfigTests.cs` | create (T-5.1, T-5.2 — all AC-5.2 permutations enumerated) |
| `tests/VectorJobSearch.UnitTests/VectorJobSearch.UnitTests.csproj` | add `Microsoft.AspNetCore.TestHost`; reference Infrastructure |
| `tests/VectorJobSearch.IntegrationTests/` | amend test config only: valid dummy `embedding_provider` so the health suite still starts (R-3) |
| `specs/002-embedding-provider/tasks.md` | create/update via the tasks skill; `[X]` flips commit-atomic with evidence (process.md) |
| README.md | amend: `embedding_provider` configuration docs (env var name, user-secrets; no key values) |

## 8. Traceability table

| Design element | Spec condition |
|---|---|
| `IEmbeddingGenerator<string, Embedding<float>>` as the only pipeline-facing type; concrete type in Infrastructure + Program.cs wiring | AC-1.1; tech-stack Constraints; N-AC (pipeline must not reference concrete types) |
| Result = vector (finite floats) + dimensionality + ModelId; mapping validates finiteness/dimension | AC-1.3; T-1.3; Edge Cases (non-finite, dimension mismatch) |
| `GenerateEmbeddingAsync(string)` delegates to `GenerateAsync([text])` | AC-2.3 |
| Empty/whitespace text rejected with offending-input error before any request construction; single + batch | AC-2.1, AC-2.2 |
| Batch order preservation via response `index`; whole-batch rejection on any bad element | AC-3.1, AC-3.2; Edge Case (one bad element) |
| Empty batch → zero results, zero stub requests | AC-3.3; N-AC (no network call on invalid input) |
| Request path/auth/model at stub; response → result mapping | AC-4.1, AC-4.2 |
| Explicit bounded retry on 429/5xx (SDK retry disabled) | AC-4.3; Open Question (retry tuning → implementation) |
| No retry on non-transient 4xx; status code in error | AC-4.4; N-AC (exactly one request) |
| Connection failure/timeout → error naming endpoint; no hang | AC-4.5 |
| `embedding_provider` config section: name, base URL, api key (env/secrets), model | AC-5.1 |
| Startup validation → exit 1, message names setting; unknown provider lists accepted values; runs before any network call | AC-5.2; Success Criterion (all permutations enumerated in T-5.2); interview record "fail fast" |
| TestHost stub, no live API in any automated suite; CI has no provider secret | Cost constraint; N-AC (CI must not call live metered API); Test ID Registry note |
| Batch-first abstraction; stateless provider | Scale-headroom constraint; Non-Goals (no caching/persistence added) |
| No cosine/CLI/caching/extra providers | Non-Goals (scope guard — nothing in §7 outside spec conditions) |
| tests/ UnitTests contract suites with Test IDs verbatim | Test ID Registry (15/15) |
| tasks.md `[X]` evidence discipline | process.md (spec–test agreement; commit-atomic closure) |

**Explicit proposals (not spec-traced):** none — every element above maps to a spec condition.