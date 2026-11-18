# Tasks — Spec 002: Embedding Provider Abstraction + First Provider

**Status:** Ready for implementation
**Source:** [`spec.md`](spec.md) · [`plan.md`](plan.md)
**Working branch (per repo convention):** `feat/002-embedding-provider`

Conventions for every task below:

- `[P]` marks tasks that may run in parallel with other `[P]` tasks in the same phase (they touch different files and depend only on already-complete tasks).
- `[USn]` tags appear only on user-story tasks (US1–US5, one per spec behavior B1–B5). Untagged tasks are setup/foundational/polish.
- All paths are relative to the repository root `/home/stephen/projects/vector-job-search`.
- **Use the dotnet CLI for all .NET project/package/reference changes** (`dotnet add package`, `dotnet add reference`) — never hand-edit `.csproj` package references. Only then edit generated files as tasks direct (spec 001 convention).
- All dotnet commands (`dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`) run **at the repository root**, where `VectorJobSearch.sln` and `global.json` live.
- **Cost constraint (mission.md, spec Constraints):** "No automated test, lint, or CI step may call a paid or metered live embedding API; CI exercises the provider against a stub only." Every test task below runs against the in-process `EmbeddingStubServer` (plan.md R-5) — zero external HTTP, no API key in CI.
- **Test ID contract (spec Test ID Registry):** "Test IDs must appear verbatim in test names or comments in the source tree." The 15 filters listed in plan.md §6 must each select at least one test.
- **Abstraction boundary (tech-stack, spec constraint):** "Pipeline code never calls an embedding API directly" — the concrete type `OpenAiCompatibleEmbeddingGenerator` may appear **only** in `src/VectorJobSearch.Infrastructure/Embedding/` and the composition root wiring in `src/VectorJobSearch.Api/Program.cs` (AC-1.1 mechanical scan, plan.md §6 step 3).
- Package versions are resolved at implementation time as "latest 9.x stable" (`Microsoft.Extensions.AI.*`) / "10.x" (TestHost, DI.Abstractions) per plan.md §1 — pin what `dotnet add package` resolves.
- **Task closure (process.md):** a task is marked `[X]` only in the same commit/PR that turns its mapped test green, with evidence recorded in the commit message.

---

## Phase 1 — Setup: packages, config section, and shared test infrastructure

Goal: all four new packages are referenced, the `embedding_provider` config section exists with non-secret defaults, the TestHost stub helper exists, and `dotnet build` is green. No acceptance condition is satisfied by Phase 1 alone.

Test criteria for the phase: `dotnet build` exits 0 at the repo root; `grep -c embedding_provider src/VectorJobSearch.Api/appsettings.json` returns ≥ 1; the stub helper file compiles. The 15 spec test IDs may all still be unimplemented/failing.

- [X] 2.1.1 [P] Add packages via the dotnet CLI: `dotnet add src/VectorJobSearch.Application/VectorJobSearch.Application.csproj package Microsoft.Extensions.AI.Abstractions` (latest 9.x stable) and `dotnet add src/VectorJobSearch.Infrastructure/VectorJobSearch.Infrastructure.csproj package Microsoft.Extensions.AI.OpenAI` plus `package Microsoft.Extensions.DependencyInjection.Abstractions` (10.x) (plan.md §1 table). Confirm the onion reference direction is untouched (Api → Infrastructure → Application → Domain; Infrastructure already references Application from spec 001).
- [X] 2.1.2 [P] Add `Microsoft.AspNetCore.TestHost` (10.x) to `tests/VectorJobSearch.UnitTests/VectorJobSearch.UnitTests.csproj` and a project reference to `src/VectorJobSearch.Infrastructure/VectorJobSearch.Infrastructure.csproj` (`dotnet add reference`), per plan.md §7 artifact list. No other test-project changes.
- [X] 2.1.3 [P] Amend `src/VectorJobSearch.Api/appsettings.json`: add the `embedding_provider` section exactly per plan.md §5.1 — `"provider": "openai-compat"`, `"base_url"` (placeholder), `"model": "text-embedding-3-small"`. **No `api_key` key is ever committed** (AC-5.1: "API key (via secret/env, never hardcoded)"; the key arrives via env `EmbeddingProvider__ApiKey` or user-secrets).
- [X] 2.1.4 [P] Create `tests/VectorJobSearch.UnitTests/Embedding/EmbeddingStubServer.cs`: an in-process `Microsoft.AspNetCore.TestHost` stub that (a) **records every request** (method, path, `Authorization` header, JSON body incl. model name and input array order) with a retrievable count, and (b) is **scriptable** with responses: success payload (`{"data":[{"embedding":[floats…],"index":n}]}` in shuffled order so order-mapping is exercised), HTTP 429, HTTP 5xx, non-429 4xx, connection drop, and a vector containing non-finite values (NaN/±Infinity) (plan.md R-5; spec Edge Cases). Expose `HttpClient` via `TestServer.CreateClient()` so `OpenAIClient` can be pointed at it. File compiles but no tests use it yet.

## Phase 2 — Foundational: failing tests that block all stories

Goal: all 15 Test IDs exist as failing (RED) xUnit tests with the verbatim-Test-ID names, so each story phase makes its own tests pass. No acceptance condition is satisfied by Phase 2 alone.

Test criteria for the phase: `dotnet test --filter "FullyQualifiedName~Embedding|FullyQualifiedName~PipelineResolves|FullyQualifiedName~Startup|FullyQualifiedName~Single|FullyQualifiedName~Batch|FullyQualifiedName~Empty"` compiles and reports the 15 Test IDs failing (not crashing) for the right reasons (missing types/methods), and each of the 15 filters in plan.md §6 selects ≥ 1 test. Write tests against the seam only (`IEmbeddingGenerator<string, Embedding<float>>`, `EmbeddingProviderSettings`) — they fail to compile/resolve until Phase 3+ provides the types, which is the RED gate.

- [X] 2.2.1 [P] Create `tests/VectorJobSearch.UnitTests/Embedding/EmbeddingAbstractionTests.cs` containing failing tests with Test IDs verbatim in names/comments: **T-1.1** `PipelineResolvesConfiguredProvider` — given a valid `embedding_provider` config, the DI container resolves exactly one `IEmbeddingGenerator<string, Embedding<float>>` instance and the resolved runtime type is the configured provider's; **T-1.3** `EmbeddingResultShape` — a result carries `Vector` as "an ordered sequence of finite floating-point numbers" (assert rejection of NaN/±Infinity from the stub), dimensionality = `Vector.Count` matching the configured model output (mismatch → surfaced error, not returned), and `ModelId` = configured model; **T-2.3** `SingleDelegatesToBatch` — `GenerateEmbeddingAsync(text)` results in exactly one stub request with a one-element input array (single delegates to batch).
- [X] 2.2.2 [P] Create `tests/VectorJobSearch.UnitTests/Embedding/OpenAiCompatContractTests.cs` containing failing tests, all against `EmbeddingStubServer` (zero external HTTP), Test IDs verbatim: **T-2.1** `SingleTextEmbedding` (non-empty text → vector whose dimensionality matches the configured model output); **T-2.2** `EmptyTextRejected` (empty/whitespace-only text → error identifying the invalid input **and stub request count == 0**); **T-3.1** `BatchOrderPreserved` (N ≥ 1 non-empty texts → N results in input order, stub returns data shuffled by `index`); **T-3.2** `BatchWithEmptyTextRejected` (batch containing empty/whitespace-only elements → whole batch rejected with error identifying the offending positions, stub count == 0); **T-3.3** `EmptyBatchNoCall` (zero texts → zero results, stub count == 0); **T-4.1** `OpenAiCompatRequestShape` (stub asserts request path `POST {BaseUrl}/embeddings`, header `Authorization: Bearer {ApiKey}`, body model name = configured model); **T-4.2** `OpenAiCompatResponseMapped` (remote response mapped to result: vector, dimensions, provider/model id); **T-4.3** `TransientFailureRetried` (429 then 5xx then success → succeeds; bounded attempts recorded, assert attempt count ≤ configured max); **T-4.4** `NonTransientFailureNoRetry` (4xx other than 429 → immediate error including the remote status code, **stub records exactly one request**); **T-4.5** `UnreachableEndpointSurfacesError` (connection drop/timeout → error naming the endpoint, test completes without hanging — wrap in a short test timeout).
- [X] 2.2.3 [P] Create `tests/VectorJobSearch.UnitTests/Embedding/EmbeddingProviderConfigTests.cs` containing failing tests, Test IDs verbatim: **T-5.1** `EmbeddingProviderConfigLoading` — the `embedding_provider` section binds to `EmbeddingProviderSettings` (provider, base_url, model from appsettings; `api_key` from env var `EmbeddingProvider__ApiKey`); **T-5.2** `StartupValidationFailsFast` — **all AC-5.2 permutations enumerated**: (a) unset/blank provider name, (b) provider name not in the set of known providers, (c) missing API key, (d) missing base URL, (e) unparseable base URL — each → validation error with non-zero exit semantics **naming the offending setting**, and for (b) **also the accepted values**, before any network call.

## Phase 3 — US1 (B1) + US2 (B2) + US3 (B3): the abstraction, input guards, and batch semantics

US1, US2, and US3 share one implementation class (plan.md R-4: all guards live in the provider, ahead of request construction), so they are implemented together against the seam; their tests already exist from Phase 2.

Goal: the seam exists, single/batch methods behave per B2/B3, and the result shape holds (B1).

Test criteria for the phase: `dotnet test --filter FullyQualifiedName~PipelineResolvesConfiguredProvider` (T-1.1), `~EmbeddingResultShape` (T-1.3), `~SingleTextEmbedding` (T-2.1), `~EmptyTextRejected` (T-2.2), `~SingleDelegatesToBatch` (T-2.3), `~BatchOrderPreserved` (T-3.1), `~BatchWithEmptyTextRejected` (T-3.2), `~EmptyBatchNoCall` (T-3.3) all pass.

Acceptance conditions satisfied: **AC-1.1, AC-1.3, AC-2.1, AC-2.2, AC-2.3, AC-3.1, AC-3.2, AC-3.3** (+ Edge Cases: empty input, empty batch, one bad element among good ones, non-finite values, dimension mismatch).

- [X] 3.1 [P] Create `src/VectorJobSearch.Application/Embedding/` usage surface: package-only + minimal pipeline-facing documentation/aliasing of `IEmbeddingGenerator<string, Embedding<float>>` (plan.md §7: "pipeline-facing usage surface (abstraction aliasing/doc; no concrete types)"). **No concrete provider types appear in Application** (AC-1.1 mechanical scan).
- [X] 3.2 Create `src/VectorJobSearch.Infrastructure/Embedding/EmbeddingProviderSettings.cs`: config POCO bound from the `embedding_provider` section with a validator implementing plan.md §4 rules verbatim — `Provider`: "required, non-blank, ∈ known providers { `openai-compat` } — else startup error naming accepted values"; `BaseUrl`: "required, absolute, parseable — else startup error naming `embedding_provider:base_url`"; `ApiKey`: "required, non-empty — sourced from env/user-secrets (`embedding_provider:api_key`), never committed"; `Model`: "required, non-blank (default target: `text-embedding-3-small`)".
- [X] 3.3 Create `src/VectorJobSearch.Infrastructure/Embedding/EmbeddingProviderServiceCollectionExtensions.cs`: `AddEmbeddingProvider(IConfiguration)` (plan.md §7) — hosts the registry of known provider names (`openai-compat`), binds + validates `EmbeddingProviderSettings`, and registers exactly one `IEmbeddingGenerator<string, Embedding<float>>` for the configured provider (AC-1.1: "the application resolves exactly one provider instance for that configured setting"). Validation failure throws a descriptive validation error naming the offending setting (accepted values for unknown provider) so the composition root can convert it to exit 1 (US5 wires that).
- [X] 3.4 Create `src/VectorJobSearch.Infrastructure/Embedding/OpenAiCompatibleEmbeddingGenerator.cs` implementing `IEmbeddingGenerator<string, Embedding<float>>`: input guards **ahead of any request construction** (plan.md R-4) — empty/whitespace-only single text → error identifying the invalid input; batch with any empty/whitespace-only text → whole batch rejected with error identifying the offending positions, no partial API call; empty batch → return zero results without a network call. Response mapping: vector from response `index`-ordered data; reject non-finite (NaN/±∞) values; reject dimensionality ≠ configured model output; `ModelId` populated from the configured model. Single-text method delegates to the batch method with a one-element batch (AC-2.3). Sets `OpenAIClientOptions.MaxRetryCount = 0` (plan.md R-2) — retry is owned by this class (US4 task 4.2 adds it).

## Phase 4 — US4 (B4): the OpenAI-compatible provider wire behavior and retry

Goal: the concrete provider speaks the OpenAI-compatible wire contract, maps responses, and handles all remote-failure modes.

Test criteria for the phase: `dotnet test --filter FullyQualifiedName~OpenAiCompatRequestShape` (T-4.1), `~OpenAiCompatResponseMapped` (T-4.2), `~TransientFailureRetried` (T-4.3), `~NonTransientFailureNoRetry` (T-4.4), `~UnreachableEndpointSurfacesError` (T-4.5) all pass. Re-run the Phase 3 filters to confirm no regression.

Acceptance conditions satisfied: **AC-4.1, AC-4.2, AC-4.3, AC-4.4, AC-4.5** (+ Edge Cases: transient 429/5xx, permanent 4xx, endpoint unreachable; Negative ACs: exactly one request on non-transient 4xx, no uncontrolled hang).

- [X] 4.1 [US4] In `src/VectorJobSearch.Infrastructure/Embedding/OpenAiCompatibleEmbeddingGenerator.cs`, implement the wire request per plan.md §5.3 verbatim: "Request: `POST {BaseUrl}/embeddings`, header `Authorization: Bearer {ApiKey}`, body `{"model": <configured model>, "input": [texts…]}` (order-preserving array)" using the OpenAI SDK client pointed at the configured `Endpoint`/base URL — T-4.1 asserts path, auth header, and model at the stub; response mapping per "Response: `{"data": [{"embedding": [floats…], "index": n}], "model": … }` — mapped by response `index` back to input order" — T-4.2.
- [X] 4.2 [US4] In the same class, implement **bounded explicit retry** per plan.md R-2: transient = HTTP 429 or 5xx; defaults max 3 attempts, exponential backoff starting 500 ms (exact numbers are the spec's declared implementation-time tuning — T-4.3; make them constructor-settable so tests stay fast by injecting short delays). Non-transient 4xx (other than 429) → surface an error immediately **including the remote status code, with no retry** (AC-4.4). Connection failure/timeout → surface an error **identifying the endpoint**; no uncontrolled hang (AC-4.5). SDK internal retry stays disabled (`MaxRetryCount = 0`).

## Phase 5 — US5 (B5): configuration loading and startup fail-fast

Goal: config binds from appsettings + env, and the application exits non-zero on any invalid `embedding_provider` permutation before any network call.

Test criteria for the phase: `dotnet test --filter FullyQualifiedName~EmbeddingProviderConfigLoading` (T-5.1) and `~StartupValidationFailsFast` (T-5.2) pass; the full suite (`dotnet test`) passes including the existing health integration tests (R-3 ripple below).

Acceptance conditions satisfied: **AC-5.1, AC-5.2** (+ Edge Cases: unknown provider name, missing API key, malformed base URL; Negative AC: "The application must not start successfully with an invalid/missing `embedding_provider` config").

- [X] 5.1 [US5] Verify env-based key binding works end-to-end: `EmbeddingProvider__ApiKey` (and user-secrets path) binds into `EmbeddingProviderSettings.ApiKey` — T-5.1 asserts both the appsettings path and the env path; no code change expected beyond what Phases 3–4 built unless the test reveals a binding-name gap.
- [X] 5.2 [US5] Amend `src/VectorJobSearch.Api/Program.cs` (composition root, the only Api-side file allowed to name the concrete registration per AC-1.1): call `AddEmbeddingProvider(...)` and perform startup validation **before `app.Run()`** per plan.md R-3 — on validation failure, log an error naming the offending setting (unknown provider name also lists accepted values) and set `Environment.ExitCode = 1` / return without running, **before any network call**. All T-5.2 permutations enumerated: unset/blank provider name, unknown provider name, missing API key, missing base URL, unparseable base URL.
- [X] 5.3 [US5] Fix the R-3 ripple: amend `tests/VectorJobSearch.IntegrationTests/` test configuration **only** to supply a valid dummy `embedding_provider` (provider `openai-compat`, base URL `http://localhost:0/v1`, dummy key, model `text-embedding-3-small` — plan.md R-3 verbatim values) so the existing health suite still starts and passes. **No network call is ever made to the dummy endpoint.**

## Phase 6 — Polish and cross-cutting concerns

Goal: full-suite green, mechanical AC-1.1 scan clean, README documents the config, CI passes with no secrets.

Test criteria for the phase: `dotnet test` (all 15 Test IDs + existing suites) passes; `dotnet format --verify-no-changes` clean; the plan.md §6 quickstart steps 2–4 behave as specified; `gh run list` shows `success` with no provider secrets in CI (Cost constraint).

- [ ] 6.1 [P] Run the AC-1.1 mechanical scan from plan.md §6 step 3 and fix any violation: `grep -rn "OpenAiCompatibleEmbeddingGenerator" src/VectorJobSearch.Api src/VectorJobSearch.Application` — expect matches only in composition-root wiring/comments in `Program.cs`. Confirm pipeline code (Application) references only `IEmbeddingGenerator<string, Embedding<float>>`.
- [ ] 6.2 [P] Amend `README.md`: document the `embedding_provider` section — provider, base_url, model, and the env var name `EmbeddingProvider__ApiKey` / user-secrets alternative (plan.md §7: "env var name, user-secrets; no key values"). No secret values in the README (Cost/AC-5.1).
- [ ] 6.3 Verify the AC-5.2 fail-fast observable per plan.md §6 step 2: `env -u EmbeddingProvider__ApiKey dotnet run --project src/VectorJobSearch.Api` exits non-zero with an error naming `embedding_provider:api_key`, and no HTTP call is made. Record as evidence in the closing commit.
- [ ] 6.4 Final gates: `dotnet build`, `dotnet test` (15/15 spec Test IDs + spec-001 suites), `dotnet format --verify-no-changes` all exit 0 at the repo root; CI run green with no provider API key present (Negative AC: "CI must not call a live metered API — verified by CI running with no provider secrets and passing"). Flip `[X]` on completed tasks in this same PR (process.md commit-atomic closure).

---

## Dependencies

- Phase 1 tasks are mutually parallel; 2.1.2 (test-project packages) needs nothing from 2.1.1.
- Phase 2 test-writing needs Phase 1's stub helper (2.1.4) and package references to compile the test intent; the three Phase 2 tasks are parallel with each other.
- Phase 3 needs Phase 2 (RED tests exist) — tasks 3.2 → 3.3 → 3.4 are sequential (extensions register the generator the generator class defines; settings feed both).
- Phase 4 needs Phase 3's generator class (4.1, 4.2 both edit the same file — sequential with each other).
- Phase 5 needs Phase 3 (settings + registration extension exist) and is otherwise parallel with Phase 4; 5.3 needs 5.2.
- Phase 6 needs all story phases complete; 6.1/6.2 are parallel, 6.3 needs 5.2, 6.4 needs everything.

**Story blocking:** none of US1–US5 depends on another *story*; they share one implementation class in order B1→B5. A minimal meaningful slice is US1+US2 (the seam + single-text), but the natural MVP scope is **US1–US4** (a fully working provider); US5 is small but touches the composition root and the existing health suite (R-3 ripple), so it lands last.

## Suggested MVP scope

**US1–US4** (B1–B4): the abstraction + a working, contract-tested provider. US5 (startup fail-fast) is deliberately sequenced last because its R-3 ripple touches an existing suite, but the spec's Success Criteria ("15/15 Test IDs green") requires all five — treat full completion as the target for this feature branch.

---

## Traceability appendix

| Task | Test ID(s) | Spec condition(s) |
|---|---|---|
| 2.1.3 | — (setup for T-5.1) | AC-5.1 (config section, no secrets) |
| 2.1.4 | — (enables all contract tests) | Cost constraint; spec Test ID Registry note |
| 2.2.1 | T-1.1, T-1.3, T-2.3 | AC-1.1, AC-1.3, AC-2.3 |
| 2.2.2 | T-2.1, T-2.2, T-3.1, T-3.2, T-3.3, T-4.1, T-4.2, T-4.3, T-4.4, T-4.5 | AC-2.1, AC-2.2, AC-3.1, AC-3.2, AC-3.3, AC-4.1–AC-4.5 |
| 2.2.3 | T-5.1, T-5.2 | AC-5.1, AC-5.2 |
| 3.1 | T-1.1 (scan target) | AC-1.1; tech-stack abstraction boundary |
| 3.2 | T-5.1, T-5.2 | AC-5.1, AC-5.2 |
| 3.3 | T-1.1, T-5.2 | AC-1.1, AC-5.2 |
| 3.4 | T-1.3, T-2.1, T-2.2, T-2.3, T-3.1, T-3.2, T-3.3 | AC-1.3, AC-2.1, AC-2.2, AC-2.3, AC-3.1, AC-3.2, AC-3.3 |
| 4.1 | T-4.1, T-4.2 | AC-4.1, AC-4.2 |
| 4.2 | T-4.3, T-4.4, T-4.5 | AC-4.3, AC-4.4, AC-4.5 |
| 5.1 | T-5.1 | AC-5.1 |
| 5.2 | T-5.2 | AC-5.2 |
| 5.3 | — (keeps spec-001 health suite green) | R-3 ripple; process.md no-regression |
| 6.1 | T-1.1 (mechanical scan) | AC-1.1; Negative AC "Pipeline code must not reference concrete provider types" |
| 6.2 | — | AC-5.1 (documentation of env/secrets; no key values) |
| 6.3 | T-5.2 (observable) | AC-5.2; Negative AC "must not start successfully with invalid config" |
| 6.4 | all 15 | Success Criteria (15/15 green); Cost constraint (CI, no secrets); process.md closure |

**Coverage check:** every acceptance condition AC-1.1, AC-1.3, AC-2.1, AC-2.2, AC-2.3, AC-3.1, AC-3.2, AC-3.3, AC-4.1, AC-4.2, AC-4.3, AC-4.4, AC-4.5, AC-5.1, AC-5.2 is covered by at least one test task (AC-1.2 does not exist in the spec — numbering skips it by design). All 15 Test IDs from the registry map to tasks; no gaps.
