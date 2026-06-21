# ERP-GOLD-DEPTH — Reports Depth Report

**Sprint:** ERP-GOLD-DEPTH · **Branch:** `ke-008-code-symbols` · **Stamp:** 2026-06-21
**Companion data:** `benchmarks/results/erp-gold-report-coverage.json`

Reporting moved from core accounting + stock balance to a broader, **ledger-backed** report set
exposed over REST.

## Files

- `src/LafErp.Services/ReportsService.cs` — new report service.
- `src/LafErp.Web/ApiEndpoints.cs` — `/api/reports/*` endpoints.
- `tests/LafErp.Tests/ReportsDepthTests.cs` — 11 tests (all green).

## Characteristics

All reports are **company-scoped**, consider **submitted documents only**, and **reconcile to the
General Ledger**. They use straightforward EF aggregation (no group-by-constant), consistent with
the project's page-hang rules.

## Reports added

| Report | Output |
|--------|--------|
| `SalesRegister` | Per-invoice net/tax/grand/status/date rows |
| `PurchaseRegister` | Per-invoice rows (supplier side) |
| `SalesSummaryByCustomer` | Count + total per customer |
| `PurchaseSummaryBySupplier` | Count + total per supplier |
| `OutstandingSalesInvoices` | Submitted invoices with outstanding > 0 |
| `OutstandingPurchaseInvoices` | Submitted purchase invoices with outstanding > 0 |
| `ReceivablesAging` | Buckets 0–30 / 31–60 / 61–90 / 90+ per customer |
| `TaxSummaryReport` | Output tax, input tax, net tax |
| `StockValuation` | Qty + value per item/warehouse |
| `ReorderReport` | Items at or below a threshold |
| `WorkOrderSummary` | Count + total qty per production status |

Pre-existing accounting reports remain: GeneralLedger, TrialBalance, ProfitAndLoss, BalanceSheet,
AccountsReceivable, AccountsPayable, StockBalance.

## REST API

`/api/reports/sales-register`, `/purchase-register`, `/sales-by-customer`, `/purchase-by-supplier`,
`/receivables-aging`, `/tax-summary`, `/stock-valuation`, `/reorder`, `/work-order-summary`.

## Tests

11 xUnit tests in `ReportsDepthTests.cs`, plus reporting reconciliation scenarios in
`ScenarioLibraryDepthTests.cs` (sales/purchase reporting consistency, tax reconciliation, stock
valuation, reorder alert, work-order summary) and 13 Playwright API checks in `reports-api.spec.ts`.

## Honest limitations / not done

- **Query-based reports only** — no BI engine, no dashboards, no print/report designer.
- **Receivables aging is bucketed by posting date**, not by a payment-terms due date.
- **Export coverage is still partial.**

Reporting/BI parity therefore sits at **54/100** — real, GL-reconciled report depth, well short of
ERPNext's BI and designer tooling.
