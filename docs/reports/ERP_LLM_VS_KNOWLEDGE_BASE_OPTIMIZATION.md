# Local LLM vs Knowledge-Driven Generation — Optimization Eval

**Generated:** 2026-06-21
**Source:** `benchmarks/results/erp-llm-vs-knowledge-eval.json`
**Task:** ERP missing-module detection + code-generation approach.

## What was compared

Two approaches were evaluated for generating ERP modules locally:

1. **Local LLM** — `qwen2.5-coder:14b` (Ollama).
2. **Deterministic knowledge-driven generation** — the module-spec + engine templates + knowledge packs that the generator uses.

## Findings

**Local LLM (`qwen2.5-coder:14b`)**
- Useful for **planning and review**: missing-module detection, scenario ideas, and security/accounting review.
- Sample response: 1,178 chars in 7.8 s.
- `usefulForPlanningReview = true`.

**Deterministic knowledge-driven generation**
- Used for **code**: `usedForCode = true`.
- Compiling-code reliability rated **high** — drove **108 → 122 tests across V3 → V4 with 0 manual product edits**.
- Produced ERP V4 at **100% file autonomy** with **122 passing tests**, which the LLM cannot reliably do.

## Conclusion (verbatim)

> "For this use case, local LLM is best as REVIEWER/PLANNER (missing-module detection, scenario ideas, security/accounting review). DETERMINISTIC knowledge-driven generation (module-spec + engine templates + knowledge packs) is better for compiling production code: it produced V4 at 100% file autonomy with 122 passing tests, which the LLM cannot reliably do."

## Takeaway

The optimal split is to use the **local LLM as reviewer/planner** and **deterministic knowledge-driven generation as the code producer**. This is exactly the division embodied by the V4 generation run and the `--knowledge-usage` report.
