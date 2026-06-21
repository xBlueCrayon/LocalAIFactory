# ERP Generator Full-Suite Upgrade

## The headline upgrade: generated CREATE/EDIT UI forms

The V4 generator emitted CRUD modules with APIs and list pages but **no create UI** — a
real gap. The upgraded generator now emits, for **every generated module**:

- A reflection-driven `CatalogController.Create` — a **GET** action that builds the form and
  a **POST** action that persists the record **with audit fields**.
- A generated `Create.cshtml` form.
- **Create** links on the `/Catalog` page.

This is proven by a **Playwright test** that fills the generated form and then verifies the
persisted record via the module's API (not just a UI assertion).

## Capabilities now covered (and the honest gaps)

The generator now covers create-and-read flows across the suite. Remaining UI gaps:
**edit** and **delete** plus **list-detail** views are not yet generated — V5 added **CREATE**
only. Treat any "full CRUD UI" phrasing as create+read today.

Capabilities the generator covers across the 29 V5 modules:

| # | Capability | Status |
|---|---|---|
| 1 | CRUD entity scaffolding (24 spec + 5 governed local-LLM) | Done |
| 2 | REST API per module | Done |
| 3 | List page (`/Catalog`) | Done |
| 4 | **Create form (GET) + persist (POST) with audit** | **New this sprint** |
| 5 | Create links on catalog page | New this sprint |
| 6 | Audit fields on writes | Done |
| 7 | Double-entry GL engine | Done |
| 8 | P&L report | Done |
| 9 | Balance Sheet report | Done |
| 10 | Trial balance | Done |
| 11 | Credit Note module | New vs V4 |
| 12 | Debit Note module | New vs V4 |
| 13 | Stock Reconciliation module | New vs V4 |
| 14 | Price List module | New vs V4 |
| 15 | Job Card module | New vs V4 |
| 16 | Leave Application module | New vs V4 |
| 17 | Quotations / selling chain modules | Done |
| 18 | Purchasing chain modules | Done |
| 19 | Work order module | Done |
| 20 | Employee master | Done |
| 21 | Maker/checker workflow | Done |
| 22 | Knowledge-validated entity selection (spec + collision guard) | Done |
| 23 | SQLite run target | Done |
| 24 | MSSQL support via connection string | Done |
| 25 | Deployable published artifact | Done |
| — | **Edit / delete UI** | **Gap** |
| — | **List-detail views** | **Gap** |
| — | **EF migrations (uses `EnsureCreated`)** | **Gap** |
| — | **MRP / payroll / POS / storefront / returns depth** | **Gap** |

## Knowledge usage

`benchmarks/results/erp-generator-knowledge-usage-v2.json`: catalogues **10 ERP packs /
296 items**, maps **29 modules**; **only validated entities are emitted** (spec-driven plus a
collision guard). Autonomy was **100%** with **0 manual product-source edits**.
