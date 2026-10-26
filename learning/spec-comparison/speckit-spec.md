# Feature Specification: Project Scaffold (Phase 0)

**Feature Branch**: `001-project-scaffold`

**Created**: 2026-10-24

**Status**: Draft

**Input**: User description: "Project scaffold - see Phase 0 on the roadmap"

## Clarifications

### Session 2026-10-24

- Q: Should the scaffold include a sample configuration file populated with the four defined settings (`ideal_job`, `sources`, `embedding_provider`, `top_k`) as placeholders? → A: No — remove config from Phase 0; it will be added in a later phase.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Fresh clone builds and tests cleanly (Priority: P1)

A developer clones the repository for the first time, runs the standard build and test commands, and everything succeeds without manual fixes or missing dependencies. The repository contains the backend solution and the frontend app, each in the structure defined by the tech stack decisions.

**Why this priority**: Phase 0's exit criterion is "a skeleton project builds and tests pass." Without a clean build, nothing else in the roadmap can begin. This story alone constitutes the MVP of this feature.

**Independent Test**: Clone the repo, run the build command and the test command; both succeed, and the placeholder test suites execute green.

**Acceptance Scenarios**:

1. **Given** a freshly cloned repository on a machine with the documented prerequisites installed, **When** the developer runs the backend build, **Then** the build succeeds with no warnings introduced by the scaffold.
2. **Given** the same clone, **When** the developer runs the backend test suite, **Then** at least one placeholder unit test per backend project runs and passes.
3. **Given** the same clone, **When** the developer runs the frontend build and type check, **Then** both succeed in strict mode.
4. **Given** the same clone, **When** the developer runs the frontend test suite, **Then** at least one placeholder test runs and passes.

---

### User Story 2 - Style and formatting enforced automatically (Priority: P2)

A developer makes a change that violates the project's code style and pushes it; the continuous integration pipeline fails with a clear message pointing at the formatting or lint failure. Correcting the style makes the pipeline green.

**Why this priority**: Consistent style from the very first commit prevents churn later and satisfies the constitution's requirement that formatting checks pass in CI. It only needs to work once the build (P1) exists.

**Independent Test**: Introduce a deliberate style violation, verify CI fails; fix it, verify CI passes.

**Acceptance Scenarios**:

1. **Given** the CI pipeline is configured for the repository, **When** a push contains a style violation in backend code, **Then** the pipeline fails on the format/lint step and reports the offending file.
2. **Given** the CI pipeline is configured for the repository, **When** a push contains a type error in frontend code, **Then** the pipeline fails on the strict type-check step.
3. **Given** a push where all code conforms to style rules, **When** CI runs, **Then** the pipeline reaches the test step and passes.

---

### User Story 3 - CI runs the full quality gate on every push (Priority: P3)

A maintainer pushes changes (or opens a pull request) and the automated pipeline runs the complete quality gate — build, format check, type check, and tests — reporting a single, trustworthy green/red status for the changeset.

**Why this priority**: The constitution requires all PRs to pass build and tests before merge; automating this from day one makes the gate real rather than aspirational. It depends on Stories 1 and 2 being in place, and deployment automation is explicitly out of scope (Phase 4).

**Independent Test**: Open a pull request; observe the pipeline run and its status correctly reflecting the state of the code.

**Acceptance Scenarios**:

1. **Given** a pull request with passing code, **When** CI completes, **Then** the pull request shows a green status covering build, format, type check, and tests.
2. **Given** a pull request with a failing test, **When** CI completes, **Then** the pull request shows a red status identifying the failing test.

---

### Edge Cases

- What happens when a developer on a machine without the required SDK versions tries to build? (A documented prerequisites list and version constraints should surface a clear, actionable error.)
- How does the scaffold handle running tests where a local database is required? (Integration tests requiring SQL must be skippable or otherwise not block the "tests pass" gate on machines without a database.)
- What happens when CI runs on a fresh runner with no cached dependencies? (Dependency restore must be part of the pipeline, not a manual step.)
- What happens if the frontend placeholder test runner is invoked in an environment with no browser? (Unit tests must not require a real browser.)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The repository MUST contain a backend solution with the structure needed for later phases (API project, test project), each building cleanly.
- **FR-002**: The repository MUST contain a frontend single-page application project that builds and type-checks in strict mode.
- **FR-003**: Each backend and frontend project MUST include at least one passing placeholder test so the test infrastructure is proven end-to-end.
- **FR-004**: The repository MUST enforce a consistent code style for backend code via an automated formatter check that can run locally and in CI.
- **FR-005**: The repository MUST include an automated CI pipeline that runs the backend build, backend tests, frontend type check, frontend build, and frontend tests on every push and pull request.
- **FR-006**: The CI pipeline MUST fail when any build, test, format, or type check fails, and pass only when all succeed.
- **FR-007**: The scaffold MUST NOT include application configuration handling; configuration (schema `ideal_job`, `sources`, `embedding_provider`, `top_k` and any sample files) is deferred to a later phase and must be addable without restructuring the scaffolded project structure.
- **FR-008**: The repository MUST include a documented prerequisites list (SDK/runtime versions and how to verify them) so a new contributor can set up quickly.
- **FR-009**: The scaffold MUST include an editor configuration file so style rules apply consistently across editors and machines.
- **FR-010**: Backend integration tests that require a database MUST be structured so they do not run (or are explicitly skipped) in environments without the database, without breaking the standard test run.

### Key Entities *(include if feature involves data)*

- **CI pipeline definition**: The automated quality-gate definition living in the repository, covering build, format, type check, and tests for both backend and frontend.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer with documented prerequisites can go from fresh clone to a full green local build-and-test run in under 15 minutes.
- **SC-002**: All placeholder tests (backend and frontend) pass both locally and in CI on the first attempt after scaffolding.
- **SC-003**: Every push and pull request produces a single CI status covering build, format, type check, and tests, with a run completing in under 10 minutes.
- **SC-004**: 100% of deliberate style or type violations introduced in a test push are caught by CI with an actionable failure message.

## Assumptions

- The tech stack decisions in `specs/tech-stack.md` are final and the scaffold follows them (C#/.NET backend, React/TypeScript frontend).
- Scope is Phase 0 only: no embedding pipeline, listing sources, vector storage, deployment infrastructure, or Terraform — those belong to Phases 1–4. An empty placeholder for infrastructure-as-code directories may exist, but nothing functional is required.
- Deployment automation is out of scope; the CI pipeline stops at the quality gate (build/test/lint/type check).
- Database-dependent integration tests are scaffolded in a way that they can be added in later phases without restructuring, but only placeholder unit tests are required now.
- The repository is hosted on GitHub (per the tech stack's GitHub Actions choice), so CI is defined with GitHub Actions.
- Configuration handling (schema and sample files for `ideal_job`, `sources`, `embedding_provider`, `top_k`) is explicitly deferred to a later phase (clarified 2026-10-24); the scaffold must not require any configuration to build or test.
- Secrets management is deferred to Phase 4; the scaffold must not require any API keys to build or test.
