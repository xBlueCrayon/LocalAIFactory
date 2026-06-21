# ERP-GOLD-DEPTH — Test Expansion Report

**Sprint:** ERP-GOLD-DEPTH · **Branch:** `ke-008-code-symbols` · **Stamp:** 2026-06-21
**Companion data:** `benchmarks/results/erp-gold-depth-test-coverage.json`

## .NET (xUnit) — 255 total (up from 222, +33), all green

| New / expanded suite | Tests |
|----------------------|:-----:|
| `ManufacturingTests.cs` | 10 |
| `ReportsDepthTests.cs` | 11 |
| `ScenarioLibraryDepthTests.cs` | 12 |

These three suites account for the depth added this sprint; the remainder are the pre-existing
accounting, stock, workflow, auth, catalog, module, API and migration suites.

## Playwright — 51 total (up from 38, +13), all green

| New spec | Tests |
|----------|:-----:|
| `reports-api.spec.ts` | 13 (report/manufacturing endpoint loop + tax-summary shape + purchase-register assertions) |

## Scenarios — 26 total (up from 13)

13 base + 12 depth (`ScenarioLibraryDepthTests`) + 1 trading. Depth scenario names:

- make-to-stock full cycle
- quality-fail-then-rework passes
- production-cost-flows-to-finished-good-valuation
- procure-produce-sell finished good
- sales reporting is consistent
- receivables aging is current for new invoice
- tax summary reconciles with sales
- stock valuation reconciles after sale
- reorder alert for low stock
- work-order summary tracks status
- purchase reporting reflects procurement
- combined business day balances and reports

## Targets vs outcome

| Target | Outcome |
|--------|---------|
| .NET tests 300+ | **NOT MET** (255) |
| Playwright 50+ | MET (51) |
| Scenarios 25+ | MET (26) |

## Honest limitations / not done

The **300-test .NET floor was not reached** (255). This is a measurable improvement over 222, not
the target. The new tests concentrate on manufacturing, reports and depth scenarios; HR/payroll,
POS, e-commerce, delivery-note/return chains and batch/serial stock remain untested because those
flows were not implemented this sprint.
