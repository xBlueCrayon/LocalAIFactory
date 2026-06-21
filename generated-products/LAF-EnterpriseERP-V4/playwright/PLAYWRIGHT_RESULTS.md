# Playwright UI Test Results — LAF Enterprise ERP

**Date:** 2026-06-21 · **Browser:** Chromium (Desktop Chrome) · **Runner:** @playwright/test

The Playwright config launches the real ASP.NET Core app (`webServer`) on http://localhost:5080 (SQLite,
zero external services) and runs browser-level smoke tests against the live UI + API.

## Result: 12 passed (5.0 s)

| # | Test | Result |
|---|---|---|
| 1 | page `/` renders dashboard | ✅ |
| 2 | page `/Home/Customers` renders customers-table | ✅ |
| 3 | page `/Home/Items` renders items-table | ✅ |
| 4 | page `/Home/SalesInvoices` renders sales-invoices-table | ✅ |
| 5 | page `/Home/GeneralLedger` renders gl-table | ✅ |
| 6 | page `/Home/StockBalance` renders stock-balance-table | ✅ |
| 7 | page `/Home/WorkflowInbox` renders workflow-table | ✅ |
| 8 | page `/Home/AuditLog` renders audit-table | ✅ |
| 9 | dashboard shows seeded KPIs (customers ≥ 1) | ✅ |
| 10 | general ledger is populated and balances at the footer | ✅ |
| 11 | dev-auth login switches the acting user | ✅ |
| 12 | api health endpoint responds (200, product = LAF Enterprise ERP) | ✅ |

## How to run

```bash
cd generated-products/LAF-EnterpriseERP/playwright
npm install
npx playwright install chromium
npx playwright test
```

The `webServer` block in `playwright.config.ts` starts the app automatically (or reuses a running one).
Individual page loads completed in 50–165 ms.
