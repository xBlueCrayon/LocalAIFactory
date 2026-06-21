# LAF Enterprise ERP — Reporting Model

Reports read **only** from the immutable ledgers (GL entries, stock ledger entries) — never from
mutable document state — so they are always consistent with what was posted.

## Implemented reports

| Report | Source | Service / API |
|---|---|---|
| General Ledger | `GLEntries` filtered by company + date (+ optional account) | `AccountingService.GeneralLedger` · `GET /api/reports/general-ledger` · `/Home/GeneralLedger` |
| Trial Balance | `GLEntries` grouped by account | `AccountingService.TrialBalance` · `GET /api/reports/trial-balance` |
| Stock Balance | latest `StockLedgerEntry` per item+warehouse | `StockService.Balance` · `GET /api/reports/stock-balance` · `/Home/StockBalance` |
| Accounts Receivable / Payable | `GLEntries` filtered by party type | `AccountingService.AccountsReceivable/Payable` · `GET /api/reports/ar-ap` |

Aggregations materialize before summing so decimal totals are exact on both SQLite and SQL Server.

## Not implemented (honest gap)

Profit & Loss, Balance Sheet, AR/AP aging, period-closing, and a report-builder are **not** implemented.
`ReportDefinition` exists as a registry entity for future report metadata but does not yet drive a builder.

## Verification

The General Ledger footer and Trial Balance are proven to balance by tests
(`AccountingTests.Trial_balance_is_balanced`, `Global_gl_always_balances_after_full_cycle`) and by the
live run proof (debits 2250 = credits 2250).
