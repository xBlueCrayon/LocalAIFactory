# ERPNext-Production Local-LLM Role Decision

## Evaluation

Source: `benchmarks/results/erpnext-production-local-llm-eval.json`.

The local model **qwen2.5-coder:14b** was evaluated for its role in ERP generation. Finding:
it is **useful as a REVIEWER / PLANNER** — strong at gap detection (spotting missing modules,
flagging where a spec is thin) — but it is **not** the component that writes the compiling
product code.

The compiling code is written by **deterministic templates + knowledge**. This is what kept
V5 at **100% autonomy with 0 manual product-source edits**, a green build, and **134 passing
.NET tests**.

## Division of labor

| Role | Owner | Why |
|---|---|---|
| Gap detection, planning, review | Local LLM (qwen2.5-coder:14b) | Good at spotting missing pieces |
| Writing compiling product code | Deterministic templates + knowledge | Reproducible, builds clean, testable |

## Conclusion (verbatim from the eval)

> "Local LLM is best as reviewer/planner. Deterministic generation writes the compiling code."

This is why the generator does not depend on model output to produce a buildable ERP: the
LLM advises, the deterministic pipeline builds.
