# LAF Enterprise ERP V3 — Test Expansion Results

**Date:** 2026-06-21

## Totals

- **108 xUnit (.NET) tests pass.**
- **13 Playwright (Chromium) tests pass.**

### .NET 108 breakdown

| Group | Count |
|-------|------:|
| Engine tests (accounting, stock, workflow, RBAC, API, controls, ops/import) | 74 |
| P&L + Balance Sheet report tests | 2 |
| Real-life scenario test | 1 |
| Generation provenance test | 1 |
| Generated CRUD module tests (15 modules × 2) | 30 |
| **Total** | **108** |

## Test classes

From `tests/LafErp.Tests/` (attribution per `laf-erp-v3-generation-attribution.json`):

- `AccountingTests.cs`
- `AccountingReportsTests.cs` (P&L + Balance Sheet)
- `StockTests.cs`
- `WorkflowTests.cs`
- `ApiTests.cs`
- `ControlsAndValidationTests.cs`
- `OpsAndImportTests.cs`
- `ModuleGeneratedTests.cs`
- `CatalogGeneratedTests.cs` (LLM-proposal-derived catalog modules)
- `RealLifeScenarioTests.cs`
- `GenerationProvenanceTests.cs`
- `TestHost.cs` (shared host)

## Real-life scenario

`RealLifeScenarioTests.cs` drives an end-to-end business flow (the same kind of flow gate **H-06** records as PASS), exercising the accounting/stock/workflow engine as a connected scenario rather than isolated units. Real-life scenario coverage is honestly scored at **42%** — one real scenario, not a broad suite.

## Targets NOT reached

The sprint targeted **150 .NET** and **50 Playwright** tests. Those targets were **not** reached:

- .NET: **108 / 150**.
- Playwright: **13 / 50**.

**Why:** the adaptive generation loop converged at the generator's current capability (Stop B — three consecutive iterations flat at 108 passing tests; see `LAF_ERP_UNLIMITED_GENERATION_ITERATION_LOG.md`). Adding more tests of the **same shape** over **list/read CRUD skeletons** would not raise meaningful coverage; moving the number requires new capability (create/edit forms, more report depth, deeper scenarios), not more of the same. The honest number is reported rather than padded.

## Browser tests

13 Playwright (Chromium) tests with real `admin` login + navigation + 11 screenshots, no HTTP 500. See `LAF_ERP_V3_BROWSER_AND_LOGIN_PROOF.md`.
