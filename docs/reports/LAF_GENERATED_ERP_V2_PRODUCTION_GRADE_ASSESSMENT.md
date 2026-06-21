# LAF-Generated ERP V2 — Production-Grade Assessment

**Date:** 2026-06-21 · **Verdict: PILOT-grade, NOT production-grade (~35% production readiness).**

## What is genuinely solid

- **Architecture:** clean layered modular monolith (Core → Data → Services → Web), no cycles, controllers
  delegate to services.
- **Financial integrity:** double-entry GL that refuses unbalanced vouchers and balances globally
  (test-proven); immutable posted documents; reversal-not-edit on cancel.
- **Inventory integrity:** immutable stock ledger, moving-average valuation, negative-stock guard.
- **Controls:** maker/checker (submitter ≠ approver), threshold approval, mandatory reject reason, audit on
  every transition — all tested, including an explicit end-to-end scenario.
- **Quality bar:** 82 .NET tests + 13 Playwright tests pass; app runs with 0 HTTP 500s.
- **Generation:** emitted by a repeatable generator; governed local-LLM catalog layer with a collision guard.

## What blocks production (honest gaps)

| Area | Gap |
|---|---|
| **Authentication** | dev cookie only — no real auth, no Windows/SSO binding, no credential storage, no 2FA |
| **Transport** | runs over HTTP for the demo; no TLS/HSTS in-app |
| **Migrations** | schema via `EnsureCreated`, not EF migrations — no controlled upgrades |
| **Module coverage** | no manufacturing, HR/payroll, POS, website/eCommerce; partial selling/buying (no quotation/delivery/receipt/returns) |
| **Reporting** | no P&L, Balance Sheet, aging, period closing, report builder |
| **UI** | read-oriented; only the catalog has a write path (API); no general create/edit forms |
| **Scale** | no production-hardware load test; in-memory report aggregation |
| **Ops** | no monitoring/alerting, backup/restore drill, or SIEM integration wired |

## Critical/high issues found & fixed

The three generation bugs (catalog endpoint marker, minimal-API binding, catalog route) were fixed **in the
generator** and re-emitted. No outstanding critical/high code issues remain in the generated product; the
remaining items above are **scope/maturity gaps**, not defects.

## Bottom line

V2 is a credible **PILOT-grade** ERP core with real, tested financial controls — suitable to demo and to
build on — but **not production-grade**: production needs real authentication, TLS, migrations, broader
module coverage, full reporting, write UIs, and an operations/security hardening pass (operator-owned).
