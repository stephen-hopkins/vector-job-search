# Spec 001 — Phase 0: Repository Scaffold

**Status:** Draft
**Roadmap phase:** Phase 0 — Foundation (exit criteria: "A skeleton project builds and tests pass")
**Constitution references:** [`mission.md`](mission.md) (cost constraints, scale headroom), [`tech-stack.md`](tech-stack.md) (tooling, testing, quality gates), [`roadmap.md`](roadmap.md)

---

## Purpose

Establish a skeleton repository that builds, lints, and tests cleanly on CI for both the backend (.NET 10 / ASP.NET Core) and frontend (React 19 + TypeScript / Vite), structured so later phases add features without structural rework.

## Behavior (WHAT)

### B1 — Solution structure

The repository contains a multi-project .NET solution organized by clean architecture (onion), plus a React SPA, laid out as:

```
src/Api/               — ASP.NET Core host (Minimal APIs), composition root
src/Core/              — innermost layer; no framework or external dependencies
src/Application/       — use cases; references Core only
src/Infrastructure/    — EF Core, HTTP clients, embedding providers; references Application
src/web/               — React 19 + TypeScript 5.9 SPA (Vite)
tests/Backend.UnitTests/          — xUnit
tests/Backend.IntegrationTests/   — xUnit (scaffolded; SQL-backed tests deferred to Phase 2)
tests/Architecture.Tests/         — dependency-direction enforcement
tests/ (frontend tests live within src/web per Vite convention — Vitest)
```

Dependency arrows point inward only: `Api → Infrastructure → Application → Core`. `Core` references nothing else in the solution. No project references `Api`.

**Acceptance conditions:**

- **AC-1** `dotnet build` succeeds at the solution root with exit code 0.
- **AC-2 (Test ID: ARCH-001)** `tests/Architecture.Tests/DependencyDirectionTests` contains a test asserting `Core` has no project or package references; it passes.
- **AC-3 (Test ID: ARCH-002)** An architecture test asserts `Infrastructure` is not referenced by `Application` or `Core`; it passes.
- **AC-4** `ls src/Api src/Core src/Application src/Infrastructure src/web tests/Backend.UnitTests tests/Backend.IntegrationTests tests/Architecture.Tests` succeeds (directory layout matches).

### B2 — Backend skeleton is testable and green

The backend projects compile, contain at least one meaningful unit test, and a minimal API endpoint exists as a placeholder for later phases.

**Acceptance conditions:**

- **AC-5 (Test ID: BT-001)** `Backend.UnitTests` contains a passing test that exercises a type in `src/Core` (e.g., a placeholder domain value object), proving the test project references and runs.
- **AC-6** `dotnet test` at the solution root runs all backend tests, exit code 0.
- **AC-7 (Test ID: BT-002)** A unit test asserts the placeholder API endpoint (e.g., `GET /health`) returns HTTP 200 using `WebApplicationFactory` — proving the Api host starts in the test harness.

### B2b — Integration test project is scaffolded and discovered

The `Backend.IntegrationTests` project exists, compiles, and is discovered by the test runner — with a single placeholder test and no external dependencies (no SQL instance, no service containers, no network). This pre-books the project structure and CI wiring so Phase 2 adds files, not structure.

**Acceptance conditions:**

- **AC-8 (Test ID: BIT-001)** `Backend.IntegrationTests` contains one passing placeholder test; `dotnet test` includes it in the run summary (project is wired into the solution and discovered).
- **AC-9** The integration test project requires no database, container, or network to pass (CI runs it on the standard runner; verified by the workflow passing without extra services).

### B3 — Frontend skeleton builds, type-checks, and tests green

The React SPA compiles under TypeScript strict mode and has a passing test suite.

**Acceptance conditions:**

- **AC-10** `npm run build` in `src/web` exits 0 and produces a production bundle (Vite build output exists).
- **AC-11** `tsc` (strict mode, `noEmit`) exits 0.
- **AC-12 (Test ID: FT-001)** A Vitest test in `src/web` passes (e.g., renders a placeholder App component), exit code 0.

### B4 — Formatting is enforced

Consistent style is mechanically verifiable on both stacks.

**Acceptance conditions:**

- **AC-13** `dotnet format --verify-no-changes` at the solution root exits 0.
- **AC-14** `.editorconfig` exists at the repository root.
- **AC-15 (Test ID: FT-002)** Frontend has a lint/format check script (e.g., ESLint/Prettier via `npm run lint`) that exits 0.

### B5 — CI runs the gates on every push and PR

A GitHub Actions workflow builds, tests, and format-checks both stacks. No deployment step.

**Acceptance conditions:**

- **AC-16** `.github/workflows/ci.yml` exists; on push and pull_request it runs, and each step must succeed:
  - `dotnet build` + `dotnet test` + `dotnet format --verify-no-changes`
  - `npm ci` + `npm run build` + `tsc --noEmit` + `npm run lint` + `npm test` in `src/web`
- **AC-17** The workflow file passes `actionlint` (or YAML schema validation) — CI config itself is valid.
- **AC-18** A green run of the workflow on the default branch is visible in the Actions tab (manual verification; not code).

### B6 — Traceability

Every spec requirement maps bidirectionally to a test ID (per learning contract S1).

**Acceptance conditions:**

- **AC-19** A traceability table exists in the repo (this file, below) and every Test ID listed appears in the source tree (grep for the ID in test names or a checkin comment), and vice versa.

## Test ID registry (traceability table)

| Test ID | Requirement | Suite | Mechanically verified by |
|---|---|---|---|
| ARCH-001 | Core has no references | Architecture.Tests | `dotnet test` |
| ARCH-002 | Inward-only dependencies | Architecture.Tests | `dotnet test` |
| BT-001 | Core unit testable | Backend.UnitTests | `dotnet test` |
| BT-002 | Api host starts, /health returns 200 | Backend.UnitTests | `dotnet test` |
| BIT-001 | Integration test project wired into test discovery | Backend.IntegrationTests | `dotnet test` |
| FT-001 | App renders placeholder | src/web Vitest | `npm test` |
| FT-002 | Frontend lint clean | src/web | `npm run lint` |

## Constraints (from constitution, restated as verifiable properties)

- **C-1** No infrastructure cost: scaffold adds no cloud resources; CI uses free GitHub-hosted runners.
- **C-2** No in-memory-only storage or single-user assumptions introduced — the scaffold contains no persistence yet, by design.
- **C-3** Every CI-visible quality gate is a shell command exit code — mechanically checkable by an agent without judgment.

## Non-Goals

- **No configuration loading** — the config schema (`ideal_job`, `sources`, `embedding_provider`, `top_k`) is already decided on the roadmap; no loading/validation code in Phase 0.
- **No Terraform / `infra/` directory** — deferred to Phase 4.
- **No deployment** — CI stops at build/test/format; no deploy job, no Azure resources.
- **No embedding, listing, or persistence code** — Phases 1–3.
- **No frontend beyond a placeholder** — real UI is Phase 3.
- **No SQL-backed integration tests** — the `Backend.IntegrationTests` *project* is scaffolded (see B2b), but tests requiring a database, container, or network are Phase 2+.

## Open Questions

1. **Architecture test library** — use `NetArchTest`/`ArchUnitNET` vs. hand-rolled reflection assertions in `Architecture.Tests`? Hand-rolled is dependency-free; decide at implementation.
2. **Frontend lint tool choice** — ESLint only, or ESLint + Prettier? Both satisfy AC-13; pick one at implementation.
3. **CI monorepo strategy** — single workflow vs. separate backend/frontend jobs (or path filters)? Single workflow is simpler; revisit when build times matter.
4. **`/health` endpoint semantics** — placeholder for CI testability vs. a real health check contract? Decide in Phase 1/4.
5. **Solution file location** — `VectorJobSearch.sln` at repo root vs. in `src/`? Root is conventional; confirm at implementation.
