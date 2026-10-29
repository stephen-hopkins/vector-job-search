# Tasks — Spec 001: Initial Project Scaffold

**Status:** Ready for execution
**Source:** [`spec.md`](spec.md) · [`plan.md`](plan.md) · [`test-plan.md`](test-plan.md)

Conventions for every task below:

- `[P]` marks tasks that may run in parallel with other `[P]` tasks in the same phase (they touch different files and depend only on already-complete tasks).
- `[USn]` tags appear only on user-story tasks (US1–US5, one per spec behavior B1–B5).
- All paths are relative to the repository root `/home/stephen/projects/vector-job-search`.
- **Use the dotnet CLI to create or modify .NET artifacts — never author solution files, project files, NuGet references, or `global.json` by hand.** Solutions come from `dotnet new sln`, projects from the appropriate template (`dotnet new classlib`, `dotnet new webapi`, `dotnet new xunit`, etc.), the SDK pin from `dotnet new globaljson`, and wiring/config via `dotnet sln add`, `dotnet add reference`, `dotnet add package`. Only then edit the generated files (e.g. to adjust `TargetFramework`, set `rollForward`, or trim template boilerplate) as the tasks direct.
- All dotnet CLI commands (`dotnet new`, `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `dotnet build -warnaserror`) run **at the repository root**, because `VectorJobSearch.sln` and `global.json` live there (plan.md §1, R-2).
- The backend reference direction (onion) is exactly: `src/VectorJobSearch.Api` → `src/VectorJobSearch.Infrastructure` → `src/VectorJobSearch.Application` → `src/VectorJobSearch.Domain`. **No layer points inward past its neighbor; `Domain` references nothing** (plan.md §1). Domain/Application/Infrastructure stay **empty** in Phase 0 — "no abstractions, interfaces, or placeholder files invented 'for later'" (plan.md R-3).
- Constraint **C-4 — "the scaffold makes no data-model or persistence decisions; no code in the skeleton presumes in-memory-only storage or single-user assumptions (verifiable: the skeleton contains no persistence code at all)"** applies to every implementation task. No task below may add entities, repositories, or storage code.
- The RED-phase test suite already exists: `tests/run-project-scaffold-tests.sh` (shell suite, 19 test IDs) and `tests/VectorJobSearch.IntegrationTests/HealthEndpointTests.cs` (xUnit, `TestSuite=P0-HEALTH`). Test tasks below **amend or complete** these files — do not rewrite them from scratch unless a task says so.

---

## Phase 1 — Setup: project initialization and shared infrastructure

Goal: the repository has its final layout (solution + SDK pin at root, four onion projects under `src/`, Vite app at `src/web/`, tests under `tests/`) so every later phase has a file to land in. Nothing in this phase needs to pass gates yet.

Test criteria for the phase: `ls` shows the layout; `dotnet build` may or may not pass yet (it will after Phase 2). No acceptance condition is satisfied by Phase 1 alone.

- [ ] T1.1 [P] Create `global.json` at the repository root **with `dotnet new globaljson --sdk-version 10.0.113`**, then edit the generated file only to set `"rollForward": "latestFeature"` so it reads `{"sdk":{"version":"10.0.113","rollForward":"latestFeature"}}` (plan.md §1: "installed SDK `10.0.113`"; E-4: "the backend pins its SDK version; `dotnet build` with an older installed SDK exits non-zero with an SDK-version error message rather than attempting an incompatible build"). Verify `dotnet --version` still works from the repo root.
- [ ] T1.2 [P] Create the solution and four C# projects **with the dotnet CLI, not by hand-authoring files**: `dotnet new sln -n VectorJobSearch` at the repository root, then `dotnet new classlib` for `VectorJobSearch.Domain`, `VectorJobSearch.Application`, `VectorJobSearch.Infrastructure` (delete each template's `Class1.cs` — these projects stay empty per C-4) and `dotnet new webapi` for `VectorJobSearch.Api`, each created in its `src/<Name>` directory. Add references with `dotnet add reference` and register everything with `dotnet sln add`. Final state: onion reference direction (net10.0, C# 14 — constraint C-2: "the backend targets .NET 10 / C# 14"):
  - `src/VectorJobSearch.Domain/VectorJobSearch.Domain.csproj` — no ProjectReferences, no package references
  - `src/VectorJobSearch.Application/VectorJobSearch.Application.csproj` — references Domain only
  - `src/VectorJobSearch.Infrastructure/VectorJobSearch.Infrastructure.csproj` — references Application only
  - `src/VectorJobSearch.Api/VectorJobSearch.Api.csproj` — references Infrastructure only
  Add all four plus both test projects — `tests/VectorJobSearch.UnitTests/VectorJobSearch.UnitTests.csproj` (pure-unit tests, empty in Phase 0) and `tests/VectorJobSearch.IntegrationTests/VectorJobSearch.IntegrationTests.csproj` (host-level tests; home of the health suite) — to the solution (`dotnet sln add`). Domain/Application/Infrastructure contain **no source files** (C-4). Reduce `src/VectorJobSearch.Api/Program.cs` to a minimal compiling placeholder (`var builder = WebApplication.CreateBuilder(args); var app = builder.Build(); app.Run();` with `public partial class Program;` for test visibility — plan.md artifact list: "`Program.cs` with `GET /health`, `public partial Program`"; strip the `webapi` template's sample weather/forecast endpoints and OpenAPI boilerplate) — the health route itself is T4.2.
- [ ] T1.3 [P] Create `.editorconfig` at the repository root (AC-8: "A `.editorconfig` file exists at the repository root") — start from `dotnet new editorconfig`, which generates the .NET editorconfig defaults (dotnet format's baseline), then extend it so it covers C# (`.cs`) at minimum so `dotnet format` has a ruleset, plus `charset = utf-8`, `insert_final_newline = true`, and `.json`/`.md`/`.yml` sections for consistent CI behavior.
- [ ] T1.4 [P] Scaffold the frontend at `src/web/` (Vite + React 19 + TypeScript 5.9): project name **`web`** (plan.md: "frontend project named `web`") — prefer the Vite scaffolder (`npm create vite@latest web -- --template react-ts` from `src/`, or equivalent) over hand-writing files, then adjust the generated project. `src/web/package.json` pins `"react"`/`"react-dom"` `^19`, `typescript` `~5.9`, plus devDependencies `vite`, `vitest`, `@testing-library/react`, `@testing-library/jest-dom`, `jsdom` (add via `npm install`/`npm install -D`, not hand edits). Constraint C-2: "the frontend is React 19 with TypeScript 5.9 in strict mode"; constraint C-3: "frontend tests use Vitest with React Testing Library". `src/web/tsconfig.json` must set `"strict": true` (C-2, verifiable by reading the file). Scripts in `package.json`: `build` → `tsc -b && vite build` (or equivalent strict build), `typecheck` → `tsc --noEmit`, `test` → `vitest run`. Add `"engines": { "node": ">=24" }` (plan.md R-6 proposal). Add `src/web/vitest` jsdom config (or `test.environment` in `vite.config.ts`) so RTL render tests work. Placeholder page component `src/web/src/App.tsx` renders static text only (spec Non-Goal: "the scaffold frontend is a placeholder page only").
- [ ] T1.5 [P] Amend `tests/run-project-scaffold-tests.sh` layout constants only (plan.md R-2): change `BACKEND_DIR="$REPO_ROOT/backend"` → `BACKEND_DIR="$REPO_ROOT"` (the solution root *is* the repo root; `P0-DOTNET-SDK-PIN` then finds `global.json` at the repo root) and `FRONTEND_DIR="$REPO_ROOT/frontend"` → `FRONTEND_DIR="$REPO_ROOT/src/web"`. Do not change any test logic. Re-read the whole script first and update any other path assumptions that reference `backend/` or `frontend/` to match the new layout.
- [ ] T1.6 [P] Fix the stale informational `$id` in `specs/001-project-scaffold/contracts/health-response.schema.json` (plan.md R-1: the `$id` points at `tests/schemas/…` but the schema lives in `contracts/`, which is what the test's `RepoPaths.HealthResponseSchema` reads) → change `$id` to `https://raw.githubusercontent.com/stephen-hopkins/vector-job-search/main/specs/001-project-scaffold/contracts/health-response.schema.json`. **Do not move the file** ("the test is the source of truth for the path") and do not alter `required: ["status"]`, `enum ["healthy", "degraded", "unhealthy"]`, or `additionalProperties: false`.

---

## Phase 2 — Foundational: complete the failing tests that block all stories

Goal: the RED-phase test suites are complete, layout-correct, and demonstrably failing for the right reasons, so each story phase can make its own tests pass. No acceptance condition is satisfied by Phase 2 alone; it unblocks all of US1–US5.

Test criteria for the phase: `bash tests/run-project-scaffold-tests.sh` runs end-to-end and reports FAIL/SKIP (not crash) for every test ID that depends on scaffold artifacts; `dotnet test` compiles the test project and fails only on `NotImplementedException` from `AppFactory`.

- [X] T2.1 Create the backend test projects **via the dotnet CLI**: `tests/VectorJobSearch.UnitTests` (pure-unit tests; empty in Phase 0) and `tests/VectorJobSearch.IntegrationTests` (host-level tests; `HealthEndpointTests.cs` lives here): `dotnet new xunit -o tests/VectorJobSearch.<Name>` (delete each template's `UnitTest1.cs`), target net10.0, then `dotnet add package` for `xunit.runner.visualstudio`, `Microsoft.AspNetCore.Mvc.Testing`, and `JsonSchema.Net` (IntegrationTests only), and `dotnet add reference src/VectorJobSearch.Api/VectorJobSearch.Api.csproj` (IntegrationTests only) (constraint C-3: "backend tests use xUnit"; plan.md §1: "xUnit + `Microsoft.AspNetCore.Mvc.Testing` (WebApplicationFactory) + `JsonSchema.Net`"). Add both projects to `VectorJobSearch.slnx` with `dotnet sln add`. Confirm `dotnet test` at the repo root compiles and reports 4 failed tests, all failing with `NotImplementedException` from `AppFactory.CreateClient()` — this proves the tests are wired to the (still missing) API. *Evidence: `dotnet test` → "Failed: 4, Passed: 0, Total: 4" in VectorJobSearch.IntegrationTests.dll, all NotImplementedException at HealthEndpointTests.cs AppFactory.CreateClient().*
- [X] T2.2 In `tests/VectorJobSearch.IntegrationTests/HealthEndpointTests.cs`, fix the one mis-indented line (line 41: `var schema = JsonSchema.FromFile(RepoPaths.HealthResponseSchema);        var result = ...` — two statements jammed on one line, plan.md R-4) so the file is `dotnet format`-clean; make **no other content change**. The four tests and their trait `[Trait("TestSuite", "P0-HEALTH")]` stay exactly as written: `P0-HEALTH-GET` (AC-9: GET `/health` → 200, `application/json`, body validates `contracts/health-response.schema.json`), `P0-HEALTH-STANDALONE` (AC-10: 200 with env vars `VJS_`/`OPENAI_`/`AZURE_`/`ADZUNA_`/`REED_`/`CONNECTIONSTRING_`-prefixed removed), `P0-HEALTH-METHOD-405` (E-2: POST `/health` → 405, no health payload), `P0-HEALTH-UNKNOWN-404` (E-3: unknown route → 404 not 500).
- [ ] T2.3 Verify every shell test in `tests/run-project-scaffold-tests.sh` still matches the test-plan mapping after the T1.5 constant change; run `bash tests/run-project-scaffold-tests.sh` and record (in the commit message or session notes) that all scaffold-dependent IDs report FAIL/SKIP and layout-dependent IDs (e.g. `P0-DOTNET-SDK-PIN`, `P0-NO-TERRAFORM`, `P0-NO-SECRETS`) behave as test-plan.md "Open items" predicts ("may already pass (negative conditions); they guard against future regressions"). Fix any test that crashes (as opposed to fails) due to layout drift, changing only path constants inside it.

---

## Phase 3 — US1 (B1): Both projects build from a clean checkout

Goal: "A fresh clone of the repository can produce a backend build and a frontend build using only the commands documented in the README, with no manual repair steps" (spec B1, verbatim).

Test criteria: `P0-BUILD-BACKEND`, `P0-BUILD-FRONTEND`, `P0-TYPECHECK-FRONTEND`, `P0-README-NOPLACEHOLDER`, `P0-DOTNET-SDK-PIN`, `P0-FRONTEND-INSTALL-ORDER`, `P0-BUILD-NOWARN` all PASS via `bash tests/run-project-scaffold-tests.sh`.

Acceptance conditions satisfied: **AC-1, AC-2, AC-3, AC-4** (+ edge cases E-1, E-4; success criterion SC-2).

- [ ] T3.1 [US1] [P] Test task — run `bash tests/run-project-scaffold-tests.sh P0-BUILD-BACKEND P0-DOTNET-SDK-PIN` and confirm `P0-BUILD-BACKEND` currently FAILS (it expects: "`dotnet build` run at the solution root exits with code 0" — AC-1 verbatim) while `P0-DOTNET-SDK-PIN` PASSES (E-4: "`global.json` at the repo root exists, installed SDK satisfies it"). This is the failing-test gate for AC-1; do not implement before observing the failure.
- [ ] T3.2 [US1] [P] Test task — run `bash tests/run-project-scaffold-tests.sh P0-FRONTEND-INSTALL-ORDER` and confirm it FAILS (E-1 verbatim: "running the frontend build before the documented dependency-install step exits non-zero; after the install step, the same build exits 0. Both outcomes are exit-code verifiable"). The test needs `src/web` to exist (Phase 1) and `npm install --prefix src/web` to be documented in README (T3.5) — sequence: run it, observe failure, and treat it as the failing-test gate for AC-2.
- [ ] T3.3 [US1] Make the backend build green: fix any compile errors in `src/VectorJobSearch.Api/Program.cs` and the four csproj files so `dotnet build` at the repo root exits 0 (AC-1) and `dotnet build -warnaserror` exits 0 (SC-2 verbatim: "Backend build completes with 0 warnings") — any csproj/package changes go through `dotnet add reference` / `dotnet add package` (or template selection in T1.2), not hand edits. Keep the onion projects empty (C-4). Add `src/VectorJobSearch.Api/appsettings.json` (standard ASP.NET Core defaults; no secrets — N-AC-4: "the scaffold requires no API keys or credentials").
- [ ] T3.4 [US1] Make the frontend gates green: `npm install --prefix src/web` then confirm `npm --prefix src/web run build` exits 0 (AC-2: "The frontend production build command documented in the README exits with code 0") and `npm --prefix src/web run typecheck` exits 0 (AC-3: "The frontend TypeScript strict-mode type-check command documented in the README exits with code 0"), with `tsconfig.json` `"strict": true` (C-2). If the placeholder `App.tsx` has type errors, fix them without adding functionality (spec Non-Goal: "the scaffold frontend is a placeholder page only").
- [ ] T3.5 [US1] Rewrite `README.md`'s **Getting Started** section as machine-verified gates. Every gate is an HTML marker comment followed by a fenced bash block whose **first command line** is the gate command, runnable from the repo root as written (plan.md §5 README gate contract):

  | Marker | First command line |
  |---|---|
  | `<!-- gate: P0-BUILD-BACKEND -->` | `dotnet build` |
  | `<!-- gate: P0-TEST-BACKEND -->` | `dotnet test` |
  | `<!-- gate: P0-FORMAT-BACKEND -->` | `dotnet format --verify-no-changes` |
  | `<!-- gate: P0-FRONTEND-INSTALL-ORDER -->` | `npm install --prefix src/web` |
  | `<!-- gate: P0-BUILD-FRONTEND -->` | `npm --prefix src/web run build` |
  | `<!-- gate: P0-TYPECHECK-FRONTEND -->` | `npm --prefix src/web run typecheck` |
  | `<!-- gate: P0-TEST-FRONTEND -->` | `npm --prefix src/web test` |

  Also document running the backend (`dotnet run --project src/VectorJobSearch.Api`) and the health URL `http://localhost:<port>/health`. The Getting Started section must contain no placeholders — AC-4 verbatim: "`grep -rn -e "TBC" -e "coming soon" README.md` returns no matches in that section" (the shell test also greps `TBD`). No placeholders anywhere in the section, per test-plan.md `P0-README-NOPLACEHOLDER`.
- [ ] T3.6 [US1] Verify: `bash tests/run-project-scaffold-tests.sh P0-BUILD-BACKEND P0-BUILD-FRONTEND P0-TYPECHECK-FRONTEND P0-README-NOPLACEHOLDER P0-DOTNET-SDK-PIN P0-FRONTEND-INSTALL-ORDER P0-BUILD-NOWARN` — all seven PASS.

## Phase 4 — US2 (B2): Test suites exist and pass

Goal: "Both projects carry at least one meaningful automated test, proving their test harnesses are wired up and runnable from a clean clone" (spec B2, verbatim).

Test criteria: `P0-TEST-BACKEND` and `P0-TEST-FRONTEND` PASS via `bash tests/run-project-scaffold-tests.sh`; `dotnet test` reports ≥1 passed (the P0-HEALTH suite will be the backend's tests — they pass after Phase 5; if executing strictly phase-by-phase, US2's frontend half is green here and its backend half completes with Phase 5).

Acceptance conditions satisfied: **AC-5, AC-6**.

Dependencies: backend half of AC-5 is blocked by US4 (the only backend tests are the health tests, which need the health endpoint). Frontend half is independent.

- [ ] T4.1 [US2] Test task — run `bash tests/run-project-scaffold-tests.sh P0-TEST-BACKEND P0-TEST-FRONTEND` and confirm both currently FAIL. `P0-TEST-BACKEND` expects AC-5 verbatim: "`dotnet test` run at the solution root exits with code 0 and reports at least 1 passed test". `P0-TEST-FRONTEND` expects AC-6 verbatim: "The frontend test command documented in the README exits with code 0 and reports at least 1 passed test, including at least one UI component render test" (the shell test also checks ≥1 test file uses Testing Library `render(`).
- [ ] T4.2 [US2] Create the frontend render test `src/web/src/App.test.tsx` (or `__tests__` equivalent): a Vitest + React Testing Library test named with its test ID prefix (test-name contract: "frontend: Vitest test names prefixed with their P0 ID", so the test name contains `P0-TEST-FRONTEND`), asserting the placeholder `App` component renders (e.g. `render(<App />)` and expect its static text). Configure jsdom (`@testing-library/jest-dom` setup optional). Constraint C-3 verbatim: "frontend tests use Vitest with React Testing Library (verifiable by reading test project references)". Constraint C-5/N-AC-2: "no frontend test performs network I/O" (plan.md R-7) — the render test must not mock or call any network API.
- [ ] T4.3 [US2] Verify the frontend half: `npm --prefix src/web test` exits 0 with ≥1 passed test including the render test; `bash tests/run-project-scaffold-tests.sh P0-TEST-FRONTEND` PASS. (Backend half `P0-TEST-BACKEND` goes green in T6.5 once the health endpoint exists; if Phase 5 has already run, verify it here too.)

## Phase 5 — US4 (B4): Backend serves a health endpoint  *(pulled ahead of US3/US5 because it unblocks AC-5)*

Goal: "The backend skeleton starts a web host and exposes a health route that reports service status. No other HTTP surface is required" (spec B4, verbatim).

Test criteria: `dotnet test --filter "TestSuite=P0-HEALTH"` reports 4 passed (`P0-HEALTH-GET`, `P0-HEALTH-STANDALONE`, `P0-HEALTH-METHOD-405`, `P0-HEALTH-UNKNOWN-404`).

Acceptance conditions satisfied: **AC-9, AC-10** (+ edge cases E-2, E-3; completes AC-5's backend half).

Dependencies: blocked by Phase 2 (test project must compile). Blocks nothing else; US2's backend half completes here.

- [ ] T5.1 [US4] Test task — confirm the failing state: `dotnet test --filter "TestSuite=P0-HEALTH"` reports 4 failures, all `NotImplementedException` from `tests/VectorJobSearch.IntegrationTests/HealthEndpointTests.cs` `AppFactory.CreateClient()`. Re-read that file's implementation note first: it requires "a test-visible factory named `VectorJobSearch.IntegrationTests.AppFactory` (idempotent) creating a WebApplication client for the API project with all external-service environment variables removed" and the API project to expose `public partial class Program` ("referencing VectorJobSearch.Api with InternalsVisibleTo or a public partial Program class").
- [ ] T5.2 [US4] Implement `AppFactory` in `tests/VectorJobSearch.IntegrationTests/HealthEndpointTests.cs` (replace the `throw new NotImplementedException(...)` body): `WebApplicationFactory<Program>` from `Microsoft.AspNetCore.Mvc.Testing`, reusing one shared host across calls (idempotent — its own doc comment requires "repeat calls reuse the already-built host"), keeping the existing env-var-stripping loop over `ExternalServicePrefixes` (`VJS_`, `OPENAI_`, `AZURE_`, `ADZUNA_`, `REED_`, `CONNECTIONSTRING_`) intact. Use the **in-memory test client, no port binding** (plan.md R-7, N-AC-2: "the complete backend and frontend test suites exit 0 with outbound network access blocked").
- [ ] T5.3 [US4] Implement the health route in `src/VectorJobSearch.Api/Program.cs`: `app.MapGet("/health", () => Results.Json(new { status = "healthy" }))` (or TypedResults equivalent). The response body is exactly `{"status":"healthy"}` and validates `specs/001-project-scaffold/contracts/health-response.schema.json`, which requires verbatim: `status` — "type": "string", **required**, `enum ["healthy", "degraded", "unhealthy"]`, "Service status reported by the health route. The standalone skeleton reports \"healthy\" (AC-10)", and the object has `additionalProperties: false` — "the response body is exactly `{"status":"healthy"}`" (plan.md §4). AC-10 verbatim: "The health route returns 200 with no external services configured or reachable (proving the skeleton starts standalone)" — read no configuration, no env vars, no services. E-2 verbatim: "a POST to the health route does not return the health payload; it is rejected with an HTTP 405 status" (default ASP.NET Core method filtering gives this for a GET-only route — verify, don't add a workaround). E-3 verbatim: "a request to any non-existent route returns HTTP 404, not a 500". Ensure `public partial class Program;` exists. **No other HTTP surface** (B4) and no persistence code (C-4).
- [ ] T5.4 [US4] Verify: `dotnet test --filter "TestSuite=P0-HEALTH"` → 4 passed; `dotnet test` → exits 0 with ≥1 passed (AC-5 verbatim); `bash tests/run-project-scaffold-tests.sh P0-TEST-BACKEND` → PASS. Also live-check per plan.md quickstart: `dotnet run --project src/VectorJobSearch.Api` then `curl -i http://localhost:<port>/health` (expect 200 `{"status":"healthy"}`), `curl -i -X POST .../health` (expect 405), `curl -i .../no-such-route` (expect 404).

## Phase 6 — US3 (B3): Format gate passes on the backend

Goal: "The repository's C# code conforms to the repository style configuration, enforced mechanically" (spec B3, verbatim).

Test criteria: `P0-FORMAT-BACKEND` and `P0-EDITORCONFIG` PASS via `bash tests/run-project-scaffold-tests.sh`.

Acceptance conditions satisfied: **AC-7, AC-8** (+ edge case E-5: "the gate itself is the mechanical check").

Dependencies: none beyond Phase 3 (`.editorconfig` from T1.3; code exists to format). Runs in parallel with US4 if desired, but is sequenced after it here so formatting is applied once, to final code.

- [ ] T6.1 [US3] Test task — run `bash tests/run-project-scaffold-tests.sh P0-FORMAT-BACKEND P0-EDITORCONFIG` and record current state. `P0-EDITORCONFIG` (AC-8 verbatim: "A `.editorconfig` file exists at the repository root") should already PASS from T1.3; `P0-FORMAT-BACKEND` (AC-7 verbatim: "`dotnet format --verify-no-changes` run at the solution root exits with code 0") is expected to FAIL until T6.2 — observe the failure, and note E-5 verbatim: "`dotnet format --verify-no-changes` exits non-zero when any file needs formatting changes".
- [ ] T6.2 [US3] Run `dotnet format` at the repo root (not `--verify-no-changes`) so it rewrites all C# files to the `.editorconfig` style — this includes `src/VectorJobSearch.Api/Program.cs`, all csproj-adjacent files, and `tests/VectorJobSearch.IntegrationTests/HealthEndpointTests.cs`. Review the diff: it must be whitespace/style only, no behavior change (plan.md R-4: "no other content change"). If `dotnet format` reports analyzer warnings that are real defects, fix them; suppress nothing.
- [ ] T6.3 [US3] Verify: `dotnet format --verify-no-changes` exits 0; `dotnet build -warnaserror` still exits 0 (formatting must not break SC-2); `dotnet test` still 4 passed; `bash tests/run-project-scaffold-tests.sh P0-FORMAT-BACKEND P0-EDITORCONFIG` → both PASS.

## Phase 7 — US5 (B5): CI enforces the quality gates on every push

Goal: "A GitHub Actions workflow builds, tests, and format-checks both projects, and frontend type-checks, on every push and pull request" (spec B5, verbatim).

Test criteria: `P0-CI-TRIGGERS` PASS locally (file inspection); `P0-CI-GREEN`, `P0-CI-DURATION` PASS after push (AC-13 verbatim: "The workflow run for the scaffold commit completes with conclusion `success` (verifiable via `gh run list`)"; SC-4: green run ≤ 600 s).

Acceptance conditions satisfied: **AC-11, AC-12, AC-13, AC-14** (+ N-AC-1 red probe in Phase 8; constraint C-1: "the workflow uses only GitHub-hosted free runners (see AC-14) and provisions no cloud resources").

Dependencies: blocked by US1–US4 (CI must run the real commands and they must pass). AC-13/SC-4 verification requires a GitHub remote and push — stays red until the scaffold commit is pushed (test-plan.md "Open items").

- [ ] T7.1 [US5] Test task — confirm `P0-CI-TRIGGERS` currently FAILS: `bash tests/run-project-scaffold-tests.sh P0-CI-TRIGGERS` (it inspects the workflow file for `on:` triggers, gate step literals, and `runs-on:` values). Then write `.github/workflows/ci.yml` to satisfy, verbatim: AC-11 — "A workflow file exists under `.github/workflows/` that triggers on both `push` and `pull_request` events (verifiable by reading the workflow file's `on:` triggers)"; AC-12 — "The workflow contains steps that run the backend build + tests, the backend format check, the frontend production build, the frontend type-check, and the frontend tests (verifiable by the presence of these steps in the workflow file)"; AC-14 — "The workflow uses GitHub-hosted runners only — no self-hosted or paid runner configuration (verifiable by reading the `runs-on:` values in the workflow file)".
- [ ] T7.2 [US5] Author `.github/workflows/ci.yml` with the plan.md §5 CI workflow contract, exactly: all `runs-on: ubuntu-latest`; `actions/setup-dotnet` reading the version from `global.json` (R-6: single source of truth); `actions/setup-node` with Node 24; frontend steps preceded by `npm install --prefix src/web` (E-1 ordering); step names containing the literal gate IDs the shell test greps for: `P0-BUILD-BACKEND`, `P0-TEST-BACKEND`, `P0-FORMAT-BACKEND`, `P0-BUILD-FRONTEND`, `P0-TYPECHECK-FRONTEND`, `P0-TEST-FRONTEND`; "No deployment steps, no secrets, no self-hosted runners" (N-AC-4, C-1).
- [ ] T7.3 [US5] Verify locally: `bash tests/run-project-scaffold-tests.sh P0-CI-TRIGGERS` → PASS. Then commit the full scaffold (Phase 8's negative checks should pass first if executing in order) and push; verify `gh run list` shows conclusion `success` for the scaffold commit (AC-13) and `gh run view <run-id> --json startedAt,updatedAt` shows duration ≤ 600 s (SC-4 verbatim: "A green CI run for the scaffold commit completes within 10 minutes"). `P0-CI-GREEN` and `P0-CI-DURATION` → PASS.

## Phase 8 — Polish: negative acceptance conditions, hermeticity, cross-cutting gates

Goal: every negative acceptance condition and remaining success criterion is mechanically green, and the spec's test-ID registry obligation is met.

Test criteria: the full suite `bash tests/run-project-scaffold-tests.sh` reports PASS for everything except (if not yet pushed) `P0-CI-GREEN`, `P0-CI-RED`, `P0-CI-DURATION`; after Phase 7's push and the T8.2 probe, those PASS too. Zero FAIL, zero SKIP.

Acceptance conditions satisfied: **N-AC-1, N-AC-2, N-AC-3, N-AC-4** (+ SC-1, SC-3, SC-5 aggregate; E-5 via the format gate already green).

Dependencies: blocked by all of US1–US5 (it verifies the finished scaffold and requires the pushed commit for the CI checks).

- [ ] T8.1 [P] Test task — run `bash tests/run-project-scaffold-tests.sh P0-NO-TERRAFORM P0-NO-SECRETS P0-HERMETIC` and confirm all three PASS. They verify, verbatim: N-AC-3 — "`git ls-files '*.tf'` returns an empty list; the scaffold provisions no Terraform/Azure resources"; N-AC-4 — "a grep of tracked files for key-shaped strings returns nothing (the skeleton runs standalone per AC-10)"; N-AC-2 — "the complete backend and frontend test suites exit 0 with outbound network access blocked (loopback allowed), proving no test calls an external API. (The health-endpoint test targets loopback and is unaffected.)" If `P0-HERMETIC` fails, fix the offending test to use loopback/in-memory transport only (plan.md R-7: "health test uses `WebApplicationFactory`'s in-memory client (no port binding); no frontend test performs network I/O") — do not weaken the check.
- [ ] T8.2 Execute the `P0-CI-RED` probe procedure once (test-plan.md §"P0-CI-RED probe procedure", verbatim): "1. Create branch `probe/p0-ci-red` with a deliberately failing test; push. 2. Confirm `gh run list` reports `failure` for that branch (the `P0-CI-RED` test checks for this record). 3. Delete the branch and revert." This proves N-AC-1 verbatim: "a workflow run whose commit contains an intentionally failing test reports conclusion `failure` (verifiable via `gh run list`; test ID `P0-CI-RED`)". Then run `bash tests/run-project-scaffold-tests.sh P0-CI-RED` → PASS.
- [ ] T8.3 [P] Verify the test-ID registry obligation (spec: "Every Test ID must appear in the future source tree (test names or comments) so `grep -rn "P0-"` in the implemented repository finds them"): run `grep -rn "P0-"` at the repo root and confirm every ID from the registry is found — backend via `[Trait("TestSuite", "P0-HEALTH")]` and per-test comments in `HealthEndpointTests.cs`, frontend via the Vitest test name, workflow via step names, shell suite via its function names/comments. Add missing ID comments only where the grep comes up empty; do not remove any.
- [ ] T8.4 Final clean-clone verification (SC-1 verbatim: "From a fresh `git clone`, the complete documented verification sequence (backend build, backend tests, backend format check, frontend install, frontend build, frontend type-check, frontend tests) runs with every command exiting 0 — 0 manual repair steps"): clone the repo to `/tmp/opencode/vjs-clone-check`, run every README Getting Started gate command in the documented order, then `bash tests/run-project-scaffold-tests.sh` — expect every test ID PASS (SC-3: "≥1 passing backend test, ≥1 passing frontend test including ≥1 component render test; 0 skipped/ignored tests"). Report any failure before declaring the spec complete.

---

## Dependencies between story phases

- **Phase 1 → everything**: the layout must exist before any test or implementation lands.
- **Phase 2 → US1–US5**: the test harnesses must compile/run before their owning stories go green.
- **US1 (B1)** blocks **US2 (B2)** (tests can't pass if builds fail), **US3 (B3)** (nothing to format), and **US5 (B5)** (CI runs the build).
- **US4 (B4)** is the only source of backend tests, so **US4 blocks the backend half of AC-5 (US2)** and is executed between US2 and US3 above; US2's frontend half is independent of US4.
- **US3 (B3)** is sequenced after US4 so `dotnet format` runs once against final code (no hard dependency, only ordering).
- **US5 (B5)** is blocked by US1–US4 (its steps run all prior gates). AC-13/SC-4 additionally require a push to GitHub.
- **Phase 8** is blocked by all stories; `P0-CI-RED` additionally requires the Phase 7 push.

## Suggested MVP scope

**US1 only** (B1: both projects build from a clean checkout) is the minimal deliverable — it proves the repo compiles and its instructions are real. However, since the RED tests already exist and US4 is only ~2 files, the pragmatic MVP is **US1 + US2 + US4** (builds, tests, health endpoint), which satisfies AC-1 through AC-10 except AC-7/8; defer US3, US5, and Phase 8 if time-boxed.

## Traceability appendix — task → spec condition

| Task | Spec condition(s) | Test ID(s) |
|---|---|---|
| T1.1 | E-4, C-2 | P0-DOTNET-SDK-PIN |
| T1.2 | C-2, C-4 (empty onion) | — (enables P0-BUILD-BACKEND, P0-TEST-BACKEND, P0-FORMAT-BACKEND) |
| T1.3 | AC-8 | P0-EDITORCONFIG |
| T1.4 | AC-2, AC-3, C-2, C-3 | P0-BUILD-FRONTEND, P0-TYPECHECK-FRONTEND, P0-TEST-FRONTEND |
| T1.5 | SC-1 (documented sequence) | all shell IDs |
| T1.6 | AC-9 (schema correctness) | P0-HEALTH-GET (via `RepoPaths.HealthResponseSchema`) |
| T2.1 | AC-5, C-3 | P0-TEST-BACKEND |
| T2.2 | AC-7, E-5 (format-clean test file), R-4 | P0-FORMAT-BACKEND |
| T2.3 | SC-5 (all ACs command-verifiable) | all shell IDs |
| T3.1 | AC-1, E-4 | P0-BUILD-BACKEND, P0-DOTNET-SDK-PIN |
| T3.2 | E-1 | P0-FRONTEND-INSTALL-ORDER |
| T3.3 | AC-1, SC-2, N-AC-4 | P0-BUILD-BACKEND, P0-BUILD-NOWARN |
| T3.4 | AC-2, AC-3, C-2 | P0-BUILD-FRONTEND, P0-TYPECHECK-FRONTEND |
| T3.5 | AC-4, AC-2, AC-3, AC-6, E-1 | P0-README-NOPLACEHOLDER (+ gate extraction for all README-gated IDs) |
| T3.6 | AC-1, AC-2, AC-3, AC-4, E-1, E-4, SC-2 | P0-BUILD-BACKEND, P0-BUILD-FRONTEND, P0-TYPECHECK-FRONTEND, P0-README-NOPLACEHOLDER, P0-FRONTEND-INSTALL-ORDER, P0-DOTNET-SDK-PIN, P0-BUILD-NOWARN |
| T4.1 | AC-5, AC-6 | P0-TEST-BACKEND, P0-TEST-FRONTEND |
| T4.2 | AC-6, C-3, N-AC-2 | P0-TEST-FRONTEND, P0-HERMETIC |
| T4.3 | AC-6, (AC-5) | P0-TEST-FRONTEND, (P0-TEST-BACKEND) |
| T5.1 | AC-9, AC-10, E-2, E-3 | P0-HEALTH-GET, P0-HEALTH-STANDALONE, P0-HEALTH-METHOD-405, P0-HEALTH-UNKNOWN-404 |
| T5.2 | AC-10, N-AC-2, R-7 | P0-HEALTH-STANDALONE, P0-HERMETIC |
| T5.3 | AC-9, AC-10, E-2, E-3, C-4 | P0-HEALTH-GET, P0-HEALTH-STANDALONE, P0-HEALTH-METHOD-405, P0-HEALTH-UNKNOWN-404 |
| T5.4 | AC-5, AC-9, AC-10, E-2, E-3 | P0-TEST-BACKEND + all P0-HEALTH-* |
| T6.1 | AC-7, AC-8, E-5 | P0-FORMAT-BACKEND, P0-EDITORCONFIG |
| T6.2 | AC-7, E-5 | P0-FORMAT-BACKEND |
| T6.3 | AC-7, AC-8, SC-2 | P0-FORMAT-BACKEND, P0-EDITORCONFIG, P0-BUILD-NOWARN |
| T7.1 | AC-11, AC-12, AC-14, C-1 | P0-CI-TRIGGERS |
| T7.2 | AC-11, AC-12, AC-14, C-1, N-AC-4, E-1 | P0-CI-TRIGGERS |
| T7.3 | AC-13, SC-4 | P0-CI-GREEN, P0-CI-DURATION, P0-CI-TRIGGERS |
| T8.1 | N-AC-2, N-AC-3, N-AC-4 | P0-HERMETIC, P0-NO-TERRAFORM, P0-NO-SECRETS |
| T8.2 | N-AC-1 | P0-CI-RED |
| T8.3 | Test ID Registry ("grep -rn 'P0-'") | all P0 IDs |
| T8.4 | SC-1, SC-3, SC-5 | all test IDs |

**Gap check:** every spec condition maps to at least one task — AC-1→T3.1/3.3, AC-2→T3.2/3.4/3.5, AC-3→T3.4/3.5, AC-4→T3.5, AC-5→T4.1/T5.4, AC-6→T4.1/4.2/4.3, AC-7→T6.1/6.2/6.3, AC-8→T1.3/T6.1, AC-9→T5.1/5.3, AC-10→T5.1/5.2/5.3, AC-11→T7.1/7.2, AC-12→T7.1/7.2, AC-13→T7.3, AC-14→T7.1/7.2, N-AC-1→T8.2, N-AC-2→T8.1, N-AC-3→T8.1, N-AC-4→T8.1 (and T3.3, T7.2), E-1→T3.2, E-2→T5.1/5.3, E-3→T5.1/5.3, E-4→T1.1/T3.1, E-5→T6.1/6.2, C-1→T7.1/7.2, C-2→T1.1/1.2/1.4, C-3→T2.1/T4.2, C-4→T1.2/T5.3, SC-1→T1.5/T8.4, SC-2→T3.3/T6.3, SC-3→T8.4, SC-4→T7.3, SC-5→T2.3/T8.4. **No gaps.**
