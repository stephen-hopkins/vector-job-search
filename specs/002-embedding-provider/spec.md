# Spec: 002 — Embedding Provider Abstraction + First Provider

- **Status:** Draft
- **Roadmap phase:** Phase 1 — Core Matching Pipeline (item 1 of 4)
- **References:** [`specs/mission.md`](../mission.md), [`specs/tech-stack.md`](../tech-stack.md), [`specs/process.md`](../process.md)
- **Related specs:** [`001-project-scaffold`](../001-project-scaffold/spec.md)

## Purpose

This spec delivers the provider-agnostic embedding abstraction for Vector Job Search plus the first concrete implementation against an OpenAI-compatible hosted API (config: `nano-gpt`, model class `text-embedding-3-small`). Every later phase (ideal-job embedding, listing embedding, ranking) consumes this abstraction; no pipeline code may call an embedding API directly. The abstraction is the seam that keeps providers swappable by configuration alone.

## Behavior (WHAT)

### B1 — Embedding abstraction

The system exposes an embedding abstraction that converts text into a dense numeric vector plus metadata. Pipeline code depends only on this abstraction.

- **AC-1.1** Given any valid `embedding_provider` configuration, the application resolves exactly one provider instance for that configured setting; no pipeline code references a concrete provider type. *(Verified by: grep-style code scan — no direct provider instantiation outside composition root; plus test T-1.1.)*
- **AC-1.3** An embedding result carries, at minimum: the vector (as an ordered sequence of finite floating-point numbers), the vector's dimensionality, and the provider/model identifier that produced it. *(T-1.3)*

### B2 — Single-text embedding

- **AC-2.1** Given non-empty text and a working provider, requesting an embedding returns a vector whose dimensionality matches the provider's configured model output. *(T-2.1)*
- **AC-2.2** Given text that is empty or whitespace-only, requesting an embedding is rejected with an error that identifies the invalid input; no network call to the provider is made. *(T-2.2 — test asserts both the error and zero outbound HTTP requests)*
- **AC-2.3** The single-text method delegates to the batch method with a one-element batch. *(T-2.3)*

### B3 — Batch embedding

- **AC-3.1** Given a batch of N ≥ 1 non-empty texts, the system returns N results in the same order as the input texts. *(T-3.1)*
- **AC-3.2** Given a batch containing one or more empty/whitespace-only texts, the entire batch is rejected with an error identifying the offending positions; no network call is made. *(T-3.2)*
- **AC-3.3** Given an empty batch (zero texts), the system returns zero results without a network call. *(T-3.3)*

### B4 — First provider: OpenAI-compatible hosted API

- **AC-4.1** The provider sends embeddings requests to the base URL from configuration, authenticating with the API key from configuration. *(T-4.1 — stub server asserts request path, auth header, model name)*
- **AC-4.2** The provider maps the remote API's response onto the abstraction's result type (vector, dimensions, provider/model id). *(T-4.2)*
- **AC-4.3** Given the remote API returns a transient failure (HTTP 5xx or 429), the provider retries a bounded number of times before surfacing an error. *(T-4.3)*
- **AC-4.4** Given the remote API returns a non-transient failure (HTTP 4xx other than 429), the provider surfaces an error immediately, including the remote status code, with no retry. *(T-4.4)*
- **AC-4.5** Given the remote API is unreachable (connection failure/timeout), the provider surfaces an error identifying the endpoint; the application does not hang or crash uncontrolled. *(T-4.5)*

### B5 — Configuration

- **AC-5.1** Provider configuration is read from application configuration under an `embedding_provider` setting with, at minimum: provider name, base URL, API key (via secret/env, never hardcoded), and model identifier. *(T-5.1 — config-loading unit test per tech-stack testing note)*
- **AC-5.2** On startup with a missing or invalid `embedding_provider` config — unset/blank provider name, provider name not in the set of known providers, missing API key, or missing/unparseable base URL — the application fails during startup validation with a non-zero exit code and a message naming the offending setting (for an unknown provider name, also the accepted values), before any network call. *(T-5.2)*

## Test ID Registry

| Test ID | AC | Suite | Command |
|---|---|---|---|
| T-1.1 | AC-1.1 | Unit (abstraction) | `dotnet test --filter FullyQualifiedName~PipelineResolvesConfiguredProvider` |
| T-1.3 | AC-1.3 | Unit (abstraction) | `dotnet test --filter FullyQualifiedName~EmbeddingResultShape` |
| T-2.1 | AC-2.1 | Contract (stub HTTP) | `dotnet test --filter FullyQualifiedName~SingleTextEmbedding` |
| T-2.2 | AC-2.2 | Contract (stub HTTP) | `dotnet test --filter FullyQualifiedName~EmptyTextRejected` |
| T-2.3 | AC-2.3 | Unit (abstraction) | `dotnet test --filter FullyQualifiedName~SingleDelegatesToBatch` |
| T-3.1 | AC-3.1 | Contract (stub HTTP) | `dotnet test --filter FullyQualifiedName~BatchOrderPreserved` |
| T-3.2 | AC-3.2 | Contract (stub HTTP) | `dotnet test --filter FullyQualifiedName~BatchWithEmptyTextRejected` |
| T-3.3 | AC-3.3 | Contract (stub HTTP) | `dotnet test --filter FullyQualifiedName~EmptyBatchNoCall` |
| T-4.1 | AC-4.1 | Contract (stub HTTP) | `dotnet test --filter FullyQualifiedName~OpenAiCompatRequestShape` |
| T-4.2 | AC-4.2 | Contract (stub HTTP) | `dotnet test --filter FullyQualifiedName~OpenAiCompatResponseMapped` |
| T-4.3 | AC-4.3 | Contract (stub HTTP) | `dotnet test --filter FullyQualifiedName~TransientFailureRetried` |
| T-4.4 | AC-4.4 | Contract (stub HTTP) | `dotnet test --filter FullyQualifiedName~NonTransientFailureNoRetry` |
| T-4.5 | AC-4.5 | Contract (stub HTTP) | `dotnet test --filter FullyQualifiedName~UnreachableEndpointSurfacesError` |
| T-5.1 | AC-5.1 | Unit (config) | `dotnet test --filter FullyQualifiedName~EmbeddingProviderConfigLoading` |
| T-5.2 | AC-5.2 | Unit (config/startup) | `dotnet test --filter FullyQualifiedName~StartupValidationFailsFast` |

All contract tests run against a local stub/fake HTTP endpoint (no live API calls in CI — see interview record). Test IDs must appear verbatim in test names or comments in the source tree.

## Constraints

- **Cost:** No automated test, lint, or CI step may call a paid or metered live embedding API; CI exercises the provider against a stub only (mission.md Cost Constraints).
- **Swappability:** Any OpenAI-compatible provider (OpenAI, Azure OpenAI, nano-gpt) must be selectable via configuration alone — base URL, API key, model (tech-stack.md).
- **Abstraction boundary:** Pipeline code never calls an embedding API directly (tech-stack.md Constraints & Anti-Patterns).
- **Scale headroom:** The abstraction must not bake in single-use assumptions (e.g. it exposes batch semantics so later phases can embed many listings without redesign).
- **Toolchain:** C# 14 / .NET 10, xUnit for tests (tech-stack.md).

## Non-Goals

- **Cosine similarity computation** — next Phase 1 spec (embed listing vs. reference vector).
- **Ideal-job reference embedding workflow** — next Phase 1 spec.
- **CLI presentation of scores** — later Phase 1 spec.
- **Embedding caching / persistence** — Phase 2/3 (listing storage includes embeddings; mission.md says avoid re-embedding unchanged listings).
- **Runtime provider switching via UI/API** — Phase 5 ("configurable embedding providers").
- **Additional concrete providers** (OpenAI, Azure OpenAI endpoints) — the abstraction makes them config-swappable; none are implemented or tested here.
- **Token/cost metering or per-request accounting** — future consideration.

## Clarifications (interview record)

- **Q:** Should the spec cover just the first concrete provider, or also the abstraction's config-swappable contract for future providers?
  **A:** "Abstraction + first provider."
- **Q:** Where does this spec stop — provider only, or all of Phase 1?
  **A:** "Provider only (Recommended)" — cosine similarity, CLI output, and the reference-vector workflow are specced separately.
- **Q:** When should a missing/invalid API key or provider endpoint surface?
  **A:** "Fail fast at startup (Recommended)" — startup validation with non-zero exit before any API call.
- **Q:** Should the interface validate inputs (empty/whitespace text)?
  **A:** "Reject empty input (Recommended)."
- **Q:** How should the provider be tested in automated CI given the cost constraint?
  **A:** "Stub in CI (Recommended)" — contract tests against a stub; real provider exercised manually or in a separately gated suite.
- **Q:** Single text or batching?
  **A:** "Include single and batch methods, with the single calling the batch."

## Edge Cases

- Empty / whitespace-only text input → rejected without network call (AC-2.2, AC-3.2).
- Empty batch → zero results, no network call (AC-3.3).
- Batch with one bad element among good ones → whole batch rejected atomically, no partial API call (AC-3.2).
- Unknown provider name in config → covered by AC-5.2 (folded in from a redundant B1 AC after intervention).
- Missing API key / malformed base URL → startup failure with non-zero exit (AC-5.2).
- Remote API transient failure (429/5xx) → bounded retry (AC-4.3).
- Remote API permanent failure (other 4xx) → immediate error with status, no retry (AC-4.4).
- Endpoint unreachable / connection dropped → surfaced error, no hang (AC-4.5).
- Non-finite values (NaN/Infinity) in a remote response → violated result shape → AC-1.3 contract test asserts rejection (via T-1.3 stub response).
- Vector dimensions not matching model expectation → treated as a provider error, surfaced, not silently returned (covered by AC-1.3/T-1.3 shape assertions).

## Negative Acceptance Conditions

- The application **must not** start successfully with an invalid/missing `embedding_provider` config (AC-5.2 — non-zero exit is the verifiable failure).
- The system **must not** make any network call when input is invalid (AC-2.2, AC-3.2, AC-3.3 — asserted via zero recorded stub requests).
- The provider **must not** retry non-transient 4xx failures (AC-4.4 — stub records exactly one request).
- CI **must not** call a live metered API (constraint — verified by CI running with no provider secrets and passing).
- Pipeline code **must not** reference concrete provider types (AC-1.1 — mechanical scan).

## Success Criteria

- 100% of the ACs above have corresponding passing automated tests (15/15 Test IDs green via `dotnet test`).
- Full test suite runs in CI in under the existing project budget with zero network cost to any metered API (no provider API key present in CI).
- A developer can switch provider (base URL / API key / model) via configuration only, with **zero code changes** outside the composition root (verifiable by inspection diff in a future phase exercise; this spec's AC-1.1 enforces the seam).
- Startup validation rejects all invalid configuration permutations listed in AC-5.2 with non-zero exit codes (all permutations enumerated in T-5.2).

## Open Questions

- **Retry policy specifics** (max attempts, backoff curve) — decide at implementation of T-4.3; bounded retry is the contract, exact numbers are tuning. *(Decision point: Phase 1 implementation.)*
- **Gated live smoke-test suite against the real provider** — recommended but not required here; whether/where it runs is decided when CI secrets are set up. *(Decision point: Phase 4 — secrets management.)*
- **Whether the abstraction also exposes token-usage telemetry** for cost metering — deferred unless a later phase needs it. *(Decision point: Phase 2, when listing collection drives embedding volume.)*