# LAF ERP — Unlimited Generation Iteration Log

**Date:** 2026-06-21
**Loop:** `scripts/generator/run-laf-erp-unlimited-generation-loop.ps1`
**Result file:** `benchmarks/results/laf-erp-unlimited-generation-loop.json`
**Policy:** `adaptive-no-fixed-limit`

## What this is

An adaptive generation loop with **no fixed iteration limit**. It keeps regenerating/building/testing and stops only when a convergence condition is met — it does not stop at an arbitrary count.

## Stop condition reached

**STOP B** — two consecutive iterations with no measurable improvement (converged at the generator's current capability).

## Per-iteration metrics

| Iteration | Build | Tests | Passing tests | Improved vs prev? |
|----------:|-------|-------|--------------:|-------------------|
| 1 | ok | green | 108 | yes |
| 2 | ok | green | 108 | no |
| 3 | ok | green | 108 | no |

**Final passing tests: 108** (flat across iterations 1–3).

## Why it converged

The generator reached the ceiling of its **current** capability. With a fixed module spec, a fixed template set, and list/read CRUD skeletons, re-running the loop reproduces the same 108 passing tests. There is no measurable improvement to capture because the inputs and emission logic are unchanged between iterations — so Stop B fires honestly rather than spinning forever.

## What would actually move the number

Convergence is a property of the **inputs**, not a hard wall. To raise the plateau, change one of:

1. **Generator capability** — emit create/edit UI forms, deeper accounting (cost centres, dimensions), EF migrations instead of `EnsureCreated`.
2. **Module spec** — add richer modules/fields to `tools/LocalAIFactory.Generator/specs/erpnext-grade-modules.json` (with the collision guard still enforced).
3. **Knowledge** — feed the `laf-erp-generation-lessons-v1` pack lessons back into generation so the generator avoids known failure patterns and produces more correct, more testable output.

Until one of those changes, the loop will keep converging at 108. That is the honest meaning of "unlimited but converged."
