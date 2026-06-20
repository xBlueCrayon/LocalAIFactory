# Scenario: Meridian Equipment Finance — Leasing & Arrears Management Platform

> Fictional scenario for the LocalAIFactory enterprise capability simulation suite.
> All entities, names, and figures are invented. This document describes a *target*
> system to reason about; it is not a description of any shipped product.
>
> **Accounting / regulatory disclaimer:** Any reference to impairment staging,
> default classification, or IFRS 9 ECL (expected credit loss) in this scenario is
> *awareness-only*. Such treatment is an accounting and regulatory interpretation
> that requires sign-off by qualified finance and risk functions. Nothing here is
> accounting, legal, or regulatory advice.

## Business Problem

Meridian Equipment Finance is a mid-sized fictional lessor that finances commercial
equipment (forklifts, dental chairs, commercial kitchens, light fleet) under finance
and operating leases. Today the contract book, the amortization math, and the
collections workflow live in three disconnected places: a contract spreadsheet, an
ageing accounting package, and a collections team working from exported CSVs and
sticky notes. As a result:

- Amortization schedules are recomputed by hand when a contract is restructured,
  and the recomputed numbers frequently disagree with the accounting ledger.
- Arrears are detected late because ageing runs monthly, not daily, so an account
  can be 50+ days past due before anyone calls the lessee.
- Collections actions are not linked to the contract that triggered them, so the
  audit trail for "who promised to pay what, when" is reconstructed from email.
- There is no single, queryable view of exposure by contract, by collateral, or by
  arrears bucket, which makes month-end impairment discussions slow and contested.

The business wants one platform that owns the contract lifecycle from origination
through amortization, surfaces arrears the day they occur, and gives collections a
disciplined, auditable workflow.

## Current-State Process

1. Sales books a deal in a spreadsheet; a schedule is pasted in from a desktop
   calculator.
2. The accounting package ingests a monthly journal; discrepancies are emailed back.
3. A nightly bank statement import is matched to installments by hand.
4. Once a month, an ageing report is produced; accounts past due are listed by name.
5. Collections works the list informally; promises-to-pay are tracked per collector.
6. Impairment staging is argued in a month-end meeting using the ageing report and
   tribal knowledge, with no consistent rule applied across the book.

## Target-State Process

1. **Origination** captures the contract, asset(s), collateral, and commercial terms
   in one transaction; the system generates the amortization schedule deterministically.
2. **Amortization** runs as a versioned engine: every schedule is reproducible from
   contract terms, and restructures produce a new schedule version without destroying
   the prior one.
3. **Installment processing** posts due amounts on schedule and reconciles incoming
   payments (bank import) against installments, partial payments included.
4. **Arrears detection** runs daily, ageing each unpaid installment and rolling
   contracts into arrears buckets (e.g., 1–30 / 31–60 / 61–90 / 90+ days past due).
5. **Collections** receives auto-created cases for newly delinquent contracts, with a
   workflow for contact attempts, promises-to-pay, broken promises, and escalation.
6. **Staging signals** compute *candidate* impairment-stage indicators (e.g., a
   significant-increase-in-credit-risk flag, a default-candidate flag) that finance
   reviews. The platform never finalizes accounting treatment on its own.

## Users and Roles

- **Origination Officer** — creates contracts, attaches collateral, requests schedule
  generation. Cannot alter posted installments.
- **Servicing Analyst** — manages payment matching, partial payments, and
  schedule adjustments within policy.
- **Collections Agent** — works delinquent cases, logs contacts and promises-to-pay,
  raises escalations. Read-only on financial terms.
- **Collections Supervisor** — reassigns cases, approves payment plans and
  write-off recommendations, views team performance.
- **Risk / Finance Reviewer** — reviews staging candidate signals and exposure
  reports; approves or overrides staging proposals (the override is recorded, the
  decision remains theirs).
- **Auditor (read-only)** — full read access to contracts, ledger, and the audit log;
  no mutation rights.
- **Platform Administrator** — manages users, roles, and integration configuration;
  cannot edit financial records directly.

## Data Entities

- **Contract** — lessee, product type (finance/operating lease), principal/financed
  amount, rate, term, start date, day-count convention, status (Draft, Active,
  Restructured, Closed, Written-off).
- **Schedule** — a versioned amortization plan belonging to a contract; carries the
  generation method, effective date, and a supersedes/superseded-by link.
- **Installment** — a single dated line on a schedule: due date, principal portion,
  interest portion, fees, total due, amount paid, paid date, status (Pending, Paid,
  Partially Paid, Overdue, Waived).
- **Payment** — an inbound receipt (from bank import or manual entry) with allocation
  records mapping it across one or more installments.
- **Arrears** — a derived record per contract capturing days-past-due, current arrears
  bucket, total overdue amount, and the oldest unpaid installment.
- **Collateral** — asset securing the contract: description, valuation, valuation date,
  lien status, and link to the contract(s) it secures.
- **CollectionsCase** — opened when a contract enters arrears: assigned agent, status,
  contact log, promises-to-pay, payment-plan proposals, escalation level.
- **StagingSignal** — a *candidate* impairment indicator for a contract at a point in
  time: signal type, computed value, threshold breached, and reviewer disposition.
  This is advisory input, not a posted accounting entry.
- **AuditEvent** — append-only record of who did what to which entity and when.

## Integrations

- **Bank statement import** — daily file (e.g., CAMT/CSV) of inbound receipts for
  payment matching; idempotent on re-import.
- **General-ledger export** — outbound journal of postings (installment accruals,
  receipts, fee income) for the accounting system to consume; the platform is the
  sub-ledger of record for the leasing book.
- **Collateral valuation feed** — periodic valuation updates for asset revaluation.
- **Identity provider** — Windows / enterprise SSO for authentication; roles mapped
  to platform roles.
- **Notification channel** — email/queue for collections reminders and escalations.

All integrations are optional at the platform boundary: the system must still open,
display contracts, and compute schedules with only MSSQL present and every external
feed offline.

## Security and Audit Controls

- Role-based access control enforced server-side; deny-by-default for any action not
  explicitly granted to the caller's role.
- Object-level authorization on every contract/case read and write (no
  insecure-direct-object-reference: a Collections Agent cannot open a contract they
  are not assigned to merely by guessing an id).
- **Append-only audit log** for all financial mutations, staging dispositions, and
  case actions; audit rows are never updated or deleted.
- Segregation of duties: the role that proposes a write-off cannot also approve it.
- Sensitive fields (lessee identifiers, bank details) are access-controlled and
  redacted in list/export views unless the caller is authorized.
- No secrets in source; integration credentials come from environment configuration.

## Reporting Requirements

- **Arrears ageing** — contracts by arrears bucket with overdue totals, refreshed daily.
- **Exposure by collateral** — net exposure grouped by collateral type and valuation.
- **Collections performance** — promises made vs kept, broken-promise rate, cases by
  status and agent.
- **Schedule reproducibility report** — proves a stored schedule equals a fresh
  recomputation from contract terms.
- **Staging candidate summary** — counts of contracts flagged by each staging signal,
  with reviewer disposition (clearly labelled *candidate / advisory*).
- All list/report queries must select lightweight projections, never materialize large
  text columns, and must complete well under one second on a modest SQL Server.

## Failure Modes

- **Schedule drift** — stored schedule diverges from a recompute after a restructure;
  must be detected, not silently tolerated.
- **Double-applied payment** — a re-imported bank file allocates the same receipt
  twice; import must be idempotent.
- **Mis-bucketed arrears** — an off-by-one in day-count rolls a contract into the wrong
  ageing bucket and triggers wrong collections action.
- **Orphaned case** — a contract is cured (paid up) but its collections case stays open.
- **Authorization bypass** — an agent reaches a non-assigned contract by id.
- **Stale staging signal** — a signal computed on yesterday's data is treated as today's.
- **External feed outage** — bank/GL/valuation feed down; the platform must degrade to
  read-and-compute, never block the UI.

## Acceptance Criteria

- A contract created with known terms produces a deterministic, reproducible schedule;
  a fresh recompute equals the stored schedule to the defined rounding tolerance.
- A restructure creates a new schedule version and preserves the prior version intact.
- Daily arrears run buckets every unpaid installment correctly at known boundaries
  (e.g., exactly 30 and 31 days past due land in adjacent buckets).
- A delinquent contract auto-opens exactly one collections case; curing it closes the case.
- Re-importing the same bank file does not double-allocate payments.
- Every financial mutation and staging disposition writes an append-only audit row.
- An agent cannot read or act on a contract outside their assignment.
- Staging signals are computed and surfaced as *candidate* indicators only; no
  accounting treatment is finalized by the platform.
- Core pages and reports load in well under one second with external feeds offline.

## Expected Architecture

- **ASP.NET Core MVC** UI and controllers; server-side authorization on every action.
- **MSSQL + EF Core** as the system of record (sub-ledger for the leasing book);
  migrations are additive and reproducible.
- **Amortization engine** — a deterministic, side-effect-free domain service that
  takes contract terms and returns a schedule; pure enough to unit-test exhaustively
  and to re-run for reproducibility checks. Schedules are versioned, never overwritten.
- **Arrears / staging service** — a scheduled job that ages installments, rolls
  contracts into buckets, opens/closes collections cases, and computes *candidate*
  staging signals. Staging output is advisory and reviewer-gated.
- **Integration adapters** — bank import, GL export, valuation feed, all behind
  interfaces and all optional; the request path never blocks on an external call.
- **Read models / projections** — lightweight records for list and report views to
  keep queries fast and avoid materializing large columns.

## Expected Tests

- **Amortization unit tests** — known contracts with hand-verified schedules; rounding
  and day-count edge cases; reproducibility (recompute == stored).
- **Restructure tests** — new version created, prior version preserved, links correct.
- **Arrears bucketing tests** — boundary day counts (0/1, 30/31, 60/61, 90/91).
- **Payment allocation tests** — partial payments, multi-installment allocation,
  idempotent re-import (no double allocation).
- **Collections lifecycle tests** — case auto-open on delinquency, auto-close on cure,
  promise-to-pay and broken-promise transitions.
- **Authorization tests** — deny-by-default, object-level access, IDOR attempt blocked.
- **Audit tests** — every mutation appends a row; rows are immutable.
- **Staging signal tests** — signals computed correctly and labelled advisory; no
  signal alone finalizes accounting treatment.
- **Performance / degradation tests** — core pages and reports under one second with
  feeds offline.

## Expected Deployment Concerns

- Runs on IIS / Kestrel against an on-prem SQL Server; must function MSSQL-only.
- Database migrations applied on startup; additive and backward-compatible by default.
- The daily arrears/staging job must be schedulable and idempotent (safe to re-run).
- Integration credentials supplied via environment configuration, not committed.
- Time-zone and business-calendar handling pinned explicitly (ageing depends on it).
- Capacity sized for daily batch over the full contract book within the maintenance
  window.

## Rollback Considerations

- Schema changes are additive, so rolling back the application binary does not require
  a destructive down-migration; a failed release reverts to the prior version.
- The amortization engine is versioned: a bad engine release can be pinned back, and
  affected schedules recomputed and compared against the prior version.
- The daily arrears/staging job is idempotent, so a rolled-back run can simply be
  re-executed once the issue is fixed.
- Bank imports are idempotent and journaled, so a rollback does not risk double-posting.
- Staging signals are advisory and not posted to the GL, so rolling them back has no
  accounting impact — but any reviewer dispositions made meanwhile must be retained
  in the audit log.

## CEO/CTO Summary

Meridian's leasing book is currently run across a spreadsheet, an ageing accounting
package, and an informal collections process — and the three disagree. This scenario
describes one platform that owns the contract from origination through amortization,
detects arrears daily instead of monthly, and gives collections a disciplined,
auditable workflow. The amortization engine is deterministic and versioned, so every
schedule is reproducible and every restructure is traceable. Arrears bucketing and
impairment *staging signals* are computed automatically, but staging, default
classification, and ECL remain accounting and regulatory judgements that qualified
finance and risk functions own and sign off — the platform informs that judgement, it
does not make it, and it makes no compliance or certification claim. The result the
business buys is a single source of truth for exposure, earlier intervention on
arrears, and a clean audit trail from contract to collection.
