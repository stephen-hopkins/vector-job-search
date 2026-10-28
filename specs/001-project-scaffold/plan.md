# Design Plan — Spec 001: Initial Project Scaffold

**Status:** Amended per user direction — tests under `/tests`, .NET solution file and `global.json` at the repo root, .NET projects under `/src`, frontend project named `web`, backend structured with Clean Architecture (onion).

---

## 1. Technical context

**Languages/runtimes** (confirmed installed locally):

- **Backend:** C# 14 on .NET 10 (LTS) — `dotnet` CLI; installed SDK `10.0.113` (C-2). ASP.NET Core **Minimal APIs** host.
- **Frontend:** React 19 + TypeScript 5.9 in **strict mode**, built with **Vite** (C-2); Node v24.22.1, npm 11.19.1 installed.
- **Backend tests:** xUnit + `Microsoft.AspNetCore.Mvc.Testing` (WebApplicationFactory) + `JsonSchema.Net` (schema validation, C-3) — the existing RED-phase test at `tests/VectorJobSearch.Api.Tests/HealthEndpointTests.cs` already references these.
- **Frontend tests:** Vitest + React Testing Library (C-3), with jsdom environment for component render tests.
- **CI:** GitHub Actions, GitHub-hosted runners only (C-1, AC-14). `gh` CLI 2.103.0 available for AC-13 / N-AC-1 / SC-4 verification.

**Project/file structure this feature touches:**

```
/                                          ← repo root = .NET solution root (dotnet CLI runs here)
  VectorJobSearch.sln                      ← solution file at the repo root
  global.json                              ← E-4 SDK pin (10.0.x), at the repo root
  src/
    VectorJobSearch.Domain/                ← onion core: no dependencies (empty in Phase 0)
      VectorJobSearch.Domain.csproj
    VectorJobSearch.Application/           ← application layer; references Domain (empty in Phase 0)
      VectorJobSearch.Application.csproj
    VectorJobSearch.Infrastructure/        ← outer layer; references Application (empty in Phase 0)
      VectorJobSearch.Infrastructure.csproj
    VectorJobSearch.Api/                   ← entry point; references Infrastructure (+ transitively Application, Domain)
      VectorJobSearch.Api.csproj
      Program.cs                           ← GET /health; public partial Program for test visibility
    web/                                   ← Vite React app, project name "web"
      package.json                         ← react 19, typescript 5.9, vitest, @testing-library/react, jsdom
      tsconfig.json                        ← "strict": true (C-2)
      src/                                 ← placeholder page + one component render test
  tests/                                   ← ALL test code (location unchanged from current repo)
    run-project-scaffold-tests.sh          ← amend layout constants only
    VectorJobSearch.Api.Tests/
      VectorJobSearch.Api.Tests.csproj    ← new; added to VectorJobSearch.sln (file stays in place)
      HealthEndpointTests.cs               ← unchanged (reformatted for dotnet format)
  .github/workflows/ci.yml                 ← create
  .editorconfig                            ← create at repo root
  README.md                                ← Getting Started rewritten with machine-verified gate commands
  specs/001-project-scaffold/
    spec.md                                ← no change required (layout-agnostic; see §3 R-2)
    test-plan.md                           ← amend "Repo layout" convention + paths (see §3 R-2)
    contracts/health-response.schema.json  ← keep (proposal: fix stale $id)
```

**Reference direction (onion):** `Api → Infrastructure → Application → Domain`. No layer points inward past its neighbor; `Domain` references nothing.

**Key existing artifacts constraining the design:**

- `tests/run-project-scaffold-tests.sh` expects: `backend`/`frontend` directory constants (to be amended — see R-2), README gate markers `<!-- gate: P0-TEST-ID -->` followed by a bash fence whose **first command line** is the gate command, and workflow steps containing the literal gate IDs `P0-BUILD-BACKEND`, `P0-TEST-BACKEND`, `P0-FORMAT-BACKEND`, `P0-BUILD-FRONTEND`, `P0-TYPECHECK-FRONTEND`, `P0-TEST-FRONTEND`.
- `HealthEndpointTests.cs` requires: test-visible `AppFactory` (idempotent `CreateClient()`), `public partial class Program` in the API project (per its own implementation note), health route `GET /health`, trait `TestSuite=P0-HEALTH`. Its namespace (`VectorJobSearch.Api.Tests`) is already correct.
- Contract schema `specs/001-project-scaffold/contracts/health-response.schema.json`: body is exactly `{ "status": "healthy" | "degraded" | "unhealthy" }` with `additionalProperties: false`.

**NEEDS CLARIFICATION items:** resolved in §3.

## 2. Constitution check

| Principle (quoted) | Verdict |
|---|---|
| Mission — "Running costs must be kept as low as possible… free/low tiers are the default target." (C-1) | **Complies.** CI uses only GitHub-hosted free runners (AC-14); scaffold provisions nothing (N-AC-3). |
| Tech stack — "Language: C# 14 / Runtime: .NET 10 (LTS); Frontend: React 19 + TypeScript 5.9" (C-2) | **Complies.** Project files pin these versions; `global.json` pins the .NET SDK (E-4). |
| Tech stack testing — "xUnit — backend…; Vitest…; React Testing Library" (C-3) | **Complies.** Test projects reference exactly these. |
| Tech stack — "Avoid design dead ends — no in-memory-only storage, no single-user assumptions baked into the data model" (C-4) | **Complies.** Skeleton contains no persistence code at all — the empty Domain/Application/Infrastructure projects establish *structure*, not data-model decisions. The onion layout is growth headroom that costs nothing up front. |
| Tech stack — "Keep the provider behind the abstraction — never call an embedding API directly" | **Complies (vacuously).** No embedding code exists in Phase 0; the scaffold must not add any. The onion layering gives Phase 1 a natural home for the provider abstraction (Application interface, Infrastructure implementation). |
| Quality gates — "All PRs must pass build and tests before merge (GitHub Actions CI); Lint/format check… on every push; TypeScript strict-mode compile… on every push" | **Complies.** Workflow runs all gates on push + pull_request (AC-11, AC-12). |
| Roadmap Phase 0 exit criterion — "A skeleton project builds and tests pass." | **Complies.** This is the whole feature (B1–B5). |
| Mission — "growth headroom comes free where it costs nothing up front" | **Complies.** The onion gives Phase 1–3 features (embedding provider, listing sources, EF Core vector store) pre-destined homes without restructuring. |
| Spec Non-Goal — "Pre-commit hooks or other client-side enforcement… CI is the only quality gate." | **Complies.** Design adds none. |

No exceptions required.

## 3. Research — resolutions

**R-1: Schema `$id` points to `tests/schemas/…` but the schema lives in `specs/001-project-scaffold/contracts/` (which is what the test's `RepoPaths` reads).**
- **Decision:** keep the file in `specs/001-project-scaffold/contracts/`; fix only the informational `$id`/description so it doesn't mislead.
- **Rationale:** the test is the source of truth for the path.
- **Alternatives:** duplicating into `tests/schemas/` — rejected; two copies of a contract can drift and no test requires it. *(Explicit proposal — cosmetic docs fix.)*

**R-2: Layout conventions vs. spec and test-plan.**
- **Decision:** the **spec needs no change** — it never names concrete directories (only "the solution root", "the frontend"), and every AC is a command exit code that is layout-independent once the README documents the commands. With `VectorJobSearch.sln` at the repo root, "the solution root" in the spec's ACs literally means the repo root. The **test-plan is amended**: the "Conventions the implementation must follow" section is rewritten to the final layout (sln + `global.json` at repo root; .NET projects and `web` under `src/`; tests under `tests/`), and the mapping-table paths drop the `backend/` prefix. The **verification script's constants become**: `BACKEND_DIR="$REPO_ROOT"` (solution root; `P0-DOTNET-SDK-PIN` then finds `global.json` at the repo root) and `FRONTEND_DIR="$REPO_ROOT/src/web"`.
- **Rationale:** the test-plan's conventions section is what the verification script and README gates encode; the spec is deliberately abstract.
- **Alternatives:** keeping a `backend/` solution folder — rejected, contradicts user instruction; amending the spec's Clarifications — rejected; nothing in the spec contradicts the new layout.

**R-3: Clean Architecture at Phase 0 — how much?**
- **Decision:** four projects (`Domain`, `Application`, `Infrastructure`, `Api`) under `src/` with correct reference direction, all empty except `Api` (health endpoint + host wiring). No abstractions, interfaces, or placeholder files invented "for later" — empty projects only establish the dependency direction.
- **Rationale:** C-4 forbids data-model/persistence decisions; empty layer projects make none. The onion is user-directed and gives Phase 1's embedding-provider abstraction a natural home (`IEmbeddingGenerator`-style interface in Application, implementation in Infrastructure), avoiding a Phase-1 restructuring.
- **Alternatives:** (a) full onion with invented sample abstractions — rejected, violates "no design dead ends" spirit by guessing future shapes; (b) single Api project, defer onion — rejected, contradicts user instruction.

**R-4: `dotnet format` and the existing hand-written test file.**
- **Decision:** all C# files pass `dotnet format --verify-no-changes` (AC-7). The existing `HealthEndpointTests.cs` has one mis-indented line (line 41) that is reformatted; no other content change.
- **Rationale:** format gate is hard (AC-7, E-5).
- **Alternatives:** excluding the file — rejected; no exclusion is spec'd.

**R-5: Frontend dependency versions / package manager.**
- **Decision:** npm; `package.json` (name: `web`) pins `react`/`react-dom` ^19, `typescript` ~5.9, `vite`, `vitest`, `@testing-library/react`, `jsdom`; devDependency minors resolved at implementation time.
- **Rationale:** tech-stack.md names npm + vite; C-2/AC-3 require TS 5.9 strict.
- **Alternatives:** pinning exact versions in the plan — rejected; goes stale, and no AC checks specific minors.

**R-6: CI Node/.NET versions.**
- **Decision:** `actions/setup-dotnet` with `global.json` as version source (E-4); `actions/setup-node` with Node 24. *(Explicit proposal: add `engines.node` to `src/web/package.json` so setup-node reads it; no AC constrains this.)*
- **Rationale:** single source of truth; local/CI identical.
- **Alternatives:** hardcoding in the workflow — works, but duplicates the pin.

**R-7: `P0-HERMETIC` (N-AC-2) mechanics.**
- **Decision:** health test uses `WebApplicationFactory`'s in-memory client (no port binding); no frontend test performs network I/O. Both suites then pass under `unshare -rn` where available.
- **Rationale:** test-plan.md, N-AC-2.
- **Alternatives:** none needed; existing shell test already drives this.

**No blocking questions.**

## 4. Data model

Per C-4, the scaffold defines **no entities, no persistence, and no state** beyond the health response contract:

**Health response** (the only structured artifact; spec B4/AC-9, quoted verbatim from the contract):

- `status`: string, **required**, enum `["healthy", "degraded", "unhealthy"]` — "Service status reported by the health route. The standalone skeleton reports 'healthy' (AC-10)."
- `additionalProperties: false` — the response body is exactly `{"status":"healthy"}`.

**State transitions:** none. The skeleton is stateless ("the scaffold makes no data-model or persistence decisions" — C-4).

## 5. Contracts

### HTTP (backend, the only exposed surface)

`GET /health` — returns `200` with `Content-Type: application/json` and body `{"status":"healthy"}` (validating `health-response.schema.json`). Returns 200 with **no external services configured or reachable** (AC-10). Non-GET methods → 405 without the health payload (E-2). Any unknown route → 404, not 500 (E-3).

### README gate contract (machine-verified documentation)

Each Getting Started gate is documented as `<!-- gate: <P0-TEST-ID> -->` followed by a fenced bash block; the first command line is the gate command, runnable **from the repo root** as written (the solution root *is* the repo root):

| Gate marker | First command line | AC |
|---|---|---|
| `P0-BUILD-BACKEND` | `dotnet build` | AC-1 |
| `P0-BUILD-FRONTEND` | `npm --prefix src/web run build` | AC-2 |
| `P0-TYPECHECK-FRONTEND` | `npm --prefix src/web run typecheck` (script → `tsc --noEmit`, strict) | AC-3 |
| `P0-TEST-BACKEND` | `dotnet test` | AC-5 |
| `P0-TEST-FRONTEND` | `npm --prefix src/web test` (script → `vitest run`) | AC-6 |
| `P0-FORMAT-BACKEND` | `dotnet format --verify-no-changes` | AC-7 |
| `P0-FRONTEND-INSTALL-ORDER` | `npm install --prefix src/web` | E-1 |

### CI workflow contract (`.github/workflows/ci.yml`)

- Triggers: `push` and `pull_request` (AC-11).
- Steps labeled with each gate ID: `P0-BUILD-BACKEND`, `P0-TEST-BACKEND`, `P0-FORMAT-BACKEND`, `P0-BUILD-FRONTEND`, `P0-TYPECHECK-FRONTEND`, `P0-TEST-FRONTEND` (AC-12 — the shell test greps for these literals).
- All `runs-on:` values GitHub-hosted (`ubuntu-latest`) (AC-14).
- Frontend steps preceded by `npm install` (E-1 ordering).
- No deployment steps, no secrets, no self-hosted runners (N-AC-4, C-1).

### Test-name contract

Every test ID must appear in the source tree (`grep -rn "P0-"` — spec Test ID Registry). Backend: xUnit `[Trait("TestSuite", "P0-HEALTH")]` + per-test comments; frontend: Vitest test names prefixed with their P0 ID; workflow: step names.

## 6. Quickstart

A reviewer validates end-to-end from a fresh clone:

```bash
git clone https://github.com/stephen-hopkins/vector-job-search.git
cd vector-job-search

# 1. Backend: build, test, format  (expect: exit 0 each; dotnet test reports >=1 passed)
dotnet build
dotnet test
dotnet format --verify-no-changes
dotnet build -warnaserror                  # SC-2: zero warnings

# 2. Frontend: install, build, type-check, test  (expect: exit 0 each; vitest >=1 passed incl. render test)
npm install --prefix src/web
npm --prefix src/web run build
npm --prefix src/web run typecheck
npm --prefix src/web test

# 3. Full machine-verified suite  (expect: all PASS except P0-CI-GREEN/RED/DURATION until pushed)
bash tests/run-project-scaffold-tests.sh

# 4. Health endpoint live check
dotnet run --project src/VectorJobSearch.Api &
curl -i http://localhost:5000/health                # expect 200, {"status":"healthy"}
curl -i -X POST http://localhost:5000/health        # expect 405
curl -i http://localhost:5000/no-such-route        # expect 404

# 5. Health xUnit suite specifically
dotnet test --filter "TestSuite=P0-HEALTH"

# 6. CI gates (after the scaffold commit is pushed)
gh run list                                 # expect conclusion 'success' for HEAD (AC-13)
# then the one-time probe from test-plan.md (branch probe/p0-ci-red with a failing test -> 'failure', delete branch)
```

**Expected observable outcomes:** all commands exit 0; shell suite reports all `PASS`; `curl` shows the exact JSON `{"status":"healthy"}`; CI run green in ≤10 min (SC-4).

## Artifact list

| Artifact | Action |
|---|---|
| `VectorJobSearch.sln` | create at repo root |
| `global.json` | create at repo root (pin 10.0.x SDK) |
| `src/VectorJobSearch.Domain/` | create (empty project, no references) |
| `src/VectorJobSearch.Application/` | create (empty project, refs Domain) |
| `src/VectorJobSearch.Infrastructure/` | create (empty project, refs Application) |
| `src/VectorJobSearch.Api/` (csproj refs Infrastructure; `Program.cs` with `GET /health`, `public partial Program`; `appsettings.json`) | create |
| `src/web/` (Vite React 19 + TS 5.9 app, name "web"; scripts: `build`, `typecheck`, `test`; placeholder page; one RTL render test) | create |
| `tests/VectorJobSearch.Api.Tests/VectorJobSearch.Api.Tests.csproj` | create in place; added to solution; `HealthEndpointTests.cs` reformatted only |
| `tests/run-project-scaffold-tests.sh` | amend layout constants: `BACKEND_DIR=$REPO_ROOT`, `FRONTEND_DIR=$REPO_ROOT/src/web` |
| `.github/workflows/ci.yml` | create |
| `.editorconfig` | create at repo root |
| `README.md` Getting Started | rewrite with gate markers per §5; remove `TBC`/`coming soon`/`TBD` from that section |
| `specs/001-project-scaffold/spec.md` | no change required (layout-agnostic) |
| `specs/001-project-scaffold/test-plan.md` | amend "Repo layout" convention + suite-table/mapping paths (R-2) |
| `specs/001-project-scaffold/contracts/health-response.schema.json` | keep; *(proposal: fix stale `$id` path)* |

## Traceability table

| Design element | Spec condition |
|---|---|
| `VectorJobSearch.sln` + `global.json` at repo root; `src/` .NET projects | AC-1, AC-5, AC-7, C-2, E-4 |
| Empty Domain/Application/Infrastructure projects (reference direction only) | User-directed layout; complies with C-4 ("the skeleton contains no persistence code at all"); Mission growth-headroom |
| `dotnet build -warnaserror` clean codebase | SC-2 |
| `src/web` Vite React 19 + TS 5.9 strict | AC-2, AC-3, C-2 |
| `npm install` → build ordering documented in README | E-1 |
| ≥1 xUnit backend test (health suite, in `tests/`) | AC-5, C-3 |
| ≥1 Vitest + RTL render test (in `src/web`) | AC-6, C-3 |
| `dotnet format`-clean C# + `.editorconfig` | AC-7, AC-8, E-5 |
| `GET /health` returning `{"status":"healthy"}`, standalone startup | AC-9, AC-10, B4 |
| 405 on non-GET /health; 404 on unknown route | E-2, E-3 |
| In-memory test client (WebApplicationFactory) | N-AC-2 |
| Workflow: push+PR triggers, 6 labeled gate steps, `ubuntu-latest` | AC-11, AC-12, AC-14, C-1 |
| CI green + probe procedure | AC-13, N-AC-1, SC-4 |
| No `.tf` files, no secrets, no persistence code | N-AC-3, N-AC-4, C-4 |
| README gate markers, machine-verified Getting Started, no placeholders | AC-2, AC-3, AC-4, AC-6, SC-1 |
| Test IDs embedded in source (traits, test names, workflow steps) | Test ID Registry ("grep -rn 'P0-'") |
| Schema kept at `specs/.../contracts/` (per `RepoPaths`) | AC-9 (via P0-HEALTH-GET) |
| Tests under `/tests`, projects under `/src`, sln at root, frontend named `web`; test-plan + script constant amendments | User-directed; SC-1/SC-5 preserved (documented sequence still runs clean; every AC still command-verifiable) |
| **Proposal only (not spec-traced):** `engines.node` pin, schema `$id` fix | — |
