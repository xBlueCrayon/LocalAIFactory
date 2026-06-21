# ERP-GOLD-DEPTH — Priority Plan

**Sprint:** ERP-GOLD-DEPTH · **Branch:** `ke-008-code-symbols` · **Stamp:** 2026-06-21

Derived from the gap matrix (`ERP_GOLD_DEPTH_GAP_MATRIX.md`). This plan records what was
prioritised **for this sprint** and what was consciously deferred.

## Prioritisation principle

Deepen the areas where (a) a real, ledger-backed flow is achievable deterministically, (b) the
generator can reproduce it, and (c) the result is provable end-to-end with xUnit + scenarios.
Defer areas that need large new engines (payroll, POS, storefront, customization framework).

## In scope — done this sprint

| Priority | Area | Delivered |
|:--------:|------|-----------|
| High | Manufacturing | Real BOM + production-order lifecycle (issue/quality/complete), moving-average production costing, stock impact, immutability, audit. 10 xUnit tests. |
| High | Reporting depth | SalesRegister, PurchaseRegister, party summaries, outstanding, receivables aging, tax summary, stock valuation, reorder, work-order summary. ReportsService + REST API. 11 xUnit tests. |
| Medium | API breadth | Report + manufacturing REST endpoints (`/api/reports/*`, `/api/boms`, `/api/production-orders` + issue/complete). |
| Medium | Scenarios | 26 explicit end-to-end scenarios (13 base + 12 depth + 1 trading). |
| Medium | Live DB proof | Committed EF migrations applied LIVE to SQL Server LocalDB; app login proven. |

## Deferred — explicitly NOT done this sprint

| Priority | Area | Why deferred |
|:--------:|------|--------------|
| High | Delivery-note / return document chains | Needs real fulfilment + reversal services (stock + GL); larger than one depth sprint. |
| Medium | Manufacturing routing / multi-level BOM / WIP / labour+overhead | Needs a routing/work-centre engine and WIP accounting. |
| Medium | Batch / serial / landed-cost stock | Needs new ledger dimensions and apportionment logic. |
| Medium | HR / payroll usable flows | Needs a payroll engine (salary structure -> run -> payslip -> GL). |
| Low | POS sale flow, e-commerce cart/checkout | Needs new transactional + storefront engines. |
| Low | Customization framework, BI / print designer | Large platform features, low local priority. |

## Targets vs outcome (summary)

| Target | Outcome |
|--------|---------|
| ERPNext parity 55% | NOT MET (45%) |
| .NET tests 300+ | NOT MET (255, up from 222) |
| Playwright 50+ | MET (51) |
| Scenarios 25+ | MET (26) |
| Reproduction >= 90% | MET (92.2% test, 100% Playwright, 100% deterministic surface) |
| Production score >= 78 | MET (80.6) |

## Honest limitations / not done

The plan deliberately concentrated effort on manufacturing, reports, API and live-DB proof. The
deferred list above is real outstanding work — it is **not** implemented and must not be presented
as complete. Parity remains below the stretch target principally because HR/POS/e-commerce and the
return/delivery chains were not addressed.
