# Generated ERP — Run Proof

**Date:** 2026-06-21 · **Product:** LAF Enterprise ERP · **DB:** SQLite (`laferp.db`, zero external services)
**Host:** Windows 11, .NET 10.0.301 · **URL:** http://localhost:5080

## Startup

`dotnet run` → app creates schema via `EnsureCreated`, runs `DataSeeder` (chart of accounts, roles,
workflows, masters) and `DemoData` (small under-threshold transactions). No external service required.

## Pages — all 200, sub-130 ms

| Path | Status | Time |
|---|---|---|
| `/` (Dashboard) | 200 | 0.130 s |
| `/Home/Customers` | 200 | 0.052 s |
| `/Home/Items` | 200 | 0.013 s |
| `/Home/SalesInvoices` | 200 | 0.011 s |
| `/Home/GeneralLedger` | 200 | 0.023 s |
| `/Home/StockBalance` | 200 | 0.011 s |
| `/Home/WorkflowInbox` | 200 | 0.011 s |
| `/Home/AuditLog` | 200 | 0.011 s |
| `/Home/Login` | 200 | 0.007 s |

## APIs — all 200

`/api/health`, `/api/reports/trial-balance`, `/api/reports/ar-ap`, `/api/sales-invoices`,
`/api/workflows`, `/api/audit`, `/api/stock-ledger` — all 200, ≤ 9 ms.

## Live data proves real posting

**AR / AP:** `{ "receivable": 350.0, "payable": 1200.0 }`
(sale grand total 550 − payment 200 = 350 receivable; two purchases of 600 = 1200 payable.)

**Trial balance (balances exactly):**

| Account | Debit | Credit |
|---|---:|---:|
| Bank | 200 | 0 |
| Cost of Goods Sold | 300 | 0 |
| Creditors | 0 | 1200 |
| Debtors | 550 | 200 |
| Sales | 0 | 500 |
| Stock In Hand | 1200 | 300 |
| Tax Payable | 0 | 50 |
| **Total** | **2250** | **2250** |

Debits == Credits → the double-entry ledger is balanced.

**Workflows:** 4 instances, all `Approved` (auto-approved under threshold) — Payment, Sales Invoice,
two Purchase Invoices.

## Health

- **0 HTTP 500s** and **0 unhandled exceptions** in the run log.
- Every core page completes well under one second.

## Conclusion

The generated ERP **starts, migrates, seeds, serves every page and API, and posts a balanced
double-entry ledger + stock ledger through the real services** — on SQLite with no external
dependencies. Verified live, not asserted.
