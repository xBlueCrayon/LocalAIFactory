# Public Systems — Understanding Benchmark Results

**Date:** 2026-06-21 · **113 systems · 588 questions** · `scripts/benchmark/run-public-systems-understanding-benchmark.ps1`

This benchmark scores **evidence availability**, not model answers. It does **not** ask a model 588 questions
and grade itself — it checks whether the **required evidence** (extracted code symbols / fetched official
docs) actually exists, at what fidelity. This is deliberately conservative to avoid **fake API
understanding**.

## Headline (honest)

| Metric | Value |
|---|---|
| Systems in manifest | **113** (34 full-extraction / 74 validation-only / 5 docs-only) |
| Questions | **588** (124 require code evidence, 127 require API-doc evidence) |
| **Mean score** | **55.3 / 100** |
| Code-grounded (real extracted symbols) | **88** |
| Docs-fetched-grounded (real official-doc content) | **39** |
| Docs-metadata-only (registered, content not deep-verified) | **430** |
| Unsupported-language code gaps | honest (TS/JS/Java/Go/PHP not extracted) |

## By question type (mean)

| Type | Mean | Type | Mean |
|---|---:|---|---:|
| integration | 65.7 | data-model | 55.1 |
| extension | 62.5 | architecture | 55.1 |
| migration | 62.5 | operational | 54.9 |
| performance | 58.9 | api | 51.1 |
| auth | 56.8 | module | 56.2 |

## Reading

- **55.3 is honest, not a win.** It reflects that LocalAIFactory has **real code evidence** for the supported
  (C#/T-SQL/Python) systems and **real official-doc content** only for the **sampled** systems whose docs were
  fetched (odoo, wordpress, erpnext, airflow, grafana). The 430 "metadata-only" questions are credited at the
  honest **50** level: the official doc is *registered*, but its content was **not** deep-read, so no
  understanding is claimed beyond evidence availability.
- The **api** type scores lowest (51.1) precisely because most official API docs were registered but not
  fetched+verified — we do **not** claim API understanding we did not prove.

See `PUBLIC_SYSTEMS_DOC_API_CROSSCHECK_RESULTS.md` (what was actually fetched) and
`PUBLIC_SYSTEMS_UNSUPPORTED_GAPS.md` (what cannot be extracted).
