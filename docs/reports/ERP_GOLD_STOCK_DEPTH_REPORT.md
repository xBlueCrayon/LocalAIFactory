# ERP-GOLD-DEPTH — Stock Depth Report

**Sprint:** ERP-GOLD-DEPTH · **Branch:** `ke-008-code-symbols` · **Stamp:** 2026-06-21
**Companion data:** `benchmarks/results/erp-gold-stock-depth.json`

## Implemented (real + tested)

- **Stock ledger** — `StockLedgerEntry` records `QtyChange`, `QtyAfter`, `ValuationRate`,
  `VoucherType`, `VoucherNo`.
- **Moving-average valuation** — receipts (`MoveIn`) update the average; issues (`MoveOut`) relieve
  at the moving-average rate.
- **Stock transfer** — decrements the source warehouse and increments the destination; total
  quantity is conserved.
- **Stock adjustment / reconciliation.**
- **StockValuation report** — qty + value per item/warehouse.
- **ReorderReport** — items at or below a threshold.
- **Manufacturing stock impact** — material issue and finished-goods receipt flow through the same
  `StockService`, keeping the ledger authoritative.

## Scenarios

- `Scenario_inventory_issue_uses_moving_average` — moving average across two receipts.
- `Scenario_inventory_transfer_conserves_total_qty` — source decrement + destination increment.
- `Scenario_stock_valuation_reconciles_after_sale`.
- `Scenario_reorder_alert_for_low_stock`.

## REST API

`GET /api/stock-ledger`, `GET /api/reports/stock-balance`, `GET /api/reports/stock-valuation`,
`GET /api/reports/reorder`.

## Honest limitations / not done

- **No batch tracking.**
- **No serial-number tracking.**
- **No landed cost** (no freight/duty apportionment into item valuation).
- **No alternative valuation methods** (FIFO / standard) — moving-average only.
- **No due-date / payment-terms driven stock reporting.**

Stock parity sits at **56/100** — a real moving-average ledger with valuation and reorder
reporting, short of ERPNext's batch/serial/landed-cost depth.
