# Database Design

The data model is defined in code (`LafErp.Core` entities, mapped by `ErpDbContext` in
`LafErp.Data`) and materialised at runtime via `Database.EnsureCreated()`. The committed SQL Server
DDL is in [`database/schema.sql`](../database/schema.sql) — **42 tables**. The same model runs on
SQLite (the default, `laferp.db`) and on SQL Server (when `ConnectionStrings:Default` is set).

There is **no migrations project**; schema changes are made in the entity classes and re-materialised.

## Common base columns

Every table inherits from `EntityBase`, so each carries:

| Column | Type | Purpose |
| --- | --- | --- |
| `Id` | `int IDENTITY`, PK | surrogate key |
| `CreatedUtc` | `datetime2` | set automatically on insert |
| `UpdatedUtc` | `datetime2?` | set automatically on update |
| `CreatedBy` | `nvarchar` | acting user (defaults to `system`) |
| `UpdatedBy` | `nvarchar?` | acting user on update |
| `IsDeleted` | `bit` | soft-delete flag |

Document tables additionally inherit `DocumentBase`: `DocNo`, `Status` (`DocStatus` 0/1/2),
`PostingDate`, and `RowVersion` (rowversion concurrency token, **SQL Server only** — ignored on
SQLite).

All `decimal` columns are mapped with precision **(18, 4)** across every entity.

## Entity groups (the 42 tables)

### Setup / accounting masters (7)
`Currencies`, `Companies`, `FiscalYears`, `Accounts`, `CostCenters`, `NumberingSeries`,
`TaxTemplates`.

- **Accounts** is a self-referencing **hierarchical chart of accounts**: `ParentAccountId → Accounts.Id`,
  with `RootType` (Asset/Liability/Equity/Income/Expense), an `IsGroup` flag (group nodes vs postable
  leaves), and an optional `PartyTypeRequired` (used for the Debtors/Creditors control accounts).
- **NumberingSeries** holds `DocType`, `Prefix`, `NextNumber`, `Padding` for `PREFIX-#####` numbering.

### Party & item masters (5)
`Customers`, `Suppliers`, `ItemGroups`, `Items`, `Warehouses`.

- **Customer** references a `ReceivableAccountId`; **Supplier** a `PayableAccountId`.
- **Item** references `IncomeAccountId` and `ExpenseAccountId` (used as the COGS account on sale),
  carries `IsStockItem`, `StandardRate`, `StandardBuyRate`, and an optional `DefaultTaxTemplateId`.
- **ItemGroup** and **Warehouse** are self-referencing hierarchies (`ParentItemGroupId`,
  `ParentWarehouseId`); a Warehouse references a `StockAccountId`.

### Selling (4)
`SalesOrders` + `SalesOrderLines`, `SalesInvoices` + `SalesInvoiceLines`.

- Header → lines is a 1-many with the FK on the line; invoice lines carry `Qty`, `Rate`, `Amount`,
  `TaxRatePercent`. The invoice tracks `NetTotal`, `TaxTotal`, `GrandTotal`, `PaidAmount`,
  `OutstandingAmount`, an optional `WarehouseId`, and an `UpdateStock` flag.

### Buying (4)
`PurchaseOrders` + `PurchaseOrderLines`, `PurchaseInvoices` + `PurchaseInvoiceLines` — symmetric to
selling, referencing a `SupplierId`.

### Payments & journals (3)
`PaymentEntries`, `JournalEntries` + `JournalEntryLines`.

- **PaymentEntry** has `PartyType`, `PartyId`, `BankAccountId`, `Amount`, and an optional
  `AgainstInvoiceId` for allocation.
- **JournalEntry** lines carry `AccountId`, `Debit`, `Credit`, optional party and cost center.

### Ledgers — immutable (2)
`GLEntries`, `StockLedgerEntries`. These are **append-only**; they are never edited and never
soft-deleted. Corrections are made by posting reversing entries.

- **GLEntry**: `CompanyId`, `AccountId`, `PostingDate`, `Debit`, `Credit`, `VoucherType`,
  `VoucherNo`, optional `PartyType`/`PartyId`/`CostCenterId`, an `IsReversal` flag, `Remarks`.
- **StockLedgerEntry**: `ItemId`, `WarehouseId`, `PostingDate`, **signed** `QtyChange`,
  `ValuationRate`, running `QtyAfter` and `ValueAfter` (carried forward per item+warehouse),
  `VoucherType`/`VoucherNo`, `IsReversal`.

### CRM / Projects / Support / Assets (6)
`Leads`, `Opportunities`, `Projects`, `ProjectTasks`, `SupportTickets`, `Assets`.

### Workflow & governance (5)
`WorkflowDefinitions`, `WorkflowTransitions`, `WorkflowInstances`, `WorkflowApprovals`, `AuditEvents`.

- **WorkflowDefinition**: `DocType`, `Name`, `MakerCannotApprove`, `ApprovalThreshold`, `SubmitRole`,
  `ApproverRole`, and child `Transitions` (`FromState`, `ToState`, `Action`, `AllowedRole`).
- **WorkflowInstance**: live state per document (`DocType`, `DocumentId`, `CurrentState`,
  `SubmittedBy`, `Amount`) with child `Approvals` (each transition's `Action`, `ActedBy`,
  `FromState`, `ToState`, optional `Reason`, `ActedUtc`).
- **AuditEvent**: `EntityType`, `EntityId`, `Action`, `PerformedBy`, `Details`, `EventUtc` — one row
  per state-changing action.

### Security (4)
`AppUsers`, `AppRoles`, `AppUserRoles`, `RolePermissions`.

- **RolePermission** is the authorization matrix: `(RoleName, DocType)` → `CanRead`, `CanCreate`,
  `CanWrite`, `CanSubmit`, `CanApprove`, `CanCancel`.
- `AppUserRoles` is the user↔role join.

### Platform (2)
`ImportBatches` (CSV import run with `TotalRows`/`ImportedRows`/`FailedRows`/`Errors`),
`ReportDefinitions`.

> 7 + 5 + 4 + 4 + 3 + 2 + 6 + 5 + 4 + 2 = **42 tables**.

## Key relationships

- **Self-referencing hierarchies:** `Accounts.ParentAccountId`, `ItemGroups.ParentItemGroupId`,
  `Warehouses.ParentWarehouseId`, `CostCenters.ParentCostCenterId`.
- **Header → line (1-many):** Sales/Purchase Order + Invoice, JournalEntry, WorkflowDefinition →
  Transitions, WorkflowInstance → Approvals, Project → Tasks, AppUser → Roles. Lines are mapped with
  the FK on the child; **no cascade is configured into the immutable ledgers**.
- **Account references on masters:** Customer→Receivable account, Supplier→Payable account,
  Item→Income/Expense accounts, Warehouse→Stock account, TaxTemplate→Tax account. (These are stored
  as plain `int` account ids, resolved by the posting code, not all modelled as FK navigations.)
- **Vouchers → ledgers:** GL and stock entries are linked back to their source by
  `(VoucherType, VoucherNo)` string pair rather than a hard FK, which keeps the ledger immutable and
  decoupled.

## Indexes and unique keys

**Unique business keys (16):**

| Table | Unique key |
| --- | --- |
| Currencies | `Code` |
| Customers | `Code` |
| Suppliers | `Code` |
| Items | `Code` |
| Warehouses | `Code` |
| Accounts | `(CompanyId, Code)` |
| NumberingSeries | `DocType` |
| AppUsers | `Username` |
| AppRoles | `Name` |
| RolePermissions | `(RoleName, DocType)` |
| SalesInvoices | `DocNo` |
| PurchaseInvoices | `DocNo` |
| SalesOrders | `DocNo` |
| PurchaseOrders | `DocNo` |
| PaymentEntries | `DocNo` |
| JournalEntries | `DocNo` |

Document numbers are therefore unique per document type, and master codes are globally unique.

**Ledger / lookup indexes (non-unique, 25 total)** — the load-bearing ones for reporting:

- `GLEntries (AccountId, PostingDate)` — General Ledger by account over a date range.
- `GLEntries (PartyType, PartyId)` — AR/AP by party.
- `GLEntries (VoucherType, VoucherNo)` — voucher reversal and drill-down.
- `StockLedgerEntries (ItemId, WarehouseId, Id)` — ordered running-balance lookup per item+warehouse.

The remainder are EF-generated FK indexes (e.g. `SalesInvoices.CustomerId`, `*Lines.*Id`,
`AppUserRoles.*`, `Companies.DefaultCurrencyId`, `Accounts.ParentAccountId`).

## Concurrency, audit, and soft delete

- **Concurrency:** `DocumentBase.RowVersion` is configured as a SQL Server `rowversion`
  (optimistic-concurrency token) for `SalesInvoice`, `PurchaseInvoice`, `SalesOrder`, `PurchaseOrder`,
  `PaymentEntry`, `JournalEntry`. On SQLite the property is ignored.
- **Audit:** timestamps are stamped automatically in `SaveChanges`; semantic auditing is the
  `AuditEvents` table written by `AuditService` on every state-changing action.
- **Soft delete:** `IsDeleted` exists on every entity, but **global query filters are applied only to
  `Customer`, `Supplier`, and `Item`** — documents and ledgers retain full history. Posting code uses
  `IgnoreQueryFilters()` so a referenced master resolves even if flagged deleted.

## Provider differences (SQLite vs SQL Server)

| Aspect | SQL Server | SQLite (default/tests) |
| --- | --- | --- |
| `RowVersion` concurrency token | applied (`rowversion`) | ignored |
| Decimal precision (18,4) | enforced | mapped (SQLite stores as REAL/TEXT) |
| Committed DDL | `database/schema.sql` is the SQL Server script | n/a (created at runtime) |

See [DATABASE_SCHEMA_REPORT.md](DATABASE_SCHEMA_REPORT.md) for a summary of the DDL file.
