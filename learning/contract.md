# contract.md

> Meta-spec for the learning itself. This file is a contract, not a diary.
> If the plan drifts, update this file FIRST, then the plan (learning/plan.md).

## Objective

Learn advanced agentic engineering by building <PROJECT NAME>, with the
end-state goal of operating the project in a **human-on-the-loop (HOTL)**
mode: agents execute end-to-end in bounded domains; I supervise via
evidence, dashboards, and surgical intervention — not per-step approval.

## Definition of "human on the loop" for this project

The agent may plan, execute, verify, and merge changes from a spec without
asking me first, PROVIDED:

- Every action maps to a spec condition with mechanical proof (test ID or
  schema check), and
- Every action is classed per the policy tier table (see constitution /
  POLICY.md), and irreversible actions still escalate.

I intervene when signals warrant, not because I am watching.

## Success criteria (measurable, checked monthly)

| #   | Criterion                                                                               | Evidence                                 |
| --- | --------------------------------------------------------------------------------------- | ---------------------------------------- |
| S1  | 3+ features have specs I can trace to tests bidirectionally                             | traceability table in repo               |
| S2  | ≥70% of agent tasks merged in a week with zero prior human approval of individual steps | merge log sampled                        |
| S3  | Intervention log shows I can name the signal that caught each agent failure             | decisions.md / interventions.md          |
| S4  | Reviewer agent catches an injected spec drift in a drill test                           | drill result committed                   |
| S5  | Autonomy per action class matches the policy tier table (not gut feel)                  | tier audit notes                         |
| S6  | I can rewrite the Phase 0 prompt from memory, with the _reasons_ for each constraint    | this is a self-check, no artifact needed |

## Learning log format (append-only)

Each entry:

### YYYY-MM-DD — Phase <n> — <what happened>

- What I expected:
- What happened:
- Signal: (for interventions — what told me to step in?)
- Lesson: (one sentence. If none, say "no lesson" — that's allowed.)
- Change to constitution/specs: (or "none")

## Learning log (append-only)

### 2026-10-29 — Phase 1 — agent hand-edited files instead of using dotnet CLI

- What I expected: scaffold tasks (solution, projects, references, packages) would be created via dotnet CLI commands (`dotnet new sln|classlib|xunit`, `dotnet sln add`, `dotnet add reference/package`), as the tasks.md conventions required.
- What happened: the agent directly authored/edited files where CLI commands were the instructed mechanism.
- Signal: review of the scaffold commits showed hand-edited artifacts where the CLI should have been the only path.
- Lesson: instructions that live only in the session prompt don't survive — tool-preference rules must be embedded in a reusable agent skill.
- Change to constitution/specs: none yet — fix was to edit the implementation-mode prompt; likely to be folded into a new agent skill.

### 2026-10-29 — Phase 1 — branch name used spec-<id> style, not semantic format

- What I expected: the working branch to follow the repo's Conventional-Commits-derived naming (`<type>/<short-kebab-description>`, e.g. `feat/001-project-scaffold`), matching the committer agent's branch convention.
- What happened: the implementation session ran on a branch named `spec-001/scaffold` — a spec-Kit-style name that never matched the convention.
- Signal: branch name visible in the commit review didn't match the pattern used by all merged PRs.
- Lesson: branch-naming rules belong in the implementation prompt (and eventually the agent skill), not just in the committer agent — the implementing session is the one that creates the branch.
- Change to constitution/specs: none — fix was to add a branch-naming bullet to the implementation-mode prompt in learning/plan.md (both HITL and HOTL variants); likely to be folded into the same future agent skill.

### 2026-10-29 — Phase 1 — constitution rule conflated task closure with PR merge

- What I expected: the spec–test agreement rule could be stated as "no merge closes a task".
- What happened: functionality arrives via PRs, but tasks are closed by `[X]` marks in tasks.md — a task can span PRs. Stating the rule in terms of merge conflated the bookkeeping action with the git event.
- Signal: reviewing the drafted specs/process.md — the word "merge" was doing double duty.
- Lesson: process rules must name their actual enforcement point (task closure via evidence statements; PR merge via CI) instead of borrowing a nearby event's name.
- Change to constitution/specs: specs/process.md rewritten to gate `[X]` marking on passing mapped tests; "merge" retained only where literally a PR merge.

### 2026-11-04 — Phase 1 — Superpowers takeover: tasks.md frozen at 2/24 while all work completed

- What I expected: the implementing session would keep marking tasks `[X]` in `specs/001-project-scaffold/tasks.md` per the implement prompt and specs/process.md.
- What happened: phases 1–2 followed the prompt (T2.1/T2.2 ticked with evidence). From phase 3 the session loaded the Superpowers `executing-plans` skill, which mandates its own progress ledger — `.superpowers/sdd/<plan>/progress.md` appeared in commit 3ce3949 and every subsequent phase commit updated it while tasks.md was never touched (stale at 2/24 until manually reconciled). The skill's hard-coded artifact silently outcompeted both the session prompt and the constitution.
- Signal: git history — tasks.md diffs go to zero after 1813c16 while ledger commits continue through 54974d1; ledger created retroactively in past tense for phases 1–2.
- Lesson: a loaded skill beats a session prompt and an unenforced constitution every time — imported process tooling must be either aligned with the repo's declared artifacts or gated by a deterministic check, never trusted to coexist.
- Change to constitution/specs: specs/process.md gains "Task closure is commit-atomic" (mark `[X]` in the same commit/PR that completes the task; external ledgers are additive, never substitutes). Superpowers plugin uninstalled (see next entry).

### 2026-11-04 — Phase 1 — Superpowers uninstalled

- What I expected: Superpowers would supply in-session TDD enforcement without changing the artifact flow.
- What happened: its plan-execution skill rewired progress reporting into its own ledger and displaced tasks.md closure (previous entry); TDD enforcement was redundant with the repo's own red-green gates (traceability table + shell suite + CI).
- Signal: the phase-3+ commits showing ledger-instead-of-tasks.md divergence; no Superpowers-enforced behavior the repo's own tests didn't already enforce.
- Lesson: adopt external process frameworks only where they enforce something the repo doesn't already verify mechanically — otherwise their artifact conventions collide with yours at the session boundary.
- Change to constitution/specs: superpowers plugin removed from ~/.config/opencode/opencode.json; learning/plan.md Phase 1 step 1 no longer active (TDD enforcement carried by the spec-driven gates + CI); .superpowers/ ledger retained read-only as historical evidence.

### 2026-11-04 — Phase 1 — task-closure CI check added (advisory)

- What I expected: the new "task closure is commit-atomic" rule would need a deterministic gate to survive the next session boundary, per the Phase 3 principle (policy gates, not vigilance).
- What happened: added `tests/task-closure-check.sh` — parses open tasks in `specs/*/tasks.md`, extracts the file paths each task names, and flags any PR that changes those paths without flipping the task `[X]`; also requires every newly-`[X]` task to carry an evidence statement. Wired into CI as a first, cheap job in ADVISORY mode (warnings, exit 0); first run on this branch correctly flagged the 11 evidence-less `[X]` flips from the manual reconciliation.
- Signal: n/a (proactive; implements the lesson of the takeover entry).
- Lesson: no lesson — the advisory run immediately caught a real violation (my own reconciliation commit lacked per-task evidence), validating the check before enforcement.
- Change to constitution/specs: specs/process.md merge gate now names the check; flip to ENFORCE=1 after 2-3 clean real PRs.

### 2026-11-18 — Phase 1 — spec 002: AC mandated a registry-by-name where only config validation was a real behavior

- What I expected: the draft's B1 would contain only user-observable behaviors for the embedding abstraction.
- What happened: AC-1.2 required that "requesting a provider name not in the registry of known providers produces a startup validation error naming the accepted values." Nothing in the system requests a provider by name at runtime — the only input is the `embedding_provider` config value, read once at startup. A lookup-by-name registry is just one implementation of resolving that config value (a switch statement satisfies the same observable behavior), so the AC mandated HOW. It was also redundant: the observable behavior (startup fails on an unknown provider name) is config validation, owned by B5/AC-5.2.
- Signal: human review of the draft spec — I asked "why request by provider name here?" No automated signal existed. The spec skill's quality gate checks that each AC is mechanically verifiable, not that it is minimal or traces to a real requirement, so a redundant-but-verifiable AC passes every gate. The tell in hindsight: AC-1.2 referenced "known providers"/"accepted values" — a set that only exists because of B5's config schema. A behavior block referencing another block's data is a smell that it's derived, not primary.
- Lesson: an AC written in terms of a data structure ("the registry") instead of an input ("the config value") is HOW, not WHAT — check each AC names the real input that triggers it, and that no two ACs verify the same observable behavior from different blocks.
- Change to constitution/specs: specs/002-embedding-provider/spec.md — AC-1.2/T-1.2 deleted; the unknown-provider-name permutation folded into AC-5.2 (config block) with T-5.2; Edge Cases pointer updated; success criterion count 16→16→15... corrected to 15. No constitution change yet — candidate rule for the spec skill's quality gate if this recurs.

| Phase                                      | Status | Exit condition                                                 |
| ------------------------------------------ | ------ | -------------------------------------------------------------- |
| 0 – First spec (prompt vs `/specify` diff) | ✅      | Two draft specs exist + diff notes committed                   |
| 1 – Verifiability substrate                | ✅      | Every acceptance condition maps to a test ID; CI green         |
| 2 – Gated loop (L1/L2)                     | ☐      | 4–5 features done; intervention log has ≥10 entries            |
| 3 – Reviewer agent + policy tiers          | ☐      | Reviewer agent blocks/enforces on 3 real PRs; S2 ≥70% sustained for one week |
| 4 – Oversight surface (dashboards)         | ☐      | Drift drill passes; escalation alerting works                  |
| 5 – Sustainable HOTL (L3/L4 instincts)     | ☐      | One feature taken from one-line intent, gated only by reviewer |

## Rules for myself

1. No phase exit without the evidence in the table above. "It felt fine"
   is not evidence.
2. When I intervene, I MUST log the signal. Unlogged interventions are
   lost tuition.
3. Spec drift is fixed by editing the spec or the code — never by
   forgetting one of them.
4. If I'm approval-fatigued, the answer is a better tier policy, not
   more or fewer approvals by default.
5. Measure success by how often I'm _in the right loop_, not by how
   rarely I'm in any loop.

## Intentionally removed

(Record here what you've deleted from the plan once internalised — e.g.
the Phase 0 bootstrap prompt — with the date and why. Scaffolding earns
its removal.)
