# ERP Gold — Scenario Expansion Report

**Sprint:** ERP-GOLD HARDENING · **Stamp:** 2026-06-21

## End-to-end scenario test

`tests/LafErp.Tests/RealLifeScenarioTests.cs` — `Trading_company_buy_sell_pay_cycle` (1 `[Fact]`): a single full trading-company lifecycle exercising, in order:

1. **Purchase / stock receipt** — receive 100 widgets @ 60 → AP 6000, stock 100.
2. **Sell with maker/checker** — Sales User raises a 2000 invoice (over the 1000 threshold → stays Draft pending a checker); maker cannot approve own (`DomainException`); a separate Accounts Manager approves → posts GL + stock issue → stock 80, AR 2000.
3. **Payment with separate approver** — Accounts User records a 2000 payment (over 500 → needs approval); Accounts Manager approves → AR 0.
4. **Integrity assertions** — global GL debit == credit and > 0; an `Approve` audit event on `SalesInvoice`; a `SalesInvoice` workflow instance; ≥ 5 audit events recorded.

This one scenario chains **setup → quote-to-cash → procure-to-pay → inventory → GL balance → RBAC-negative → maker/checker-negative → audit/workflow history** in a single assertion run.

## Supporting scenario-shaped tests (same business flows, focused)

| Area | File | Examples |
|------|------|----------|
| Accounting cycle | `AccountingTests.cs` (9) | `Global_gl_always_balances_after_full_cycle`, `Payment_reduces_outstanding_receivable`, `Trial_balance_is_balanced` |
| Reports after a cycle | `AccountingReportsTests.cs` (2) | `Profit_and_loss_reflects_revenue_minus_cogs`, `Balance_sheet_balances_after_a_cycle` |
| Workflow / RBAC | `WorkflowTests.cs` (9) | maker/checker, thresholds, rejection-reason, role-blocked |
| Ops lifecycle | `OpsAndImportTests.cs` (11) | `Lead_converts_to_customer`, `Support_ticket_escalates_and_resolves`, `Asset_can_be_scheduled_for_maintenance` |

## Scenario count

**Explicitly named end-to-end scenario method: 1** (`Trading_company_buy_sell_pay_cycle`). No other `*Scenario*` test methods exist; the remaining business flows are covered by the focused tests above.

## Honest limitations

- There is a single, comprehensive end-to-end scenario method rather than a broad scenario suite; depth comes from the focused per-domain tests.
- Scenario coverage centres on accounting / selling / buying / inventory / workflow; manufacturing, HR, and POS are CRUD skeletons and are not exercised end-to-end.
