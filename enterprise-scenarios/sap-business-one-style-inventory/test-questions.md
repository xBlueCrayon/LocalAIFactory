# Test Questions — Probing the Platform's Reasoning

> Inspired-by, original, fictional scenario. These questions probe whether LocalAIFactory can
> *reason about* an inventory / procurement / order-to-cash design — not recite any vendor product.
> No compatibility, equivalence, or certification claim is implied. Each question lists what a
> **strong answer must contain**.

---

### 1. Why model stock as a ledger of movements instead of an editable on-hand number?

**A strong answer must:** explain that on-hand is a *projection* of append-only `StockMovement`
rows; identify the benefits (auditability, rebuildability, safe rollback via compensating entries,
no phantom edits); and note that the projection and the inserting movement must commit in one
transaction.

---

### 2. Two sales reps confirm orders for the last available unit at the same moment. How do you prevent an oversell?

**A strong answer must:** describe a transactional allocation that checks `available` and writes the
reservation atomically; invoke an optimistic concurrency token on `StockOnHand`; and conclude that
exactly one allocation succeeds while the other is rejected — never negative stock.

---

### 3. A GRN gets submitted twice due to a double click. What stops the stock from being received twice?

**A strong answer must:** propose an idempotency key (e.g., PO line + receipt reference), reject the
duplicate post, and explain that the movement ledger therefore never double-counts the receipt.

---

### 4. Design the EF Core schema for the core entities. What are the key types and relationships?

**A strong answer must:** list Item, Warehouse, Supplier, Customer, StockMovement, StockOnHand,
PurchaseOrder/Line, GoodsReceiptNote/Line, SalesOrder/Line, Shipment, CycleCount/Line, AuditEvent;
use `decimal` for money and quantity (never `float`); make StockMovement append-only; and define
StockOnHand as a projection keyed by (item, warehouse).

---

### 5. How does separation of duties work for cycle-count adjustments, and why does it matter?

**A strong answer must:** state that the person who records a count cannot be the sole approver of an
adjustment above a threshold; map this to a deny-by-default authorization policy; and explain it
prevents self-approved shrinkage and supports internal control.

---

### 6. What reorder logic flags an item for replenishment, and where could it go wrong?

**A strong answer must:** define the trigger as on-hand minus reserved crossing the item's reorder
point; produce a suggested quantity from the reorder quantity; and flag pitfalls such as ignoring
reserved stock, in-flight POs not yet received, or alerting on stale availability.

---

### 7. The accounting team wants email alerts for reorders, but the SMTP server is sometimes down. How do you design that?

**A strong answer must:** keep SMTP optional and off the request-rendering path; show alerts in-app
regardless; degrade gracefully when SMTP is unreachable; and read integration health from a cached
snapshot rather than probing synchronously per request.

---

### 8. A manager hand-edited a quantity in the old system. How does the new design make that impossible — and how do you correct a genuine error?

**A strong answer must:** state on-hand is read-only and only movements change it; corrections are
*new* compensating movements with a reason code; and the original (wrong) movement stays in the
append-only ledger for audit.

---

### 9. How do you write the stock list and valuation report so they don't hang on SQL Server?

**A strong answer must:** select lightweight projection records (only needed columns), avoid
materializing large text columns into lists, avoid `GroupBy(_ => 1)` / group-by-constant aggregates,
prefer separate `CountAsync()` calls, and target sub-second responses from MSSQL alone.

---

### 10. How can a malicious sales rep be stopped from viewing or shipping another warehouse's order by guessing an ID?

**A strong answer must:** describe server-side authorization on every document fetch scoped to the
caller's role and warehouse (IDOR protection); state that object IDs are never trusted from the
client; and note that hiding a link in the UI is not sufficient.

---

### 11. A release goes bad after a batch of receipts posted wrong quantities. How do you roll back safely?

**A strong answer must:** reverse the bad batch with inverse compensating movements (not deletes);
keep original history intact; rebuild the projection from the ledger to confirm correctness; roll the
schema back via the prior additive migration; and audit the rollback itself.

---

### 12. What does LocalAIFactory honestly *not* do for this scenario today?

**A strong answer must:** acknowledge it reasons about, designs, and test-plans the solution rather
than shipping a running, certified ERP; disclaim general-ledger posting, multi-currency/landed cost,
lot/serial tracking, and hardware integration as future work; and make no vendor compatibility or
equivalence claim.
