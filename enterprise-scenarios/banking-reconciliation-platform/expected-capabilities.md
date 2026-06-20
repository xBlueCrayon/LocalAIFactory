# Expected Capabilities — Reconcile

> **Synthetic exercise.** Original fictional scenario. No product is cloned. The Mauritius framing is
> **awareness-only** background and carries no regulatory meaning. This document is **not** legal,
> regulatory, or compliance advice, and claims **no** certification or conformance.

This file states honestly what LocalAIFactory can do **today** for this scenario versus what is
**future** work. It is deliberately conservative: capability is described, not promised.

---

## How this aligns with the platform

LocalAIFactory targets banking middleware (BDM-style direct debit, MSSQL, EF Core, ASP.NET Core).
This scenario sits squarely in that core strength area: direct-debit settlement, file exchange, and
ledger reconciliation. It also exercises the platform's payments and Mauritius-context **awareness**
knowledge — used here only as domain background, never as a compliance assertion.

---

## Today (current strengths)

- **Reason about the domain:** explain mandates, claims, settlement files, postings, breaks, and the
  maker/checker/approver control in concrete banking-middleware terms.
- **Propose an MVC + MSSQL + EF Core design:** entities, relationships, and migration shape that fit
  the host architecture's conventions (additive schema, lightweight list queries, no blocking
  external calls on the request path).
- **Design idempotent ingestion and posting:** content-hash + business-key registration, idempotency
  keys, and re-runnable deterministic matching.
- **Design the control model:** segregation of duties, server-side state transitions, append-only
  audit, role/scope authorisation.
- **Draft tests** for idempotency, determinism, control-total validation, and SoD enforcement.
- **Surface failure modes** (duplicates, cut-off, partial files, out-of-sequence, replayed posting)
  and how to handle each safely.

---

## Future (not yet, or needs real integration)

- **Live SFTP / core-banking connectors:** real transport, credentials, and a real ledger interface
  are environment-specific and out of scope for the simulation.
- **Counterparty-specific file formats:** parsing a particular real settlement file layout requires
  the actual specification, which is not assumed here.
- **Production cut-off scheduling and alerting** wired to real clocks, calendars, and on-call.
- **Multi-node ingestion** with distributed locking to prevent double-pull at scale.
- **Operational hardening:** load testing, failover, and disaster-recovery runbooks.

---

## Explicit non-goals / honesty notes

- No compliance, certification, or regulatory conformance is claimed or implied.
- The Mauritius context is awareness-only colour; it is not advice and not a statement about any
  real scheme or rulebook.
- The platform reasons about and scaffolds a solution; it does not connect to a real bank's
  production systems as part of this exercise.
