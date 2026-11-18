---
name: plan
description: Derive the implementation design (plan.md) for a feature from its spec. Use when the user asks to plan, design, or derive an implementation plan for a feature in specs/<feature>/spec.md. Replaces Spec Kit's /plan.
---

# Plan (implementation design)

You are in design mode. Read `specs/<feature>/spec.md` and the project constitution. Produce `specs/<feature>/plan.md` with these sections, in order:

1. **Technical context** — languages, frameworks, libraries, key packages with versions, and the project/file structure this feature touches. Mark anything you cannot determine from the repo as "NEEDS CLARIFICATION".
2. **Constitution check** — quote each constitution principle that applies to this feature and state whether the design complies, violates (with justification), or needs an exception.
3. **Research** — for every NEEDS CLARIFICATION, dependency, or integration: investigate the codebase and docs to resolve it. Record each resolution as: decision, rationale, alternatives considered. If an item cannot be resolved without a human decision, STOP and present it as a blocking question — list everything blocking in one message, not one at a time.
4. **Data model** — entities (fields, types, validation rules quoted verbatim from the spec), relationships, and state transitions.
5. **Contracts** — the interfaces this feature exposes (API endpoints, CLI commands, schemas, events), with request/response or input/output shapes. Skip if the feature is purely internal.
6. **Quickstart** — the exact commands a reviewer would run to validate the feature end-to-end, and the expected observable outcome of each.

## Rules

- The plan says WHAT and WHERE (files, interfaces, entities), not the line-by-line HOW.
- Every design decision must trace to a spec condition ID; anything in the plan that no spec condition covers is scope creep — delete it or flag it as an explicit proposal.
- End with an artifact list and the traceability table: design element → spec condition ID.
- Do not write implementation code.