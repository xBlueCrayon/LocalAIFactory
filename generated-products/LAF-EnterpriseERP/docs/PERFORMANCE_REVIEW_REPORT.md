# LAF Enterprise ERP — Performance Review Report

**Date:** 2026-06-21 · **Scope:** generated ERP proof product

## Observed

- Full .NET test suite (74 tests, incl. WebApplicationFactory boot) completes in ~1–2 s.
- 12 Playwright page loads complete in ~5 s total; individual pages render in 50–165 ms (SQLite, local).
- App cold-starts (EnsureCreated + seed + demo data) in a few seconds.

## Design notes

- **Indexes** are defined on the hot paths: GL by `(AccountId, PostingDate)`, `(PartyType, PartyId)`,
  `(VoucherType, VoucherNo)`; stock ledger by `(ItemId, WarehouseId, Id)`; unique business keys on
  master codes and document numbers.
- **Stock balance/valuation** reads the latest ledger row (`OrderByDescending(Id).First`) rather than
  re-summing the whole ledger — O(1)-ish per item+warehouse with the index.
- **Report aggregates** (Trial Balance, AR/AP, GL totals) materialize then sum in memory to guarantee
  exact decimal results across SQLite and SQL Server.

## Findings / risks

| Severity | Finding | Recommendation |
|---|---|---|
| Low | `PostSalesInvoice` issues per-line `Items.First(...)` lookups | Batch-load items per invoice if line counts grow large |
| Low | In-memory report aggregation reads all matching GL rows | Fine at POC scale; for large GL, push aggregation to SQL Server (which handles decimal sums) or add period/account filters + summary tables |
| Info | No load/stress test was run on production hardware | A real load benchmark is an operator task on a real DB/host |

## Verdict

Performance is appropriate for a POC and the schema is indexed for the obvious access patterns. **No
production-scale load test was performed** — that requires real hardware + a populated SQL Server and
is explicitly out of scope (not faked).
