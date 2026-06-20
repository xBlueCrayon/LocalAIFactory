# Acceptance Criteria — Indigo Trading Ltd Scenario

> **Synthetic, inspired-by only. Not accounting/tax/legal advice. Mauritius items awareness-only.**

A reviewer uses this checklist to judge a **solution design** produced for this scenario. Each item is
binary (met / not met) and, where possible, measurable.

## A. Ledger integrity

- [ ] Every financial effect originates from a **source document**; nothing edits the GL directly.
- [ ] Posting validation **rejects any journal where debits ≠ credits**.
- [ ] Posted journals are **immutable**; corrections are by **reversal entry only**.
- [ ] Document post + GL journal + subledger update + audit write occur in **one DB transaction** with
      full rollback on partial failure.
- [ ] Control accounts (AR, AP, inventory, bank) reconcile to their subledgers by design.

## B. Controls and audit

- [ ] **Maker/checker** is enforced on financial postings above a configurable threshold.
- [ ] **Segregation of duties**: a user cannot approve their own draft above threshold; admins cannot
      approve financial transactions.
- [ ] **RBAC is deny-by-default**, enforced server-side on every action.
- [ ] **Append-only audit trail** records actor, timestamp, action, entity, and before/after hashes;
      records are never updated or deleted.
- [ ] **IDOR protection**: document access is scoped server-side to permitted entities.

## C. Period management

- [ ] Periods support **Open / SoftClosed / HardClosed** states.
- [ ] Posting into a **hard-closed period is blocked**; soft-closed allows adjustments only per policy.
- [ ] **Pre-close validations** run (unbalanced batches, unreconciled control accounts) before locking.
- [ ] **Reopening** a closed period is a privileged, audited action with a recorded reason.

## D. Inventory consistency

- [ ] Stock movements update **stock-on-hand and valuation** in the same transaction that posts to GL.
- [ ] **Valuation basis** (e.g., weighted average) is defined and recalculated correctly on receipt.
- [ ] **Overselling** (issue > stock-on-hand) is blocked or flagged per policy, never silent.
- [ ] **Concurrent** stock issues on the same SKU do not oversell or corrupt valuation.

## E. Reporting

- [ ] **Trial Balance** as at any date with **drill-down** to journal lines and source documents.
- [ ] **P&L** and **Balance Sheet** by period and cost centre.
- [ ] **Aged Debtors / Aged Creditors** with correct ageing buckets.
- [ ] **Inventory valuation** and per-SKU movement history.
- [ ] **Payroll summary** per run (figures illustrative, marked **not tax advice**).
- [ ] **Audit report** filterable over the append-only trail.

## F. Architecture and operability

- [ ] Maps cleanly to **Core / Data / Web** with EF Core entities and migrations.
- [ ] List views use **lightweight projection records**; large text/line collections are never
      materialized into list grids.
- [ ] **MSSQL-only mode**: every core page loads quickly and every posting succeeds with no AI services.
- [ ] **No external-service call on the request path**; health from a cached snapshot.
- [ ] Schema changes are **additive and backward-compatible**.

## G. Tests and quality gates

- [ ] Unit tests cover: balanced-journal, valuation recalculation, ageing buckets, maker==checker
      rejection, closed-period rejection.
- [ ] Integration tests cover: end-to-end post, rollback on injected failure, soft/hard close behaviour.
- [ ] Concurrency test covers simultaneous stock issues.
- [ ] Authorization tests cover per-role access and a denied IDOR attempt.

## H. Discipline and disclaimers

- [ ] Finance content carries a **"not accounting/tax/legal advice"** note.
- [ ] Mauritius statutory content carries an **"awareness-only"** note.
- [ ] **No vendor compatibility/equivalence/certification** is claimed anywhere.
- [ ] The design is **honest** about what is reasoning vs. what would be future implementation.
