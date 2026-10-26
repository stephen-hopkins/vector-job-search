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

## Phase milestones

| Phase                                      | Status | Exit condition                                                 |
| ------------------------------------------ | ------ | -------------------------------------------------------------- |
| 0 – First spec (prompt vs `/specify` diff) | ✅      | Two draft specs exist + diff notes committed                   |
| 1 – Verifiability substrate                | ☐      | Every acceptance condition maps to a test ID; CI green         |
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
