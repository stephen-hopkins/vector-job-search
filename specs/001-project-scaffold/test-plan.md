# Test Plan — Spec 001 (Initial Project Scaffold)

**Status:** RED phase (tests written, implementation not started)
**Spec:** [`spec.md`](spec.md)

This plan gives **every acceptance condition exactly one test ID**. The spec's draft
registry grouped some ACs under shared IDs (e.g. AC-9 + AC-10 under `P0-HEALTH-GET`);
this plan splits them 1:1 — each AC is verified by exactly one test, and each test ID
must appear in the source tree as a name or comment (`grep -rn "P0-"` finds them).

## Test suites

| Suite | Location | Runner |
|---|---|---|
| Scaffold verification | `tests/run-project-scaffold-tests.sh` | `bash tests/run-project-scaffold-tests.sh [test-id...]` |
| Backend health (xUnit) | `tests/VectorJobSearch.Api.Tests/` | `dotnet test at the repo root` (filter: `TestSuite=P0-HEALTH`) |
| Contract schemas | `specs/001-project-scaffold/contracts/` | consumed by `P0-HEALTH-*` tests |

The shell suite covers everything that is a command exit code, file inspection, or
`gh` query. The xUnit suite covers the health-route HTTP behaviors (AC-9, AC-10,
E-2, E-3) so they also satisfy AC-5's "at least one meaningful backend test".

## Conventions the implementation must follow (assumptions locked by these tests)

- Repo layout: all test code lives under `tests/`; deliverable projects live
  under `src/`. The .NET solution file (`VectorJobSearch.sln`) and
  `global.json` live at the repository root — `dotnet build`, `dotnet test`,
  `dotnet format` all run there; `src/web/` is the Vite React app (project
  name "web"); CI workflow at `.github/workflows/`.
- Backend solution follows Clean Architecture (onion), with reference direction
  `Api → Infrastructure → Application → Domain`:
  - `src/VectorJobSearch.Domain/` — no references
  - `src/VectorJobSearch.Application/` — references Domain
  - `src/VectorJobSearch.Infrastructure/` — references Application
  - `src/VectorJobSearch.Api/` — entry point; references Infrastructure
  Phase 0 keeps Domain/Application/Infrastructure empty (structure only, no
  persistence or data-model decisions — spec C-4).
- Backend API project: `src/VectorJobSearch.Api/VectorJobSearch.Api.csproj`.
- Health route: `GET /health` (spec B4).
- README **Getting Started** section documents each gate command as an HTML marker
  comment followed by a fenced bash block whose first command line is the gate:

  ````markdown
  <!-- gate: P0-BUILD-FRONTEND -->
  ```bash
  npm --prefix frontend run build
  ```
  ````

  The tests extract and execute exactly that line, so the README is
  machine-verified (B1/AC-2, AC-3, AC-6).

## Mapping: acceptance conditions → tests

| AC / Condition | Test ID | Suite | Verification |
|---|---|---|---|
| AC-1 backend build | `P0-BUILD-BACKEND` | shell | `dotnet build` at the repo root (solution root) exits 0 |
| AC-2 frontend build | `P0-BUILD-FRONTEND` | shell | README-documented build command exits 0 |
| AC-3 frontend type-check | `P0-TYPECHECK-FRONTEND` | shell | README-documented strict type-check command exits 0 |
| AC-4 no placeholders in Getting Started | `P0-README-NOPLACEHOLDER` | shell | `grep` for `TBC`/`coming soon`/`TBD` in Getting Started returns nothing |
| AC-5 backend tests pass | `P0-TEST-BACKEND` | shell | `dotnet test` at the repo root (solution root) exits 0, reports ≥1 passed |
| AC-6 frontend tests pass incl. render test | `P0-TEST-FRONTEND` | shell | documented test command exits 0, ≥1 passed, ≥1 test file uses Testing Library `render(` |
| AC-7 format gate | `P0-FORMAT-BACKEND` | shell | `dotnet format --verify-no-changes` at the repo root (solution root) exits 0 |
| AC-8 editorconfig | `P0-EDITORCONFIG` | shell | `.editorconfig` exists at repo root |
| AC-9 health GET 200 + status JSON | `P0-HEALTH-GET` | xUnit | GET `/health` → 200, body validates against [`contracts/health-response.schema.json`](contracts/health-response.schema.json) |
| AC-10 health with nothing external configured | `P0-HEALTH-STANDALONE` | xUnit | app started with all `VJS_`/`OPENAI_`/`AZURE_`/`ADZUNA_` env vars removed → GET `/health` 200 |
| AC-11 + AC-12 + AC-14 CI definition | `P0-CI-TRIGGERS` | shell | workflow file: `on:` has `push` + `pull_request`; contains steps for all five gates; every `runs-on:` is GitHub-hosted |
| AC-13 CI green for scaffold commit | `P0-CI-GREEN` | shell | `gh run list` shows `success` for HEAD commit |
| N-AC-1 CI fails a broken commit | `P0-CI-RED` | shell | `gh run list` shows a `failure` run on branch `probe/p0-ci-red` (probe procedure below) |
| N-AC-2 hermetic tests | `P0-HERMETIC` | shell | both suites exit 0 inside `unshare -rn` (network blocked, loopback allowed) |
| N-AC-3 no infrastructure code | `P0-NO-TERRAFORM` | shell | `git ls-files '*.tf'` is empty |
| N-AC-4 no secrets | `P0-NO-SECRETS` | shell | tracked files contain no key-shaped strings |

### Edge cases

| Edge case | Test ID | Suite | Verification |
|---|---|---|---|
| E-1 build before install fails, after install passes | `P0-FRONTEND-INSTALL-ORDER` | shell | frontend build without `node_modules` exits non-zero; after documented install step, exits 0 |
| E-2 non-GET on health → 405 | `P0-HEALTH-METHOD-405` | xUnit | POST `/health` returns 405 |
| E-3 unknown route → 404 | `P0-HEALTH-UNKNOWN-404` | xUnit | GET unknown path returns 404 |
| E-4 SDK version pinned | `P0-DOTNET-SDK-PIN` | shell | `global.json` at the repo root exists, installed SDK satisfies it (older SDKs then refuse by design) |
| E-5 unformatted code fails gate | *(covered by `P0-FORMAT-BACKEND`)* | shell | the gate itself is the check, per spec |

### Success criteria (additional checks)

| Criterion | Test ID | Suite |
|---|---|---|
| SC-2 zero build warnings | `P0-BUILD-NOWARN` | shell — `dotnet build -warnaserror` at the repo root (solution root) exits 0 |
| SC-4 green CI ≤ 10 min | `P0-CI-DURATION` | shell — `gh run view` timestamps of the successful run ≤ 600 s |
| SC-1, SC-3 | aggregate | union of `P0-BUILD-*`, `P0-TEST-*`, `P0-TYPECHECK-FRONTEND`, `P0-FORMAT-BACKEND`, `P0-CI-GREEN` |
| SC-5 | meta | guaranteed by construction: every check above is a command exit code, file inspection, or `gh` query |

## `P0-CI-RED` probe procedure (N-AC-1)

CI must be observed rejecting a broken commit. Manual probe, executed once during
green-phase verification:

1. Create branch `probe/p0-ci-red` with a deliberately failing test; push.
2. Confirm `gh run list` reports `failure` for that branch (the `P0-CI-RED` test
   checks for this record).
3. Delete the branch and revert.

## Structured artifacts and their schemas

| Artifact | Schema | Validated by |
|---|---|---|
| Health endpoint JSON response | [`contracts/health-response.schema.json`](contracts/health-response.schema.json) | `P0-HEALTH-GET`, `P0-HEALTH-STANDALONE` (via JsonSchema.Net) |

This is the only structured artifact the scaffold produces; all other outputs are
command exit codes or files inspected directly.

## Open items for green phase

- The spec says `dotnet test --filter P0-HEALTH`; C# identifiers cannot contain
  hyphens, so the trait form `--filter TestSuite=P0-HEALTH` is canonical here.
- `P0-CI-GREEN`, `P0-CI-RED`, `P0-CI-DURATION` require a GitHub remote and pushes —
  they stay red until the scaffold commit exists on GitHub.
- `P0-NO-TERRAFORM` and `P0-NO-SECRETS` may already pass (negative conditions);
  they guard against future regressions.
