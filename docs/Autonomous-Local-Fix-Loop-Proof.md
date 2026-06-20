# Autonomous Local Fix Loop — Safety Proof

How the `LocalFixLoop` works and why it is safe. This is the most cautious possible automation: it
applies a patch set to an **isolated workspace**, runs **allowlisted checks only**, **rolls back on
any failure**, and **never commits or pushes**. It defaults to **dry-run**. The loop is a building
block for controlled autonomy — it does not, on its own, close the "real fix/rollback loop on
production code" gap (`docs/Known-Limitations.md` §6).

Source: `src/LocalAIFactory.Workspaces/Autonomy/LocalFixLoop.cs`, with
`ControlledExecutor` + `CommandPolicy` (`src/LocalAIFactory.Workspaces/Autonomy/`). Types in
`src/LocalAIFactory.Core/Abstractions/IAutonomousWorkspace.cs`. Covered by **5 tests** (with a
further 8 for licensing).

---

## 1. What it does

`LocalFixLoop.Run(ChangeRequest request, IReadOnlyList<FilePatch> patches, IReadOnlyList<string> checks,
bool execute, CommandRunner runner)` returns a `FixLoopResult`:

1. **Dry-run by default** (`execute == false`): nothing is modified and nothing is executed. It
   reports the plan ("apply N patches, run M checks, then HALT for human approval before any commit")
   and classifies the checks through the policy. Returns `DryRun: true, PatchApplied: false,
   Committed: false`.
2. **Execute mode** (`execute == true`):
   - Validates the target workspace exists; if not, applies nothing.
   - Applies each `FilePatch` to the **isolated workspace**, recording a backup of each file's prior
     content (or that it did not exist).
   - Runs the `checks` through `ControlledExecutor` (allowlisted build/test/read only).
   - **On success:** the patch is **retained but PENDING HUMAN APPROVAL** —
     `ChecksPassed: true, RolledBack: false, Committed: false`. It is explicitly **not** committed or
     pushed; promotion to a commit is a separate, human-approved step.
   - **On check failure or any exception:** all patches are **rolled back** (originals restored,
     created files deleted) and `RolledBack: true, Committed: false` is returned.

`Committed` is **always false** — the loop has no code path that commits, pushes, or merges.

---

## 2. Safety proof — claim by claim

| Safety claim | How it is guaranteed (in code) |
|---|---|
| **Dry-run by default** | When `execute == false`, the method returns before touching any file and only runs `ControlledExecutor` in dry-run (which executes nothing). |
| **Isolated workspace only** | All patch paths are resolved under `Path.GetFullPath(workingDir)` (the request's `TargetRepoPath`); checks run with that directory as the working directory. |
| **Path-escape rejection** | Each resolved patch path must `StartsWith` the workspace root (ordinal, case-insensitive); otherwise it throws `InvalidOperationException("Patch path escapes the workspace…")` and triggers rollback. Prevents zip-slip-style writes outside the sandbox. |
| **Allowlisted checks only** | Checks run through `ControlledExecutor`, which only ever executes commands the `CommandPolicy` classifies as **Allowed** (build/test/read). Denied and approval-gated commands are recorded and skipped, never run. |
| **Halt on first failure** | `ControlledExecutor` stops promoting after the first non-zero exit; the loop treats not-all-passed as failure and rolls back. |
| **Rollback on any failure** | On failed checks **or** any exception, `Rollback(...)` restores every backed-up file (or deletes files that did not previously exist), in reverse order, best-effort. The workspace is left exactly as found. |
| **Never commits / pushes / merges** | There is no commit/push/merge code path; `Committed` is always false. `CommandPolicy` independently denies `git merge`, force-push, history rewrites, and gates `git commit`/`git push`/`git tag` behind approval — so even a check string can't sneak a commit through. |
| **Never self-promotes** | `ControlledExecutor` returns `Promoted: false` and cannot elevate its own approval state. |
| **Testable without real processes** | The `CommandRunner` is injected, so the loop is exercised deterministically in tests without spawning processes. |

---

## 3. Command policy backing the loop

`CommandPolicy.Classify(command)` is deny-by-severity:

- **Denied (never planned, never run):** `git reset --hard`, `git clean -f[d]`, force-push, `git
  rebase`, `git filter-branch`, `git merge`, `rm -rf`, recursive force deletes, `drop database` /
  `drop table` / `truncate`, disk format/`dd`/`diskpart`, `shutdown`, `iisreset`, `netsh`/firewall
  changes, registry/service deletion, production-deploy strings, fork bombs.
- **RequiresApproval (never run autonomously):** `git commit` / `git push` / `git tag`,
  `dotnet ef migrations` / `dotnet ef database`, and **anything not on the allowlist** (default-deny).
- **Allowed (safe to run):** `dotnet build|test|restore`, the benchmark runner, read-only `git`
  (`status/diff/log/fetch/branch/rev-parse/show`), and `ls/dir/cat/type/echo`.

---

## 4. Tests

The behaviour above is covered by **5 passing tests** for the fix loop (`LocalFixLoopTests`) and a
further **8** for licensing (`LicensingTests`), included in the full suite (235 tests, ~1 s, 0
failures this sprint — `docs/Resource-and-Performance-Evidence.md`). The tests exercise dry-run vs
execute, patch apply + retain-on-pass, rollback-on-check-failure (including deleting created files),
and path-escape rejection.

---

## 5. Operator entry point

`scripts/auto/run-local-fix-loop.ps1` — **intended** operator wrapper to invoke the fix loop with a
change request, a patch set, and a list of checks, **dry-run by default** and requiring an explicit
flag to execute. Its job is to surface the `FixLoopResult` (applied / checks-passed / rolled-back,
and the safety notes) to the operator and to make clear that any commit is a separate, human-approved
step.

> Note: this script is referenced here as the documented entry point; its implementation is being
> added separately (by the author or a separate agent). The safety guarantees above live in the
> `LocalFixLoop` / `ControlledExecutor` / `CommandPolicy` code regardless of the wrapper — the script
> cannot loosen them, because the policy and the loop enforce them.

Related operator scripts already present: `scripts/auto/plan-change.ps1`,
`scripts/auto/run-approved-local-checks.ps1`, `scripts/auto/summarize-run.ps1`.

---

## 6. What this does NOT yet prove

Per `docs/Known-Limitations.md` §6: the controlled executor and fix loop run safely, but a genuine
end-to-end "diagnose → apply fix → verify via build+tests → roll back on failure" loop against a
**real defect in real code** has not been demonstrated. Closing that requires such a run, captured in
logs, entirely within the gating and audit constraints above. The loop is the safe mechanism; the
end-to-end proof on a real defect is still owed.
