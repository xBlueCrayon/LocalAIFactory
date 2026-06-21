# Local LLM — ERP Generation Model Selection

**Date:** 2026-06-21
**Result file:** `benchmarks/results/local-llm-erp-generation-model-eval.json`
**Task:** `catalog-entity-proposal` (propose CRUD catalog entities as JSON)

## What was evaluated

A **real** local-model eval was run comparing two locally-hosted models on the catalog-entity-proposal task:

- `qwen2.5-coder:14b`
- `deepseek-r1:14b`

Both ran locally (no internet, no cloud). The winner's proposal feeds V3's catalog (CRUD) layer, governed and collision-guarded.

## Results

| Model | Valid JSON | Entities | Chars | Duration | Score |
|-------|-----------|----------|-------|----------|-------|
| **qwen2.5-coder:14b** | yes | 6 | 2161 | **18.7 s** | **78.1** |
| deepseek-r1:14b | yes | 6 | 2034 | 26.0 s | 77.4 |

## Winner

**qwen2.5-coder:14b.** Both models produced 6 valid-JSON entities; the scores are close (78.1 vs 77.4). qwen won primarily on **speed** (18.7 s vs 26.0 s) at comparable quality.

## Governed usage (not blind trust)

qwen's proposal was **not** emitted verbatim. It passed through governance:

- Each proposed entity was validated and **collision-guarded** against core engine entities.
- One proposed entity, `Supplier`, was **rejected** (collision with a core engine entity / hallucination-overlap guard).
- Five entities were **accepted** (CustomerSegment, ProductCategory, EmployeeRole, MarketingCampaign, VendorContract) and the deterministic templates produced the actual compiling code.

So the LLM proposes *what* catalog entities to add; the deterministic generator produces *how* they are emitted. The compiling code is template-generated, not LLM-written.

## Honest note

The score gap is small (0.7 points) and the decision is essentially a tie broken by latency. The eval is a real local run, but it is a single task on two models — it is a selection signal, not a broad benchmark.
