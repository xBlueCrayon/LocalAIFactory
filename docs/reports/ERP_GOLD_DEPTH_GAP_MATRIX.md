# ERP-GOLD-DEPTH — Gap Matrix vs ERPNext-grade Depth

**Sprint:** ERP-GOLD-DEPTH · **Branch:** `ke-008-code-symbols` · **Stamp:** 2026-06-21
**Companion data:** `benchmarks/erpnext-study/erp-gold-depth-gap-matrix.json`

Each area is scored 0–100 against ERPNext-grade depth by **honest clean-room self-assessment**
(no ERPNext code studied or copied). Weighted parity is **45%** this sprint, up from **39%**.
This is **not** a parity claim — manufacturing and reporting rose materially; HR/payroll, POS,
e-commerce and the customization framework remain shallow.

| Area | Current | Target | Gap | Priority | Tests needed | LAF update needed | Moved this sprint |
|------|:------:|:------:|-----|:--------:|--------------|-------------------|:-----------------:|
| Accounting | 57 | 85 | Budgeting, period-close locks, more dimensions | Medium | Budget-vs-actual, period-close enforcement | Budget entity + variance; closing lock | No |
| Stock | 56 | 80 | Batch/serial, landed cost, FIFO | Medium | Batch issue, serial trace, landed-cost apportionment | Batch/Serial ledger dims; landed-cost voucher | Yes |
| Selling/Buying | 54 | 80 | Delivery notes, returns, partial fulfilment, pricing rules | High | Delivery note vs SO; return reverses stock+GL; partial receipt | Real DeliveryNote/Return services; partial state | Yes |
| Manufacturing | 50 | 75 | Multi-level BOM, routing/work-centres, scrap, WIP, labour+overhead | High | Multi-level explode, routing time, scrap relief, WIP posting | Routing/WorkCentre; WIP GL; labour/overhead rollup | Yes |
| HR & Payroll | 22 | 70 | Payroll engine, leave accrual, attendance-to-pay | Medium | Payroll run posts payslip+GL; leave accrual | Payroll engine over existing skeletons | No |
| POS | 20 | 65 | Sale flow, session, offline sync, tendering | Low | POS sale posts SI+payment+stock; session reconcile | POS sale service over PosProfile | No |
| E-commerce | 18 | 60 | Storefront, cart, checkout-to-order | Low | Cart->order->SI; published visibility | Cart/checkout service over WebProduct | No |
| CRM & Projects | 40 | 70 | Lead->opportunity->quote, project billing | Low | Lead conversion; project profitability | Pipeline state machine; project costing | No |
| Assets | 35 | 65 | Depreciation, disposal, asset GL | Low | Depreciation run posts GL; disposal gain/loss | Asset + depreciation engine | No |
| Workflow/RBAC/Audit | 62 | 85 | Configurable multi-step, field-level perms | Medium | Multi-step approval; field-level denial | Workflow designer + permission rules | No |
| Reporting/BI | 54 | 80 | BI/dashboards, print/report designer, due-date aging | Medium | Due-date aging; saved views | Report builder + dashboards; terms-driven aging | Yes |
| Import/Export | 48 | 75 | Full export, import validation preview | Low | Round-trip import/export; error preview | Export per entity; import preview | No |
| Customization framework | 20 | 70 | Runtime DocType/custom fields, scripts | Low | Custom field persists+renders; script hook | Metadata-driven entity + custom-field render | No |
| API breadth | 52 | 80 | Full CRUD+action coverage, paging, OpenAPI | Medium | Paged list; filtered query; per-doctype actions | Generic REST + OpenAPI | Yes |
| Module breadth | 40 | 75 | Larger catalog with real flows, not skeletons | Medium | Per-module end-to-end flows | Promote skeletons to real services | No |
| Multi-company/currency | 40 | 75 | FX revaluation, inter-company, consolidation | Low | FX revaluation; consolidated TB | Currency/exchange-rate engine; consolidation | No |

## What moved this sprint

- **Manufacturing** (50): from CRUD stub to real BOM + production-order lifecycle + costing + quality.
- **Stock** (56): valuation/reorder reports + manufacturing stock impact added on top of the ledger.
- **Selling/Buying** (54) and **Reporting/BI** (54): registers, summaries, aging, tax, work-order summary.
- **API breadth** (52): report + manufacturing REST endpoints.

## Honest limitations / not done

- HR/payroll (22), POS (20), e-commerce (18) and the customization framework (20) were **not**
  upgraded this sprint and remain the largest gaps.
- Delivery-note and return (reverse) document chains, batch/serial stock, landed cost, and BI /
  print-designer reporting are all still open.
- Targets in the gap matrix are **directional**, not commitments met this sprint. Weighted parity
  reached 45%, below the 55% stretch target.
