# plan.md — Spec-Driven, Human-on-the-Loop Development Plan

> Companion to contract.md (the learning contract) and the project constitution.
> This file is the _how_: detailed per-phase instructions, prompts, and resources.
> Goal: operate this project in human-on-the-loop (HOTL) mode — agents execute
> end-to-end in bounded domains; the human supervises via evidence, dashboards,
> and surgical intervention, not per-step approval.

## Mental model

Autonomy is a dial per action, not a switch per agent. HOTL vs HITL is a
property of each action class, not of the agent.

| Level | Mode        | Human role                                   | Where it fits           |
| ----- | ----------- | -------------------------------------------- | ----------------------- |
| L1    | Assisted    | Approve everything                           | Phase 0–1 (calibration) |
| L2    | Supervised  | Approve high-stakes steps only               | Phase 2                 |
| L3    | Conditional | Monitor, intervene on exceptions             | Phase 4–5 (the goal)    |
| L4    | High        | Set goals and guardrails, audit periodically | Stretch                 |

Empirical grounding: Anthropic found experienced users auto-approve MORE
(20% → 40%+ of sessions) but ALSO interrupt more (5% → 9% of turns). That is
the shift from pre-trust gatekeeping to post-trust supervision. This plan
climbs that curve deliberately. Do not skip the gatekeeping phases — the
experts in the data didn't.

Key principle: your attention is not a security control. Primary defenses
are deterministic (sandboxing, credential isolation, policy gates). Humans
supervise exceptions and irreversible actions.

---

## Phase 0 — Constitution ≠ spec: derive the first functional spec (week 1)

The constitution holds universal principles. What's missing is the per-feature
layer: requirements → design → tasks. That layering IS spec-driven development.

### Steps

1. Read the two orientation resources (below).
2. Write the first spec BY HAND-PROMPT (below). Commit the draft.
3. Run Spec Kit `/specify` + `/clarify` on the same feature intent.
4. Diff the two specs for differences in KIND (not quality):
   - Where did the template insist on something you wouldn't have?
   - Where did your version fail to demand mechanical verifiability?
   - What edge cases did `/clarify` surface that you didn't?
5. Fold the findings into the constitution.
6. Create contract.md if not present.

### Prompt (Phase 0 bootstrap — delete once internalised)

```text
You are in spec-authoring mode. Read my constitution and tech stack docs.
Interview me one question at a time to derive the FIRST functional spec
for the smallest useful vertical slice of this project. Rules:
- Separate behavior from implementation: the spec says WHAT, never HOW.
- Every requirement must have an acceptance condition you could verify
  mechanically (a test, a schema check, or a CLI command output).
- Explicitly list non-goals and open questions.
Output: a draft spec.md, then stop. Do not write code.
```

Why hand-write it when Spec Kit exists: the prompt teaches spec CRAFT
(what makes a requirement verifiable, what a non-goal is); the template
teaches tool operation. Craft transfers across every tool swap. Once you
can write this prompt cold with reasons for each constraint, stop using
it and lean on `/specify`.

### Resources

- ThoughtWorks, "Spec-driven development: unpacking 2025's new engineering practice"
  https://www.thoughtworks.com/en-us/insights/blog/agile-engineering-practices/spec-driven-development-unpacking-2025-new-engineering-practices
- Nimbalyst, "Spec-Driven Development with Coding Agents" (framework comparison)
  https://nimbalyst.com/blog/spec-driven-development-with-coding-agents/

### Exit condition

Two draft specs (hand-prompt + /specify) exist, diff notes committed.

> **Note (2026-10-26):** Phase 0 complete. The diff findings
> (`learning/spec-comparison/comparison.md`) didn't just update the
> constitution — a **new spec-writing skill** was created from them. Its
> first output is `learning/spec-comparison/spec-writing-skill-spec.md`,
> evaluated against the comparison's recommendations in Section 8 of the
> comparison doc. The skill will be tweaked on future specs based on actual
> functionality rather than the initial project scaffold.

---

## Phase 1 — Verifiability substrate (weeks 2–3)

Unsexy phase that makes HOTL possible later. You cannot be ON the loop if
there is nothing to watch. Oversight without telemetry is vibes with extra
steps.

### Steps

1. Adopt Superpowers (obra/superpowers) for TDD enforcement in-session —
   the only major framework that hard-enforces red-green-refactor.
   The spec-writing skill = what to build; Superpowers = how the agent works.
2. Generate tests + schemas from the Phase 0 spec (prompt below).
3. Wire CI to run on every agent PR.
4. Add a rule to the constitution: no merge closes a task unless the spec
   and tests agree (bidirectional spec updates — the spec changes when
   implementation legitimately diverges, and never silently).

### Prompt

```text
From the spec in specs/<feature>/spec.md, generate:
1. A test plan where every acceptance condition maps to exactly one test ID.
2. The failing tests themselves (red phase). No implementation.
3. A JSON Schema for each structured artifact this feature produces.
Write test IDs as comments in the tests so spec conditions are traceable
to code. Halt after tests are written and failing.
```

### Resources

- Superpowers: https://github.com/obra/superpowers
- Augment, "Living Specs for AI Agent Development" (bidirectional updates)
  https://www.augmentcode.com/guides/living-specs-for-ai-agent-development

### Exit condition

Every acceptance condition maps to a test ID; CI green.

---

## Phase 2 — Gated loop, then dismantle the gates deliberately (weeks 4–6)

Run the full spec-driven flow (spec-writing skill → plan prompt → tasks
prompt → implement prompt, all in the Prompt Library below) with human
approval at each gate. This is HITL — temporarily, and correctly. You need a
baseline of what "correct" looks like before you can afford to stop checking.

The prompts are self-contained replacements for Spec Kit's slash commands:
they keep the artifact structure (per-feature dir with plan.md, tasks.md)
but drop the tooling, setup scripts, and built-in pause-points. The gates
in Phase 2 are *yours* — you choose when to review each artifact — not
the tool's.

### Steps

1. Run 4–5 features through the full gated flow (Prompt Library below).
2. Log EVERY intervention: what the agent got wrong, and what signal would
   have caught it. That log becomes the oversight policy in Phase 3/4.
3. Introduce the autonomous-implementation prompt on the LAST feature, with
   escalation criteria explicit.

### Prompt

Use the **HOTL variant of Prompt 3 (Implement)** from the Prompt Library
below — that prompt IS the autonomous-implementation mode, with the
escalation criteria explicit. Run it on the LAST feature of this phase;
use the HITL variant (with phase-gate halts) for the earlier features.

The skill being taught: telling an agent when NOT to ask. Under-specified
escalation criteria produce an agent that either steamrolls or nags.

### Resources

- Anthropic, "Measuring AI Agent Autonomy in Practice"
  https://www.anthropic.com/research/measuring-agent-autonomy

### Exit condition

4–5 features done; intervention log has ≥10 entries.

---

## Phase 3 — Reviewer agent + policy tiers (weeks 6–7)

Invert the topology: poka-yoke for agents. Machinery reviews diffs; you
review what machinery can't.

### Steps

1. Give the implementer agent PR-level autonomy in a worktree/sandbox.
2. Build a separate reviewer agent, read-only tooling (prompt below).
3. Add the policy tier table to the repo (POLICY.md or constitution):

| Action class            | Examples                              | Tier                | Mechanism                                    |
| ----------------------- | ------------------------------------- | ------------------- | -------------------------------------------- |
| Reversible, local       | Refactors, tests, docs                | Autonomy            | Auto-merge if reviewer passes                |
| Stateful but local      | Schema bumps, local data files        | Autonomy + evidence | Evidence pack required; sample-review 1 in 5 |
| Irreversible / external | Migrations, deploys, third-party APIs | Escalate            | Agent pauses, queues approval item           |
| Out of policy           | Anything undefined                    | Escalate            | Agent must ask, never assume                 |

4. Enforce tiers with deterministic gates (sandbox, credential isolation,
   CI policy checks) — NOT with human vigilance. A 2026 scalex study found
   human approvers missed 1 in 3 planted threats and approved disguised
   npm scripts 52.5% of the time. Per-command approval fatigues; policy
   gates don't.

### Prompt

```text
You are a review agent. You have read-only access. Given a PR and the
spec in specs/<feature>/spec.md:
1. Map every changed file to spec condition IDs.
2. Flag any behavior in the diff NOT covered by a spec condition (scope creep).
3. Flag any spec condition without corresponding implementation or test.
4. Run the acceptance tests and report evidence.
5. Emit a verdict: PASS / FAIL(with reasons) / ESCALATE(unverifiable).
Never fix anything yourself. Your job is to be the mirror, not the hammer.
```

### Resources

- Context Studios, "Human-in-the-Loop vs Autonomous Agents (2026)"
  (scalex permission study summary)
  https://www.contextstudios.ai/comparisons/human-in-the-loop-vs-autonomous-ai-agents

### Exit condition

Reviewer agent blocks/enforces on 3 real PRs; S2 merge-rate ≥70%
sustained for one week (measured from the merge log).

---

## Phase 4 — Oversight surface (week 8+)

HOTL isn't a vibe, it's a product surface.

### Steps

1. Build the minimum console: streaming log of agent actions tagged by
   policy tier, alerts on ESCALATE and tier-irreversible events, kill switch.
2. Instrument three metrics (Anthropic's internal oversight dimensions):
   - Coverage: share of actions checked (by reviewer agent or you)
   - Review delay: how stale your awareness is
   - Escalation rate: is the agent asking at plausible moments, or drowning you?
3. Run the drift drill: corrupt a spec mid-flight; verify the reviewer
   catches the divergence. An oversight system never tested is a
   comforting architecture diagram.

### Resources

- The New Stack, "Human-on-the-Loop: The New AI Control Model"
  https://thenewstack.io/human-on-the-loop-the-new-ai-control-model-that-actually-works/
- thinkia, "Human in the loop vs autonomous agents"
  https://thinkia.com/ai-decisions/human-in-the-loop-vs-autonomous-ai-agents/
- Anthropic, "Trustworthy agents in practice" (minimal footprint principle)
  https://www.anthropic.com/research/trustworthy-agents

### Exit condition

Drift drill passes; escalation alerting works.

---

## Phase 5 — Earn L4 instincts without going full YOLO (ongoing)

### Steps

1. Let the agent propose its own task breakdowns from a one-line intent,
   with the reviewer as the only gate.
2. Study contrasting frameworks: OpenSpec (propose/apply/archive delta
   model), BMAD (role-based), Kiro (IDE-native). Focus on the invariants
   they share — the invariants matter more than the tooling.
3. Monthly retrospective: diff spec vs. what actually shipped. The spec
   that drifts silently is the one that lies.

### Resources

- OpenSpec: https://github.com/Fission-AI/OpenSpec
- "Spec-Driven Development: Invariants, Not Frameworks"
  https://ranjankumar.in/spec-driven-development-invariants-not-frameworks
- Autonomy ladder reference: https://ilmtec.ai/blog/levels-of-ai-agent-autonomy

### Exit condition

One feature taken from one-line intent to merge, gated only by reviewer.

---

## Pacing note

Anthropic's telemetry shows the pros didn't start autonomous — they learned
their way there, and real-world autonomy lags model capability (the
"deployment overhang"). Approving everything for a fortnight (Phase 2) is
how you earn the right to approve almost nothing — and to interrupt at
exactly the right moments. Measure success by how often you're in the RIGHT
loop, not how rarely you're in any loop.

---

## Prompt Library — Spec Kit replacements (HOTL-native)

These three prompts replace `/plan`, `/tasks`, and `/implement`. The
spec-writing skill already replaces `/specify` + `/clarify`. They keep
Spec Kit's artifact conventions (per-feature directory, plan.md → tasks.md
→ implementation, task IDs traceable to spec conditions) so the Phase 1
verifiability substrate and the Phase 3 reviewer agent work unchanged.
They differ from Spec Kit in two ways: no tooling or setup scripts
required, and the HITL pause-and-approve assumptions are replaced by
explicit stop conditions and evidence — the human chooses where the gates
are, not the prompt.

Run them in sequence on a feature directory
`specs/<feature>/` that already contains a `spec.md` from the
spec-writing skill.

### 1. Plan (replaces /plan)

Ask: "Derive the implementation design for the feature specified in
`specs/<feature>/spec.md`. Do not write feature code.

```text
Design mode. Read specs/<feature>/spec.md and the project constitution.
Produce specs/<feature>/plan.md with these sections, in order:

1. Technical context — languages, frameworks, libraries, key packages
   with versions, and the project/file structure this feature touches.
   Mark anything you cannot determine from the repo as
   "NEEDS CLARIFICATION".
2. Constitution check — quote each constitution principle that applies
   to this feature and state whether the design complies, violates
   (with justification), or needs an exception.
3. Research — for every NEEDS CLARIFICATION, dependency, or integration:
   investigate the codebase and docs to resolve it. Record each
   resolution as: decision, rationale, alternatives considered.
   If an item cannot be resolved without a human decision, STOP and
   present it as a blocking question — list everything blocking in one
   message, not one at a time.
4. Data model — entities (fields, types, validation rules quoted
   verbatim from the spec), relationships, and state transitions.
5. Contracts — the interfaces this feature exposes (API endpoints, CLI
   commands, schemas, events), with request/response or input/output
   shapes. Skip if the feature is purely internal.
6. Quickstart — the exact commands a reviewer would run to validate the
   feature end-to-end, and the expected observable outcome of each.

Rules: the plan says WHAT and WHERE (files, interfaces, entities), not
the line-by-line HOW. Every design decision must trace to a spec
condition ID; anything in the plan that no spec condition covers is
scope creep — delete it or flag it as an explicit proposal. End with an
artifact list and the traceability table: design element → spec
condition ID. Do not write implementation code.
```

Gate for Phase 2: review plan.md before generating tasks. Gate for
Phase 3+: reviewer agent checks traceability; you see only ESCALATEs.

### 2. Tasks (replaces /tasks)

```text
Task generation mode. Read specs/<feature>/spec.md (user stories with
priorities), plan.md (tech stack, data model, contracts), and the
constitution. Produce specs/<feature>/tasks.md:

- Organize by user story: one phase per story, in spec priority order.
- Phase 1: setup (project initialization, shared infrastructure).
- Phase 2: foundational tasks that block all stories.
- Phase 3+: user story phases. Within each: test tasks (write the
  failing tests mapped to that story's acceptance conditions first),
  then models, services, endpoints, integration — in dependency order.
- Final phase: polish and cross-cutting concerns.

Format every task EXACTLY as:
- [ ] [TaskID] [P] [Story] Description with explicit file path
where [P] marks tasks that can run in parallel (different files, no
dependence on incomplete tasks) and [Story] (e.g. [US1]) is required
only on user-story tasks. Quote every validation rule and constraint
from the data model verbatim in the task text — do not leave
constraints to implementation-time discretion.

Each user story phase must be independently testable: state its goal,
its test criteria, and the acceptance-condition IDs it satisfies. Add
a dependency section (which stories block which, if any) and a
suggested MVP scope (usually just US1).

Every acceptance condition from spec.md must be covered by at least
one test task. Include a traceability appendix: task ID → spec
condition ID. If a condition cannot be mapped to a task, STOP and
report the gap instead of silently skipping it. tasks.md must be
executable without further context: each task specific enough that an
agent can complete it with no memory of this conversation.
```

### 3. Implement (replaces /implement)

Two variants — the explicit gate is choosing which one to run.

HITL variant (Phase 2 calibration):

```text
Implementation mode, supervised. Work in specs/<feature>/.
Read tasks.md, plan.md, and the constitution. Execute tasks in
dependency order (parallel [P] tasks may be interleaved):

- For each task: implement, then run the tests mapped to it in the
  traceability table. Mark the task [X] in tasks.md ONLY when its
  tests pass; attach a one-line evidence statement ("test X passes,
  command Y output Z").
- Halt after each user-story phase and report: tasks done, evidence,
  spec conditions satisfied. Wait for my review before continuing.
- Prefer CLI scaffolding/management commands over directly creating or
  editing files — especially for solutions, projects, and dependencies
  (e.g. `dotnet new sln|classlib|webapi|xunit`, `dotnet sln add`,
  `dotnet add reference`, `dotnet add package`; `npm create vite`;
  `npm install`). Edit generated files only to customize what the CLI
  cannot express.
- Work on a branch named `<type>/<short-kebab-description>` following
  Conventional Commits types (e.g. `feat/001-project-scaffold`,
  `fix/health-route-405`). If you inherit a branch with another style,
  rename it with `git branch -m` before committing.
- STOP and ask only when: a spec condition is ambiguous, a task
  requires an irreversible action (migrations, external API writes,
  deletes), or plan.md conflicts with the code.
```

HOTL variant (Phase 2 last feature onward — this is the autonomous-
implementation mode referenced in Phase 2):

```text
Autonomous implementation mode. Target: L3 supervision.
- Implement the tasks in specs/<feature>/tasks.md in order.
- For each task: implement, run the mapped tests from the traceability
  table, and attach a one-line evidence statement ("test X passes,
  command Y output Z").
- Prefer CLI scaffolding/management commands over directly creating or
  editing files — especially for solutions, projects, and dependencies
  (e.g. `dotnet new sln|classlib|webapi|xunit`, `dotnet sln add`,
  `dotnet add reference`, `dotnet add package`; `npm create vite`;
  `npm install`). Edit generated files only to customize what the CLI
  cannot express.
- Work on a branch named `<type>/<short-kebab-description>` following
  Conventional Commits types (e.g. `feat/001-project-scaffold`,
  `fix/health-route-405`). If you inherit a branch with another style,
  rename it with `git branch -m` before committing.
- STOP and ask only when: a spec condition is ambiguous, a task requires
  an irreversible action (migrations, external API writes, deletes), or
  the design doc conflicts with the code.
- Everything else: proceed without asking. Log decisions to
  decisions.md with rationale instead of seeking approval.
- If a test cannot be made to pass without deviating from plan.md,
  update the spec and plan via a recorded delta — never silently.
End with a summary table: task → status → evidence → spec condition IDs.
```

### Mapping to what was removed

| Spec Kit command   | Replacement                                             |
| ------------------ | ------------------------------------------------------- |
| `/specify`         | spec-writing skill (already exists)                      |
| `/clarify`         | embedded in the skill's interview loop                  |
| `/plan`            | Prompt 1 above (+ research/data-model/contracts/quickstart) |
| `/tasks`           | Prompt 2 above (+ strict checklist format)              |
| `/implement`       | Prompt 3 above (HITL or HOTL variant)                   |
| setup scripts, extension hooks, checklists dir | dropped — the traceability table and Phase 1 CI carry the enforcement load |

Spec Kit itself remains a useful reference for prompt patterns
(repo: https://github.com/github/spec-kit, prompts under
`templates/commands/`) — but as reading material on craft, not a
runtime dependency.

---

## Updates Log

> **2026-10-26 — Phase 0 complete.** The diff findings
> (`learning/spec-comparison/comparison.md`) didn't just update the
> constitution — a **new spec-writing skill** was created from them. Its
> first output is `learning/spec-comparison/spec-writing-skill-spec.md`,
> evaluated against the comparison's recommendations in Section 8 of the
> comparison doc. The skill will be tweaked on future specs based on actual
> functionality rather than the initial project scaffold.

> **2026-10-27 — Spec Kit dependency removed.** Spec Kit assumed HITL
> gates at every step (`/specify` → `/plan` → `/tasks` → `/implement`,
> each pausing for human approval), which conflicts with the HOTL goal.
> The spec-writing skill (which embeds clarify-style questioning
> internally) replaces `/specify` + `/clarify`; the Prompt Library
> replaces `/plan`, `/tasks`, `/implement`. The prompts keep Spec Kit's
> artifact structure (plan.md, tasks.md, per-feature directory) but
> replace its pause-and-approve flow with explicit stop conditions and
> evidence statements. Phase 0 is unchanged — it records what actually
> happened, including the Spec Kit comparison.
