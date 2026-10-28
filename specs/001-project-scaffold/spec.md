# Spec 001 — Initial Project Scaffold

**Status:** Draft
**Roadmap phase:** Phase 0 — Foundation ("Scaffold repository (project structure, tooling, linting, tests)")
**Constitution references:** [`specs/mission.md`](../mission.md), [`specs/tech-stack.md`](../tech-stack.md), [`specs/roadmap.md`](../roadmap.md)

---

## Purpose

This spec delivers the repository scaffold for Vector Job Search: a compiling, tested skeleton of both deliverables (the .NET backend and the React frontend), the tooling that keeps them consistent (format gate, type checks), a CI workflow that enforces the project's quality gates on every push, and a README whose setup instructions are real and machine-verified. It satisfies the Phase 0 exit criterion: a skeleton project builds and tests pass.

## Behavior (WHAT)

### B1 — Both projects build from a clean checkout

A fresh clone of the repository can produce a backend build and a frontend build using only the commands documented in the README, with no manual repair steps.

- **AC-1:** `dotnet build` run at the solution root exits with code 0.
- **AC-2:** The frontend production build command documented in the README exits with code 0.
- **AC-3:** The frontend TypeScript strict-mode type-check command documented in the README exits with code 0.
- **AC-4:** The README's Getting Started section contains no placeholder markers; `grep -rn -e "TBC" -e "coming soon" README.md` returns no matches in that section.

### B2 — Test suites exist and pass

Both projects carry at least one meaningful automated test, proving their test harnesses are wired up and runnable from a clean clone.

- **AC-5:** `dotnet test` run at the solution root exits with code 0 and reports at least 1 passed test.
- **AC-6:** The frontend test command documented in the README exits with code 0 and reports at least 1 passed test, including at least one UI component render test.

### B3 — Format gate passes on the backend

The repository's C# code conforms to the repository style configuration, enforced mechanically.

- **AC-7:** `dotnet format --verify-no-changes` run at the solution root exits with code 0.
- **AC-8:** A `.editorconfig` file exists at the repository root.

### B4 — Backend serves a health endpoint

The backend skeleton starts a web host and exposes a health route that reports service status. No other HTTP surface is required.

- **AC-9:** An HTTP GET request to the health route documented by the backend returns status 200 with a JSON body reporting the service status.
- **AC-10:** The health route returns 200 with no external services configured or reachable (proving the skeleton starts standalone).

### B5 — CI enforces the quality gates on every push

A GitHub Actions workflow builds, tests, and format-checks both projects, and frontend type-checks, on every push and pull request.

- **AC-11:** A workflow file exists under `.github/workflows/` that triggers on both `push` and `pull_request` events (verifiable by reading the workflow file's `on:` triggers).
- **AC-12:** The workflow contains steps that run the backend build + tests, the backend format check, the frontend production build, the frontend type-check, and the frontend tests (verifiable by the presence of these steps in the workflow file).
- **AC-13:** The workflow run for the scaffold commit completes with conclusion `success` (verifiable via `gh run list`).
- **AC-14:** The workflow uses GitHub-hosted runners only — no self-hosted or paid runner configuration (verifiable by reading the `runs-on:` values in the workflow file).

## Test ID Registry

| Test ID | AC(s) | Suite | Verification |
|---|---|---|---|
| `P0-BUILD-BACKEND` | AC-1 | backend CI job | `dotnet build` exit code 0 |
| `P0-BUILD-FRONTEND` | AC-2 | frontend CI job | documented frontend build command exit code 0 |
| `P0-TYPECHECK-FRONTEND` | AC-3 | frontend CI job | documented type-check command exit code 0 |
| `P0-README-NOPLACEHOLDER` | AC-4 | docs check | `grep -rn -e "TBC" -e "coming soon" README.md` returns no Getting Started matches |
| `P0-TEST-BACKEND` | AC-5 | backend CI job | `dotnet test` exit code 0, ≥1 passed |
| `P0-TEST-FRONTEND` | AC-6 | frontend CI job | documented frontend test command exit code 0, ≥1 passed incl. ≥1 render test |
| `P0-FORMAT-BACKEND` | AC-7 | backend CI job | `dotnet format --verify-no-changes` exit code 0 |
| `P0-EDITORCONFIG` | AC-8 | docs check | `.editorconfig` exists at repo root |
| `P0-HEALTH-GET` | AC-9, AC-10 | backend test suite | automated GET of the health route asserting 200 + status JSON (`dotnet test --filter P0-HEALTH`) |
| `P0-CI-GREEN` | AC-13 | CI | `gh run list` shows `success` for the scaffold commit |
| `P0-CI-TRIGGERS` | AC-11, AC-12, AC-14 | CI definition | workflow file inspection (`on:`, step list, `runs-on:`) |
| `P0-CI-RED` | N-AC-1 | CI | workflow run on a commit with an intentionally failing test reports `failure` |

Every Test ID must appear in the future source tree (test names or comments) so `grep -rn "P0-"` in the implemented repository finds them.

## Constraints

Restated from the constitution as verifiable properties:

- **C-1 — Zero added cost:** the scaffold introduces no paid services or paid CI capacity; the workflow uses only GitHub-hosted free runners (see AC-14) and provisions no cloud resources.
- **C-2 — Stack conformance:** the backend targets .NET 10 / C# 14 and the frontend is React 19 with TypeScript 5.9 in strict mode (verifiable by reading the project files: target framework, TypeScript version, `"strict": true`).
- **C-3 — Testing stack conformance:** backend tests use xUnit; frontend tests use Vitest with React Testing Library (verifiable by reading test project references).
- **C-4 — No design dead ends:** the scaffold makes no data-model or persistence decisions; no code in the skeleton presumes in-memory-only storage or single-user assumptions (verifiable: the skeleton contains no persistence code at all).
- **C-5 — Hermetic tests:** the full test suite passes with outbound network access blocked (loopback allowed), so CI and local verification are identical (see N-AC-2).

## Non-Goals

- **Embedding provider abstraction and first provider** — Phase 1 spec.
- **Ideal-job embedding, similarity scoring** — Phase 1 spec.
- **Pluggable listing-source architecture and first real source** — Phase 2 spec.
- **Vector store / database integration** — Phase 2–3 specs.
- **Ranked-results web UI and backend↔frontend wiring** — Phase 3 spec (the scaffold frontend is a placeholder page only).
- **Terraform, Azure provisioning, deployment pipeline** — Phase 4 spec (see N-AC-3).
- **Configuration schema and config loading** — deliberately undecided; values will be chosen at a later date (see Open Questions).
- **Full setup/configuration/usage documentation** — Phase 5 (this spec only makes Getting Started instructions real).
- **Pre-commit hooks or other client-side enforcement** — not planned; CI is the only quality gate.

## Clarifications

Recorded verbatim from the spec interview:

1. **Q:** "Phase 0 says 'scaffold repository'. The tech stack has two deliverables — a .NET backend and a React frontend. Should this spec cover BOTH a backend and a frontend skeleton, or backend-only with the frontend deferred?"
   **A:** "Both backend and frontend."
2. **Q:** "Should the scaffold include the GitHub Actions CI workflow (build + tests + lint/format checks on push), or is CI deferred to Phase 4?"
   **A:** "Yes, include CI."
3. **Q:** "The roadmap marks 'Define configuration schema (ideal_job, sources, embedding_provider, top_k)' as done, but no artifact exists — just the field names. Should this scaffold include a working, validated config model, or is config loading Phase 1 work?"
   **A:** "Remove the reference to config from the roadmap — config values will be decided at a later date." *(Actioned: the checkbox was removed from `specs/roadmap.md` Phase 0.)*
4. **Q:** "Should the backend skeleton expose any HTTP surface at all (e.g., a health endpoint proving the web host starts), or is it purely a compiling solution with passing tests and no endpoints?"
   **A:** "Include a health endpoint."
5. **Q:** "The README's Getting Started section is currently placeholders ('TBC', 'coming soon'). Should this scaffold make the README's build/test/run instructions real and machine-verified, or leave docs for Phase 5?"
   **A:** "Yes, real instructions."

6. **Q:** "Should the backend test code live in a single `Api.Tests` project, or split into unit and integration test projects?"
   **A:** "Rename to two projects: `VectorJobSearch.UnitTests` (future pure-unit tests) and `VectorJobSearch.IntegrationTests` (host-level tests such as the health-endpoint suite). Both are xUnit, both are registered in the solution and run by `dotnet test`." *(Actioned during Phase 2: `tests/VectorJobSearch.Api.Tests` → `tests/VectorJobSearch.IntegrationTests` + new `tests/VectorJobSearch.UnitTests`.)*

## Edge Cases

- **E-1 — Frontend built without dependencies installed:** running the frontend build before the documented dependency-install step exits non-zero; after the install step, the same build exits 0. Both outcomes are exit-code verifiable.
- **E-2 — Health route called with a non-GET method:** a POST to the health route does not return the health payload; it is rejected with an HTTP 405 status. (Test: `P0-HEALTH` suite.)
- **E-3 — Unknown route requested:** a request to any non-existent route returns HTTP 404, not a 500. (Test: `P0-HEALTH` suite.)
- **E-4 — Wrong .NET SDK version:** the backend pins its SDK version; `dotnet build` with an older installed SDK exits non-zero with an SDK-version error message rather than attempting an incompatible build. (Verifiable by exit code and error text.)
- **E-5 — Unformatted code pushed:** `dotnet format --verify-no-changes` exits non-zero when any file needs formatting changes (the gate itself is the mechanical check; also covered by N-AC-1's red-run logic via AC-7).

## Negative Acceptance Conditions

- **N-AC-1 — CI must not pass a broken commit:** a workflow run whose commit contains an intentionally failing test reports conclusion `failure` (verifiable via `gh run list`; test ID `P0-CI-RED`).
- **N-AC-2 — Tests must not depend on the outside world:** the complete backend and frontend test suites exit 0 with outbound network access blocked (loopback allowed), proving no test calls an external API. (The health-endpoint test targets loopback and is unaffected.)
- **N-AC-3 — No infrastructure code in Phase 0:** `git ls-files '*.tf'` returns an empty list; the scaffold provisions no Terraform/Azure resources.
- **N-AC-4 — No secrets in the repository:** the scaffold requires no API keys or credentials; a grep of tracked files for key-shaped strings returns nothing (the skeleton runs standalone per AC-10).

## Success Criteria

- **SC-1:** From a fresh `git clone`, the complete documented verification sequence (backend build, backend tests, backend format check, frontend install, frontend build, frontend type-check, frontend tests) runs with every command exiting 0 — 0 manual repair steps.
- **SC-2:** Backend build completes with 0 warnings (`dotnet build -warnaserror` exits 0).
- **SC-3:** Test counts at delivery: ≥1 passing backend test, ≥1 passing frontend test including ≥1 component render test; 0 skipped/ignored tests.
- **SC-4:** A green CI run for the scaffold commit completes within 10 minutes (verifiable via `gh run view <run-id> --json startedAt,updatedAt`).
- **SC-5:** 100% of acceptance conditions in this spec are verifiable by command exit codes, file/schema inspection, or `gh` output; 0 require human judgment.

## Open Questions

- **OQ-1 — Configuration schema:** what configuration values exist (`ideal_job`, `sources`, `embedding_provider`, `top_k` had been provisionally named), their shape, and loading behavior. **Decision point:** before Phase 1 implementation begins, as the embedding provider will need provider settings from configuration.
- **OQ-2 — Frontend linting tooling:** whether an ESLint config joins the existing TypeScript strict-mode gate. **Decision point:** with the Phase 3 frontend spec, or earlier if CI review demands it; TypeScript strict compile is the enforced gate until then.