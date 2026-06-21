# LAF Safe-Patch Real-Life Benchmark — Report

**Stamp:** 2026-06-21
**Tasks:** `benchmarks/software-reasoning/patch-tasks.json`
**Results:** `benchmarks/results/laf-safe-patch-benchmark.json`

## Purpose

Prove that the model-driven patch loop is **safe under real-life scenarios**: it applies only in
isolation, gates on build/test, rolls back on failure, blocks unsafe or hallucinated plans before
any write, and cannot commit or push.

## Result

| Metric | Value |
| --- | --- |
| Tasks | 10 |
| Passed | 10 |
| Score | 100% |
| All safe | yes |
| Main repo mutated | NO |
| Rollback proven | yes |
| Commit/push capability | NO |
| Model required | NO |

Each scenario is backed by a passing xUnit test (`ModelDrivenRunnerTests` + `AgentRunnerTests`).

## What "safe" means here

- A plan is **risk-assessed** before any write; sandbox-escape (hallucinated path) and
  forbidden-in-script commands are **blocked before touching disk**.
- Accepted plans are applied **only in an isolated worktree**, build/test gated.
- On any failure the worktree is **rolled back**.
- A knowledge proposal is emitted **awaiting human approval** — never auto-approved.

## Honest limitations / not met

- The loop uses a **FAKE planner** in tests; there is **no live-model dependency**. A real local
  model would only propose plans, which are then risk-assessed, sandboxed, gated, and rolled back.
- This is a **SAFE validated-patch loop, not an unattended autonomous agent**: propose → validate
  deterministically → apply in isolation → roll back, with a human approving before anything reaches
  the main repo.
- "10/10" proves **safety and rollback**, not that a model autonomously fixes real bugs end-to-end.
