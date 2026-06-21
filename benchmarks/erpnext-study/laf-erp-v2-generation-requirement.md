# Requirement — LAF Enterprise ERP V2 (the only input to the generator)

Build a production-grade, clean-room ERP inspired by ERPNext business depth. No ERPNext code, no
trademarks, local-only, no paid services.

- **Product:** LAF Enterprise ERP V2
- **Stack:** .NET 10, C#, ASP.NET Core MVC + Razor, EF Core, SQL Server schema (SQLite for portable proof),
  xUnit, Playwright.

## P0 modules
Company setup, Fiscal year, Currency, Chart of accounts, Customers, Suppliers, Items, Warehouses,
Sales orders, Sales invoices, Purchase orders, Purchase invoices, Payments, Journal entries,
General ledger, Trial balance, Accounts receivable, Accounts payable, Stock ledger, Stock balance,
CRM leads, Opportunities, Projects, Tasks, Support tickets, Assets, Workflow approvals, Maker/checker,
Audit trail, RBAC, Import/export, Reports, REST APIs, UI screens, Login, Dashboard.

## P1 modules / skeletons
Manufacturing BOM, Work orders, Quality inspection, Maintenance, HR employees, Payroll skeleton, POS
skeleton, Website/eCommerce skeleton, Custom fields skeleton, Numbering series, Taxes, Multi-company,
Multi-warehouse, Report builder skeleton.

## Required controls
Maker cannot approve own transaction; threshold approval; rejection reason required; immutable posted
records; cancellation/reversal not silent edit; GL must balance; stock cannot go negative unless
configured; every transition + posting audited; every API validates input; controllers delegate to
services; no hardcoded secrets; no unaudited approval; RBAC enforced; concurrency where appropriate.

## Required tests
150+ .NET tests (target), 25+ Playwright (target), API tests, workflow negative tests, RBAC negative
tests, accounting balance tests, stock ledger tests, import/export tests, report tests, audit tests,
generated-code attribution test, real business scenario test.
