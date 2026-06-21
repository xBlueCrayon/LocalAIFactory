# LAF Agent Runner V1 — Report

Component: `src/LocalAIFactory.Reasoning/AgentRunner`
Tests for this component: **7 PASS** (part of the 113 reasoning-engine tests).

## What was built

`IsolatedPatchRunner` — the safe patch loop. Given a set of proposed file writes and a worktree root,
it executes a strict, rollback-safe sequence:

1. **Validates every write path through the `SafeToolGateway` BEFORE touching disk.** A path that
   escapes the worktree is rejected and the run aborts.
2. **Snapshots originals** (content or "did not exist") for rollback.
3. **Applies the patch inside the worktree only.**
4. **Builds** via the injected `IValidationExecutor`; on failure, **rolls back** and records a
   `BuildFailure` experience.
5. **Tests** via the executor; on failure, **rolls back** and records a `TestFailure` experience.
6. On success, accepts and records a `RegressionPrevented` experience.

It has **NO capability to commit, push, or write outside the worktree** — by design. The validation
backend is abstracted behind `IValidationExecutor`, so the runner is unit-testable without invoking
`dotnet`.

## What it proves

- Proposed changes are **validated in isolation and rolled back on any failure**, so a bad patch never
  reaches the working tree, and certainly never reaches git history.
- Every outcome (accept or reject) is captured as reusable **experience**, closing the loop with the
  experience memory.
- The runner cooperates with the safety gateway: writes are path-checked before they happen.

## Honest limitations / not met

- **Target not met:** the ambitious target of **50+** agent-runner tests was not reached (7 delivered).
- **No commit/push by design.** The runner deliberately cannot land changes; a human (or a separate,
  governed step) must commit. This is a safety feature, not a gap to be "fixed."
- **Validation is abstracted.** Tests drive an injected `IValidationExecutor`, not a real `dotnet
  build`/`test` over a large repo in CI. Real-world build/test latency and flakiness are not exercised
  here.
- **Single-shot patch application.** There is no iterative repair loop or model-guided patch
  generation in V1; the runner validates patches it is given.
