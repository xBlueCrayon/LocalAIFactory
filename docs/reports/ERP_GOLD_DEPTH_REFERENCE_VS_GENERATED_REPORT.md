# ERP-GOLD-DEPTH — Reference vs Generated Reproduction Report

**Sprint:** ERP-GOLD-DEPTH · **Branch:** `ke-008-code-symbols` · **Stamp:** 2026-06-21
**Companion data:** `benchmarks/results/erp-gold-depth-reference-vs-generated.json`

This sprint's depth was written into the generator templates/specs, then reproduced by a pure
deterministic generator run, to prove the additions are template-driven and not one-off edits.

## Reference — LAF Enterprise ERP Gold (depth)

`generated-products/LAF-EnterpriseERP-Gold`

| Metric | Value |
|--------|------:|
| .NET (xUnit) tests | 255 |
| Playwright tests | 51 |
| End-to-end scenarios | 26 |
| Modules | 28 (23 deterministic + 5 local-LLM-proposed) |

Depth added: real manufacturing (BOM + production-order lifecycle + costing + quality), report
depth (registers, summaries, aging, tax, stock valuation, reorder, work-order summary), report +
manufacturing REST API, 26 end-to-end scenarios, and the live SQL Server (LocalDB) migration + app
proof.

## Reproduction — LAF Enterprise ERP GoldGenerated-Depth

`generated-products/LAF-EnterpriseERP-GoldGenerated-Depth` — built by a pure deterministic
`--mode erp-gold` run (no local-LLM proposal).

| Metric | Value |
|--------|------:|
| .NET (xUnit) tests | 235 |
| Playwright tests | 51 |
| End-to-end scenarios | 26 |
| Modules | 23 (all deterministic) |

## Reproduction metrics

| Metric | Value | Verdict |
|--------|------:|---------|
| .NET test reproduction | 92.2% (235 / 255) | MET (>= 90% target) |
| Playwright reproduction | 100% | MET |
| Deterministic-surface reproduction | 100% | MET |
| Module reproduction | 82.1% (23 / 28) | LLM-extras only |

**Explanation of the gap:** the 20-test .NET difference (255 − 235) is exactly the **5
non-deterministic local-LLM catalog modules × 4 tests**. All depth (manufacturing, reports, API,
26 scenarios) is template-driven and reproduces identically; the 23/28 = 82% module figure reflects
the LLM extras only.

## Honest limitations / not done

- Module reproduction is **not** 100% (82%) because the 5 local-LLM-proposed catalog modules are,
  by design, non-deterministic and a pure generator run does not regenerate them.
- The reproduced product (`GoldGenerated-Depth`) therefore has 235 tests and 23 modules, not the
  reference's 255 / 28. The reproducible **deterministic surface** is 100% — that is the honest
  claim, not full Gold equivalence.
