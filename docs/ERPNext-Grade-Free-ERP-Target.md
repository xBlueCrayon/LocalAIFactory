# ERPNext-Grade Free ERP Target

This document defines what "ERPNext-grade free ERP" means for the LocalAIFactory ERP
generation track, broken down by functional domain. It is the yardstick against which
LAF-Generated ERP (currently **V5**) is measured.

**Honest status (2026-06-21):** LAF ERP V5 is classified **ERP_PILOT_READY**. It has
**NOT** reached ERPNext free-grade (`reachedErpNextFreeGrade=false`). This document is the
*target*, not a claim that the target is met.

For each domain, "done-to-grade" states the bar a self-hosted ERPNext community edition
clears, then splits the work into **locally achievable** (deterministic generation +
knowledge + SQLite/MSSQL, no internet) vs **external** (requires real auth infra, CA TLS,
third-party review, or customer sign-off).

---

## Setup / Foundation

- **Done-to-grade:** company, fiscal year, chart of accounts, cost centers, UOM, tax
  templates, number series, role-based permissions, multi-currency scaffolding.
- **Locally achievable:** seeded company/fiscal config, generated CoA, deterministic
  number series, audit fields on every write.
- **External:** real identity provider for role/permission enforcement (Windows/SSO/OIDC).

## Accounting

- **Done-to-grade:** double-entry GL, journal entries, P&L, Balance Sheet, trial balance,
  AR/AP, payments, credit/debit notes, bank reconciliation.
- **Locally achievable (V5 has):** double-entry GL, P&L, Balance Sheet, trial balance,
  CreditNote + DebitNote modules. Balance Sheet proven balanced.
- **External:** external auditor sign-off on accounting correctness for production use.

## Selling

- **Done-to-grade:** quotation -> sales order -> delivery note -> sales invoice ->
  payment; price lists, discounts, customer master.
- **Locally achievable (V5 has):** quotations, sales-order-class modules, PriceList,
  customer/employee masters via CRUD + create-form UI.
- **External:** none strictly; depth (returns, multi-warehouse allocation) remains.

## Buying

- **Done-to-grade:** purchase request -> purchase order -> receipt -> purchase invoice ->
  payment; supplier master.
- **Locally achievable (V5 has):** purchase-side CRUD modules + DebitNote.
- **External:** none strictly; depth remains.

## Stock / Inventory

- **Done-to-grade:** item master, warehouses, stock entry, stock reconciliation, valuation,
  batch/serial, reorder.
- **Locally achievable (V5 has):** StockReconciliation module + item/warehouse CRUD.
- **External:** none; valuation depth + batch/serial remain local gaps.

## Manufacturing

- **Done-to-grade:** BOM, work order, job card, MRP, capacity planning.
- **Locally achievable (V5 has):** WorkOrder + JobCard modules.
- **External:** none; **MRP / capacity planning is a remaining local gap.**

## CRM

- **Done-to-grade:** lead, opportunity, customer, contact, communication log.
- **Locally achievable:** lead/opportunity/contact CRUD generatable from spec.
- **External:** email/telephony integration would be external.

## Projects

- **Done-to-grade:** project, task, timesheet, billing.
- **Locally achievable:** project/task CRUD generatable.
- **External:** none.

## Support / Helpdesk

- **Done-to-grade:** issue/ticket, SLA, assignment.
- **Locally achievable:** ticket CRUD + workflow status.
- **External:** inbound email channel is external.

## Assets

- **Done-to-grade:** asset master, depreciation, maintenance.
- **Locally achievable:** asset CRUD + scheduled depreciation logic.
- **External:** none.

## HR / Payroll

- **Done-to-grade:** employee, attendance, leave, salary structure, payroll run.
- **Locally achievable (V5 has):** employee master, LeaveApplication module.
- **External:** **full payroll run is a remaining local gap;** statutory/tax filing is
  external.

## POS / E-commerce

- **Done-to-grade:** POS screen, offline POS, online storefront, cart, checkout.
- **Locally achievable:** POS CRUD/transaction modules generatable.
- **External:** **POS UX + storefront are remaining local gaps;** payment-gateway
  integration is external.

## Workflow / Approvals

- **Done-to-grade:** maker/checker, multi-level approval, document state machine, audit.
- **Locally achievable (V5 has):** maker/checker workflow + audit on writes (proven in the
  engine real-life scenario).
- **External:** approval routing tied to real org identity is external.

## Technical / Platform

- **Done-to-grade:** schema migrations, backup/restore, performance under load, role
  security, REST API, deployable artifact.
- **Locally achievable (V5 has):** REST API per module, deployable published artifact,
  SQLite + MSSQL support. **Remaining local gaps:** EF migrations (V5 uses `EnsureCreated`),
  edit/delete + list-detail UI, backup/restore drill, performance load test.
- **External:** real auth (Windows/SSO/OIDC), CA-signed TLS, external security review,
  customer acceptance.

---

## Summary of the gap to grade

Local gates remaining for the next rung (**ERP_LOCAL_PRODUCTION_READY**): EF migrations,
edit/delete + list-detail UI (V5 added **CREATE** only), backup/restore drill, performance
load test, and full module depth (MRP, payroll, POS, storefront, returns).

External gates (beyond any local generator): real authentication, CA TLS, external security
review, and customer acceptance.
