# Process

> Part of the project constitution (see [mission.md](mission.md),
> [tech-stack.md](tech-stack.md), [roadmap.md](roadmap.md)). This file holds
> engineering-process invariants: rules that agents, reviewers, and CI must
> obey, stated as verifiable properties.

## Merge Gates

Functionality is submitted via PRs. A task is closed by marking it `[X]` in
`tasks.md`; the PR merge is the gate that verifies closure was earned.

- **Spec–test agreement:** A task may be marked `[X]` only when its mapped
  tests pass. Verifiable: every `[X]` task in `tasks.md` has an evidence
  statement referencing a passing test ID from the traceability table
  (test ID → spec acceptance condition).
- **No silent spec drift:** If implementation legitimately diverges from the
  spec, the spec and plan are updated via a recorded delta in the same PR —
  never silently. Verifiable: any PR whose behavior differs from
  `specs/<feature>/spec.md` contains a change to that spec (or a delta
  section within it).
- **CI is the quality gate:** All PRs pass build and tests before merge.
  There are no client-side enforcement hooks; CI is the only gate.
