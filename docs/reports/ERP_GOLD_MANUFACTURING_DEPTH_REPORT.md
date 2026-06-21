# ERP-GOLD-DEPTH — Manufacturing Depth Report

**Sprint:** ERP-GOLD-DEPTH · **Branch:** `ke-008-code-symbols` · **Stamp:** 2026-06-21
**Companion data:** `benchmarks/results/erp-gold-manufacturing-coverage.json`

Manufacturing moved from a **CRUD catalog stub** to a **real, tested production flow**.

## Files

- `src/LafErp.Core/ManufacturingEntities.cs` — `Bom`, `BomLine`, `ProductionOrder`, `ProductionStatus`.
- `src/LafErp.Services/ManufacturingService.cs` — lifecycle + costing service.
- `tests/LafErp.Tests/ManufacturingTests.cs` — 10 tests (all green).

## Entities

| Entity | Key fields |
|--------|-----------|
| `Bom` | `Name`, `FinishedItemId`, `Quantity` (units per run), `IsActive`, `Lines` |
| `BomLine` | `BomId`, `ComponentItemId`, `Quantity` (component qty per run) |
| `ProductionOrder` | `DocNo`, `BomId`, `CompanyId`, `WarehouseId`, `FinishedItemId`, `Quantity`, `Status`, `MaterialCost`, `UnitCost`, `QualityInspected` |

## Lifecycle

```
Draft -> MaterialsIssued -> QualityPassed  -> Completed (immutable)
                          \-> QualityFailed -> (re-inspect) -> QualityPassed -> Completed
```

`Cancelled` is defined in the enum. `Completed` is terminal and immutable.

## Business rules (enforced + tested)

- A BOM needs **at least one component line**; all quantities must be positive.
- `IssueMaterials` scales each line's need by `orderQty / bomQty` (runs), relieves component stock
  through `StockService` (moving-average), and **blocks if any component is short**.
- `MaterialCost` accumulates from the **moving-average valuation** of the materials actually issued.
- Quality **fail blocks completion**; re-inspection from `QualityFailed` back to `QualityPassed`
  is allowed.
- `Complete` requires a quality pass, receives the finished good at
  `UnitCost = MaterialCost / Quantity`, then the order is immutable.
- Every transition is **audited** via `AuditService`.
- Material issue and finished-goods receipt flow through `StockService`, so the stock ledger and
  valuation stay authoritative.

## REST API

`GET /api/boms`, `GET /api/production-orders`, `POST /api/production-orders/{id}/issue`,
`POST /api/production-orders/{id}/complete`, `GET /api/reports/work-order-summary`.

## Tests (10, all green)

Cover BOM validation, material scaling, stock-shortage blocking, costing, quality fail/rework, and
completion immutability. Reinforced by manufacturing scenarios in `ScenarioLibraryDepthTests.cs`
(make-to-stock, quality-fail-then-rework, production-cost-to-valuation, procure-produce-sell).

## Honest limitations / not done

- **No multi-level BOM** — single-level explode only.
- **No routing / operations / work-centres / scheduling.**
- **No scrap handling.**
- **No WIP accounting** — there are no in-process GL postings.
- **Costing is material-only** — no labour and no overhead absorption.

These are recorded as backlog (see the Ollama review) and keep the manufacturing parity score at
**50/100** — real depth, not ERPNext-grade manufacturing.
