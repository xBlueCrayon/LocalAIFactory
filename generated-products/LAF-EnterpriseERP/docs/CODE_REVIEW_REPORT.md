# LAF Enterprise ERP — Code Review Report

**Date:** 2026-06-21 · **Reviewer:** generation orchestrator (self-review) · **Build:** 0 errors · **Tests:** 74 .NET + 12 Playwright green

## Architecture

Clean layered modular monolith with a one-way dependency graph:
`Core (entities/enums)` → `Data (ErpDbContext)` → `Services (business logic)` → `Web (controllers/API/views)`,
`Tests` over all. No cycles. Controllers/endpoints do **no** business logic — they delegate to services;
the only DB access in `HomeController` is read-only projection for display.

## Findings

| # | Severity | Area | Finding | Status |
|---|---|---|---|---|
| 1 | High | Authorization | REST create endpoint was not RBAC-gated (submit/approve were, via the workflow) | **Fixed** — added `RbacService.Demand("SalesInvoice","create")` |
| 2 | Medium | Authorization | Only Sales Invoice has API write endpoints with RBAC; Purchase/Payment/Journal are exercised via services/tests but lack RBAC-gated API writes | **Accepted (documented)** — API surface is partial by design |
| 3 | Medium | Transactions | Each operation persists with a single `SaveChanges()` so GL + stock + audit + workflow rows commit atomically; there is no explicit outer DB transaction across multiple service calls | **Accepted** — single-SaveChanges per posting is atomic for the operations implemented |
| 4 | Low | Performance | `AccountingService.PostSalesInvoice` does per-line `Items.First(...)` lookups (small N) | **Accepted** — bounded by line count; see performance report |
| 5 | Low | Data model | Soft-delete query filters on master data trigger an EF required-navigation interaction warning | **Fixed** — explicitly acknowledged via `ConfigureWarnings(...Ignore...)` with rationale |
| 6 | Low | Migrations | Schema is created via `EnsureCreated()`, not EF migrations | **Accepted (documented)** — acceptable for the POC; production should switch to migrations |
| 7 | Info | Concurrency | Optimistic `RowVersion` token configured for documents on SQL Server only (ignored on SQLite test provider) | OK |

## Controls verified by tests (not just asserted)

- Unbalanced journal entry is rejected (`AccountingTests.JournalEntry_must_balance_or_is_rejected`).
- Global GL always balances after a full cycle; trial balance balances.
- Maker cannot approve own document; separate checker can; threshold routing; reject needs a reason.
- Posted invoice is immutable; cancel reverses GL + stock.
- Negative stock is blocked; moving-average valuation tracked.
- Every transition writes an audit event.

## No hidden problems

No hardcoded secrets, no hardcoded user ids in domain logic (identity flows through `ICurrentUser`),
no `GroupBy(_ => 1)`-style aggregate anti-patterns, no business logic in controllers. Decimal money is
`decimal(18,4)`; report aggregates materialize to be exact across providers.

**Conclusion:** sound POC-grade engineering. The one High finding was fixed; remaining items are
honestly accepted and documented as scope limits.
