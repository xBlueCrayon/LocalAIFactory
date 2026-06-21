# Architecture

LAF Enterprise ERP is a **modular monolith**: a single deployable ASP.NET Core application, internally
partitioned into layered .NET projects with a strict one-way dependency graph. There is no
microservice fan-out, no message bus, and no external runtime dependency in the default configuration
— it runs from a single `dotnet run` against a local SQLite file.

## Tech stack

| Concern | Choice |
| --- | --- |
| Runtime | .NET 10 (`net10.0`), `ImplicitUsings`, `Nullable` enabled, `LangVersion latest` |
| Web | ASP.NET Core MVC (controllers + Razor views) **and** minimal-API REST endpoints in the same host |
| ORM | EF Core |
| Database | SQL Server (when `ConnectionStrings:Default` is set) **or** SQLite (`laferp.db`, the default) |
| Schema lifecycle | `Database.EnsureCreated()` on startup (no migrations project) + `DataSeeder` + `DemoData` |
| Tests | xUnit (`LafErp.Tests`, in-memory SQLite via `WebApplicationFactory` and direct `TestHost`); Playwright (TypeScript) for browser smoke tests |
| Build orchestration | `LAF-EnterpriseERP.slnx`; `Directory.Build.props` pins `net10.0` and opts out of the parent repo's central package management |

## Projects and dependency direction

Four projects participate in the solution, plus a separate Playwright harness:

```
            ┌─────────────────────────────────────────────┐
            │ LafErp.Web   (DI root / composition root)    │
            │  - HomeController + 9 Razor views            │
            │  - ApiEndpoints (minimal-API REST)           │
            │  - HttpCurrentUser (dev-auth cookie)         │
            │  - Program.cs: EnsureCreated + Seed + Demo   │
            └───────────────┬─────────────────────────────┘
                            │ depends on
            ┌───────────────▼─────────────────────────────┐
            │ LafErp.Services  (business logic)            │
            │  Accounting, Stock, Workflow, Audit,         │
            │  Numbering, Rbac, Import, Sales, Purchase,   │
            │  Payment, Journal, Crm, Project, Support,    │
            │  Asset, DataSeeder, DemoData, ICurrentUser   │
            └───────────────┬─────────────────────────────┘
                            │ depends on
            ┌───────────────▼─────────────────────────────┐
            │ LafErp.Data   (ErpDbContext, EF mapping)     │
            └───────────────┬─────────────────────────────┘
                            │ depends on
            ┌───────────────▼─────────────────────────────┐
            │ LafErp.Core   (entities + enums, no deps)    │
            └─────────────────────────────────────────────┘
```

`LafErp.Tests` depends on all of the above (it references `Program` for `WebApplicationFactory`
integration tests and constructs services directly for unit tests). The `playwright/` folder is a
standalone Node project and is **not** part of `LAF-EnterpriseERP.slnx`.

The dependency graph is **acyclic and strictly inward** — `Core` knows nothing about persistence or
the web; `Data` knows only `Core`; `Services` orchestrate `Data` + `Core`; `Web` composes everything.

## Layer responsibilities

### LafErp.Core
Pure domain. Contains the entity classes (grouped into `SetupEntities`, `MasterEntities`,
`TransactionEntities`, `LedgerEntities`, `OpsEntities`, `PlatformEntities`) and all enums + base types
in `Common.cs`:

- `EntityBase` — surrogate `Id`, audit timestamps (`CreatedUtc`/`UpdatedUtc`/`CreatedBy`/`UpdatedBy`)
  and a `IsDeleted` soft-delete flag.
- `DocumentBase : EntityBase` — adds `DocNo`, `DocStatus Status`, `PostingDate`, and an optimistic
  concurrency `RowVersion`.
- `DocStatus { Draft=0, Submitted=1, Cancelled=2 }` — the clean-room document lifecycle.
- `DomainException` — a dedicated type for business-rule violations, distinct from infrastructure
  errors. The API layer maps it to HTTP 400.

### LafErp.Data
`ErpDbContext` exposes 42 `DbSet`s and configures the model in `OnModelCreating`:

- All `decimal` properties get precision (18, 4).
- Soft-delete query filters on `Customer`, `Supplier`, `Item` (documents keep full history).
- Unique business keys and ledger query indexes (see [DATABASE_DESIGN.md](DATABASE_DESIGN.md)).
- A `rowversion` concurrency token is applied **only under SQL Server**; under SQLite the property is
  ignored (the test provider has no rowversion).
- `SaveChanges`/`SaveChangesAsync` stamp `CreatedUtc`/`UpdatedUtc` automatically.

There is **no migrations project**; the schema is materialised at runtime with `EnsureCreated()`. A
committed `database/schema.sql` (the SQL Server DDL) is produced via the `-- schema` switch in
`Program.cs`.

### LafErp.Services
The behavioural core. Each service is registered scoped via `AddErpServices()`. Notable services:

- **AccountingService** — double-entry GL posting + financial reports.
- **StockService** — signed stock ledger with moving-average valuation.
- **WorkflowService** — the generic submit/approve/reject engine (maker/checker, threshold, audit).
- **RbacService** — authorization checks against the role-permission matrix.
- **NumberingService**, **AuditService**, **ImportService**, plus the document orchestrators
  (`SalesService`, `PurchaseService`, `PaymentService`, `JournalService`) and operational modules
  (`CrmService`, `ProjectService`, `SupportService`, `AssetService`).

Identity is abstracted behind `ICurrentUser` (`Username` + `Roles`). Tests bind it to a mutable
`CurrentUser`; the web binds it to `HttpCurrentUser`.

### LafErp.Web
Composition root and the two delivery surfaces:

- **MVC + Razor** — `HomeController` serves 9 read-oriented pages and the dev-auth login.
- **Minimal-API REST** — `ApiEndpoints.Map(app)` registers JSON endpoints under `/api`, wrapping
  domain calls so that `DomainException` becomes a 400.
- **Startup** — `Program.cs` chooses the provider, runs `EnsureCreated` + `DataSeeder.Seed` +
  `DemoData.Post`, and (with no `HttpContext` at startup) acts as an all-roles admin so demo posts
  auto-approve.

## Bounded contexts

The single schema is internally organised into cohesive groups that act as bounded contexts:

| Context | Core entities | Owning services |
| --- | --- | --- |
| **Setup / Accounting masters** | Company, FiscalYear, Currency, Account, CostCenter, NumberingSeries, TaxTemplate | NumberingService, DataSeeder |
| **Party & item masters** | Customer, Supplier, Item, ItemGroup, Warehouse | ImportService |
| **Selling** | SalesOrder/Line, SalesInvoice/Line | SalesService |
| **Buying** | PurchaseOrder/Line, PurchaseInvoice/Line | PurchaseService |
| **Payments & Journals** | PaymentEntry, JournalEntry/Line | PaymentService, JournalService |
| **Ledgers (immutable)** | GLEntry, StockLedgerEntry | AccountingService, StockService |
| **Workflow & governance** | WorkflowDefinition/Transition, WorkflowInstance, WorkflowApproval, AuditEvent | WorkflowService, AuditService |
| **Security** | AppUser, AppRole, AppUserRole, RolePermission | RbacService |
| **CRM / Projects / Support / Assets** | Lead, Opportunity, Project, ProjectTask, SupportTicket, Asset | CrmService, ProjectService, SupportService, AssetService |
| **Platform** | ImportBatch, ReportDefinition | ImportService |

These are *logical* contexts within one process and one database — appropriate for a modular monolith.
They are not independently deployable.

## Cross-cutting concerns

- **Auditing** — `AuditService.Record(...)` appends one `AuditEvent` per state-changing action; the
  workflow engine records an event on every transition.
- **Numbering** — `NumberingService.Next(docType)` issues `PREFIX-#####` document numbers from a
  per-doctype `NumberingSeries` row.
- **Soft delete** — masters carry `IsDeleted`; global query filters hide deleted masters while
  posting code uses `IgnoreQueryFilters()` to resolve references regardless.
- **Concurrency** — `RowVersion` optimistic token on document types under SQL Server.

## Notable architectural decisions

- **EnsureCreated over migrations.** The product favours a zero-friction first run; there is no
  migration history, so schema evolution would require introducing one.
- **Reports aggregate in memory.** `TrialBalance`, AR/AP and GL totals materialise rows and sum
  decimals client-side so results are exact and identical across SQLite and SQL Server.
- **Posting via delegate.** The workflow engine takes an `onPost` `Action`; GL/stock posting happens
  only on the transition to `Submitted`, keeping the workflow generic and the posting logic in the
  accounting/stock services.
