#!/usr/bin/env bash
# task-closure-check.sh — enforce "Task closure is commit-atomic"
# (specs/process.md Merge Gates).
#
# Two checks:
#   1. Closure-atomicity: if the diff changes a file path named in an OPEN
#      task's text (tasks.md tasks carry explicit file paths), the same PR
#      must flip that task's checkbox from [ ] to [X].
#   2. Evidence: every checkbox flipped to [X] in this PR must carry an
#      evidence statement referencing a passing test ID (P0-... or similar).
#
# Modes:
#   advisory (default) — print violations as warnings, exit 0.
#   enforcing          — pass ENFORCE=1 (or --enforce); exit 1 on violations.
#
# Usage: bash tests/task-closure-check.sh [base-ref]
#        (default base-ref: origin/main; falls back to merge-base with HEAD)
#
# Status: advisory during shakedown (recorded in specs/process.md); flip to
# ENFORCE=1 in CI after 2-3 clean real PRs.

set -euo pipefail

ENFORCE="${ENFORCE:-0}"
if [ "${1:-}" = "--enforce" ]; then ENFORCE=1; shift; fi
BASE_REF="${1:-origin/main}"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

# Resolve base: prefer given ref, else fall back to merge-base so this works
# on feature branches whose remote ref may not exist locally.
if ! git rev-parse --verify --quiet "$BASE_REF^{commit}" >/dev/null 2>&1; then
  BASE_REF="$(git merge-base HEAD "$BASE_REF" 2>/dev/null || true)"
fi
if [ -z "${BASE_REF:-}" ]; then
  echo "task-closure-check: cannot resolve base ref — skipping (advisory mode only)"
  [ "$ENFORCE" = "1" ] && { echo "task-closure-check: no base ref in enforcing mode — FAIL"; exit 1; }
  exit 0
fi

changed_files=$(git diff --name-only "$BASE_REF"...HEAD 2>/dev/null | sort -u)
if [ -z "$changed_files" ]; then
  echo "task-closure-check: no changed files vs $BASE_REF — PASS (vacuous)"
  exit 0
fi

python3 - "$BASE_REF" <<'PYEOF'
import os, re, subprocess, sys

enforce = os.environ.get("ENFORCE", "0") == "1"
base_ref = sys.argv[1]

diff = subprocess.run(
    ["git", "diff", f"{base_ref}...HEAD", "--unified=0", "--", "specs"],
    capture_output=True, text=True,
).stdout.splitlines()

added_x_tasks = []   # tasks newly flipped to [X] in this PR
for line in diff:
    if line.startswith("+"):
        m = re.match(r"\+\s*-\s*\[X\]\s*(T[\d.]+)", line)
        if m:
            added_x_tasks.append(m.group(1))

changed = set(subprocess.run(
    ["git", "diff", "--name-only", f"{base_ref}...HEAD"],
    capture_output=True, text=True,
).stdout.split())

PATH_RE = re.compile(r"(?:src|tests|\.github|specs|README\.md|\.editorconfig|global\.json|VectorJobSearch\.slnx?)(?:/[\w.\-]+)*")

violations = []
import glob as globmod
for tasks_file in sorted(globmod.glob("specs/*/tasks.md")):
    try:
        lines = open(tasks_file).read().splitlines()
    except OSError:
        continue
    spec = tasks_file.split("/")[1]
    for line in lines:
        m = re.match(r"- \[ \] (T[\d.]+)", line)
        if not m:
            continue
        tid = m.group(1)
        named = {p.rstrip(".,;:)_\"'`") for p in PATH_RE.findall(line)}
        hit = sorted(named & changed)
        if hit:
            violations.append(
                f"{spec} {tid}: PR changes {', '.join(hit)} named in this OPEN task "
                f"but does not mark it [X] (specs/process.md: task closure is commit-atomic)")

# Evidence check: every newly-[X] task line must cite a test ID / evidence.
evidence_missing = []
for tasks_file in sorted(globmod.glob("specs/*/tasks.md")):
    for line in open(tasks_file).read().splitlines():
        m = re.match(r"- \[X\] (T[\d.]+)", line)
        if m and m.group(1) in added_x_tasks:
            if not re.search(r"(Evidence|evidence|PASS|passed|P\d+-)", line):
                evidence_missing.append(
                    f"{tasks_file} {m.group(1)}: flipped to [X] without an evidence statement")

problems = violations + evidence_missing
for p in problems:
    print(f"::warning task-closure-check::{p}" if not enforce else f"::error task-closure-check::{p}")
if problems:
    mode = "ENFORCING" if enforce else "ADVISORY (would fail once enforced)"
    print(f"task-closure-check: {len(problems)} violation(s) — {mode}")
    sys.exit(1 if enforce else 0)
print("task-closure-check: PASS — no open-task file overlap without closure; all new [X] carry evidence")
PYEOF
