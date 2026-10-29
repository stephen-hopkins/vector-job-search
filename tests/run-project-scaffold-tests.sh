#!/usr/bin/env bash
# Spec 001 scaffold verification suite (RED phase).
#
# Verifies every acceptance condition of specs/001-project-scaffold/spec.md that
# is a command exit code, file inspection, or `gh` query. Each test ID maps to
# exactly one acceptance condition (see tests/TEST-PLAN.md):
#
#   P0-BUILD-BACKEND          AC-1   dotnet build exits 0
#   P0-BUILD-FRONTEND         AC-2   README-documented frontend build command exits 0
#   P0-TYPECHECK-FRONTEND     AC-3   README-documented strict type-check command exits 0
#   P0-README-NOPLACEHOLDER   AC-4   no placeholders in README Getting Started
#   P0-TEST-BACKEND           AC-5   dotnet test exits 0 with >=1 passed
#   P0-TEST-FRONTEND          AC-6   documented frontend test command exits 0, >=1 passed, >=1 render test
#   P0-FORMAT-BACKEND         AC-7   dotnet format --verify-no-changes exits 0 (also covers E-5)
#   P0-EDITORCONFIG           AC-8   .editorconfig exists at repo root
#   P0-CI-TRIGGERS            AC-11, AC-12, AC-14  workflow on:/steps/runs-on:
#   P0-CI-GREEN               AC-13  gh run list shows success for HEAD commit
#   P0-CI-RED                 N-AC-1 gh run list shows failure for probe/p0-ci-red
#   P0-HERMETIC               N-AC-2 suites pass with outbound network blocked
#   P0-NO-TERRAFORM           N-AC-3 git ls-files '*.tf' is empty
#   P0-NO-SECRETS             N-AC-4 no key-shaped strings in tracked files
#   P0-FRONTEND-INSTALL-ORDER E-1    build fails without node_modules, passes after install
#   P0-DOTNET-SDK-PIN         E-4    backend/global.json exists and installed SDK satisfies it
#   P0-BUILD-NOWARN            SC-2   dotnet build -warnaserror exits 0
#   P0-CI-DURATION             SC-4   green CI run completes within 10 minutes
#
# Usage: tests/run-project-scaffold-tests.sh [test-id...]   (default: run all)

set -u
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
readonly REPO_ROOT
BACKEND_DIR="$REPO_ROOT"        # solution root is the repo root (VectorJobSearch.sln + global.json live here)
FRONTEND_DIR="$REPO_ROOT/src/web"
README="$REPO_ROOT/README.md"
WORKDIR="$(mktemp -d /tmp/opencode/p0-verify.XXXXXX)"   # scratch area for red-phase runs
VENV="$WORKDIR/venv"

pass_list=(); fail_list=(); skip_list=()

log()  { printf '  %s\n' "$*"; }
mark_pass() { pass_list+=("$1"); printf 'PASS  %s\n' "$1"; }
mark_fail() { fail_list+=("$1"); printf 'FAIL  %s — %s\n' "$1" "${2:-}"; }
mark_skip() { skip_list+=("$1"); printf 'SKIP  %s — %s\n' "$1" "${2:-}"; }

# Extract the command line from the first fenced bash block that follows an
# HTML gate marker in the README, e.g.:
#   <!-- gate: P0-BUILD-FRONTEND -->
#   ```bash
#   npm --prefix frontend run build
#   ```
# Returns the first command line of that block. Fails if the marker is absent.
readme_gate_command() {  # $1 = test id
  local test_id="$1"
  awk -v id="$test_id" '
    index($0, "<!-- gate: " id " -->") { ingate=1; next }
    ingate && /^```bash/ { inblock=1; next }
    inblock && /^```/ { exit }
    inblock && NF > 0 && !found { print; found=1 }
  ' "$README"
}

# Isolation for red-phase runs: run a command with outbound network blocked but
# loopback allowed (N-AC-2 semantics), if the environment supports it.
run_hermetic() {
  if unshare -rn true 2>/dev/null; then unshare -rn "$@"; else "$@"; fi
}

require_gh() { command -v gh >/dev/null 2>&1; }
require_unshare() { unshare -rn true 2>/dev/null; }

# ---------------------------------------------------------------- AC-1
P0-BUILD-BACKEND() {  # AC-1: dotnet build at the solution root exits 0
  [ -d "$BACKEND_DIR" ] || { mark_fail "$FUNCNAME" "backend/ not created yet"; return; }
  (cd "$BACKEND_DIR" && dotnet build >/dev/null 2>&1) \
    && mark_pass "$FUNCNAME" \
    || mark_fail "$FUNCNAME" "dotnet build in backend/ failed"
}

# ---------------------------------------------------------------- AC-2
P0-BUILD-FRONTEND() {  # AC-2: documented frontend production build exits 0
  local cmd
  cmd="$(readme_gate_command "$FUNCNAME")" || true
  [ -n "$cmd" ] || { mark_fail "$FUNCNAME" "README has no gate marker for $FUNCNAME"; return; }
  [ -d "$FRONTEND_DIR/node_modules" ] || { mark_fail "$FUNCNAME" "frontend dependencies not installed"; return; }
  log "running README command: $cmd"
  (cd "$REPO_ROOT" && eval "$cmd" >/dev/null 2>&1) \
    && mark_pass "$FUNCNAME" \
    || mark_fail "$FUNCNAME" "documented build command failed: $cmd"
}

# ---------------------------------------------------------------- AC-3
P0-TYPECHECK-FRONTEND() {  # AC-3: documented strict-mode type-check exits 0
  local cmd
  cmd="$(readme_gate_command "$FUNCNAME")" || true
  [ -n "$cmd" ] || { mark_fail "$FUNCNAME" "README has no gate marker for $FUNCNAME"; return; }
  [ -d "$FRONTEND_DIR/node_modules" ] || { mark_fail "$FUNCNAME" "frontend dependencies not installed"; return; }
  log "running README command: $cmd"
  (cd "$REPO_ROOT" && eval "$cmd" >/dev/null 2>&1) \
    && mark_pass "$FUNCNAME" \
    || mark_fail "$FUNCNAME" "documented type-check command failed: $cmd"
}

# ---------------------------------------------------------------- AC-4
P0-README-NOPLACEHOLDER() {  # AC-4: no placeholders in README Getting Started
  local matches
  [ -f "$README" ] || { mark_fail "$FUNCNAME" "README.md missing"; return; }
  matches="$(sed -n '/^## Getting Started/,/^## /p' "$README" \
    | grep -n -i -e 'TBC' -e 'coming soon' -e 'TBD' || true)"
  if [ -n "$matches" ]; then mark_fail "$FUNCNAME" "placeholders found: $matches"
  else mark_pass "$FUNCNAME"; fi
}

# ---------------------------------------------------------------- AC-5
P0-TEST-BACKEND() {  # AC-5: dotnet test exits 0 and reports >= 1 passed
  local output
  [ -d "$BACKEND_DIR" ] || { mark_fail "$FUNCNAME" "backend/ not created yet"; return; }
  output="$(cd "$BACKEND_DIR" && dotnet test 2>&1)" \
    || { mark_fail "$FUNCNAME" "dotnet test failed: $(echo "$output" | tail -n 2)"; return; }
  echo "$output" | grep -Eq 'Passed!|Passed: *[1-9][0-9]*|✔' \
    && mark_pass "$FUNCNAME" \
    || mark_fail "$FUNCNAME" "no passing tests reported"
}

# ---------------------------------------------------------------- AC-6
P0-TEST-FRONTEND() {  # AC-6: documented frontend test command exits 0, >=1 passed, >=1 render test
  local cmd output render_hits
  cmd="$(readme_gate_command "$FUNCNAME")" || true
  [ -n "$cmd" ] || { mark_fail "$FUNCNAME" "README has no gate marker for $FUNCNAME"; return; }
  [ -d "$FRONTEND_DIR/node_modules" ] || { mark_fail "$FUNCNAME" "frontend dependencies not installed"; return; }
  log "running README command: $cmd"
  output="$(cd "$REPO_ROOT" && eval "$cmd" 2>&1)" \
    || { mark_fail "$FUNCNAME" "documented test command failed: $(echo "$output" | tail -n 2)"; return; }
  echo "$output" | grep -Eq '[1-9][0-9]* (passed|passed,)' || echo "$output" | grep -Eq 'Test Files\s+[1-9][0-9]* passed' \
    || { mark_fail "$FUNCNAME" "no passing frontend tests reported"; return; }
  render_hits="$(grep -rE "from ['\"]@testing-library/react['\"]" "$FRONTEND_DIR/src" --include='*.tsx' --include='*.ts' -l 2>/dev/null | xargs -r grep -l 'render(' 2>/dev/null || true)"
  [ -n "$render_hits" ] \
    && mark_pass "$FUNCNAME" \
    || mark_fail "$FUNCNAME" "no React Testing Library render test found"
}

# ---------------------------------------------------------------- AC-7 / E-5
P0-FORMAT-BACKEND() {  # AC-7: dotnet format --verify-no-changes exits 0
  [ -d "$BACKEND_DIR" ] || { mark_fail "$FUNCNAME" "backend/ not created yet"; return; }
  (cd "$BACKEND_DIR" && dotnet format --verify-no-changes >/dev/null 2>&1) \
    && mark_pass "$FUNCNAME" \
    || mark_fail "$FUNCNAME" "dotnet format --verify-no-changes failed"
}

# ---------------------------------------------------------------- AC-8
P0-EDITORCONFIG() {  # AC-8: .editorconfig exists at repo root
  [ -f "$REPO_ROOT/.editorconfig" ] \
    && mark_pass "$FUNCNAME" \
    || mark_fail "$FUNCNAME" ".editorconfig missing at repo root"
}

# ---------------------------------------------------------------- AC-11,12,14
P0-CI-TRIGGERS() {  # AC-11+AC-12+AC-14: workflow definition inspection
  local wf found
  found="$(find "$REPO_ROOT/.github/workflows" -type f 2>/dev/null | head -n 1)"
  [ -n "$found" ] || { mark_fail "$FUNCNAME" "no workflow file under .github/workflows/"; return; }
  wf="$found"
  # AC-11: triggers on push AND pull_request
  grep -Eq '^\s*(push|pull_request):' "$wf" || { mark_fail "$FUNCNAME" "workflow missing push/pull_request triggers"; return; }
  grep -Eq '^\s*push:' "$wf" && grep -Eq '^\s*pull_request:' "$wf" \
    || { mark_fail "$FUNCNAME" "workflow must trigger on both push and pull_request"; return; }
  # AC-12: steps for all five gates, identified by their P0 test IDs in comments or step names
  local gate
  for gate in P0-BUILD-BACKEND P0-TEST-BACKEND P0-FORMAT-BACKEND P0-BUILD-FRONTEND P0-TYPECHECK-FRONTEND P0-TEST-FRONTEND; do
    grep -q "$gate" "$wf" || { mark_fail "$FUNCNAME" "workflow missing step for $gate"; return; }
  done
  # AC-14: GitHub-hosted runners only (ubuntu-latest and friends; no self-hosted)
  local runs_on bad
  runs_on="$(grep -E '^\s*runs-on:' "$wf")"
  [ -n "$runs_on" ] || { mark_fail "$FUNCNAME" "no runs-on found"; return; }
  bad="$(echo "$runs_on" | grep -v -E 'ubuntu-(latest|[0-9]+)|windows-[0-9]+|macos-(latest|[0-9]+)' || true)"
  [ -z "$bad" ] && mark_pass "$FUNCNAME" \
    || mark_fail "$FUNCNAME" "non-GitHub-hosted runner: $bad"
}

# ---------------------------------------------------------------- AC-13
P0-CI-GREEN() {  # AC-13: workflow run for scaffold (HEAD) commit concludes success
  local sha status
  require_gh || { mark_skip "$FUNCNAME" "gh CLI not available"; return; }
  sha="$(git -C "$REPO_ROOT" rev-parse HEAD)"
  status="$(gh run list --repo "$(git -C "$REPO_ROOT" remote get-url origin)" \
      --commit "$sha" --json conclusion --limit 1 --jq '.[0].conclusion' 2>/dev/null || true)"
  [ "$status" = "success" ] && mark_pass "$FUNCNAME" \
    || mark_fail "$FUNCNAME" "latest run for commit ${sha:0:8} not 'success' (got '${status:-none}')"
}

# ---------------------------------------------------------------- N-AC-1
P0-CI-RED() {  # N-AC-1: a run with an intentionally failing test reports failure
  local status
  require_gh || { mark_skip "$FUNCNAME" "gh CLI not available"; return; }
  status="$(gh run list --repo "$(git -C "$REPO_ROOT" remote get-url origin)" \
      --branch probe/p0-ci-red --json conclusion --limit 1 --jq '.[0].conclusion' 2>/dev/null || true)"
  [ "$status" = "failure" ] && mark_pass "$FUNCNAME" \
    || mark_fail "$FUNCNAME" "expected a 'failure' run on branch probe/p0-ci-red (got '${status:-none}') — see TEST-PLAN.md probe procedure"
}

# ---------------------------------------------------------------- N-AC-2
P0-HERMETIC() {  # N-AC-2: full suites pass with outbound network blocked
  local status
  require_unshare || { mark_skip "$FUNCNAME" "unshare not available (needs userns)"; return; }
  # Green phase: run the real backend and frontend test suites with outbound
  # network blocked. Loopback is raised explicitly (a fresh netns starts with
  # lo DOWN; the in-memory test client and local testhost sockets need it).
  # Both suites must exit 0, proving no test calls an external API.
  # Note: assumes NuGet/npm caches are warm (dotnet restore outside the netns
  # would need network); N-AC-2 targets test behavior, not package restore.
  local log="$WORKDIR/hermetic.log"
  unshare -rn bash -c \
    "ip link set lo up && cd '$REPO_ROOT' && dotnet test >'$log' 2>&1 && npm --prefix src/web test >>'$log' 2>&1"
  status=$?
  [ $status -eq 0 ] && mark_pass "$FUNCNAME" \
    || { cat "$log"; mark_fail "$FUNCNAME" "suites failed under network isolation (exit $status)"; }
}

# ---------------------------------------------------------------- N-AC-3
P0-NO-TERRAFORM() {  # N-AC-3: git ls-files '*.tf' returns an empty list
  local tf
  tf="$(git -C "$REPO_ROOT" ls-files '*.tf')"
  [ -z "$tf" ] && mark_pass "$FUNCNAME" \
    || mark_fail "$FUNCNAME" "Terraform files tracked: $tf"
}

# ---------------------------------------------------------------- N-AC-4
P0-NO-SECRETS() {  # N-AC-4: no key-shaped strings in tracked files
  local hits
  hits="$(git -C "$REPO_ROOT" grep -lE \
    'sk-[A-Za-z0-9]{20,}|AKIA[0-9A-Z]{16}|ghp_[A-Za-z0-9]{36}|-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----' \
    -- 2>/dev/null || true)"
  [ -z "$hits" ] && mark_pass "$FUNCNAME" \
    || mark_fail "$FUNCNAME" "key-shaped strings in: $hits"
}

# ---------------------------------------------------------------- E-1
P0-FRONTEND-INSTALL-ORDER() {  # E-1: build fails without node_modules, passes after install
  local cmd install_cmd rc
  cmd="$(readme_gate_command P0-BUILD-FRONTEND)" || true
  [ -n "$cmd" ] || { mark_fail "$FUNCNAME" "README has no gate marker for P0-BUILD-FRONTEND"; return; }
  [ -d "$FRONTEND_DIR" ] || { mark_fail "$FUNCNAME" "frontend/ not created yet"; return; }
  if [ ! -d "$FRONTEND_DIR/node_modules" ]; then
    # Part 1: build WITHOUT dependencies must fail.
    (cd "$REPO_ROOT" && eval "$cmd" >/dev/null 2>&1) \
      && mark_fail "$FUNCNAME" "build unexpectedly succeeded without node_modules" \
      || { log "part 1 ok: build fails without node_modules"; mark_fail "$FUNCNAME" "RED: install step not run yet (green phase completes part 2)"; }
    return
  fi
  # Part 2 (with node_modules present): documented install command must restore a clean checkout.
  install_cmd="$(readme_gate_command "$FUNCNAME")" || true
  [ -n "$install_cmd" ] || { mark_fail "$FUNCNAME" "README has no gate marker for $FUNCNAME"; return; }
  rm -rf "$FRONTEND_DIR/node_modules"
  (cd "$REPO_ROOT" && eval "$install_cmd" >/dev/null 2>&1) || { mark_fail "$FUNCNAME" "documented install command failed"; return; }
  (cd "$REPO_ROOT" && eval "$cmd" >/dev/null 2>&1) \
    && mark_pass "$FUNCNAME" \
    || mark_fail "$FUNCNAME" "build failed after documented install step"
}

# ---------------------------------------------------------------- E-4
P0-DOTNET-SDK-PIN() {  # E-4: backend/global.json exists and installed SDK satisfies it
  local gj version
  gj="$BACKEND_DIR/global.json"
  [ -f "$gj" ] || { mark_fail "$FUNCNAME" "backend/global.json missing"; return; }
  version="$(grep -oE '"version"[[:space:]]*:[[:space:]]*"[^"]+"' "$gj" | grep -oE '[0-9]+\.[0-9]+\.[0-9]+')"
  [ -n "$version" ] || { mark_fail "$FUNCNAME" "global.json has no usable version pin"; return; }
  dotnet --list-sdks | grep -q "^$version" \
    && mark_pass "$FUNCNAME" \
    || mark_fail "$FUNCNAME" "installed SDKs do not include pinned $version"
}

# ---------------------------------------------------------------- SC-2
P0-BUILD-NOWARN() {  # SC-2: dotnet build -warnaserror exits 0
  [ -d "$BACKEND_DIR" ] || { mark_fail "$FUNCNAME" "backend/ not created yet"; return; }
  (cd "$BACKEND_DIR" && dotnet build -warnaserror >/dev/null 2>&1) \
    && mark_pass "$FUNCNAME" \
    || mark_fail "$FUNCNAME" "build produced warnings (-warnaserror failed)"
}

# ---------------------------------------------------------------- SC-4
P0-CI-DURATION() {  # SC-4: green CI run for HEAD completes within 10 minutes
  local sha run_id started ended elapsed
  require_gh || { mark_skip "$FUNCNAME" "gh CLI not available"; return; }
  sha="$(git -C "$REPO_ROOT" rev-parse HEAD)"
  run_id="$(gh run list --repo "$(git -C "$REPO_ROOT" remote get-url origin)" \
      --commit "$sha" --json databaseId --limit 1 --jq '.[0].databaseId' 2>/dev/null || true)"
  [ -n "$run_id" ] || { mark_fail "$FUNCNAME" "no CI run found for HEAD commit"; return; }
  read -r started ended < <(gh run view "$run_id" --json startedAt,updatedAt --jq '[.startedAt, .updatedAt] | join(" ")' 2>/dev/null)
  elapsed=$(( $(date -d "$ended" +%s) - $(date -d "$started" +%s) ))
  [ "$elapsed" -le 600 ] && mark_pass "$FUNCNAME" \
    || mark_fail "$FUNCNAME" "run took ${elapsed}s (> 600s)"
}

ALL_TESTS=(
  P0-BUILD-BACKEND
  P0-BUILD-FRONTEND
  P0-TYPECHECK-FRONTEND
  P0-README-NOPLACEHOLDER
  P0-TEST-BACKEND
  P0-TEST-FRONTEND
  P0-FORMAT-BACKEND
  P0-EDITORCONFIG
  P0-CI-TRIGGERS
  P0-CI-GREEN
  P0-CI-RED
  P0-HERMETIC
  P0-NO-TERRAFORM
  P0-NO-SECRETS
  P0-FRONTEND-INSTALL-ORDER
  P0-DOTNET-SDK-PIN
  P0-BUILD-NOWARN
  P0-CI-DURATION
)

selected=("$@")
[ ${#selected[@]} -eq 0 ] && selected=("${ALL_TESTS[@]}")

printf 'Spec 001 scaffold verification (RED phase) — %d test(s)\n\n' "${#selected[@]}"
for t in "${selected[@]}"; do
  if declare -F "$t" >/dev/null; then "$t"; else mark_fail "$t" "unknown test id"; fi
done

printf '\nSummary: %d passed, %d failed, %d skipped\n' \
  "${#pass_list[@]}" "${#fail_list[@]}" "${#skip_list[@]}"
[ ${#fail_list[@]} -eq 0 ] || exit 1
