# LAF Enterprise ERP — User Acceptance Test Script

A human can run this in ~10 minutes to validate the generated ERP. No external services needed.

## 0. Start

```bash
cd generated-products/LAF-EnterpriseERP
dotnet build LAF-EnterpriseERP.slnx -c Release
dotnet run --project src/LafErp.Web --no-launch-profile --urls http://localhost:5080
```

Open http://localhost:5080.

## 1. Dashboard
- [ ] KPI cards show Customers, Suppliers, Items, Sales/Purchase Invoices, Pending Approvals, AR, AP.
- [ ] AR shows 350.00 and AP shows 1200.00 (from the demo data).

## 2. Master data
- [ ] `Customers` lists `CUST-0001 Acme Industries`.
- [ ] `Items` lists `WIDGET` and `GADGET` with sell/buy rates.

## 3. Accounting
- [ ] `General Ledger` shows posted rows; the footer **Total Debit equals Total Credit**.
- [ ] `Sales Invoices` lists invoices with a status badge (Submitted).

## 4. Inventory
- [ ] `Stock Balance` shows on-hand qty and valuation for the demo items.

## 5. Controls
- [ ] `Workflow Inbox` shows approved workflow instances with the submitter.
- [ ] `Audit Log` shows Create/Submit/Approve events with the acting user and timestamp.

## 6. Maker/checker (dev auth)
- [ ] Open `Switch User`, sign in as `alice` with roles `Sales User|Accounts User`.
- [ ] Confirm you return to the dashboard (the acting identity is now alice).
- [ ] (Optional, via API) POST a large sales invoice as alice → it stays Draft (pending); approving as
      alice is rejected (maker cannot approve own); approving as `bob` (`Accounts Manager`) succeeds.

## 7. API
- [ ] `GET /api/reports/trial-balance?companyId=1` returns rows that sum equal debit/credit.
- [ ] `GET /api/health` returns `{ "status": "ok", "product": "LAF Enterprise ERP" }`.

## Pass criteria
All boxes tick, no page errors, GL balances. Record the outcome in `docs/HUMAN_SIGNOFF_FORM.md`.
