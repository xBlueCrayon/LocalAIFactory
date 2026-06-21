# Production-Grade Review — LAF Enterprise ERP V2 (LAF-generated)

**Subject:** `generated-products/LAF-EnterpriseERP-LAFGenerated`
**Reviewed as:** a candidate for production deployment in a banking-middleware estate.
**Provenance:** Every product file in this solution was *emitted by a generator*
(`tools/LocalAIFactory.Generator`) — 66 engine files from LocalAIFactory ERP-knowledge
templates plus 4 catalog files from a governed local-LLM (`qwen2.5-coder:14b`) proposal.
No product file was hand-edited after emission; the generator records this in
`benchmarks/results/laf-erp-v2-generation-attribution.json`.

> **Verdict up front: PILOT-grade, not production-grade.** The domain core (double-entry GL,
> immutable stock ledger, maker/checker workflow, audit, RBAC) is real, tested, and behaves
> correctly. The platform around it — authentication, transport security, persistence migration
> strategy, write-path UI, and several whole ERP modules — is not present. This is a credible
> internal pilot / reference implementation, not something to put in front of money movement.

---

## 1. Architecture

Clean, acyclic five-project .NET 10 solution (`LAF-EnterpriseERP-LAFGenerated.slnx`):

| Project | Depends on | Role |
|---|---|---|
| `LafErp.Core` | — | Entities, enums, `EntityBase`/`DocumentBase`, `DomainException` |
| `LafErp.Data` | Core | `ErpDbContext`, model configuration, indexes |
| `LafErp.Services` | Core, Data | All domain services (accounting, stock, workflow, RBAC, import, ops) |
| `LafErp.Web` | Core, Data, Services | ASP.NET Core MVC + minimal-API endpoints, dev-auth, DI root |
| `LafErp.Tests` | all | xUnit + WebApplicationFactory integration tests |

Plus a sibling `playwright/` project (Node) for browser smoke tests. Layering is correct:
controllers and API endpoints delegate to services; business rules live only in services and the
domain core. There are no project-reference cycles. Money/quantity decimals are configured to
`precision 18, scale 4` globally in `OnModelCreating`.

**Good.** The boundaries are clean enough that the engine could be lifted onto a different host.

---

## 2. Domain model & schema

`ErpDbContext` defines ~40 entity sets spanning setup (Company, FiscalYear, Account, CostCenter,
NumberingSeries, TaxTemplate, Currency), masters (Customer, Supplier, Item, ItemGroup, Warehouse),
transactions (Sales/Purchase Order + Invoice + lines, PaymentEntry, JournalEntry + lines), ledgers
(`GLEntry`, `StockLedgerEntry`), ops (Lead, Opportunity, Project, ProjectTask, SupportTicket,
Asset), and platform (Workflow definition/transition/instance/approval, AuditEvent, AppUser/Role,
RolePermission, ImportBatch, ReportDefinition). The three generated catalog entities
(`CustomerSegment`, `PaymentTerm`, `TaxCode`) extend `EntityBase`.

Schema quality is reasonable:

- Unique business keys on `Customer.Code`, `Supplier.Code`, `Item.Code`, `Warehouse.Code`,
  `Account(CompanyId,Code)`, `AppUser.Username`, `RolePermission(RoleName,DocType)`, and document
  numbers (`SalesInvoice.DocNo`, etc.).
- Ledger query indexes on `GLEntry(AccountId,PostingDate)`, `(PartyType,PartyId)`,
  `(VoucherType,VoucherNo)` and `StockLedgerEntry(ItemId,WarehouseId,Id)`.
- Soft-delete query filters on master data only; documents retain full history.
- An optimistic-concurrency `RowVersion` is applied **only on SQL Server** and ignored on SQLite.

**Missing for production:** there are **no EF migrations**. Schema is created with
`Database.EnsureCreated()` at startup (`Program.cs`). That is fine for a pilot on a throwaway
database but is unacceptable for an estate that needs versioned, reviewable, forward-only schema
change. This is the single largest persistence gap.

---

## 3. Services & transaction boundaries

Domain logic is concentrated in `LafErp.Services`. The key services:

- **`AccountingService`** — posts balanced GL for sales/purchase invoices, payments and journals;
  produces General Ledger, Trial Balance, AR/AP and `GlTotals`. Reversal writes mirrored entries
  rather than deleting.
- **`StockService`** — immutable signed stock ledger with carried-forward moving-average valuation.
- **`WorkflowService`** — generic submit/approve/reject engine; posting happens via a
  caller-supplied `Action onPost` delegate only on the transition to Submitted.
- **`SalesService` / `PurchaseService` / `PaymentService` / `JournalService`** — orchestrate
  numbering → workflow → posting and call `SaveChanges()`.

**Transaction-boundary caveat (production concern):** services call `_db.SaveChanges()` directly;
there is **no explicit database transaction / unit-of-work wrapping a submit**. On the in-memory
SQLite test provider this is invisible, and on a single `SaveChanges` the GL writes are atomic, but
a submit that spans *posting + document status update + audit* across more than one `SaveChanges`
(e.g. `WorkflowService.Submit` mutates the instance, then the caller saves) is not wrapped in an
ambient transaction. For a banking estate this should be a single committed transaction with
rollback on any domain failure. Today, correctness rests on the happy path and on domain checks
throwing *before* the first write.

---

## 4. Financial, stock and workflow controls (the strong part)

These are real and covered by tests:

- **Double-entry that refuses to unbalance.** `PostJournalEntry` throws `DomainException` if
  `debit != credit`, if a line is both debit and credit, or if any amount is negative. Invoice and
  payment postings are constructed as balanced pairs. The integration test asserts the seeded demo
  trial balance balances (debit == credit), and `RealLifeScenarioTests` asserts `GlTotals` balance
  after a full buy/sell/pay cycle.
- **Immutable stock ledger + moving-average + negative-stock guard.** `MoveOut` throws when
  `qty > on-hand` unless `AllowNegativeStock` is set (default false). Valuation is carried forward.
- **Maker/checker.** `WorkflowService.Approve` throws *"the submitter may not approve their own
  document"* when `SubmittedBy == current user` and `MakerCannotApprove` is set (default true).
  Amounts at or below the per-document threshold auto-approve; above threshold a separate approver
  with the approver role is required. `Reject` requires a non-empty reason.
- **Immutability / reversal-not-edit.** `SalesService.EditRate` refuses to edit a submitted invoice
  (*"cancel and reissue instead"*); `Cancel` writes reversing GL + stock entries rather than
  deleting. Tests confirm cancel returns AR to its starting value and GL still balances.
- **Audit.** `AuditService.Record` appends one event per state change; the real-life test asserts
  ≥ 5 audit events and a Submit/Approve trail.
- **RBAC.** `RbacService.Can/Demand` checks a seeded role-permission matrix; a parameterized test
  asserts the matrix (e.g. a Sales User may submit but not approve a SalesInvoice).

**This layer is production-*shaped* and would survive scrutiny.** The gap is everything *around* it.

---

## 5. Exception handling & validation

- Domain violations use a dedicated `DomainException`, distinct from infrastructure errors.
- The API layer maps `DomainException → HTTP 400` via a `Guard` wrapper; everything else bubbles to
  500. `Insufficient_stock_returns_400_not_500` proves a domain rule does not crash the process.
- Input validation lives in services (positive quantity, non-negative rate, known customer,
  non-empty invoice, positive payment) and is unit-tested.

**Gap:** validation is code-level only. There is no model-level `[Required]`/data-annotation layer,
no FluentValidation, and the catalog write path validates only a reflective required-`Name` check.

---

## 6. API design

`ApiEndpoints.Map` exposes a REST-ish surface under `/api`: GET collections for customers, suppliers,
items, warehouses, sales/purchase invoices, payments, journals, stock-ledger; reports
(general-ledger, trial-balance, stock-balance, ar-ap); workflows; audit; and a `/health` probe.
Write endpoints exist for sales invoices (create / submit / approve / cancel) and the generated
catalog POSTs. List endpoints project to anonymous DTOs (they do not leak full entities), which is
the right instinct for response shaping.

**Gaps for production:** no authentication/authorization on the API itself (the RBAC `Demand` gate
is only wired on the sales-invoice create path), no pagination contract (lists use `Take(100/200)`),
no API versioning, no OpenAPI/Swagger document, and no content negotiation beyond JSON.

---

## 7. UI safety

Nine Razor pages (dashboard, customers, items, sales invoices, general ledger, stock balance,
workflow inbox, audit log, dev-auth login) plus the generated `/Catalog` page. The UI is
**read-oriented**: every Home page is a server-rendered list/report. The only write paths in the UI
are the dev-auth login form and the catalog POST (exercised via API). There are **no create/edit
forms** for invoices, customers, items, etc. — records are created through services/API.

This is *safe by omission* (little to attack in the UI), but it also means the product is not
operable end-to-end by a non-technical user from the browser. Controllers correctly hold no business
logic and read through services.

---

## 8. Tests & Playwright

- **xUnit: 82 cases pass.** Breakdown by counting `[Fact]` + expanded `[Theory]`/`[InlineData]`:
  Accounting 9, Stock 6, Workflow 9, Ops/Import 11, Controls+Validation 14, API 25 (incl. 22 route
  inline cases), Real-life scenario 1 → **74 engine cases**; plus **6** generated-catalog CRUD cases,
  **1** generation-provenance reflection test, and the **1** real-life scenario is already counted in
  engine. (74 engine + 6 catalog + 1 provenance + 1 real-life = 82.)
- `ApiTests` boots the **real** app over an isolated SQLite file via `WebApplicationFactory` and
  asserts 13 API routes + 9 UI routes return 200, the demo trial balance balances, and create→submit
  works.
- **Playwright:** a Chromium suite launches the live app on SQLite and smoke-tests every page + the
  health API, capturing screenshots (`playwright/screenshots/`). The committed
  `test-results/.last-run.json` is `passed`. **Honest note:** the committed
  `playwright-report.json` is a *stale* artifact from the V1 output path
  (`generated-products/LAF-EnterpriseERP`) and reports **12 passed**; the current spec set
  (`erp.spec.ts` = 11 tests + `real-life-login-and-navigation.spec.ts` = 1 test that walks 9 pages
  including `/Catalog`) is what is in this tree. Treat the report.json count as indicative, not
  exact.

**Coverage gap worth flagging:** the **catalog POST endpoint is not covered by any test.** The
xUnit catalog tests construct `CatalogCrudService<T>` directly (bypassing DI), and Playwright only
issues GETs and reads the catalog count page. See the related DI finding in
`CODE_REVIEW_REPORT.md` §Findings — the open-generic `CatalogCrudService<>` registration line is
emitted *after* `return services;` in `ServiceRegistration.cs` and is therefore unreachable, so a
live `POST /api/catalog/*` would fail to resolve the service. This is masked precisely because
nothing tests that path.

---

## 9. Deployment

`Program.cs` uses SQL Server when a `Default` connection string is present, otherwise a local SQLite
file (`laferp.db`) — so the product runs on any host with zero external services, consistent with
LocalAIFactory's MSSQL-only / no-cloud posture. Schema + demo data are created on startup.
A `dotnet run -- schema` mode prints SQL Server DDL.

**Gaps:** no `Dockerfile`/IIS publish profile in the product, no health/readiness separation, no
configuration for secrets/connection strings beyond appsettings, and (as above) no migration story.

---

## 10. Security

Summarized here; full detail in `SECURITY_REVIEW_REPORT.md`.

- **Real & tested:** RBAC permission matrix, maker/checker separation, mandatory reject reason,
  immutable posted documents, append-only audit trail.
- **Not present:** real authentication (identity is a plaintext `erp_user` / `erp_roles` **cookie**
  chosen on a dev login page — trivially spoofable), TLS enforcement, at-rest encryption, secrets
  management, CSRF protection on the dev-auth form, 2FA/SSO. `HttpCurrentUser` defaults an
  unauthenticated request to `admin` with all roles.

**This is not production-secure and must not be exposed beyond a trusted pilot network.**

---

## 11. Performance

See `PERFORMANCE_REVIEW_REPORT.md`. Hot read paths are indexed; pages render in tens of
milliseconds on SQLite in the smoke tests; reports aggregate in memory after a projected pull (exact
decimal sums on every provider). No production load test has been run (out of scope for a generated
pilot).

---

## 12. Explicit "missing for production" list

| # | Missing | Severity |
|---|---|---|
| 1 | Real authentication (cookie dev-auth only; defaults to admin/all-roles) | **Blocker** |
| 2 | TLS enforcement, at-rest encryption, secrets management, CSRF, 2FA/SSO | **Blocker** |
| 3 | EF migrations (uses `EnsureCreated`; no versioned schema change) | **Blocker** |
| 4 | Explicit transaction/unit-of-work around multi-step submits | High |
| 5 | API auth/authorization on all endpoints; pagination; versioning; OpenAPI | High |
| 6 | Write-path UI (create/edit forms) — UI is read-only today | High |
| 7 | Whole modules absent: Manufacturing, HR/Payroll, POS, Website/eCommerce | High (scope) |
| 8 | Trading docs absent: quotation, delivery note, receipt, returns | Medium |
| 9 | Financial statements absent: P&L, Balance Sheet, AR/AP aging | Medium |
| 10 | Test coverage for the catalog POST path (currently untested) | Medium |
| 11 | Structured logging, metrics, distributed tracing, audit-of-reads | Medium |
| 12 | Stale `playwright-report.json` (V1 path) — regenerate against this tree | Low |

---

## 13. Conclusion

LAF Enterprise ERP V2 is a **well-formed, honestly-scoped pilot**. Its accounting, inventory and
governance engine is correct and tested to a standard you would expect from a deliberate reference
implementation — and notably, that engine was *emitted by a generator from encoded ERP knowledge*,
not hand-written. The reasons it is not production-grade are concentrated and well-understood:
authentication/transport security, schema migration, transactional rigor, write-path completeness,
and four missing functional domains. None of these are subtle; all are tracked above.

**Recommendation:** suitable as an internal pilot / generator-capability proof and as the seed for
hardening. Do not deploy against real money movement until items 1–4 are closed.
</content>
</invoke>
