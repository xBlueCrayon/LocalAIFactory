# Scenario: Inventory, Procurement, and Order-to-Cash for a Mid-Market Distributor

> **Inspired-by notice.** This is an *original, fictional* enterprise scenario authored for
> LocalAIFactory's capability simulation suite. It is *inspired by* the problem space that
> mid-market ERP suites address (inventory, procurement, order-to-cash). It does **not**
> reproduce, clone, or describe any vendor product, manual, or screen. It makes **no** claim
> of compatibility, equivalence, interoperability, or certification with any commercial ERP.
> Any resemblance to a specific product's data model or terminology is coincidental and
> generic to the domain.

---

## Business Problem

**Northwind Bearing & Hydraulics Ltd.** (NBH) is a fictional industrial-parts distributor
operating three regional warehouses. NBH buys bearings, seals, hydraulic fittings, and lubricants
from upstream manufacturers, holds stock, and resells to repair shops and OEMs.

Today NBH runs its operation on a sprawl of spreadsheets, a legacy desktop accounting package, and
email. The consequences are concrete and measurable:

- **Phantom stock.** The "available" quantity in the spreadsheet diverges from physical shelves,
  producing oversells and emergency cancellations.
- **Reactive purchasing.** Reorders happen when a salesperson notices a shortage, not when a
  reorder point is breached, causing stockouts on fast movers and dead capital on slow ones.
- **No audit trail.** Nobody can answer "who changed this quantity, when, and why" — a problem for
  internal controls and dispute resolution.
- **Slow order-to-cash.** Sales orders, picking, and invoicing are reconciled by hand, so cash
  collection lags and margin leakage goes unnoticed.

NBH wants a single internal system of record for **items, stock, procurement, and sales
fulfilment** with a defensible audit trail and proactive replenishment.

---

## Current-State Process

1. A buyer eyeballs a stock spreadsheet and emails a supplier to place a purchase order.
2. Goods arrive; a warehouse clerk signs a paper delivery note and later types quantities into the
   spreadsheet — sometimes days later.
3. Sales takes an order by phone, checks the (stale) spreadsheet, and promises a date.
4. A picker walks the floor with a printed list; shortfalls are discovered at the shelf.
5. Shipping marks the order "done" in email; accounting raises an invoice from the email thread.
6. Month-end stock valuation is a manual spreadsheet exercise that rarely reconciles.

Failure is normal, not exceptional: every step depends on a human re-keying state.

---

## Target-State Process

A web application that holds authoritative state and enforces movement-by-movement integrity:

1. **Procurement.** Buyer raises a **Purchase Order (PO)** against a supplier and items.
2. **Receiving.** Goods arrive and are booked via a **Goods Receipt Note (GRN)** referencing the PO;
   stock increments are posted as immutable **stock movements**.
3. **Selling.** Sales raises a **Sales Order (SO)**; the system reserves available stock and exposes
   a real, current availability figure.
4. **Fulfilment.** Warehouse executes **pick → pack → ship**; each transition posts movements and
   updates order status.
5. **Replenishment.** When on-hand minus reserved crosses an item's **reorder point**, the system
   flags a suggested reorder.
6. **Valuation & counts.** Stock is valued on a documented cost basis; **cycle counts** reconcile
   system vs physical and post adjustment movements with a reason code.

Every quantity change is a typed, attributable, append-only movement. The on-hand figure is a
**projection of movements**, never a directly edited number.

---

## Users and Roles

| Role | Responsibilities | Cannot do |
| --- | --- | --- |
| **Buyer / Procurement** | Create and approve POs, manage suppliers, set reorder points | Ship goods; edit posted movements |
| **Warehouse Clerk** | Post GRNs, execute pick/pack/ship, perform cycle counts | Approve POs; change prices |
| **Sales Rep** | Create SOs, check availability, allocate stock | Receive goods; alter valuation |
| **Finance / Controller** | Review valuation, approve write-offs, read audit trail | Pick/ship physical stock |
| **Warehouse Manager** | Approve cycle-count adjustments above a threshold | Bypass approval for own counts |
| **System Administrator** | Manage users, roles, warehouse master data | Self-approve financial adjustments (separation of duties) |

Roles map to ASP.NET Core authorization policies; sensitive transitions require a distinct approver.

---

## Data Entities

Core entities (EF Core, MSSQL). Money as `decimal(18,4)`; quantities as `decimal(18,3)`.

- **Item** — SKU, description, unit of measure, valuation method, reorder point, reorder quantity,
  active flag.
- **Warehouse** — code, name, address, active flag.
- **Supplier** — code, name, payment terms, contact.
- **Customer** — code, name, credit terms, contact.
- **StockMovement** — *append-only ledger*: item, warehouse, movement type
  (`Receipt`, `Issue`, `TransferIn`, `TransferOut`, `AdjustmentPlus`, `AdjustmentMinus`),
  signed quantity, unit cost, source document reference, reason code, timestamp, actor.
- **StockOnHand** — projection per (item, warehouse): on-hand, reserved, available = on-hand − reserved.
- **PurchaseOrder** / **PurchaseOrderLine** — supplier, status (`Draft`, `Approved`, `PartiallyReceived`,
  `Closed`, `Cancelled`), lines with item, ordered qty, unit price.
- **GoodsReceiptNote** / **GoodsReceiptLine** — references a PO; posts `Receipt` movements.
- **SalesOrder** / **SalesOrderLine** — customer, status (`Draft`, `Confirmed`, `Picking`, `Packed`,
  `Shipped`, `Invoiced`, `Cancelled`), lines with item, ordered qty, allocated qty, price.
- **Shipment** — references an SO; posts `Issue` movements on ship.
- **CycleCount** / **CycleCountLine** — counted qty vs system qty, variance, status, approver.
- **AuditEvent** — append-only: entity, key, action, actor, UTC timestamp, before/after snapshot.

---

## Integrations

All integrations are **optional and degrade gracefully** — the system is fully usable standalone
with only MSSQL present.

- **Email notifications** (optional SMTP) for reorder alerts and PO approvals. Absent SMTP = alerts
  visible in-app only; no hard dependency.
- **CSV import/export** for item, supplier, and customer master data (offline, file-based).
- **Accounting hand-off** as a generated, reviewable export file (no live coupling to any external
  ledger). The system never assumes an external service is reachable to render a page.

No integration is on the request-rendering path. This mirrors LocalAIFactory's rule that pages
must render with MSSQL alone.

---

## Security and Audit Controls

- **Authentication** via the host platform's identity (e.g., Windows/Negotiate or forms), no
  passwords stored in plaintext.
- **Role-based authorization** with deny-by-default policies on every controller action.
- **Separation of duties.** The actor who raises a PO or records a count cannot be the sole approver
  of the matching financial adjustment above a configured threshold.
- **Append-only audit.** `StockMovement` and `AuditEvent` are never updated or deleted; corrections
  are *new* compensating movements with reason codes.
- **IDOR protection.** Every document fetch is scoped to entities the caller's role and warehouse
  permit; object IDs are authorization-checked server-side, never trusted from the client.
- **Immutable valuation history.** Cost layers are written once; revaluations are explicit events.

---

## Reporting Requirements

- **Stock-on-hand by item/warehouse** with available vs reserved.
- **Stock valuation report** on the documented cost basis, with as-of date.
- **Reorder suggestions** — items below reorder point with suggested quantities.
- **Open POs and expected receipts** aging report.
- **Open SOs and fulfilment status** with backorder visibility.
- **Movement ledger** — full filterable history for any item or document.
- **Cycle-count variance report** with approval status.
- **Audit trail report** — who/what/when for any entity.

All list/report queries must select lightweight projection rows; large text columns are never
materialized into list views (consistent with LocalAIFactory query rules).

---

## Failure Modes

- **Oversell / negative stock.** Concurrent SOs allocate the same units. Mitigation: allocation is a
  transactional check against `available`, with optimistic concurrency on `StockOnHand`.
- **Double receipt.** A GRN is posted twice. Mitigation: GRN idempotency keyed on PO line + receipt
  reference; duplicate posts rejected.
- **Lost movement.** A crash between writing the movement and updating the projection. Mitigation:
  movement insert and projection update in one transaction; projection is rebuildable from the ledger.
- **Stale availability.** A page caches an old number. Mitigation: availability read from the
  projection at request time, no cross-request caching of quantities.
- **Privilege creep.** A clerk approves their own count. Mitigation: separation-of-duties policy.
- **Phantom edits.** Someone hand-edits a quantity. Mitigation: on-hand is read-only; only movements
  change it.

---

## Acceptance Criteria

See `acceptance-criteria.md` for the measurable checklist. In summary, the system is accepted when
on-hand equals the sum of movements for every item/warehouse, oversells are impossible under
concurrency, every state change is attributable in the audit trail, and all core pages render from
MSSQL alone within sub-second targets.

---

## Expected Architecture (ASP.NET Core MVC + MSSQL + EF Core)

- **Presentation:** ASP.NET Core MVC controllers + Razor views, Bootstrap 5. Thin controllers;
  no synchronous external calls on the request path.
- **Domain/Application:** services for procurement, fulfilment, valuation, and replenishment.
  Stock mutation flows through a single **movement-posting service** that owns the transaction.
- **Data:** EF Core `DbContext` over MSSQL. Additive, backward-compatible migrations. Money/quantity
  as `decimal`; never `float`. Optimistic concurrency token on `StockOnHand`.
- **Authorization:** policy-based, deny-by-default, separation-of-duties for approvals.
- **Projections:** `StockOnHand` maintained transactionally and rebuildable from `StockMovement`.
- **No GroupBy-on-constant aggregates;** prefer separate `CountAsync()` calls and lightweight
  projection records for lists (per LocalAIFactory query discipline).

---

## Expected Tests

- **Unit:** movement math (signed quantities), reorder-point trigger logic, valuation per cost basis,
  separation-of-duties policy decisions.
- **Integration:** GRN posts increment movements and projection equally; ship posts decrement;
  projection rebuilt from ledger equals live projection.
- **Concurrency:** two parallel allocations against one unit — exactly one succeeds.
- **Authorization:** each role can/can't reach each action; IDOR attempt across warehouses denied.
- **Idempotency:** duplicate GRN rejected; duplicate ship rejected.
- **Reporting:** valuation as-of date and on-hand totals reconcile to the ledger.

---

## Expected Deployment Concerns

- Runs on IIS/Kestrel against MSSQL; no GPU, no internet dependency required to operate.
- Database migrated and seeded on startup; first run needs only a reachable SQL Server.
- Secrets (SMTP, connection strings) via environment variables or git-ignored overrides, never
  committed.
- Health of optional integrations read from a cached snapshot, never probed synchronously per request.

---

## Rollback Considerations

- **Schema:** migrations are additive; a failed release rolls back to the prior migration without
  data loss because corrections are compensating movements, not edits.
- **Data:** because the movement ledger is append-only, a bad batch is reversed by posting inverse
  movements with a reason code — original history is preserved.
- **Release:** blue/green or staged swap; the projection can be rebuilt from the ledger after a
  rollback to guarantee on-hand correctness.
- **Audit:** rollback events themselves are audited; no silent state changes.

---

## CEO/CTO Summary

NBH replaces spreadsheet-driven inventory with a single internal system of record. The core idea is
simple and defensible: **stock is a ledger, not a number.** Every receipt, issue, transfer, and
adjustment is an immutable, attributable movement; on-hand is a projection of that ledger. This
makes oversells preventable, valuation reconcilable, replenishment proactive, and every change
auditable. It runs locally on MSSQL with no external-service dependency on the request path, deploys
on existing IIS infrastructure, and rolls back safely because history is never overwritten. The
business outcome is fewer stockouts, less dead capital, faster order-to-cash, and an audit trail
that answers "who, what, when, and why."
