# LAF Gradual Self-Improvement Loop — Report

**Stamp:** 2026-06-21
**Phase:** Phase 7 (gradual self-improvement)
**Benchmark:** `benchmarks/results/laf-learning-loop-summary.json`

## Honest framing first

**A standalone LearningLoop module was NOT built this sprint.** This report describes a loop that is
**composed from pieces that already exist**, not a new project. That is the accurate state and is
stated up front so the rest of the report is read correctly.

## How the loop is composed

| Step | Existing piece |
| --- | --- |
| Propose a change | `IPatchPlanner` (model or stub) → `PatchPlan` |
| Validate safely | `ModelDrivenPlanPatchVerifyRunner` → risk assess → `IsolatedPatchRunner` (build/test gated) → rollback on failure |
| Record outcome | `ExperienceMemory` (accepted/rejected entries) |
| Propose knowledge | `KnowledgeProposal` / `GrowthProposal`, both `Approved=false` |
| Approve | **Human** (require-approval discipline holds) |

The model-driven runner already **proposes knowledge and records experience**, so the "learn from a
validated change" loop exists as a behaviour of the V2 pieces working together.

## What holds

- **Require-approval discipline holds**: nothing is auto-approved; nothing reaches the main repo
  without a human.
- The loop is **safe and validated**: propose → risk-assess → sandbox → build/test → rollback.

## Classification

- **Reached:** `LAF_SOFTWARE_REASONING_ENGINE_V2_LOCAL_CORE_READY`.
- **Not reached:** `LAF_SOFTWARE_DEVELOPMENT_REASONING_AGENT_PILOT_READY`.

## Honest limitations / not met

- **No standalone learning-loop project exists**; the flow is composed, not packaged as a module.
- The loop is tested with a **fake planner**, not a live model — so "self-improvement" is
  demonstrated structurally, not with a model autonomously improving the codebase.
- It is a **safe validated core, not an unattended autonomous agent**. A human approves every
  knowledge proposal and every change before it lands.
- No measurement of "improvement over iterations" against a live model is claimed.
