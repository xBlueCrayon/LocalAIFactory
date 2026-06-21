# LAF-Generated ERP V2 — Real-Life Scenario

**Date:** 2026-06-21 · **Automated test:** `tests/LafErp.Tests/RealLifeScenarioTests.cs`
(`Trading_company_buy_sell_pay_cycle`) — **PASSES**.

A trading/distribution company cycle, executed end-to-end through the generated ERP's services and
asserted at each step:

| Step | Action | Assertion |
|---|---|---|
| 1 | Seed company / fiscal year / CoA / customer / supplier / item / warehouse | seeded by `DataSeeder` |
| 2 | Purchase 100 widgets @ 60 (submit + approve) | stock = 100, **AP = 6000** |
| 3 | Sell 20 widgets @ 100 = 2000 (> 1000 threshold) as `alice` (Sales User) | invoice stays Draft (pending) |
| 4 | `alice` tries to approve her own invoice | **rejected** — maker cannot approve own |
| 5 | `bob` (Accounts Manager) approves | posts GL + stock; status = Submitted |
| 6 | Verify stock issue | stock = **80** |
| 7 | Verify AR posting | **AR = 2000** |
| 8 | Receive payment 2000 (> 500 threshold), submit as `cathy`, approve as `bob` | maker/checker enforced |
| 9 | Verify AR reduced | **AR = 0** |
| 10 | Verify GL balances globally | **debit == credit, > 0** |
| 11 | Verify audit log + workflow history | SalesInvoice Approve audited; workflow instance present; ≥ 5 audit events |

Every step is asserted by the test, so the scenario cannot silently regress. This proves the
generator-emitted ERP enforces the **real business controls** (double-entry balance, stock movement,
maker/checker separation, threshold approval, audit) on a realistic trading workflow — not just CRUD.

No HTTP 500s occur; the scenario runs against the in-memory SQLite provider in the test and against the
live SQLite app in the run proof.
