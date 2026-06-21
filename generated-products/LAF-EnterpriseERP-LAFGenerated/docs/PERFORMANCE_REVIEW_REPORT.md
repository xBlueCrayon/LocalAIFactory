# LAF Enterprise ERP V2 — Performance Review

**Date:** 2026-06-21 · **Scope:** generator-emitted ERP V2

## Observed

- 82-test .NET suite (incl. WebApplicationFactory boot) completes in ~2 s.
- 13 Playwright page loads render in 50–165 ms each (SQLite, local); full login+navigation run ~900 ms.
- App cold-start (EnsureCreated + seed + demo data) in a few seconds; 26/26 endpoints respond ≤ ~130 ms.

## Design (inherited from the engine templates)

- **Indexes** on GL `(AccountId, PostingDate)`, `(PartyType, PartyId)`, `(VoucherType, VoucherNo)`; stock
  ledger `(ItemId, WarehouseId, Id)`; unique business keys on master codes and document numbers.
- **Stock balance/valuation** reads the latest ledger row (O(1)-ish per item+warehouse), not a full re-sum.
- **Report aggregates** materialize then sum in memory for exact decimals across SQLite and SQL Server.
- **Generated catalog** queries are simple `Set<T>()` reads with `Take(500)` bounds.

## Findings

| Severity | Finding | Recommendation |
|---|---|---|
| Low | per-line item lookups in invoice posting | batch-load for large invoices |
| Low | in-memory report aggregation reads all matching GL rows | push to SQL Server / add summary tables at scale |
| Info | no production-hardware load test | operator task on a real DB/host |

## Verdict

Appropriate for a PILOT; the schema is indexed for the obvious access patterns. **No production-scale load
test was run** (out of scope, not faked).
