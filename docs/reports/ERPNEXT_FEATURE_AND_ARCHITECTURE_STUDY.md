# ERPNext / Frappe Feature & Architecture Study

A clean-room study of ERPNext and the Frappe Framework it is built on, compiled from
**official public sources only** for the purpose of an independent reimplementation. No source
code or large verbatim text was copied; behaviour and structure are summarized in our own words.
Inline citations point to the official documentation and repositories.

**Primary official sources**

- ERPNext user manual: <https://docs.frappe.io/erpnext> (the legacy `docs.erpnext.com` host now
  301-redirects here)
- Frappe Framework docs: <https://docs.frappe.io/framework>
- Frappe REST API: <https://docs.frappe.io/framework/user/en/api/rest>
- Users & Permissions: <https://docs.frappe.io/framework/user/en/basics/users-and-permissions>
- DocType basics: <https://docs.frappe.io/framework/user/en/basics/doctypes>
- Workflow: <https://docs.frappe.io/erpnext/workflow>
- Chart of Accounts: <https://docs.frappe.io/erpnext/user/manual/en/chart-of-accounts>
- Accounting reports: <https://docs.frappe.io/erpnext/user/manual/en/accounting-reports>
- Asset module: <https://docs.frappe.io/erpnext/user/manual/en/asset>
- Frappe HR: <https://docs.frappe.io/hr>
- Source repos: <https://github.com/frappe/erpnext>, <https://github.com/frappe/frappe>

---

## 0. Honesty note on scope

ERPNext is **very large**. It is a mature, GPLv3 ERP that has accreted ~20 years of banking-grade
accounting, full perpetual-inventory stock, manufacturing (multi-level BOM, work orders, job
cards), procurement, sales, CRM, projects, support, assets, quality, and (historically) HR/payroll
— all built on top of the Frappe metadata framework, which is itself a complete low-code platform
(DocType engine, ORM, REST API, workflow engine, permission engine, report engine, print engine,
website/portal, and a background-job/scheduler runtime).

A faithful clean-room reimplementation is a multi-year effort. The official user manual
**module landing pages are intentionally high-level**; the real depth lives in (a) deep concept
pages, (b) the Frappe School courses, and (c) the source repositories. This study captures the
**shape** (modules, entities, lifecycle, APIs, permissions) accurately, and flags where official
prose was thin so those areas can be confirmed against source before implementation.

---

## 1. Two-layer architecture: ERPNext on Frappe

ERPNext is an **application** built on the **Frappe Framework**, a metadata-driven full-stack
framework (Python backend, JS frontend, MariaDB/MySQL database)
(<https://github.com/frappe/erpnext>). The clean separation matters for a reimplementation:

- **Frappe (platform):** DocType model, database abstraction, auto-generated REST API, role/
  permission engine, workflow engine, report engine, print formats, website/portal, background
  workers and scheduler, data import/export, and the customization layer (custom fields, property
  setters, client/server scripts). MIT-licensed.
- **ERPNext (domain app):** the business modules — accounting, stock, selling, buying,
  manufacturing, CRM, projects, support, assets, quality — each expressed as a set of DocTypes,
  controllers (server-side Python lifecycle hooks), client scripts, reports, and print formats.
  GPLv3-licensed. (<https://github.com/frappe/erpnext>)

In a from-scratch build you would effectively reimplement **both layers**: a metadata/entity
framework with a submit/cancel lifecycle and permissions, plus the domain logic that posts ledgers
on submit.

### The DocType concept (the heart of everything)

A **DocType** is metadata that defines both the data model and the default UI for an entity. Saving
a DocType generates a backing SQL table named `tab{DocType}` (e.g. `Sales Invoice` → `tabSales
Invoice`), and Frappe auto-provides List and Form views, validation, and REST endpoints
(<https://docs.frappe.io/framework/user/en/basics/doctypes>). Variants:

- **Normal** DocType — its own table, one row per document.
- **Child** DocType (`istable`) — embedded rows of a parent (e.g. invoice line items), stored with
  `parent`, `parenttype`, `parentfield`, `idx`.
- **Single** DocType (`issingle`) — exactly one record (settings pages); stored in `tabSingles`.
- **Virtual** DocType — no table; data sourced elsewhere.

Fields are typed (`Data`, `Link`, `Select`, `Currency`, `Table`, `Dynamic Link`, etc.). **Link**
fields are foreign keys to another DocType; **Dynamic Link** pairs a doctype-name field with a
record-name field (how Contact/Address attach to Customer or Supplier). Naming is controlled by a
**naming rule / naming series** (e.g. `SINV-.YYYY.-`).

---

## 2. The docstatus lifecycle and audit model

Every **submittable** DocType carries a `docstatus` integer that is the backbone of ERP integrity
(<https://docs.frappe.io/framework/user/en/api/document>):

- **0 = Draft** — editable, no ledger side-effects.
- **1 = Submitted** — locked; `on_submit` posts side-effects (GL Entries, Stock Ledger Entries,
  etc.).
- **2 = Cancelled** — `on_cancel` reverses those side-effects; the document becomes immutable.

A cancelled document can be **Amended** into a new draft (linked via `amended_from`, usually with an
amendment suffix). This draft → submit → cancel → amend cycle, with immutable ledgers, is the
*single most important behaviour to replicate*: financial and stock ledgers are never edited in
place; they are posted on submit and reversed on cancel.

**Audit trail:** beyond docstatus, Frappe records field-level change history in the **Version**
DocType for tracked doctypes (`track_changes`), plus comments/activity and (optionally) document
following. GL Entry and Stock Ledger Entry are themselves immutable append-only ledgers (cancellation
writes reversing entries / `is_cancelled` flags rather than deleting).

---

## 3. Roles, permissions, and maker-checker

Source: <https://docs.frappe.io/framework/user/en/basics/users-and-permissions>.

- **Role-based:** a User holds Roles; each DocType has DocPerm rows granting actions to roles.
- **Permission actions:** read, write, create, delete, **submit, cancel, amend**, report, export,
  import, print, email, share, set_user_permissions.
- **Permission levels (permlevel):** fields are tagged with a level (default 0). A role needs
  permission *at that level* to read/write those fields — this gives **field-level security**
  (e.g. only managers edit pricing/discount fields at level 1).
- **User Permissions:** restrict a user to documents whose link field matches an allowed value
  (e.g. only their Company or Territory). Enforced automatically on list/get/report.
- **if_owner:** a permission can apply only to documents the user created.
- **Auto roles:** Guest (anonymous), All (every signed-up user), Administrator (superuser bypassing
  checks), and Desk User (v15).
- **Maker-checker / segregation of duties** is achieved by (a) restricting `submit` to approver
  roles and/or (b) a named **Workflow** whose transitions are role-gated.

Representative ERPNext roles: System Manager, Accounts User/Manager, Stock User/Manager, Sales
User/Manager, Purchase User/Manager, Manufacturing User/Manager, Projects User, Support Team, HR
User/Manager, Leave/Expense Approver, Quality Manager, Item Manager, Auditor, Website Manager. (See
`erpnext-role-permission-inventory.json`.)

---

## 4. Workflow engine (approvals)

Source: <https://docs.frappe.io/erpnext/workflow> and the Workflow/Workflow State/Workflow Action
doctypes.

Submittable docs already have the implicit draft/submit/cancel lifecycle; **named Workflows are an
optional layer** that overlays an explicit state machine:

- **Workflow** binds to a `document_type` and stores a `workflow_state_field`.
- **Workflow Document States** (child) map each named state to a `doc_status` (0/1/2) and an
  `allow_edit` role; reaching a `doc_status=1` state submits the document.
- **Workflow Transitions** (child) define `state → next_state` via an `action`, gated by an
  `allowed` role and an optional Python `condition` (e.g. `doc.grand_total > 100000` forces an extra
  approver).
- **Workflow State** and **Workflow Action Master** are reusable label masters (with styling).
- Email alerts notify users of the next available actions.

Typical approval workflows (representative, see `erpnext-workflow-inventory.json`): Sales Order,
Purchase Order, Journal Entry, Material Request, and (Frappe HR) Leave Application and Expense
Claim. Multi-level approval is expressed as extra intermediate states/transitions, optionally
condition-gated by amount.

---

## 5. REST API and integration model

Source: <https://docs.frappe.io/framework/user/en/api/rest>.

Frappe **auto-generates** a REST API for every DocType. Two endpoint families:

- **Resource API** — `/api/resource/{doctype}`:
  - `GET` list (params: `fields`, `filters`, `or_filters`, `order_by`, `limit_start`,
    `limit_page_length` [default 20]); `GET /{name}` for one document (with child tables);
    `POST` create; `PUT /{name}` partial update; `DELETE /{name}`.
  - All calls respect the role/user permission model.
- **Method API** — `/api/method/{dotted.path}` invokes a whitelisted Python function
  (`@frappe.whitelist()`); `GET` for read-only, `POST` for state-changing (auto-commits). This is
  how submit/cancel, reports (`frappe.desk.query_report.run`), counts (`frappe.client.get_count`),
  and file upload (`upload_file`) are exposed.

**Auth:** token (`Authorization: token api_key:api_secret`), password/session
(`/api/method/login`), or OAuth2 bearer. Responses wrap payloads in `data` (resource) or `message`
(method); errors carry `exc`/`exc_type`.

**Outbound integration:** the **Webhook** DocType fires HTTP callbacks on document events
(after_insert, on_submit, …) — the inverse of the inbound API. Because DocType is itself a DocType,
schema can be discovered at `/api/resource/DocType/{name}`. (See `erpnext-api-inventory.json`.)

---

## 6. Accounting (double-entry GL)

Sources: accounts manual, chart-of-accounts, accounting-reports.

- **Chart of Accounts** is a tree of **Account** nodes. Each account has a **root_type** (Asset,
  Liability, Income, Expense, Equity) and a `report_type` (Balance Sheet vs Profit and Loss). Only
  **leaf ledger accounts** (`is_group = 0`) receive postings; **group accounts** aggregate. Accounts
  carry an `account_type` (Bank, Receivable, Payable, Tax, Depreciation, …, 20+ values) that drives
  automation. (<https://docs.frappe.io/erpnext/user/manual/en/chart-of-accounts>)
- **Double-entry GL:** submitting any accounting transaction (Sales Invoice, Purchase Invoice,
  Payment Entry, Journal Entry, and stock vouchers when perpetual inventory is on) posts **balanced
  GL Entries** (debit = credit) to the **GL Entry** ledger. GL Entry is immutable; cancellation
  posts reversing/`is_cancelled` rows.
- **Fiscal Year** scopes periods; period-close vouchers and accounting-period locks control posting.
- **Cost Center** (and accounting dimensions) tag postings for departmental analysis; **Budget**
  controls spending against cost centers.
- **Parties:** Customer/Supplier/Employee are party types on receivable/payable accounts. Payment
  Entry allocates against invoices (Payment Reconciliation handles unallocated amounts).
- **Taxes:** Sales/Purchase Taxes and Charges templates, tax rules, tax withholding (TDS),
  multi-currency with exchange-rate handling.
- **Core financial reports:** General Ledger, Trial Balance, Balance Sheet, Profit and Loss, Cash
  Flow, Accounts Receivable/Payable (with **ageing buckets**), Sales/Purchase registers, Budget
  Variance, Gross Profit (<https://docs.frappe.io/erpnext/user/manual/en/accounting-reports>). Most
  are **Script Reports** (Python-built).

---

## 7. Stock / Inventory (perpetual, ledger-based)

Source: stock manual + stock-balance/stock-ledger reports.

- **Item** master (with Item Group tree, variants via Item Attributes, UOM conversions, batch/serial
  flags, valuation method) is shared across selling, buying, stock and manufacturing.
- **Warehouse** is a tree; leaf warehouses hold stock. **Bin** caches per item-warehouse balances
  (actual/ordered/reserved/projected qty).
- **Stock Ledger Entry (SLE)** is the **immutable per-movement ledger**: `item_code`, `warehouse`,
  `actual_qty`, `qty_after_transaction`, `incoming_rate`, `valuation_rate`, `stock_value`,
  `stock_value_difference`, plus the source `voucher_type`/`voucher_no`. Stock Balance and Stock
  Ledger reports are built from SLEs.
- **Valuation:** FIFO or Moving Average (per item), with serial/batch-specific valuation. Perpetual
  inventory posts matching GL Entries (stock-in-hand vs expense/COGS) on submit.
- **Transactions that write SLEs:** Stock Entry (transfer/issue/receipt/manufacture/repack),
  Delivery Note (out), Purchase Receipt (in), and invoices when `update_stock` is set. **Material
  Request** initiates demand (purchase/transfer/issue/manufacture).
- **Stock Reconciliation** sets opening stock or adjusts to a physical count. A reposting engine
  recomputes balances/valuation for back-dated entries.

---

## 8. Sales cycle (order-to-cash) and Purchase cycle (procure-to-pay)

- **Selling:** Quotation → Sales Order → Delivery Note (stock-out) → Sales Invoice (GL). Customer
  master, Price List + Item Price, Pricing Rule (conditional discounts/free items), Product Bundle.
  Each step can be created *from* the previous, carrying forward items and tracking `per_delivered`
  / `per_billed`. (<https://docs.frappe.io/erpnext/user/manual/en/selling>)
- **Buying:** Material Request → Request for Quotation → Supplier Quotation → Purchase Order →
  Purchase Receipt (stock-in) → Purchase Invoice (GL). Supplier master, supplier scorecard,
  subcontracting. (<https://docs.frappe.io/erpnext/user/manual/en/buying>)

The pattern is symmetric: an **order** document drives a **stock** document and a **billing**
document, with completion percentages tracking fulfilment, and GL/SLE postings happening on submit
of the relevant documents.

---

## 9. Manufacturing

Source: manufacturing manual.

- **BOM (Bill of Materials):** components/raw materials, optional operations, scrap, and costing;
  supports multi-level sub-assemblies; `is_active`/`is_default`.
- **Work Order:** executes a BOM for a quantity; tracks material transferred for manufacturing,
  WIP and finished-goods warehouses, and `produced_qty`.
- **Job Card:** tracks a single **Operation** of a Work Order at a **Workstation**, with time logs.
- **Production Plan:** converts demand (sales orders / forecasts) into Material Requests and Work
  Orders.
- Stock Entries (purpose = Manufacture) consume raw materials and receive finished goods, posting
  SLEs and valuation.

---

## 10. CRM, Projects, Support, Quality, Maintenance

- **CRM:** Lead → Opportunity → (Quotation/Customer). Contact and Address are shared via dynamic
  links; Campaigns and lead sources; activity logging. (Note: a separate standalone *Frappe CRM*
  app also exists; ERPNext keeps its own CRM module.)
  (<https://docs.frappe.io/erpnext/user/manual/en/CRM>)
- **Projects:** Project → Task (dependencies, Gantt) → Timesheet (billable). Project costing/
  profitability; Activity Type/Cost; billable timesheets flow to Sales Invoice.
- **Support/Helpdesk:** Issue (ticket) with Service Level Agreement (response/resolution targets,
  holiday lists), Warranty Claim, email-to-ticket, customer portal. The modern *Frappe Helpdesk* app
  uses **HD Ticket** as the equivalent of Issue.
- **Quality:** Quality Inspection (incoming/in-process/outgoing) against templates; Quality Goal/
  Procedure/Review/Meeting; non-conformance and actions. Inspections link from Purchase Receipt,
  Delivery Note, Stock Entry and Work Order.
- **Maintenance:** Maintenance Schedule and Maintenance Visit (item/serial servicing), plus
  asset-side Asset Maintenance and Asset Repair.

---

## 11. Assets

Source: <https://docs.frappe.io/erpnext/user/manual/en/asset>.

- **Asset** and **Asset Category** (shared depreciation/account rules). Assets can be auto-created
  on Purchase Receipt, created manually, composed via capitalization, or imported as existing
  assets with historical depreciation.
- **Depreciation methods:** Straight Line, Written Down Value, Double Declining Balance, Manual —
  with frequency, salvage value, pro-rata daily, and shift-based options. The depreciation schedule
  posts **Journal Entries**.
- **Asset Movement** transfers location/custodian; assets can be scrapped, sold, or value-adjusted.
  Asset Maintenance and insurance tracking round it out. Reports: Asset Depreciation Ledger, Asset
  Depreciations and Balances.

---

## 12. HR / Payroll (now the separate Frappe HR app)

Source: <https://docs.frappe.io/hr>.

HR & Payroll were **split out of ERPNext core into the standalone Frappe HR (hrms) app** (around
v14). It covers the employee lifecycle (Employee, onboarding/separation, promotion/transfer),
Attendance (including geolocation check-in) and Shift, Leave (Leave Application/Allocation/Policy/
Type), Payroll (Salary Component → Salary Structure → Salary Slip → Payroll Entry, which posts a
Journal Entry), Expense Claim and Employee Advance with multi-level approval, Recruitment (Job
Opening/Applicant), and Performance (Appraisal/KRA). It integrates with ERPNext accounting. A
reimplementation can treat HR as a **separable bounded context**.

---

## 13. Website / Portal, eCommerce, POS

- **Website/Portal:** Frappe ships a website builder — Web Page, Blog Post, and **Web Form**
  (no-code forms backed by DocTypes) — plus a customer/supplier self-service portal (view orders/
  invoices/issues, supplier RFQ portal). Driven by the Frappe web framework.
- **eCommerce:** recent versions move the web shop into a separate **Frappe Webshop** app (item
  listing, cart, online orders).
- **POS:** POS Profile (warehouse, payment modes, price list, write-off account) configures the
  point of sale; **POS Invoice** is an offline-capable Sales Invoice; **POS Opening/Closing Entry**
  reconcile a shift's cash. Loyalty Program and Mode of Payment support retail. (See product pages,
  e.g. <https://erpnext.com/version-15>.)

---

## 14. Reports, dashboards, print formats

- **Report types (Frappe):** Report Builder (config), **Query Report** (parameterized SQL), **Script
  Report** (Python builds columns/rows — used for all the heavy financials), and dashboard charts /
  number cards. Reports are run via `frappe.desk.query_report.run`.
- **Dashboards:** module workspaces show number cards and charts (e.g. outstanding AR, sales trend);
  a global dashboard aggregates KPIs.
- **Print Formats:** every DocType has a standard auto print format; the Print Format Builder and
  Jinja/HTML formats customize invoices, POs, quotations, etc. Letterheads and translations apply.

(See `erpnext-report-inventory.json` for the key report catalogue.)

---

## 15. Customization, import/export, scripting

- **Custom Field** and **Property Setter** (via *Customize Form*) extend/override standard DocTypes
  *without forking the app* — critical to ERPNext's upgradeability.
- **Naming Series** configures auto-naming prefixes/counters per DocType.
- **Client Script** (browser JS on a form) and **Server Script** (sandboxed Python on events/API)
  add behaviour without app code; **Notification** automates emails/alerts on events.
- **Data Import / Export** (CSV/Excel) and Bulk Update handle mass data operations, mapping columns
  to DocType fields and respecting validation/permissions.

---

## 16. Deployment model

Source: Frappe installation docs and `frappe_docker`.

- **bench** is the CLI/orchestrator that manages a *bench* (a set of apps + sites) and dev runtime.
- **Database:** MariaDB/MySQL (default). **Redis** provides cache, queue, and socketio pub/sub.
- **Processes:** a WSGI app server (Gunicorn in production / Werkzeug in dev), **background workers**
  (RQ-based queues: short/default/long) draining Redis queues, a **scheduler** that enqueues
  periodic jobs, and a **Socket.IO** (Node) server for realtime. Nginx fronts static files and
  proxies; Supervisor (or systemd/Docker) supervises processes in production.
- Containerized deployment via `frappe_docker`. (<https://github.com/frappe/frappe_docker>,
  <https://docs.frappe.io/framework/user/en/installation>)

For a clean-room build, the equivalent decisions are: relational DB as system of record, a
queue/worker tier for heavy posting (reposting, payroll, bulk submit) and scheduled jobs, a realtime
channel, and a reverse proxy.

---

## 17. Common production issues (honest, from official guidance)

These are recurring operational themes documented or widely covered in official channels:

- **Background-job backlog / stuck workers** — long-running submits (reposting stock, payroll,
  bulk operations) saturate queues; the scheduler can be disabled inadvertently
  (`scheduler_disabled` / maintenance mode).
- **Stock reposting and back-dated transactions** — inserting back-dated stock entries triggers
  recomputation of subsequent SLEs/GL; large reposts are heavy and must run on the long queue.
- **Permission/visibility surprises** — User Permissions plus permlevel can hide fields or whole
  records unexpectedly; the Role Permissions Manager is the place to diagnose.
- **Cancellation/amendment chains** — cancelling a submitted document with downstream links
  (invoice → payment) requires unwinding in the correct order due to immutable ledgers.
- **Custom-field / Property Setter drift on upgrade** — customizations stored as data survive
  upgrades, but conflicting client/server scripts can break after version bumps.
- **Database migrations (`bench migrate`)** patch DocType schema and run data patches; failures mid-
  migration leave a site in maintenance mode.

---

## 18. Inventory summary (companion JSON files)

This study ships with structured inventories in `benchmarks/erpnext-study/`:

| File | Contents |
|------|----------|
| `erpnext-official-source-registry.json` | 19 official sources (docs, repos, API) with reliability + what-learned |
| `erpnext-feature-inventory.json` | 15 modules (14 business + Frappe platform) with key features |
| `erpnext-doctype-inventory.json` | ~64 of the most important DocTypes with key fields and links |
| `erpnext-api-inventory.json` | REST conventions, auth schemes, ~20 endpoints |
| `erpnext-workflow-inventory.json` | docstatus model + 6 representative approval workflows |
| `erpnext-report-inventory.json` | ~32 key reports by module and type |
| `erpnext-role-permission-inventory.json` | permission model + ~24 roles |

---

## 19. Gaps / caveats for the reimplementation team

- **Module landing pages are thin.** Stock Ledger Entry, Selling/Buying detail, and the Workflow
  internals returned only high-level prose; exact field lists and posting logic must be confirmed
  against the source repos (`github.com/frappe/erpnext`) before implementation. Field lists in the
  doctype inventory are documented behaviour summarized in our own words, not authoritative schema
  dumps.
- **Role bundles vary by version.** The standard roles listed are conventional ERPNext defaults;
  exact `DocPerm` rows differ across v13/v14/v15 and should be read from the shipped fixtures.
- **HR, CRM, Helpdesk, Webshop are now separate apps.** HR/Payroll (Frappe HR), the modern CRM,
  Helpdesk (HD Ticket), and Webshop have been extracted from ERPNext core; scope them as separate
  bounded contexts.
- **Named workflows are optional/site-specific.** The approval workflows here are *representative*
  configurations from the official workflow docs, not bundled defaults; the implicit docstatus
  lifecycle is the always-present mechanism.
- **Regional/tax/compliance** logic (country-specific tax, e-invoicing) is large and version- and
  jurisdiction-specific; treat as a pluggable layer.

---

*Compiled from official Frappe/ERPNext documentation and repositories listed in §0. No source code
or large verbatim text was reproduced; all descriptions are independent summaries for clean-room
reimplementation.*
