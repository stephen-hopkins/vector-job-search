---
name: Spec Writing
description: Author a functional specification for agentic spec-driven development by interviewing the user, producing mechanically verifiable acceptance conditions, edge cases, negative ACs, and quantified success criteria. Use whenever the user asks to write, draft, or revise a spec, specification, or functional spec.
---

# Spec Writing

You are in spec-authoring mode. Read the project's constitution and tech stack docs before starting (look in `.specify/memory/`, `specs/`, or ask the user where they live).

The user will provide details of what the spec should be for. If they have not, ask them what the spec should cover as your first question. Then interview the user **one question at a time** to derive the functional spec for that scope.

## Core Rules

- Separate behavior from implementation: the spec says WHAT, never HOW.
- Every requirement must have an acceptance condition that could be verified mechanically — a test, a schema check, or a CLI command exit code/output. Never "works correctly" or "looks good".
- Explicitly list non-goals and open questions.
- Do not write code. Output a draft `spec.md`, then stop.

## Required Sections

The output spec must contain, in order:

1. **Header** — status, roadmap phase (or slice name), and references to the constitution/tech-stack documents the spec draws its constraints from.
2. **Purpose** — one short paragraph: what this spec delivers and why.
3. **Behavior (WHAT)** — numbered behavior blocks (B1, B2, ...). Each block states WHAT the system does, with acceptance conditions (AC-1, AC-2, ...) under it. Every AC is mechanically verifiable.
4. **Test ID Registry** — a traceability table mapping every AC that needs a test to a unique Test ID, its suite, and the command that verifies it. Test IDs must be grep-able in the future source tree (test names or comments).
5. **Constraints** — constitution/project constraints restated as verifiable properties.
6. **Non-Goals** — explicit list of what is deliberately out of scope, each tagged with where it will be handled (a later phase/spec) if applicable.
7. **Clarifications** — every interview question and the user's answer, recorded verbatim. Convert answers into resolved decisions in the spec body; only genuinely deferred items remain in Open Questions.
8. **Edge Cases** — identify edge cases yourself from the behavior being specified. Consider how the feature behaves when things go wrong: bad, missing, or boundary inputs; unavailable dependencies; limits exceeded; or whatever else this specific feature makes relevant. Each edge case must resolve to a mechanically verifiable acceptance condition, not prose.
9. **Negative Acceptance Conditions** — alongside positive ACs, consider states what the feature MUST NOT do, or ways it must observably fail, decline, or reject. Specify each so the failure itself is mechanically verifiable (a defined exit code, error code, or rejected-input test).
10. **Success Criteria** — quantified, measurable outcomes appropriate to this feature. Decide for yourself what is worth measuring (outcome quality, limits, verification effort), but every criterion must be checkable without judgment.
11. **Open Questions** — only genuinely deferred decisions. Each must be tagged with the phase or moment it will be decided.

## Interview Protocol

- One question at a time. Wait for the answer before asking the next.
- Record each Q&A verbatim in the Clarifications section as you go.
- Prefer resolving ambiguity through the interview over `[NEEDS CLARIFICATION]` markers. If the interview ends with unresolved items that genuinely cannot be settled now, they go to Open Questions with a decision point — never silently guessed.
- Stop interviewing when you can write every behavior block with mechanical ACs. Do not ask about implementation details (libraries, patterns, file layout) — those are HOW, not WHAT.

## Quality Gate (self-check before delivering)

Before outputting the draft, verify:

- [ ] No AC requires human judgment to check.
- [ ] Every AC maps to a command, test, or schema check.
- [ ] Every Test ID in the registry corresponds to an AC, and vice versa where tests apply.
- [ ] Edge cases exist and each ends in a verifiable AC.
- [ ] At least one negative acceptance condition is considered (or explicitly noted why none apply).
- [ ] Success criteria are quantified.
- [ ] No implementation details (frameworks, libraries, code structure) leaked into behavior statements.
- [ ] All non-goals listed with disposition.
- [ ] Clarifications recorded; open questions tagged with decision points.

## Output

Write the draft spec to `specs/NNN-spec-summary/spec.md`, where:

- `NNN` is the next available 3-digit sequential number (scan existing directories in `specs/` and increment — e.g., if `specs/001-project-scaffold` exists, the next is `002`)
- `spec-summary` is a 2–4 word kebab-case summary of the spec's subject (e.g., `001-project-scaffold`, `002-job-listing-ingestion`)

Then stop. Do not write code.
