# Local LLM Reasoning Proof

**Date:** 2026-06-21 · **Model:** `qwen2.5-coder:14b` (local, via Ollama) · `scripts/ai/test-local-llm-reasoning.ps1`

The local LLM is an **optional, replaceable processor**, not the product. MSSQL is the source of truth; every
model output is a **proposal**, never authoritative. This proof runs 8 grounded reasoning tasks against the
**real local model** and scores them deterministically (keyword/pattern checks on the actual output).

> **Environment:** Ollama reachable; `qwen2.5-coder:14b` + `deepseek-r1:14b` present (no internet required for
> inference). First call ~12.5 s (model load), ~79 tok/s thereafter. No model was pulled.

## Results — mean 90 / 90-cap (PASS)

| # | Task | Score | Result |
|---|---|---:|---|
| 1 | Summarize a workflow document | 90 | grounded + relevant |
| 2 | Extract roles / states / transitions | 90 | grounded (found maker/checker/approver + states) |
| 3 | Propose a SQL schema | 90 | produced `CREATE TABLE` for the workflow |
| 4 | Propose service-layer validations | 90 | segregation/screening/amount/currency rules |
| 5 | Identify missing audit controls | 90 | flagged the missing audit write + maker≠checker |
| 6 | Compare a code fixture to a workflow rule | 90 | correctly said the code does **not** enforce maker≠checker |
| 7 | Produce a test plan | 90 | reject/approve/segregation/screening cases |
| 8 | **Say "insufficient evidence" instead of hallucinating** | **90** | **refused to fabricate** an account balance not in the doc ✅ |

**The hallucination-refusal test is the most important:** asked for a balance the document does not contain,
the model replied with an insufficient-evidence refusal rather than inventing a number. A fabricated number
would have scored **0** (unsafe).

## Pipeline proven (helper scripts)

- `extract-workflow-rules-with-local-llm.ps1` → extracted **3 roles, 6 states, 5 transitions, 5 validations**
  from the synthetic workflow doc (matching the document exactly), written as a **PENDING_REVIEW proposal**.
- `propose-knowledge-items-with-local-llm.ps1` → proposed **3** concise knowledge items, **PENDING_REVIEW**,
  **not installed** into MSSQL.

Both proposals are written to a **git-ignored** `.tmp-llm-proposals/` folder, with `authoritative=false` and
`reviewStatus=PENDING_REVIEW` — they never overwrite memory.

## Honest scope

- **Score is capped at 90** ("grounded + safe + reviewable"). **100** requires human review + conversion to
  approved, versioned MSSQL knowledge — **not** claimed here.
- Scoring is a **deterministic auto-heuristic** (keyword/pattern checks on real output), not a human review —
  it proves the model produced grounded, on-topic, non-hallucinated output, not that the output is
  production-correct.
- The LLM is **optional**: every other proof in this repository (build, tests, benchmark, deployment,
  knowledge base) passes with the LLM off the critical path. Memory is the product; the model is replaceable.
