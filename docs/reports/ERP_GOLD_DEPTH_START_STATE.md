# ERP-GOLD-DEPTH — Phase 0: Start State

**Sprint:** ERP-GOLD-DEPTH
**Branch:** `ke-008-code-symbols`
**Starting commit:** `0d3e491`
**Stamp:** 2026-06-21

## Goal

Move the generated reference ERP from `ERP_LOCAL_PRODUCTION_READY_CORE` toward ERPNext-grade
**module depth** — turning skeleton modules (notably manufacturing and reporting) into real,
tested business flows, while reproducing every addition through the deterministic generator.

This is a depth sprint, not a parity claim. No ERPNext code was studied or copied (clean-room).

## Entry gates (verified at sprint start)

| Gate | State at start |
|------|----------------|
| Main solution build (`LocalAIFactory.sln`, Release) | 0 errors |
| Main app unit tests | 240 / 240 PASS |
| Knowledge packs | PASS |
| Production readiness gate V3 | NEAR_GA |
| Security review | PASS |
| SQL Server LocalDB available | Yes (`(localdb)\MSSQLLocalDB`) |
| SQL Express available | Yes (`.\SQLEXPRESS`) |

## Reference product baseline at start

| Metric | Value at start |
|--------|----------------|
| Classification | ERP_LOCAL_PRODUCTION_READY_CORE |
| ERPNext parity (honest self-assessment) | 39% |
| Production-grade mean | 78 |
| .NET (xUnit) tests | 222 |
| Playwright tests | 38 |
| End-to-end scenarios | 13 |
| Manufacturing | CRUD skeleton only |
| Reports | Core accounting + stock |
| Live SQL Server proof | Verified design, not yet applied to a live instance |

## Honest limitations / not done (at start of sprint)

- Manufacturing was a catalog stub (entity + CRUD), with no BOM-driven production, costing, or
  quality gating.
- Reporting covered only the core accounting (GL/TB/P&L/BS/AR-AP) and stock balance — no
  registers, party summaries, aging, tax summary, valuation, reorder, or work-order summary.
- The committed EF migration had been verified by design but **not applied to a live SQL Server
  instance** in this environment.
- HR/payroll, POS and e-commerce were (and remained) CRUD skeletons.
- No delivery-note or return (reverse) document chains existed.

These are the gaps the sprint targeted (manufacturing, reports, live SQL proof) and the gaps it
explicitly did **not** close (HR/POS/e-commerce, return chains) — see the gap matrix and the
production review for the after-state.
