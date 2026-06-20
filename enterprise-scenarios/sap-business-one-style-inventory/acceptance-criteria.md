# Acceptance Criteria — Measurable Checklist

> Inspired-by, original, fictional scenario. No vendor compatibility or equivalence is claimed.
> Each item below is phrased to be objectively verifiable (pass/fail).

## Data integrity

- [ ] For every (item, warehouse), `StockOnHand.OnHand` equals the signed sum of all
      `StockMovement` rows for that pair.
- [ ] Rebuilding the projection from the movement ledger reproduces the live `StockOnHand` exactly.
- [ ] `available = on-hand − reserved` holds at all times and is never negative for a confirmed
      allocation.
- [ ] On-hand is never set by direct edit; the only writer of quantity is the movement-posting service.
- [ ] Money columns are `decimal(18,4)` and quantity columns `decimal(18,3)` — no `float`/`double`.

## Concurrency and correctness

- [ ] Under two parallel allocations competing for one available unit, exactly one succeeds and the
      other is rejected (no oversell, no negative available).
- [ ] A duplicate GRN post (same PO line + receipt reference) is rejected idempotently.
- [ ] A duplicate shipment post for the same SO line is rejected.
- [ ] A movement insert and its projection update either both commit or both roll back.

## Workflow

- [ ] A PO can move Draft → Approved → PartiallyReceived → Closed, and illegal transitions are blocked.
- [ ] An SO can move Draft → Confirmed → Picking → Packed → Shipped → Invoiced, and illegal
      transitions are blocked.
- [ ] Posting a GRN against an approved PO increments the correct item/warehouse on-hand.
- [ ] Shipping an SO decrements the correct item/warehouse on-hand and clears the reservation.
- [ ] When on-hand minus reserved crosses an item's reorder point, the item appears in the reorder
      suggestions list with a suggested quantity.
- [ ] A cycle count posts an adjustment movement equal to (counted − system) with a reason code.

## Security and audit

- [ ] Every controller action denies by default and requires an explicit role policy.
- [ ] A user cannot be the sole approver of a financial adjustment they themselves originated above
      the configured threshold (separation of duties enforced).
- [ ] A request for a document outside the caller's permitted warehouse/role is denied server-side
      (IDOR blocked) — not merely hidden in the UI.
- [ ] Every create/approve/post/adjust action writes an append-only `AuditEvent` with actor, UTC
      timestamp, and before/after snapshot.
- [ ] No `StockMovement` or `AuditEvent` row is ever updated or deleted; corrections are compensating
      entries.

## Reporting

- [ ] The valuation report reconciles to the movement ledger for a given as-of date.
- [ ] Stock-on-hand, open-PO, open-SO, reorder, variance, and audit reports each render from MSSQL
      alone.
- [ ] List and report queries select lightweight projection rows; large text columns are not
      materialized into list views.

## Platform / runtime discipline

- [ ] All core pages render with only MSSQL present (no internet, no GPU, no optional integration).
- [ ] No controller action or Razor view makes a synchronous call to an optional external service.
- [ ] No query uses `GroupBy(_ => 1)` or group-by-constant aggregate projection.
- [ ] Each core page returns in well under one second on SQL Express-class hardware.
- [ ] Database migrations are additive and the solution starts, migrates, and seeds automatically.
