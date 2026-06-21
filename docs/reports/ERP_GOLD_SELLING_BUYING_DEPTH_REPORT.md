# ERP-GOLD-DEPTH — Selling & Buying Depth Report

**Sprint:** ERP-GOLD-DEPTH · **Branch:** `ke-008-code-symbols` · **Stamp:** 2026-06-21
**Companion data:** `benchmarks/results/erp-gold-selling-buying-depth.json`

## Implemented (real + tested)

**Quote-to-cash**

- SalesInvoice create -> submit (maker/checker, amount-threshold approval) -> GL + stock posting
  -> payment.
- Sales register, sales-by-customer summary, outstanding sales invoices.
- Receivables aging (posting-date buckets), tax summary (output side).

**Procure-to-pay**

- PurchaseInvoice create -> submit/approve -> GL posting -> payment.
- Purchase register, purchase-by-supplier summary, outstanding purchase invoices.
- Tax summary (input side); net tax = output − input.

## Scenarios

`Scenario_sales_reporting_is_consistent`, `Scenario_purchase_reporting_reflects_procurement`,
`Scenario_tax_summary_reconciles_with_sales`, `Scenario_receivables_aging_is_current_for_new_invoice`,
`Scenario_procure_produce_sell_finished_good`, `Scenario_combined_business_day_balances_and_reports`.

## REST API

`/api/sales-invoices` (+ submit/approve/cancel), `/api/purchase-invoices`, `/api/payments`, and the
selling/buying reports under `/api/reports/*`.

## Honest limitations / not done

- **Delivery notes** against sales orders — `DeliveryNote` is a CRUD skeleton with no stock relief.
- **Sales/purchase returns** — `CreditNote` / `DebitNote` skeletons do **not** reverse stock or GL.
- **Partial delivery / partial receipt.**
- **Pricing rules / price-list-driven rate resolution.**
- **Purchase-receipt-to-invoice three-way matching.**

Selling/Buying parity sits at **54/100** — a real quote-to-cash and procure-to-pay spine with
maker/checker and registers, short of ERPNext's delivery/return/partial-fulfilment depth.
