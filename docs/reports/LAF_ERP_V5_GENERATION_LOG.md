# LAF ERP V5 Generation Log

## Command & spec

- **Spec:** `tools/LocalAIFactory.Generator/specs/erpnext-production-suite.json`
- **Generator:** `tools/LocalAIFactory.Generator`
- **Output:** `generated-products/LAF-EnterpriseERP-V5`

## Output

- **29 CRUD modules** = 24 from spec + 5 governed local-LLM modules.
- **74 product files.**
- **100% autonomy**, **0 manual product-source edits**.

## New modules vs V4

CreditNote, DebitNote, StockReconciliation, PriceList, JobCard, LeaveApplication.

## Engine

Carries **double-entry GL + P&L + Balance Sheet** (and trial balance).

## Build & tests (green)

- **Build: 0 errors.**
- **134 xUnit tests pass** (V4 was 122).
- **14 Playwright tests pass** (V4 was 13), including the new create-form UI test that fills
  the generated form and verifies the record via the module API.

## Knowledge usage

`benchmarks/results/erp-generator-knowledge-usage-v2.json`: 10 ERP packs / 296 items,
29 modules mapped; only validated entities emitted (spec + collision guard).

## Honest note

V5 is **ERP_PILOT_READY**, not production-grade and not ERPNext free-grade
(`reachedErpNextFreeGrade=false`). Generated UI is **CREATE + read** only (no edit/delete yet),
and the database uses `EnsureCreated` rather than EF migrations.
