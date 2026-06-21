# LAF ERP V5 Production-Grade Review

Source: `benchmarks/results/laf-erp-v5-production-grade-score.json`.

## Score & classification

- **Production-grade score: 57** (V4 was 50, V3 48).
- **Classification: `ERP_PILOT_READY`.**
- **`reachedErpNextFreeGrade = false`.**

Readiness ladder:
`ERP_NOT_READY` -> `ERP_DEMO_READY` -> **`ERP_PILOT_READY` (V5)** ->
`ERP_LOCAL_PRODUCTION_READY` -> `ERP_PRODUCTION_READY_WITH_EXTERNAL_PROOFS` ->
`ERP_PRODUCTION_READY_FULL`.

## What V5 has (local gates passed)

- 29 CRUD modules (24 spec + 5 governed local-LLM).
- Generated **create-form UI** (GET form + POST persist with audit) + create links.
- Double-entry GL, P&L, Balance Sheet (balanced), trial balance.
- Maker/checker workflow + audit on writes.
- REST API per module.
- Runs on SQLite; MSSQL supported via connection string.
- Deployable published artifact.
- Build clean; 134 .NET + 14 Playwright tests pass.

## Local gates remaining (for ERP_LOCAL_PRODUCTION_READY)

1. EF migrations (V5 uses `EnsureCreated`).
2. Edit/delete + list-detail UI (V5 added **CREATE** only).
3. Backup/restore drill.
4. Performance load test.
5. Full module depth: MRP, payroll, POS, storefront, returns.

## External gates (beyond local generation)

1. Real authentication (Windows / SSO / OIDC).
2. CA-signed TLS.
3. External security review.
4. Customer acceptance.

## Honest verdict

V5 is a genuine, test-backed pilot that runs and balances its books locally. It is **not**
production-grade and **not** ERPNext free-grade. The score gain to 57 reflects real new
capability (create UI + 6 modules + tests), not a re-grading of the bar.
