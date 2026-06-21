# LAF Enterprise ERP V5

**Authoritative current status:** [`docs/reports/CURRENT_STATUS.md`](../reports/CURRENT_STATUS.md)
**Path:** `generated-products/LAF-EnterpriseERP-V5/`
**Status:** `ERP_PILOT_READY` · **current** product (supersedes V1–V4)
**Verified:** 2026-06-21 · Commit `96fbbc4`

## Status at a glance

| Metric | Value |
|---|---|
| Build | ✅ 0 errors |
| Tests | ✅ **134 .NET + 14 Playwright** pass |
| Classification | `ERP_PILOT_READY` |
| ERPNext parity | ~**48%** |
| Production-grade | ~**57%** |
| Generation autonomy | **100%** (0 manual product edits) |
| CRUD modules | **29** |

This is a high pilot-grade ERP generated entirely by the LocalAIFactory generator with **zero manual
edits to the product**. It is **not** ERPNext free-grade and **not** production-grade — see the honest
position below.

## Implemented

- **Double-entry general ledger** with a typed, hierarchical chart of accounts.
- **P&L and Balance Sheet** classification and reporting.
- **Stock ledger.**
- **Maker/checker, audit trail, and RBAC** controls.
- **Generated create UI forms** for the modules.
- **REST APIs** for the modules.
- **29 CRUD modules** spanning accounting/selling/buying, inventory/manufacturing,
  hr/pos/ecommerce, customization/maintenance, and general ERP.
- **Local-production publish** that runs on **SQLite** and is **MSSQL-compatible**.

## Missing

**Local depth (backlog):**
- EF migrations
- Edit / delete UI (create forms exist; full edit/delete UI does not)
- Backup / restore drill
- Full module depth (MRP, payroll, POS, storefront, returns-posting)

**External gates (not faked):**
- Real authentication (Entra/OIDC)
- CA-issued TLS
- Independent external security review
- Signed customer acceptance / pilot

## Build / run / publish (local)

From the repository root:

```powershell
# Build the generated product (source-only in repo)
dotnet build generated-products/LAF-EnterpriseERP-V5

# Publish a local-production build (SQLite; MSSQL-compatible)
pwsh scripts/erp-v5/publish-local-production.ps1

# Run the published app
C:\LAFEnterpriseERP-V5\LafErp.Web.exe
```

## Knowledge provenance

ERP V5 is generated from the catalogued ERP knowledge packs (11 packs / 322 items, 28+ modules
mapped). See [`docs/knowledge-engine/GENERATOR_KNOWLEDGE_USAGE.md`](../knowledge-engine/GENERATOR_KNOWLEDGE_USAGE.md).

## Why it is not ERPNext-grade

ERPNext free-grade implies deep, mature modules (full MRP, payroll, POS, storefront, returns
posting), production hardening (migrations, backup/restore, edit/delete UI everywhere), and external
assurance (real auth, CA TLS, security review). ERP V5 delivers the **accounting and stock core plus
broad CRUD breadth at ~48% ERPNext parity / ~57% production-grade** — genuinely pilot-ready, but with
the local-depth backlog and external gates above still open. We claim **pilot-ready**, not parity.

## Version history

V1 (hand-built reference) → V2 (template-copy generation) → V3 (data-driven, +P&L/Balance Sheet) →
V4 (expanded spec) → **V5 (current)**. Progression: parity 36→48%, tests 74→134, modules 0→29. See
[`docs/reports/ERP_V1_V2_V3_V4_V5_VS_ERPNEXT_COMPARISON.md`](../reports/ERP_V1_V2_V3_V4_V5_VS_ERPNEXT_COMPARISON.md).
