# LAF ERP V5 Real-Life Scenario Results

## Engine scenario (passing test)

The engine real-life scenario test passes end to end:

**buy -> sell -> maker/checker -> pay -> balanced GL.**

- Purchase and sale recorded through the modules.
- Maker/checker workflow applied (document approval).
- Payment recorded.
- **General Ledger ends balanced** (double-entry), and the Balance Sheet reports
  `balanced = true` with P&L netProfit 200 (income 500 / expense 300).

## Create-form persistence (passing test)

The generated **create form persists a record with audit fields**, verified via the module
API by the Playwright create-form test.

## Coverage of the 20 requested scenarios: tested vs documented-only

Honest split — only the items below are covered by **automated tests**; the rest are
**documented-only** (target behavior, not yet test-backed):

**Covered by tests (engine + UI):**

1. Record a purchase
2. Record a sale
3. Maker/checker approval on a document
4. Record a payment
5. GL stays balanced (double-entry)
6. P&L produces income/expense/net profit
7. Balance Sheet balances
8. Create a record via the generated create form (with audit)
9. Read a record back via the module API
10. New-module CRUD smoke (CreditNote/DebitNote/StockReconciliation/PriceList/JobCard/LeaveApplication)

**Documented-only (target, not yet test-backed):**

11. Edit an existing record (UI not yet generated)
12. Delete a record (UI not yet generated)
13. Multi-warehouse stock allocation
14. Stock valuation depth (batch/serial)
15. MRP / production planning
16. Full payroll run
17. POS checkout flow
18. Online storefront order
19. Returns / RMA flow
20. Backup/restore operational drill

## Honest note

V5 is **ERP_PILOT_READY**. The core financial loop and the new create path are test-proven;
the documented-only scenarios are the remaining local/external gaps, not completed work.
