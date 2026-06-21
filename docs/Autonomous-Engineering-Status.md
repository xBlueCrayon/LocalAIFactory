# Autonomous Engineering — Honest Status

The honest status of autonomous (AI-assisted) code engineering in LocalAIFactory. The capability is
real and **safe by construction**, but deliberately conservative: it produces **governed proposals**,
runs only **allowlisted** commands, **halts on failure**, **never self-promotes**, and **never
commits**. It is **proven on a synthetic workspace, not yet on a real repository.**

> Mechanism detail and the claim-by-claim safety proof:
> [`Autonomous-Local-Fix-Loop-Proof.md`](Autonomous-Local-Fix-Loop-Proof.md). Operator runbook:
> [`Controlled-Autonomous-Engineering-Runbook.md`](Controlled-Autonomous-Engineering-Runbook.md).
> Authoritative source: `MASTER_VISION.md`.

---

## 1. What is implemented (and safe)

Three layers, all in `src/LocalAIFactory.Workspaces/Autonomy/` with types in
`src/LocalAIFactory.Core/Abstractions/IAutonomousWorkspace.cs`:

### CommandPolicy (allow / deny / approval)

`CommandPolicy.Classify(command)` is **deny-by-severity, default-deny**:

- **Denied (never planned, never run):** `git reset --hard`, `git clean -f[d]`, force-push,
  `git rebase`, `git filter-branch`, `git merge`, `rm -rf`, recursive force deletes,
  `drop database`/`drop table`/`truncate`, disk format/`dd`/`diskpart`, `shutdown`, `iisreset`,
  `netsh`/firewall changes, registry/service deletion, production-deploy strings, fork bombs.
- **RequiresApproval (never run autonomously):** `git commit`/`git push`/`git tag`,
  `dotnet ef migrations`/`dotnet ef database`, and **anything not on the allowlist**.
- **Allowed (safe to run):** `dotnet build|test|restore`, the benchmark runner, read-only `git`
  (`status/diff/log/fetch/branch/rev-parse/show`), and `ls/dir/cat/type/echo`.

### ControlledExecutor (halts on failure, never promotes)

Runs only **Allowed** commands; denied/approval-gated commands are recorded and **skipped, never
run**. It **stops promoting after the first non-zero exit** and returns `Promoted: false` — it cannot
elevate its own approval state.

### LocalFixLoop (isolated workspace, rollback-on-failure, never commits)

`LocalFixLoop.Run(...)` is the most cautious automation in the system:

- **Dry-run by default** — with `execute == false` it touches no file and executes nothing; it only
  reports the plan ("apply N patches, run M checks, then HALT for human approval before any commit").
- **Isolated workspace only** — all patch paths resolve under the request's workspace root; a path
  that escapes the root throws and triggers rollback (zip-slip-style writes rejected).
- **Allowlisted checks only** — checks run through `ControlledExecutor`.
- **On success** the patch is **retained but PENDING HUMAN APPROVAL** — explicitly **not** committed
  or pushed.
- **On any check failure or exception** all patches are **rolled back** (originals restored, created
  files deleted), workspace left exactly as found.
- **`Committed` is always false** — there is no commit/push/merge code path, and `CommandPolicy`
  independently denies merges/force-pushes/history rewrites and gates commit/push/tag, so even a check
  string cannot sneak a commit through.

---

## 2. What it does NOT do

- It does **not** commit, push, merge, or tag — ever.
- It does **not** run anything outside the allowlist; default-deny.
- It does **not** self-approve or self-promote.
- It does **not** modify anything outside the isolated workspace.
- It does **not** yet close a genuine "diagnose → apply fix → verify → roll back on failure" loop
  against **real code**.

---

## 3. Proven on a synthetic workspace, not a real repo

The behaviour above is covered by **5 passing tests** for the fix loop (`LocalFixLoopTests`) and **8**
for licensing (`LicensingTests`), inside the full suite (**235 tests, 0 failures** this release). The
tests exercise dry-run vs execute, patch-apply + retain-on-pass, rollback-on-check-failure (including
deleting created files), and path-escape rejection — but they run against a **synthetic workspace**
with an injected `CommandRunner`, not a real defect in a real repository.

So: the **mechanism is safe and tested**; the **end-to-end value on a real repo is not yet
demonstrated**. The loop is a building block for controlled autonomy, not a finished autonomous
engineer.

---

## 4. Operator entry points

- `scripts/auto/plan-change.ps1` — plan a change set.
- `scripts/auto/run-approved-local-checks.ps1` — run allowlisted checks.
- `scripts/auto/run-local-fix-loop.ps1` — the documented fix-loop wrapper, **dry-run by default**,
  requiring an explicit flag to execute; it surfaces the `FixLoopResult` (applied / checks-passed /
  rolled-back) and makes clear any commit is a separate, human-approved step. The wrapper **cannot
  loosen** the guarantees — the policy and the loop enforce them regardless of the script.
- `scripts/auto/summarize-run.ps1` — summarise a run.

Runs and approvals are recorded in the `AuditEvent` trail
([`Controlled-Autonomous-Engineering-Runbook.md`](Controlled-Autonomous-Engineering-Runbook.md)).

---

## 5. Exact proof to ship

To advance autonomous engineering from "safe mechanism" to "demonstrated on real code" (mirrors
[`Known-Limitations.md`](Known-Limitations.md) §6):

- An **end-to-end run on a real defect** in a real repository that:
  1. applies a change to an **isolated workspace**,
  2. **verifies** it via build + tests through `ControlledExecutor` (allowlisted only),
  3. **automatically rolls back on failure**,
  4. **halts for human approval** before any commit (which remains a separate, gated, audited step),
- all of it **captured in logs**, and entirely **within the existing command-policy, approval, and
  audit constraints** — nothing auto-committed, nothing auto-promoted.

Until that captured run exists, the position is: the controlled executor and fix loop run safely; the
real-defect end-to-end proof is still owed. No claim is made that the system autonomously fixes
production code.
