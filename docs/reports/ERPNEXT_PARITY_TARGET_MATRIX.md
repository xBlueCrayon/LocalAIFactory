# ERPNext Parity Target Matrix

**Date:** 2026-06-21 · **Product:** LAF Enterprise ERP (clean-room)
**Machine-readable:** `benchmarks/erpnext-study/erpnext-parity-target-matrix.json` (67 feature rows)

The matrix enumerates ERPNext capability groups as build targets, each with required entities, workflows,
reports, APIs, UI screens, tests, a priority (P0–P3), an implementation status, and a parity score. After
building LAF Enterprise ERP, the statuses were updated to reflect what was actually implemented and tested.

## Status spread (67 features)

| Status | Count | Meaning |
|---|---:|---|
| Tested | 4 | Implemented **and** proven by automated tests (workflow/approvals, audit trail) |
| Implemented | 11 | Built (core setup) |
| Partial | 33 | Partially built (accounting, inventory, selling, buying, CRM, projects, support, assets, reports, APIs, import/export, customization) |
| NotStarted | 19 | Not built (P&L/Balance Sheet/period-close, quotation/delivery/receipt/returns, reconciliation/batch/serial, manufacturing, HR/payroll, POS, website, custom-field UI, report builder) |

**Mean parity score ≈ 34.8 / 100**, consistent with the overall ERPNext-grade ≈ 36% in
`docs/reports/LAF_ERP_VS_ERPNEXT_COMPARISON.md`.

## Priority coverage

- **P0 (core ERP):** substantially implemented — chart of accounts, double-entry GL, payments, journal
  entries, inventory/stock ledger, sales & purchase invoices, maker/checker workflow, audit, RBAC, core
  reports, REST APIs. Gaps remain (quotation/delivery/receipt, P&L/BS).
- **P1 (important):** partial — CRM, projects, support, assets, import/export, reports/dashboards.
- **P2 (extended):** not started — manufacturing, HR/payroll.
- **P3 (nice-to-have):** not started — POS, website/portal/eCommerce.

## Honesty note

No row is marked `Implemented`/`Tested` unless the corresponding code (and, for `Tested`, a passing test)
exists. The 19 `NotStarted` rows are stated plainly — there is no padding and no overclaim of parity.
