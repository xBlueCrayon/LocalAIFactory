# Scenario: Indigo Trading Ltd (Mauritius) — Small-Business Accounting, Payroll & Inventory

> **Synthetic scenario.** Indigo Trading Ltd is a fictional company invented for the LocalAIFactory
> enterprise capability simulation suite. This scenario is *inspired by* the general category of
> small-business accounting/payroll/inventory software. It is **not** a clone of, nor compatible
> with, nor equivalent to, nor derived from any vendor product, manual, or data format. No vendor
> certification, compatibility, or equivalence is implied.
>
> **Finance content is illustrative only and is not accounting, tax, or legal advice.**
> **Mauritius statutory references are awareness-only** and must be confirmed with a qualified local
> professional before any real-world use.

---

## Business Problem

Indigo Trading Ltd is a fictional Port Louis-based importer and wholesaler of homeware and small
electricals, with ~45 employees and three cost centres (Warehouse, Retail Counter, Admin). Today the
finance team runs the books across disconnected spreadsheets, a desktop ledger of unknown provenance,
and a separate stock list. The consequences are concrete:

- Month-end close takes 9–12 working days because journals, bank reconciliation, and stock valuation
  are reconciled by hand across files that drift out of sync.
- There is no enforced segregation of duties: the same clerk can raise, post, and pay an invoice.
- Inventory on the floor and inventory in the books disagree by 4–7% at every stock count, with no
  audit trail explaining the movement.
- Payroll is run in a spreadsheet; statutory deductions are computed manually and are error-prone.
- Management cannot get a trustworthy trial balance or aged-debtor view on demand.

The company wants a single, auditable, local-first system that owns the **General Ledger (GL)**,
**Accounts Payable/Receivable (AP/AR)**, **Inventory**, and **Payroll**, with a defensible audit
trail and a faster, controlled period close.

## Current-State Process

- **Sales:** Counter and wholesale orders are written on a pad, keyed into a spreadsheet invoice
  template, and emailed as PDF. Receivables are tracked in a second spreadsheet.
- **Purchasing:** Purchase orders are informal; supplier invoices are filed in a binder and entered
  into the desktop ledger when someone has time.
- **Inventory:** A "master stock" spreadsheet is edited directly. Goods-received and goods-issued are
  not consistently recorded; shrinkage is discovered only at the annual count.
- **Banking:** Bank statements are reconciled monthly by visually matching lines.
- **Payroll:** A monthly spreadsheet computes gross-to-net per employee; deductions and contributions
  are typed in by hand.
- **Reporting:** Trial balance and P&L are assembled manually at month-end. There is no reliable
  drill-down from a report figure to its source documents.

## Target-State Process

A single ASP.NET Core MVC application backed by MSSQL that enforces:

- **Document-driven posting:** Every financial effect originates from a source document (sales
  invoice, purchase invoice, payment, stock movement, payroll run) that posts a balanced journal to
  the GL. Nothing edits the ledger directly.
- **Maker/checker on financial postings:** A *maker* drafts a transaction; a *checker* with the right
  role approves it before it posts. Below a configurable threshold, auto-approval may apply per
  policy.
- **Perpetual inventory:** Goods-received and goods-issued movements update stock-on-hand and a
  chosen valuation basis (e.g., weighted average) in the same transaction that posts to the GL.
- **Controlled period close:** Periods can be opened, soft-closed (no new postings, adjustments only),
  and hard-closed (locked). Closing runs validations (e.g., unbalanced batches, unreconciled control
  accounts) before locking.
- **Append-only audit trail:** Every post, approval, reversal, and master-data change writes an
  immutable audit record.

## Users and Roles

- **Finance Clerk (Maker):** Drafts invoices, payments, journals, and stock movements. Cannot
  self-approve above threshold.
- **Finance Manager (Checker):** Approves/rejects drafted transactions; runs period close.
- **Payroll Officer:** Manages employee records and payroll runs; cannot post GL journals outside
  payroll.
- **Inventory Controller:** Manages items, locations, and stock counts; raises adjustments for
  checker approval.
- **Auditor (read-only):** Full read access to ledgers, documents, and the audit trail; no write.
- **Administrator:** Manages users, roles, chart of accounts structure, and system parameters; cannot
  approve their own financial transactions (segregation of duties).

## Data Entities

- **ChartOfAccounts** (AccountCode, Name, Type {Asset, Liability, Equity, Income, Expense},
  IsControlAccount, ParentAccountId, IsActive)
- **JournalBatch** (BatchId, PeriodId, Status {Draft, PendingApproval, Posted, Reversed},
  MakerUserId, CheckerUserId, CreatedUtc, PostedUtc, Description)
- **JournalLine** (LineId, BatchId, AccountCode, Debit, Credit, CostCentre, Narrative)
- **Period** (PeriodId, FiscalYear, PeriodNo, Status {Open, SoftClosed, HardClosed}, OpenedUtc,
  ClosedUtc)
- **Customer / Supplier** (party master, payment terms, control-account linkage)
- **SalesInvoice / PurchaseInvoice** (header + lines, status, links to posted JournalBatch)
- **Payment / Receipt** (allocation to invoices, bank account, reconciliation status)
- **InventoryItem** (Sku, Name, UnitOfMeasure, ValuationBasis, ReorderLevel)
- **StockMovement** (MovementId, Sku, LocationId, Quantity, Direction {In, Out, Adjust},
  UnitCostSnapshot, SourceDocumentRef, PostedJournalBatchId)
- **Employee** (master), **PayrollRun** (header), **PayrollLine** (per-employee gross/deductions/net)
- **AuditEvent** (EventId, UtcTimestamp, ActorUserId, Action, EntityType, EntityId, BeforeHash,
  AfterHash, Detail) — append-only

## Integrations

- **Bank statement import:** Ingest a delimited bank export (a neutral, internally-defined CSV layout
  — not any vendor format) for reconciliation matching.
- **Outbound documents:** Generate PDF invoices/payslips from internal templates.
- **Statutory export (awareness-only):** Produce a generic, internally-defined export that a Mauritius
  payroll/tax professional could use as an input. The platform does **not** file anything and makes no
  compliance guarantee.
- **No external AI dependency required:** Must run MSSQL-only (no GPU, no Ollama, no Qdrant) and still
  render every page and post every transaction.

## Security and Audit Controls

- **Authentication & RBAC:** Role-based authorization on every controller action; deny-by-default.
- **Segregation of duties:** Enforced maker/checker; no user may approve their own draft above
  threshold; admins cannot approve financial transactions.
- **Append-only audit trail:** Every financial post, approval, reversal, and master-data change is
  recorded with actor, timestamp, and before/after hashes. Audit records are never updated or deleted.
- **IDOR protection:** All document access is scoped server-side to the user's permitted entities.
- **Immutable posted ledgers:** Posted journals are corrected only by reversing entries, never edited.
- **Secrets:** No credentials in source; connection strings via environment/local override.

## Reporting Requirements

- **Trial Balance** as at any date, with drill-down to journal lines and source documents.
- **Profit & Loss** and **Balance Sheet** by period and cost centre.
- **Aged Debtors / Aged Creditors** (e.g., current, 30/60/90+).
- **Inventory Valuation** and **stock movement history** per SKU/location.
- **Payroll summary** per run (gross, deductions, net, employer contributions) — figures illustrative,
  *not tax advice*.
- **Audit report:** filterable view of the append-only audit trail.

## Failure Modes

- **Unbalanced journal:** A batch whose debits ≠ credits must be rejected at draft validation, never
  posted.
- **Posting into a closed period:** Must be blocked with a clear error; only an open or soft-closed
  (adjustment) period accepts postings per policy.
- **Negative or oversold stock:** Issuing more than stock-on-hand must be blocked or flagged per
  configured policy, never silently allowed.
- **Self-approval:** Maker == Checker above threshold must be refused.
- **Duplicate posting / double-submit:** Idempotency on document posting to prevent duplicate journals.
- **Partial failure mid-post:** Document post + GL journal + stock movement must be one transaction;
  partial commits must roll back fully.
- **MSSQL-only mode:** With no AI services present, all pages load and all postings succeed.

## Acceptance Criteria

(See `acceptance-criteria.md` for the full measurable checklist.) At a minimum, a solution design is
acceptable only if it: posts only balanced, document-sourced journals; enforces maker/checker and
segregation of duties; keeps an append-only audit trail; supports open/soft-close/hard-close periods
with pre-close validation; keeps inventory and GL consistent within one transaction; and renders all
core reports MSSQL-only.

## Expected Architecture (ASP.NET Core MVC + MSSQL + EF Core)

- **Web (MVC):** Controllers per domain area (Ledger, Sales, Purchasing, Inventory, Payroll,
  Reporting, Admin). Razor views for lists/detail; lightweight projection records for list views
  (never materialize large text/line collections into list grids).
- **Core:** Entities, enums, value objects (Money, AccountCode), domain services (posting engine,
  period-close validator, valuation calculator), abstractions.
- **Data:** `AppDbContext` (EF Core), migrations, additive schema changes only. Posting wrapped in an
  explicit transaction; concurrency tokens on balances and periods.
- **Posting engine:** A single service that, given a source document, builds a balanced
  `JournalBatch`, applies maker/checker state, updates control accounts and inventory, and writes the
  audit event — all in one DB transaction.
- **Authorization:** Policy-based RBAC; segregation-of-duties checks in the approval handler.
- **No request-path dependency on external services;** health is read from a cached snapshot.

## Expected Tests

- **Unit:** balanced-journal validation; weighted-average cost recalculation on receipt/issue;
  aged-bucket calculation; maker==checker rejection; closed-period rejection.
- **Integration:** end-to-end document post (invoice → journal → control account → audit) in one
  transaction; rollback on injected failure; period soft-close blocks new posting but allows
  adjustment; hard-close locks.
- **Concurrency:** two simultaneous stock issues on the same SKU do not oversell or corrupt valuation.
- **Authorization:** each role can only reach permitted actions; IDOR attempt on another party's
  invoice is denied.
- **MSSQL-only smoke:** every core page returns quickly with no AI services running.

## Expected Deployment Concerns

- IIS / Kestrel behind a reverse proxy; runs on the bank's internal network, no internet.
- MSSQL connection via environment variable; Data Protection keys in a git-ignored `keys/` folder.
- Automatic migrate + seed on startup; first-run creates chart of accounts skeleton and roles.
- Backups: regular MSSQL backup with tested restore; audit trail included.
- Capacity: small dataset, but reports must stay sub-second via indexed projections.

## Rollback Considerations

- **Financial rollback is by reversal, not edit:** a wrongly posted batch is reversed with a
  mirror-image journal; the original remains for audit.
- **Deployment rollback:** schema changes are additive and backward-compatible so a previous app
  version can run against the new schema; keep a tested DB backup before each release.
- **Period reopen:** reopening a hard-closed period is a privileged, audited action requiring checker
  authority and a recorded reason.
- **Data migration rollback:** any import is staged and validated before posting; a failed import
  leaves no partial ledger effect.

## CEO/CTO Summary

Indigo Trading replaces a fragile spreadsheet patchwork with one local-first, auditable system that
owns the ledger, receivables/payables, inventory, and payroll. The design's value is **control and
trust**: every number on a report drills down to a balanced, document-sourced journal; every change
is approved under maker/checker and recorded in an append-only audit trail; and period close becomes
a validated, repeatable process rather than a two-week scramble. It runs entirely on the internal
network with only SQL Server required, so there is no cloud or external-service dependency to fail.
The risk posture is conservative: immutable posted ledgers, reversal-only corrections, additive
schema changes, and tested backups. *Financial and statutory specifics in this document are
illustrative awareness only and are not accounting, tax, or legal advice.*
