# ERP Generator Knowledge-Usage Report

**Generated:** 2026-06-21
**Source:** `benchmarks/results/erp-generator-knowledge-usage.json`
**Feature:** `tools/LocalAIFactory.Generator --knowledge-usage`

## The feature

The generator gained a `--knowledge-usage` flag. When run, it **catalogues the ERP knowledge packs available to the generator** and **maps each generated module to a knowledge category**, then emits the result to `benchmarks/results/erp-generator-knowledge-usage.json`. This produces an auditable link between what the generator emitted and the knowledge that backs it.

## Packs catalogued (9 ERP packs, 274 items)

| Pack | Items |
|------|-------|
| enterprise-workflows-v1 | 40 |
| erp-full-suite-generation-v1 | 24 |
| erp-hr-pos-ecommerce-customization-v1 | 18 |
| erp-inventory-manufacturing-v2 | 20 |
| erp-test-scenario-ui-api-report-v2 | 27 |
| erp-testing-and-scenarios-v1 | 24 |
| laf-erp-generation-lessons-v1 | 22 |
| production-grade-erp-controls-v1 | 22 |
| production-issue-fixes-v1 | 77 |
| **Total** | **274** |

## Modules mapped (22 modules → knowledge category)

All 22 generated modules are mapped to a knowledge category:

- **accounting/selling/buying:** Quotation, DeliveryNote
- **inventory/manufacturing:** PurchaseReceipt, MaterialRequest, StockTransfer, BillOfMaterials, WorkOrder, QualityInspection
- **hr/pos/ecommerce:** Employee, AttendanceRecord, SalaryComponent, Timesheet, PosProfile, WebProduct, EmployeeRole
- **customization/maintenance:** MaintenanceSchedule, CustomFieldDef, NotificationRule, CustomerSegment
- **general-erp:** ProductCategory, MarketingCampaign, VendorContract

## Only validated entities are emitted

The module spec and engine templates implement the rules captured in these packs. The report shows the packs the generator catalogued and the category each emitted module maps to. **Modules without supporting knowledge are not emitted** — the spec and collision guard only emit validated entities. (This is why 17 spec modules + 5 governed local-LLM modules yield 22, with one local-LLM candidate rejected by the collision guard.)

## Honest note

This report proves the generator *catalogues and uses* the knowledge packs; it is not a measure of module depth or ERPNext parity. Depth and external gates remain the blockers to 100% (see `ERP_LEARNING_100_PERCENT_DEFINITION.md`).
