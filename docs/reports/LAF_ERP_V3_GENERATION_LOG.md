# LAF Enterprise ERP V3 — Generation Log

**Date:** 2026-06-21
**Product:** LAF Enterprise ERP V3
**Target:** `generated-products/LAF-EnterpriseERP-V3`
**Honesty note:** V3 is **PILOT-grade**, not production-grade. This log records what was generated and how, with no overclaim.

## Command

The product was emitted by the LocalAIFactory data-driven generator:

```
tools/LocalAIFactory.Generator
```

driven by a module-spec JSON plus the templated accounting/stock/workflow engine.

## Inputs

| Input | Path |
|-------|------|
| Module spec | `tools/LocalAIFactory.Generator/specs/erpnext-grade-modules.json` |
| Spec schema | `tools/LocalAIFactory.Generator/specs/erp-module-spec.schema.json` |
| Requirement | `benchmarks/erpnext-study/laf-erp-v3-production-grade-requirement.md` |

## What was generated

- **73 product files total** (`totalProductFiles: 73`).
- **71 template/engine files** (accounting, stock, workflow, RBAC, web, tests scaffold).
- **15 generated CRUD modules**:
  - **10 from the deterministic module spec:** BillOfMaterials, WorkOrder, QualityInspection, Employee, SalaryComponent, PosProfile, WebProduct, MaintenanceSchedule, CustomFieldDef, NotificationRule.
  - **5 from the governed local-LLM proposal:** CustomerSegment, ProductCategory, EmployeeRole, MarketingCampaign, VendorContract.
  - **1 LLM-proposed entity rejected** by the collision guard: `Supplier` (collided with a core engine entity).

Each generated CRUD module emits: entity record + `DbSet` + generic `CatalogCrudService<T>` registration + REST `GET`/`POST` + a `/Catalog` list page + 2 tests.

## Attribution (autonomy)

Source: `benchmarks/results/laf-erp-v3-generation-attribution.json`

| Classification | Files |
|----------------|-------|
| `LAF_GENERATED` | 71 |
| `LOCAL_LLM_PROPOSAL_USED` | 2 |
| **Total product files** | **73** |
| **Generation autonomy** | **100%** |
| **Manual product-source edits** | **0%** |

Autonomy = (LAF_GENERATED + LAF_GENERATED_THEN_FIXED_BY_LAF + LOCAL_LLM_PROPOSAL_USED) / total product files. The generator itself is infrastructure and is excluded from the product denominator.

## Build / test result

- **Build:** green (`dotnet build LAF-EnterpriseERP-V3.slnx`).
- **.NET tests:** **108 xUnit pass**.
- **Browser tests:** **13 Playwright (Chromium) pass** with login + navigation + 11 screenshots.
- **Live run:** http://localhost:5082 (SQLite), 13/14 probed endpoints returned 200 (the one miss was a probe-URL typo, not an app error), 0 unhandled exceptions.

## Honest verdict

V3 is genuinely generated (100% autonomy, 0% manual product-source edits) and builds and tests green, but it remains a **PILOT-grade** ERP: the CRUD modules are list/read skeletons, and production gates for real auth, TLS, and external pen-test are not met (see `LAF_ERP_V3_PRODUCTION_GRADE_REVIEW.md`).
