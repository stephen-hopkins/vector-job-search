# Spec Comparison: Hand-Rolled vs. Spec Kit for Agentic SDD

**Date:** 2026-10-26
**Scope:** Comparison of `hand-rolled-spec.md` and `speckit-spec.md` (both specify Phase 0: Repository Scaffold), evaluated against researched best practices for spec-driven development with AI coding agents.

---

## 1. Researched Best Practices for Specs in Agentic SDD

Sources: GitHub Spec Kit documentation, Thoughtworks ("Spec-driven development: unpacking 2025's key new AI-assisted engineering practices"), IBM ("What is Spec-Driven Development?"), O'Reilly ("How to Write a Good Spec for AI Agents"), Microsoft Developer Blog (SDD posts), and arXiv papers: SDAD (2608.20341), Spec Kit Agents (2604.05278), and "Spec-Driven Development for Agentic Software Engineering" (2609.00252).

The consistent principles across these sources:

1. **The spec is the contract between human and agent.** It is the single source of truth the agent implements against and the human reviews against — no vibes, no guessing, no prompt-and-pray (IBM, amux, GitHub Spec Kit).
2. **Acceptance criteria must be machine-verifiable.** The agent should be able to *prove* it is done; completion should be checkable via command exit codes and test results, not human judgment. "Specification quality is the execution fuel for autonomous delivery" (SDAD).
3. **Ground every artifact in repository evidence.** The Spec Kit Agents paper found the largest measured quality gains (+0.15 on a 1–5 composite score, statistically significant) came from hooks that validated each spec stage against the actual repository, rather than free-floating prose.
4. **Run a clarification loop before implementation.** Elicit decisions from the human before code is written and record them; "refine the plan until there's no room for misinterpretation" (O'Reilly; GitHub Spec Kit's clarify phase).
5. **Maintain bidirectional traceability.** Requirements, tasks, and tests must map to each other so verification is mechanical in both directions — Spec Kit's cross-artifact analysis and SDAD's independent multi-agent verification both depend on this.
6. **Surface open decisions explicitly.** Ambiguity left in a spec becomes a silent, possibly wrong, choice by the agent.
7. **Keep specs structured and bounded.** Rich context fits in context windows and evolves with the project (O'Reilly); separate the "why" (stories, priorities, rationale) from the "what" (behavior, ACs).
8. **Define non-goals and scope boundaries explicitly.** Agents over-deliver when boundaries are implicit; a dedicated non-goals section is cheaper than reworking generated code.

---

## 2. Format & Structural Differences

| Aspect | Hand-rolled | Spec Kit |
|---|---|---|
| Style | Behavior/AC-based, technical, implementation-prescriptive | User stories + FRs per the Spec Kit template |
| Traceability | Explicit Test ID registry (ARCH-001/002, BT-001/002, BIT-001, FT-001/002) mapping requirements ↔ tests | None — no test IDs, no traceability table |
| Clarifications | Implicit (Open Questions section) | Explicit Q&A "Clarifications" section (e.g., config removed from Phase 0) |
| Open items | 5 named Open Questions (arch test lib, lint tool, CI layout, `/health` semantics, `.sln` location) | None — presents decisions as settled |
| Metadata | Status, roadmap phase, constitution references | Feature branch, creation date, input description, status |

## 3. In Hand-Rolled but NOT Spec Kit

1. **Full clean-architecture layout** — exact directory tree (`src/Core`, `src/Application`, `src/Infrastructure`) with onion dependency rules (`Api → Infrastructure → Application → Core`). Spec Kit's FR-001 only vaguely says "API project, test project."
2. **Architecture enforcement tests** — dedicated `tests/Architecture.Tests` project with dependency-direction assertions (ARCH-001/002). Entirely absent from Spec Kit.
3. **Integration test project scaffolding (B2b)** — `Backend.IntegrationTests` pre-wired with a placeholder test requiring no DB/container/network. Spec Kit only *assumes* this without requiring it.
4. **`/health` endpoint + WebApplicationFactory test (BT-002)** — proves the API host boots in the test harness.
5. **CI config validation** — `actionlint`/schema check of the workflow file itself (AC-17).
6. **Traceability contract** — bidirectional spec↔test mapping (B6/AC-19), grep-able IDs.
7. **Constraints section** — constitution-derived verifiable properties (C-1 no cost, C-2 no in-memory storage assumptions, C-3 mechanical gates).
8. **Explicit Non-Goals list** — no Terraform, no deployment, no embedding/persistence, etc.

## 4. In Spec Kit but NOT Hand-Rolled

1. **User stories with priorities** (P1 clean build, P2 style enforcement, P3 CI gate) with rationale for each priority.
2. **Given/When/Then acceptance scenarios** — including *negative* CI scenarios (style violation fails the pipeline, failing test turns PR red).
3. **Edge cases section** — wrong SDK versions, no local DB, no dependency cache on CI runner, no browser environment for frontend tests.
4. **FR-008: documented prerequisites list** (SDK versions + verification commands).
5. **Quantified success criteria** — fresh clone to green build <15 min (SC-001), CI run <10 min (SC-003), 100% of violations caught (SC-004).
6. **FR-010: DB-dependent tests explicitly skippable** — a skip mechanism, not just deferral.
7. **Assumptions section** — GitHub Actions hosting, no API keys needed to build/test, IaC placeholder allowed.
8. **Feature branch name and creation metadata.**

## 5. Where They Substantively Disagree

- **Architecture rigor:** Hand-rolled mandates clean architecture + mechanical enforcement; Spec Kit leaves structure nearly unspecified.
- **Test philosophy:** Hand-rolled = every requirement has a named, greppable Test ID; Spec Kit = "at least one placeholder test per project."
- **Deferred config handling:** Both defer it, but Spec Kit makes it a formal FR (FR-007) backed by a recorded clarification; hand-rolled lists it as a non-goal.
- **Uncertainty handling:** Hand-rolled surfaces open decisions; Spec Kit hides them, which risks the implementer (human *or* agent) making silent choices.

## 6. Best-Practice Scorecard

| Best practice | Hand-rolled | Spec Kit |
|---|---|---|
| Machine-verifiable ACs | ✅ Every AC is a shell-command exit code; C-3 states "mechanically checkable by an agent without judgment" | ⚠️ Softer — Gherkin scenarios and FRs, many requiring human observation |
| Traceability | ✅ Bidirectional Test ID registry | ❌ |
| Repository grounding | ✅ Constraints, constitution refs, exact layout | ⚠️ References tech-stack.md but stays vague |
| Clarification loop | ⚠️ Open questions, no recorded decisions | ✅ Formal Clarifications section |
| Why/priorities/rationale | ❌ | ✅ P1/P2/P3 with rationale |
| Edge cases | ❌ | ✅ Four concrete edge cases |
| Non-goals / boundaries | ✅ Dedicated section | ⚠️ Buried in assumptions |
| Measurable success criteria | ⚠️ Implicit | ✅ Timeboxed and quantified |

**Summary:** the hand-rolled spec is more *implementation-prescriptive and mechanically verifiable* — the properties the literature says matter most when an agent executes the spec. The Spec Kit spec is more *user- and risk-oriented* — better at capturing the "why," failure modes, and human onboarding.

---

## 7. Recommendation

**Keep the hand-rolled spec as the canonical spec for Phase 0.** Its machine-verifiable acceptance criteria, bidirectional test traceability, exact structural contract, and explicit open questions align with the core best practices for agentic SDD. An agent can recover from a missing edge-case section, but it cannot recover from acceptance criteria it cannot mechanically verify.

However, merge in the following missing strengths from the Spec Kit version:

### Additions to make to the hand-rolled spec

1. **Clarifications section (decision log).** Convert the Open Questions into resolved decisions where possible, and record each as a Q&A entry (as Spec Kit does). Leave only genuinely deferred decisions in Open Questions, each with a named decision point ("decide at implementation of X").

2. **Edge cases section.** Add the four Spec Kit edge cases as explicit requirements or ACs:
   - Building without the required SDK versions → clear, actionable error (ties into prerequisites below).
   - Running tests on a machine with no local database → SQL-dependent tests must not block the gate (formalize as an AC alongside B2b).
   - CI on a fresh runner with no dependency cache → dependency restore must be an explicit pipeline step (make it an AC under B5).
   - Frontend tests in an environment with no browser → Vitest unit tests must not require a real browser (make it an AC under B3).

3. **Prerequisites documentation (FR-008).** Add a behavior/AC: a documented prerequisites list (SDK/runtime/Node versions and how to verify them, e.g., `dotnet --version`, `node --version`) exists at the repo root (README or CONTRIBUTING), and the versions match what CI uses.

4. **User stories with priorities and rationale.** Add a short "Why" preamble per behavior block (B1–B6): B2/B3 = P1 (Phase 0 exit criterion), B4 = P2, B5 = P3, with one line of rationale each. This preserves ordering intent for both humans and agents without restructuring the spec.

5. **Negative CI scenarios.** Extend B5's acceptance conditions with the Spec Kit scenarios: a push containing a style violation fails on the format/lint step (this follows from AC-13/AC-15 but should be stated as a scenario), and a PR with a failing test shows a red status identifying the failing test.

6. **Quantified success criteria.** Add a Success Criteria section: fresh clone → full green local build-and-test in under 15 minutes; CI run completes in under 10 minutes. These give an agent a self-check target and give review a non-binary quality signal.

7. **DB-test skip mechanism (FR-010).** Strengthen B2b/AC-9: not only must Phase 0 integration tests pass without a database, but the scaffold must structure SQL-backed tests so later phases can add them without restructuring (e.g., trait-based filtering or a marker convention), so the standard `dotnet test` run never breaks on DB-less machines.

8. **Metadata parity.** Add the feature branch name, creation date, and input description to the header, matching Spec Kit's provenance conventions.

### What NOT to import

- Do **not** replace the AC/exit-code format with Gherkin scenarios as the primary contract — keep Given/When/Then as supplementary illustration only.
- Do **not** soften the directory-layout or architecture-test requirements to match Spec Kit's vaguer FR-001 — that specificity is the hand-rolled spec's greatest strength for agentic execution.

### Resulting hybrid, in one line

> Hand-rolled spec (exit-code ACs, Test ID traceability, exact structure, open questions) + Spec Kit's clarification log, edge cases, priorities/rationale, prerequisites, negative CI scenarios, quantified success criteria, and DB-test skip requirement.

---

## 8. Follow-Up: Evaluation of the Spec-Writing Skill's Output

A new spec-writing skill was built using the recommendations above, and its first output — `spec-writing-skill-spec.md` (Spec 001 — Initial Project Scaffold, Phase 0) — was evaluated against Section 7's recommendations. Results:

### Fully incorporated (5 of 8)

| Recommendation | Status in `spec-writing-skill-spec.md` |
|---|---|
| 1. Clarifications section (decision log) | ✅ Full verbatim Q&A log (5 entries), including the roadmap-config removal. Open Questions (OQ-1, OQ-2) each carry a named **Decision point**. |
| 5. Negative CI scenarios | ✅ N-AC-1 (failing test → red PR, with `P0-CI-RED` Test ID) and E-5 (style violation fails the format gate). |
| 6. Quantified success criteria | ✅ Dedicated Success Criteria section (SC-1–SC-5), including the CI-runs-under-10-minutes criterion (SC-4). |
| "What NOT to import" | ✅ Exit-code ACs remain the primary contract; no Gherkin anywhere; structural specificity preserved (Test ID Registry, Constraints C-1–C-5, Non-Goals). |
| Hand-rolled core strengths | ✅ Test ID traceability, Constraints, Non-Goals, and machine-verifiable ACs all carried over. |

### Partially incorporated (2 of 8)

- **3. Prerequisites documentation (FR-008):** README instructions are made real and grep-verified (AC-4), and E-4 pins the SDK version — but there is no AC requiring a documented prerequisites list (SDK/Node versions + `dotnet --version` / `node --version` verification commands matching what CI uses). E-4 only proves the build *fails* on a wrong SDK; it doesn't require the correct versions to be *documented*.
- **6 (minor gap):** SC-1 requires 0 manual repair steps but drops the "fresh clone → green in under 15 minutes" local timebox. Only the CI 10-minute bound (SC-4) survived.

### Not incorporated (2 of 8)

- **2. Edge cases section — the four Spec Kit cases:** The spec has a strong Edge Cases section (E-1–E-5), but only two of the four recommended cases map over:
  - ✅ Wrong SDK version → E-4
  - ✅ Style violation on push → E-5
  - ❌ CI on a fresh runner with no dependency cache — no AC making dependency restore an explicit pipeline step (AC-12 lists build/test/format steps but not restore/install for the backend).
  - ❌ Frontend tests requiring no real browser — never stated, even though the Vitest/RTL choice (C-3) makes it true by construction.
  - (The "no local database" case is arguably moot — C-4 forbids persistence code entirely in Phase 0, so there are no DB-dependent tests to skip.)
- **4. User stories with priorities and rationale:** Entirely absent. No P1/P2/P3 ordering, no "Why" preamble per behavior block (B1–B5), no rationale for priority. The Purpose section explains the phase goal but not per-behavior ordering intent.

### Absent but applicable

- **7. DB-test skip mechanism (FR-010):** Not present — defensible, since this spec's scope excludes persistence entirely (C-4). The forward-looking intent ("structure tests so later phases can add DB tests without restructuring") could still be worth a line.
- **8. Metadata parity:** Partially absent. Status and roadmap phase are in the header, but there is no feature branch name, no creation date, and no input description (Spec Kit's provenance conventions).

### Verdict

The skill's output successfully merges the core hand-rolled strengths (exit-code ACs, Test ID Registry, Constraints, Non-Goals, decision-pointed Open Questions) with about **60% of the Spec Kit additions** — most importantly the Clarifications log and negative CI scenarios, which were the highest-value items. None of the gaps break the spec's core virtue: every AC remains mechanically verifiable (SC-5 even asserts this).

### Decision on the remaining gaps

**The remaining gaps are deliberately excluded from the skill at present.** The skill will be tweaked on future specs based on actual *functionality* rather than the initial project scaffold — functionality specs are more typical of how the skill will be used, and a project-scaffold spec is not the best test bed for judgments about priorities/rationale placement, prerequisites lists, DB-test skip mechanisms, or timeboxed criteria. Revisit these gaps once the skill has produced one or more functionality specs (Phase 1+), where their value can be assessed against realistic use.
