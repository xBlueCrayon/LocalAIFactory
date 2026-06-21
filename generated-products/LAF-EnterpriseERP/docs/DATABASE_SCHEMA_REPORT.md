# Database Schema Report

This report summarises [`database/schema.sql`](../database/schema.sql), the committed SQL Server DDL
for the LAF Enterprise ERP data model. The file is generated from the EF Core model via
`dotnet run --project src/LafErp.Web -- schema` (which prints `ErpDbContext.Database.GenerateCreateScript()`
and exits without contacting a database).

## Summary

| Metric | Value |
| --- | --- |
| Total `CREATE TABLE` statements | **42** |
| Unique indexes | **16** |
| Non-unique indexes | **25** |
| Approx. DDL size | ~970 lines |
| Target | SQL Server (`int IDENTITY` PKs, `datetime2`, `decimal(18,4)`, `nvarchar`, `bit`, `rowversion`) |

Every table has an `Id int NOT NULL IDENTITY` primary key and the shared audit columns
(`CreatedUtc`, `UpdatedUtc`, `CreatedBy`, `UpdatedBy`, `IsDeleted`).

## The 42 tables

Setup/accounting masters: `Currencies`, `Companies`, `FiscalYears`, `Accounts`, `CostCenters`,
`NumberingSeries`, `TaxTemplates`.

Party & item masters: `Customers`, `Suppliers`, `ItemGroups`, `Items`, `Warehouses`.

Selling: `SalesOrders`, `SalesOrderLines`, `SalesInvoices`, `SalesInvoiceLines`.

Buying: `PurchaseOrders`, `PurchaseOrderLines`, `PurchaseInvoices`, `PurchaseInvoiceLines`.

Payments & journals: `PaymentEntries`, `JournalEntries`, `JournalEntryLines`.

Immutable ledgers: `GLEntries`, `StockLedgerEntries`.

CRM/Projects/Support/Assets: `Leads`, `Opportunities`, `Projects`, `ProjectTasks`, `SupportTickets`,
`Assets`.

Workflow & governance: `WorkflowDefinitions`, `WorkflowTransitions`, `WorkflowInstances`,
`WorkflowApprovals`, `AuditEvents`.

Security: `AppUsers`, `AppRoles`, `AppUserRoles`, `RolePermissions`.

Platform: `ImportBatches`, `ReportDefinitions`.

## Key tables

- **Accounts** — hierarchical chart of accounts; FK `ParentAccountId → Accounts(Id)`; unique
  `(CompanyId, Code)`. Carries `RootType`, `IsGroup`, `PartyTypeRequired`.
- **GLEntries** — immutable double-entry ledger. Indexed on `(AccountId, PostingDate)`,
  `(PartyType, PartyId)`, `(VoucherType, VoucherNo)`. `Debit`/`Credit` are `decimal(18,4)`.
- **StockLedgerEntries** — immutable signed stock movements with running `QtyAfter`/`ValueAfter`.
  Indexed on `(ItemId, WarehouseId, Id)` to read the latest running balance in order.
- **SalesInvoices / PurchaseInvoices** — document tables with `DocStatus`, money totals, and a
  `RowVersion` rowversion concurrency column; `DocNo` is unique.
- **RolePermissions** — the RBAC matrix; unique `(RoleName, DocType)` with six boolean capability
  columns.
- **WorkflowDefinitions / WorkflowInstances / WorkflowApprovals** — the approval engine's persisted
  state and per-transition history.

## Constraints

- **Primary keys:** one per table (`PK_<Table>` on `Id`).
- **Foreign keys:** EF-generated for every navigation/line relationship (e.g.
  `FK_SalesInvoiceLines_SalesInvoices_SalesInvoiceId`, `FK_Companies_Currencies_DefaultCurrencyId`,
  `FK_Accounts_Accounts_ParentAccountId`). Header→line relationships do **not** cascade into the
  immutable ledger tables.
- **Unique indexes (16):** master codes (`Currencies`, `Customers`, `Suppliers`, `Items`,
  `Warehouses` on `Code`), `Accounts (CompanyId, Code)`, `NumberingSeries (DocType)`,
  `AppUsers (Username)`, `AppRoles (Name)`, `RolePermissions (RoleName, DocType)`, and `DocNo` on each
  of the six document types (SalesInvoice, PurchaseInvoice, SalesOrder, PurchaseOrder, PaymentEntry,
  JournalEntry).
- **Non-unique indexes (25):** the three GL reporting indexes, the stock-ledger running-balance index,
  and EF FK indexes.

> Note: the committed `schema.sql` is the SQL Server script. In SQLite mode the equivalent schema is
> created at runtime by `EnsureCreated()`, with the `RowVersion` columns omitted (SQLite has no
> `rowversion` type).
