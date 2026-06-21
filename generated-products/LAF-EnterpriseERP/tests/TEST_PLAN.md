# LAF Enterprise ERP — Test Plan

Two layers of automated tests, both run in CI-style locally and all passing.

## .NET tests (xUnit) — 74 tests

| File | Covers |
|---|---|
| `AccountingTests` | JE balance rule, balanced posting, sales/purchase GL, AR/AP, COGS, payment allocation, global GL balance, trial balance |
| `WorkflowTests` | maker≠checker, auto-approve threshold, over-threshold approver, reject reason, role gating, audit on transition |
| `StockTests` | receipt/issue qty, negative-stock guard, moving-average valuation, cancel restores stock, stock balance |
| `ImmutabilityTests` | posted invoice immutable, draft editable, no double submit, cancel reverses GL |
| `ValidationTests` | no lines, negative qty, unknown customer, positive payment, RBAC matrix (theory) |
| `OpsTests` | lead conversion (+guard), task approval gating, support escalate/resolve, asset maintenance |
| `AuditAndImportTests` | audit on create, CSV import good/bad/duplicate rows, export, seed consistency |
| `ApiTests` | every REST endpoint 200, every UI page renders, trial balance populated+balances, create→submit invoice via API, domain error → HTTP 400 |

Backed by an in-memory **SQLite** database (real relational provider) seeded per test via `TestHost`.

## Playwright tests (Chromium) — 12 tests

Browser-level smoke against the live app: every page renders its table, dashboard KPIs, GL footer
balances, dev-auth login switches user, API health. See `playwright/PLAYWRIGHT_RESULTS.md`.

## How to run

```bash
dotnet test generated-products/LAF-EnterpriseERP/LAF-EnterpriseERP.slnx -c Release
cd generated-products/LAF-EnterpriseERP/playwright && npx playwright test
```

## Coverage philosophy

Tests assert **business invariants** (the ledger balances, stock never goes negative, makers can't
approve their own work), not just method calls — so they fail if a control is removed.
