# LAF Software Reasoning Engine V2 — Start State

**Sprint:** Software Reasoning Engine V2
**Start commit:** `5e70877` (the V1 baseline)
**Stamp:** 2026-06-21

This document records the verified state of the repository at the start of the V2 sprint, so
that the V2 deltas can be measured honestly against it.

## Starting gates (V1 baseline at `5e70877`)

| Gate | Start state |
| --- | --- |
| Build | 0 errors (full solution) |
| Reasoning-family tests | 113 |
| Factory test total | 251 |
| Knowledge packs | 39 (growing) |
| Product maturity | V3 NEAR_GA |
| Security | PASS |

## What V1 already provided (not rebuilt in V2)

- The isolated-worktree patch runner (`IsolatedPatchRunner`) with build/test gates and rollback.
- The experience memory and safe-tool gateway used by the reasoning family.
- The code graph and the V1 software-reasoning benchmark.

## Scope of V2 (measured against this baseline)

V2 adds three new product projects (`LocalAIFactory.CodeBlocks`, `LocalAIFactory.PythonBridge`,
`LocalAIFactory.KnowledgeGrowth`) plus model-driven plan→patch→verify orchestration, GPU-aware
local-model orchestration, a safe Python bridge, a knowledge-growth scraper, a V2 UI/API surface,
and six new knowledge packs. The detailed deltas and the honest list of unmet targets are recorded
in `docs/architecture/LAF_SOFTWARE_REASONING_ENGINE_V2.md` and
`docs/reports/LAF_REASONING_V2_PRODUCT_DEFINITION.md`.

## Honest limitations / not met

- This is a **start-state snapshot only**; it makes no claim about the V2 end state.
- The baseline figures (reasoning 113, factory 251, 39 packs) are the *starting* numbers, not the
  sprint targets. The V2 sprint did **not** meet every target it set — see the product-definition
  report for the full not-met list (reasoning 220+ and factory 300+ were not reached).
