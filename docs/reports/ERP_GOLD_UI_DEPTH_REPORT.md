# ERP-GOLD-DEPTH — UI Depth Report

**Sprint:** ERP-GOLD-DEPTH · **Branch:** `ke-008-code-symbols` · **Stamp:** 2026-06-21
**Companion data:** `benchmarks/results/erp-gold-ui-depth.json`

## Existing UI (unchanged this sprint)

- **Dashboard / Home** (`Home/Index`).
- **Login** — real PBKDF2 auth, cookie, audited.
- **Workflow inbox** (`Home/WorkflowInbox`).
- **Catalog** — Create / List / Index / Edit / **Deactivate** (soft-delete) for master + catalog
  entities (`Catalog` controller and views).
- Read views: Customers, Items, SalesInvoices, GeneralLedger, StockBalance, AuditLog.

## API surface (where this sprint's depth landed)

- **Report REST API** — `/api/reports/*`.
- **Manufacturing REST API** — `/api/boms`, `/api/production-orders` (+ issue / complete).
- **Document action endpoints** — sales-invoice submit / approve / cancel.
- **Catalog CRUD endpoints**.

## Playwright

51 tests total (all green), including the new `reports-api.spec.ts` (13 tests: a report/manufacturing
endpoint loop plus tax-summary shape and purchase-register assertions).

## Honest limitations / not done

The following were **NOT** added this sprint:

- **Bespoke per-document submit/approve/cancel UI buttons** — these document actions are exercised
  via the REST API and xUnit/Playwright, not new Razor pages.
- **Dedicated Razor pages** for every manufacturing/report document.
- **BI dashboards / charts** in the UI.
- **Print/report designer** UI.

Depth this sprint went into **services, REST API and tests**, not into new bespoke UI pages. UI
completeness sits at **78/100**; the existing CRUD/dashboard/workflow-inbox UI is unchanged.
