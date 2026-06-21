# ERP Gold — Reporting, Import & Export Report

**Sprint:** ERP-GOLD HARDENING · **Stamp:** 2026-06-21

## Reporting

Query-based, company-scoped financial and stock reports:

| Report | Notes |
|--------|-------|
| General Ledger (GL) | Per-account entries |
| Trial Balance | Balanced — `Trial_balance_is_balanced` |
| Profit & Loss | Revenue minus COGS — `Profit_and_loss_reflects_revenue_minus_cogs` |
| Balance Sheet | Balances after a cycle — `Balance_sheet_balances_after_a_cycle` |
| AR / AP | Receivables / payables aging by party |
| Stock reports | Stock ledger / balance / valuation |

All reports are derived from queries (no precomputed cubes) and are scoped to a company.

## Import / Export

| Capability | Detail | Proving test |
|------------|--------|--------------|
| CSV import | `ImportService` with `ImportBatch` row counts + per-row error capture | `Csv_import_reports_good_and_bad_rows` |
| Duplicate rejection | Duplicate business codes rejected on import | `Csv_import_rejects_duplicate_codes` |
| Export round-trip | Customer export round-trips its header | `Export_customers_round_trips_header` |

Source: `tests/LafErp.Tests/OpsAndImportTests.cs`, `AccountingReportsTests.cs`, `AccountingTests.cs`.

## Honest limitations

- **Export coverage is partial** — not every entity has an export path.
- **No BI / analytics / dashboard layer and no print designer** — these remain documented ERPNext-parity gaps (`benchmarks/results/erp-gold-erpnext-parity-score.json`, `reportingBI` ~42).
- Reports are query-based; there is no scheduled/cached reporting engine.
