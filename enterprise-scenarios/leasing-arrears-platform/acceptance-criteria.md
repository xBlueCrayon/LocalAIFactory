# Acceptance Criteria — Leasing & Arrears Platform

Measurable, checkable criteria. Each item is pass/fail. No item asserts compliance or
certification; staging/default/ECL items are advisory-only.

## Amortization & schedules

- [ ] A contract with fixed known terms produces a schedule whose installment count
      equals the term, with principal fully amortized to zero at the final installment
      (within rounding tolerance, residual posted to the last line).
- [ ] Sum of principal portions across all installments equals the financed amount to
      the defined tolerance (e.g., ≤ 0.01 currency units).
- [ ] Recomputing a stored schedule from contract terms reproduces it exactly to
      tolerance (reproducibility check passes).
- [ ] A restructure creates a new schedule version; the prior version remains readable
      and is linked via supersedes/superseded-by.

## Installments & payments

- [ ] A full on-time payment marks the matched installment Paid with the correct paid date.
- [ ] A partial payment marks the installment Partially Paid and tracks the remaining due.
- [ ] One payment can be allocated across multiple installments correctly.
- [ ] Re-importing an identical bank file allocates zero additional amounts (idempotent).

## Arrears & bucketing

- [ ] The daily ageing job runs to completion and is safe to re-run (idempotent).
- [ ] An installment exactly N days past due lands in the correct bucket at boundaries
      0/1, 30/31, 60/61, and 90/91 days.
- [ ] A contract's arrears record reflects the oldest unpaid installment and the
      correct total overdue amount.

## Collections

- [ ] A contract entering arrears auto-opens exactly one collections case (no duplicates).
- [ ] Curing a contract (paid up to date) auto-closes its open case.
- [ ] Promise-to-pay, kept-promise, and broken-promise transitions are recorded with
      timestamps and the acting agent.
- [ ] Escalation raises the case level and is reflected in supervisor views.

## Staging signals (advisory)

- [ ] Candidate staging signals compute deterministically from contract/arrears state.
- [ ] Every staging signal is labelled *candidate / advisory* in UI and exports.
- [ ] No staging signal, by itself, changes contract accounting status; a reviewer
      disposition is required and recorded.
- [ ] Reviewer override of a candidate signal is captured in the audit log.

## Security & audit

- [ ] All authorization is enforced server-side; deny-by-default verified for an
      unprivileged role.
- [ ] An agent cannot read or mutate a contract outside their assignment (IDOR blocked).
- [ ] The role proposing a write-off cannot also approve it (segregation of duties).
- [ ] Every financial mutation and staging disposition appends an immutable audit row.
- [ ] Audit rows cannot be updated or deleted through any application path.

## Performance & degradation

- [ ] Each core page returns HTTP 200 in well under one second on a modest SQL Server.
- [ ] List and report queries use lightweight projections (no large text columns
      materialized).
- [ ] With bank/GL/valuation feeds offline, every core page still renders and contracts
      still compute schedules (MSSQL-only operation verified).

## Deployment & rollback

- [ ] Migrations apply cleanly on startup and are additive/backward-compatible.
- [ ] Rolling back to the prior application binary requires no destructive down-migration.
- [ ] A pinned-back amortization engine version recomputes affected schedules for
      comparison.
- [ ] A rolled-back arrears run can be re-executed once without side effects.
