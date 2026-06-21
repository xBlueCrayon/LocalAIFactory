# Generator Knowledge Usage

**Authoritative current status:** [`docs/reports/CURRENT_STATUS.md`](../reports/CURRENT_STATUS.md)
**Report:** [`benchmarks/results/erp-generator-knowledge-usage-v2.json`](../../benchmarks/results/erp-generator-knowledge-usage-v2.json)
**Generated:** 2026-06-21

LocalAIFactory's product generator (`tools/LocalAIFactory.Generator`) does not emit code freely — it
**catalogues the relevant knowledge packs** and **maps each generated module to the knowledge
category that governs it**. Modules without supporting knowledge are not emitted (the spec/guard only
emits validated entities).

## The `--knowledge-usage` report

The knowledge-usage run produces a machine-readable catalogue at
`benchmarks/results/erp-generator-knowledge-usage-v2.json`. It records:

- **`erpKnowledgePacksAvailable`** — the ERP-oriented packs the generator catalogued, each with its
  item count.
- **`totalErpKnowledgeItems`** — total items across those packs.
- **`generatedModules`** — number of modules emitted.
- **`moduleToKnowledgeCategory`** — for every generated module, the knowledge category that governs
  it.
- **`note`** — the emission rule (validated entities only).

## What the current report shows

- **11 ERP knowledge packs catalogued · 322 items** (a subset of the 20 default packs — see
  [`DEFAULT_KNOWLEDGE_CATALOG.md`](DEFAULT_KNOWLEDGE_CATALOG.md)).
- **28 generated modules** mapped, across these categories:
  `accounting/selling/buying`, `inventory/manufacturing`, `hr/pos/ecommerce`,
  `customization/maintenance`, and `general-erp`.

> The factory's authoritative status reports **29 CRUD modules** for ERP V5. This report is a
> point-in-time knowledge-usage snapshot of the categorised mapping; for the authoritative module
> count and product scores see [`CURRENT_STATUS.md`](../reports/CURRENT_STATUS.md) and
> [`docs/generated-products/LAF_ENTERPRISE_ERP_V5.md`](../generated-products/LAF_ENTERPRISE_ERP_V5.md).

### Catalogued ERP packs (322 items)

| Pack | Items |
|---|---:|
| `enterprise-workflows-v1` | 40 |
| `production-issue-fixes-v1` | 77 |
| `erp-full-suite-generation-v1` | 24 |
| `erp-accounting-production-v1` | 22 |
| `erp-selling-buying-stock-production-v1` | 26 |
| `erp-inventory-manufacturing-v2` | 20 |
| `erp-hr-pos-ecommerce-customization-v1` | 18 |
| `production-grade-erp-controls-v1` | 22 |
| `laf-erp-generation-lessons-v1` | 22 |
| `erp-test-scenario-ui-api-report-v2` | 27 |
| `erp-testing-and-scenarios-v1` | 24 |
| **Total** | **322** |

### Example module → category mappings

| Module | Knowledge category |
|---|---|
| Quotation, DeliveryNote | accounting/selling/buying |
| PurchaseReceipt, MaterialRequest, StockTransfer, BillOfMaterials, WorkOrder, QualityInspection | inventory/manufacturing |
| Employee, AttendanceRecord, SalaryComponent, Timesheet, PosProfile, WebProduct, EmployeeRole | hr/pos/ecommerce |
| MaintenanceSchedule, CustomFieldDef, NotificationRule, CustomerSegment | customization/maintenance |
| CreditNote, DebitNote, PriceList, JobCard, ProductCategory, MarketingCampaign, VendorContract | general-erp |

## Why this matters

This is how the knowledge engine and the generator connect: approved knowledge is **injected first**
and **drives what gets generated**. The mapping is evidence that emitted modules trace back to vetted
knowledge rather than being produced ad hoc.
