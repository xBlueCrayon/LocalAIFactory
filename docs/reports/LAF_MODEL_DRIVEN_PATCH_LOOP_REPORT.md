# LAF Model-Driven Patch Loop — Report

**Stamp:** 2026-06-21
**Component:** `LocalAIFactory.Reasoning/AgentRunner/ModelDrivenPlanPatchVerifyRunner`
**Benchmark:** `benchmarks/results/laf-model-driven-patch-loop.json`

## Purpose

Let a model (or any planner) **drive** a patch without ever being reckless. The model only
proposes; the runner deterministically validates, sandboxes, gates, and rolls back.

## The flow

1. An **`IPatchPlanner`** (model or deterministic stub) **proposes** a `PatchPlan`. Planners may not
   execute anything.
2. **`AssessRisk`** (deterministic):
   - every target path must be inside the worktree sandbox;
   - a forbidden command in a `.ps1` patch is blocked;
   - an empty plan is flagged;
   - sandbox-escape or forbidden-command ⇒ `Blocked`.
3. Apply **only in an isolated worktree** via the V1 `IsolatedPatchRunner` (build/test gated).
4. **Roll back on failure**, record experience.
5. Emit a **`KnowledgeProposal` with `Approved=false`** (human approval required).

There is **no commit/push capability** and the main repo is never edited.

## Covered scenarios

| Scenario | Outcome |
| --- | --- |
| Safe change | accepted (build + tests green); knowledge proposal emitted |
| Build failure | rejected + rollback |
| Test failure | rejected + rollback |
| Hallucinated path | blocked before any write |
| Forbidden command in script | blocked |
| Empty plan | flagged |

## Result

| Metric | Value |
| --- | --- |
| Tasks | 10 |
| Passed | 10 |
| All safe | yes |
| Main repo mutated | NO |
| Rollback proven | yes |
| Commit/push capability | NO |
| Knowledge auto-approved | NO |

## Honest limitations / not met

- **Tested with a FAKE planner, not a live model.** A real local model would only *propose* plans,
  which are then risk-assessed, sandboxed, build/test-gated, and rolled back on failure. The live
  model path is not exercised in this benchmark.
- This is a **safe validated core, not an unattended autonomous agent**: a human still approves
  before anything reaches the main repo.
- Risk assessment is **deterministic and rule-based** (sandbox containment, forbidden-command,
  empty-plan); it is not a semantic judge of patch correctness — correctness is enforced by the
  build/test gate, not by the planner.
