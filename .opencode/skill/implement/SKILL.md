---
name: implement
description: Execute a feature's tasks.md under human supervision, halting after each user-story phase for review. Use when the user asks to implement a feature in specs/<feature>/ (HITL variant; replaces Spec Kit's /implement).
---

# Implement (HITL — supervised)

You are in implementation mode, supervised. Work in `specs/<feature>/`. Read `tasks.md`, `plan.md`, and the project constitution. Execute tasks in dependency order (parallel `[P]` tasks may be interleaved):

## Per task

- Implement, then run the tests mapped to it in the traceability table.
- Mark the task `[X]` in tasks.md ONLY when its tests pass; attach a one-line evidence statement ("test X passes, command Y output Z").

## Halts

- Halt after each user-story phase and report: tasks done, evidence, spec conditions satisfied. Wait for my review before continuing.

## Scaffolding preference

- Prefer CLI scaffolding/management commands over directly creating or editing files — especially for solutions, projects, and dependencies (e.g. `dotnet new sln|classlib|webapi|xunit`, `dotnet sln add`, `dotnet add reference`, `dotnet add package`; `npm create vite`; `npm install`). Edit generated files only to customize what the CLI cannot express.

## Branching

- Work on a branch named `<type>/<short-kebab-description>` following Conventional Commits types (e.g. `feat/001-project-scaffold`, `fix/health-route-405`). If you inherit a branch with another style, rename it with `git branch -m` before committing.

## Stop conditions

STOP and ask only when:

- a spec condition is ambiguous,
- a task requires an irreversible action (migrations, external API writes, deletes),
- or plan.md conflicts with the code.