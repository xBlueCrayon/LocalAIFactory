# ERP Gold — UI Completeness Report

**Sprint:** ERP-GOLD HARDENING · **Stamp:** 2026-06-21
**Blocker #2 (create/list/read only; no edit/delete UI): CLOSED for masters**

## What was delivered

| Component | Detail |
|-----------|--------|
| `CatalogCrudService<T>` | Generic `Create` / `Update` / `Deactivate` (soft-delete) / `List` / `Get` |
| `CatalogController` | `List`, `Edit`, `Deactivate` actions |
| Views | `List.cshtml` + `Edit.cshtml` with `data-testid` hooks for deterministic UI testing |
| Soft delete | `Deactivate` flips an active flag — no hard delete of master records |

## Design rule: documents stay immutable after posting

Posted transactional documents (sales invoices, payments, journal entries, etc.) are governed by **submit / approve / cancel / reverse**, with **no hard delete**, **by design** — this preserves the audit trail and double-entry integrity. Edit/soft-delete applies to **masters and catalog entities**, not posted documents.

## Test coverage

xUnit — `CatalogGeneratedTests.cs` (20 `[Fact]`): each of 5 LLM-proposed catalog entities (`CustomerSegment`, `ProductCategory`, `EmployeeRole`, `MarketingCampaign`, `VendorContract`) has create-and-list, requires-name, edit-updates-name, and deactivate-soft-deletes tests.

Playwright (`playwright/tests/`):

| Spec | Coverage |
|------|----------|
| `catalog-crud.spec.ts` | overview lists modules with view/edit links; create → edit → deactivate lifecycle via the UI |
| `catalog-pages.spec.ts` | list-render loop over 15 catalog entities (`Quotation`, `DeliveryNote`, `CreditNote`, `PurchaseReceipt`, `MaterialRequest`, `DebitNote`, `StockTransfer`, `PriceList`, `WorkOrder`, `JobCard`, `QualityInspection`, `Employee`, `LeaveApplication`, `Timesheet`, `WebProduct`) asserting 200 + `record-table` + `create-link` |

## Real bug found and fixed

Empty-string form fields were mapping to `null` for **non-nullable string columns**, producing a NOT-NULL constraint **500**. Fixed so empty fields map to `""`. This is a genuine defect surfaced by the new edit UI, not a synthetic test.

## Honest limitations

- Edit/soft-delete is intentionally **restricted to mutable masters**; posted documents are immutable by accounting design.
- Some bespoke (non-catalog) pages remain thin.
- Manufacturing / HR / POS modules are CRUD skeletons (no MRP/payroll/POS engines).
