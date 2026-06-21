# ERP-GOLD-DEPTH — HR / POS / E-commerce Depth Report

**Sprint:** ERP-GOLD-DEPTH · **Branch:** `ke-008-code-symbols` · **Stamp:** 2026-06-21
**Companion data:** `benchmarks/results/erp-gold-hr-pos-ecommerce-depth.json`

These three areas **remain CRUD skeletons**. They were **NOT** upgraded to usable flows this
sprint. This document states exactly what exists versus what is needed, with no inflation.

## HR & Payroll — parity 22/100

| Exists (CRUD create/list/edit/deactivate + REST endpoint) | Needed for a usable flow |
|------------------------------------------------------------|--------------------------|
| `Employee` | Salary structure -> payroll run -> payslip -> GL posting |
| `AttendanceRecord` | Attendance-to-pay linkage |
| `LeaveApplication` | Leave accrual + balance |
| `SalaryComponent` | Component-driven payslip computation |
| `Timesheet` | Timesheet-to-billing / payroll |

**Not implemented:** any payroll engine.

## POS — parity 20/100

| Exists | Needed |
|--------|--------|
| `PosProfile` (CRUD + REST) | POS sale flow (SI + payment + stock in one transaction), session open/close + reconciliation, offline sync, payment tendering |

**Not implemented:** any POS sale flow.

## E-commerce — parity 18/100

| Exists | Needed |
|--------|--------|
| `WebProduct` (CRUD + REST) | Storefront, cart, checkout -> sales order/invoice, web price lists / published-product visibility |

**Not implemented:** any cart/checkout flow.

## Honest limitations / not done

All three areas are thin CRUD skeletons (entity + create/list/edit/deactivate + a REST endpoint).
No payroll engine, no POS sale flow, and no cart/checkout were implemented this sprint. **These are
the principal reason ERPNext parity is 45% rather than the 55% stretch target.**
