---
name: tasks
description: Generate tasks.md from a feature's spec.md and plan.md, organized by user story with strict checklist format and traceability. Use when the user asks to create, generate, or break down tasks for a feature in specs/<feature>/. Replaces Spec Kit's /tasks.
---

# Tasks generation

You are in task generation mode. Read `specs/<feature>/spec.md` (user stories with priorities), `specs/<feature>/plan.md` (tech stack, data model, contracts), and the project constitution. Produce `specs/<feature>/tasks.md`:

## Structure

- Organize by user story: one phase per story, in spec priority order.
- Phase 1: setup (project initialization, shared infrastructure).
- Phase 2: foundational tasks that block all stories.
- Phase 3+: user story phases. Within each: test tasks (write the failing tests mapped to that story's acceptance conditions first), then models, services, endpoints, integration — in dependency order.
- Final phase: polish and cross-cutting concerns.

## Task format

Format every task EXACTLY as:

```
- [ ] [TaskID] [P] [Story] Description with explicit file path
```

where `[P]` marks tasks that can run in parallel (different files, no dependence on incomplete tasks) and `[Story]` (e.g. `[US1]`) is required only on user-story tasks. Quote every validation rule and constraint from the data model verbatim in the task text — do not leave constraints to implementation-time discretion.

## Gates

- Each user story phase must be independently testable: state its goal, its test criteria, and the acceptance-condition IDs it satisfies.
- Add a dependency section (which stories block which, if any) and a suggested MVP scope (usually just US1).
- Every acceptance condition from spec.md must be covered by at least one test task.
- Include a traceability appendix: task ID → spec condition ID. If a condition cannot be mapped to a task, STOP and report the gap instead of silently skipping it.
- tasks.md must be executable without further context: each task specific enough that an agent can complete it with no memory of this conversation.