# LAF Enterprise ERP — API Test Results

API integration tests (`ApiTests.cs`) boot the real ASP.NET Core app via `WebApplicationFactory<Program>`
over an isolated SQLite database and exercise the HTTP surface. **All pass.**

## Endpoint availability (theory, 13 cases) — all 200

`/api/health`, `/api/customers`, `/api/suppliers`, `/api/items`, `/api/sales-invoices`,
`/api/purchase-invoices`, `/api/payments`, `/api/stock-ledger`, `/api/reports/trial-balance`,
`/api/reports/stock-balance`, `/api/reports/ar-ap`, `/api/workflows`, `/api/audit`.

## UI route rendering (theory, 9 cases) — all 200 + contain "LAF Enterprise ERP"

`/`, `/Home/Customers`, `/Home/Items`, `/Home/SalesInvoices`, `/Home/GeneralLedger`,
`/Home/StockBalance`, `/Home/WorkflowInbox`, `/Home/AuditLog`, `/Home/Login`.

## Behavioural API tests

| Test | Asserts |
|---|---|
| `Demo_data_populates_trial_balance_and_it_balances` | Trial balance is non-empty and debits == credits |
| `Create_then_submit_sales_invoice_via_api` | POST create → 201, then POST submit → 200 |
| `Insufficient_stock_returns_400_not_500` | A domain rule violation returns **HTTP 400**, not a 500 crash |

## Significance

These prove the **wired-up** system works end to end over HTTP — routing, DI, EF, services, and the
domain-error→400 convention — not just isolated unit logic.
