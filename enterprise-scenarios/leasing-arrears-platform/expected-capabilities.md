# Expected Capabilities — Leasing & Arrears Platform

An honest split between what a LocalAIFactory-style build could realistically deliver
**today** versus what is **future** work. No capability below implies compliance,
certification, or accounting advice.

> **Awareness-only note:** impairment staging, default classification, and IFRS 9 ECL
> are accounting/regulatory interpretations requiring qualified sign-off — not advice.

## Today (realistic with the current stack)

- **Contract & schedule modelling** in MSSQL + EF Core with additive migrations.
- **Deterministic amortization engine** as a pure domain service: given terms, returns
  a schedule; fully unit-testable with hand-verified fixtures.
- **Versioned schedules** — restructures create new versions; prior versions preserved.
- **Reproducibility check** — recompute a stored schedule and compare to tolerance.
- **Installment posting and payment allocation**, including partial payments.
- **Idempotent bank import** keyed on receipt identity to prevent double allocation.
- **Daily arrears ageing and bucketing** as a schedulable, idempotent job.
- **Collections case lifecycle** — auto-open on delinquency, auto-close on cure,
  contact log, promises-to-pay, escalation.
- **Candidate staging signals** computed and surfaced as advisory, reviewer-gated input.
- **Server-side RBAC, object-level authorization, append-only audit log.**
- **Fast list/report projections** that never materialize large text columns and load
  well under one second with external feeds offline.
- **MSSQL-only operation** — every page renders with bank/GL/valuation feeds absent.

## Future (explicitly out of scope for an initial build)

- **Automated GL posting** straight into the accounting system (vs. an export the
  accounting team consumes).
- **Full IFRS 9 ECL model** (PD/LGD/EAD parameter estimation, lifetime vs 12-month ECL).
  The platform only ever produces *candidate signals* for human staging decisions.
- **Real-time payment matching** against live banking APIs (vs. daily file import).
- **Predictive collections** (ML propensity-to-pay, optimal contact timing).
- **Automated collateral revaluation** from external market data with write-back.
- **Multi-currency and cross-jurisdiction day-count/calendar packs** beyond the
  configured book.
- **Customer self-service portal** for lessees to view schedules and pay online.
- **Document generation** (contracts, demand letters) and e-signature.
- **Straight-through restructuring** with automated approval routing.

## Explicit non-goals

- The platform does **not** decide impairment stage, default status, or ECL — it
  surfaces candidate signals for qualified reviewers.
- The platform makes **no** compliance, certification, or audit-opinion claim.
- The platform is **not** a general accounting system; it is the sub-ledger for the
  leasing book and exports to accounting.
