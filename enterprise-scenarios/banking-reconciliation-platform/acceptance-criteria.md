# Acceptance Criteria — Reconcile

> **Synthetic exercise.** Original fictional scenario. Mauritius framing is awareness-only. Not legal,
> regulatory, or compliance advice; no certification claimed.

A measurable checklist. Each item is pass/fail and independently verifiable.

## Ingestion and file integrity

- [ ] Re-ingesting a file with the same content hash creates **no** second `SettlementFile` row.
- [ ] Re-ingesting a file with the same business key + business date is rejected as a duplicate.
- [ ] A file whose declared record count does not match its actual contents is rejected **atomically**
      (zero claims persisted from that file).
- [ ] A file whose control total does not match the sum of its claims is rejected atomically.
- [ ] An out-of-sequence file (sequence gap or regression) is held for review, not processed.
- [ ] Every received and every sent file appears in the file register with hash and sequence.

## Reconciliation engine

- [ ] Matching the same claims against the same postings twice yields **identical** outcomes.
- [ ] Each claim resolves to exactly one of Matched / Break / Unmatched.
- [ ] An amount or status mismatch produces a Break with expected vs actual recorded.
- [ ] Re-running matching after a rollback reproduces the prior outcomes.

## Controls (maker / checker / approver)

- [ ] A user cannot be both maker and checker on the same exception.
- [ ] A correction at or above the configured threshold requires a distinct approver.
- [ ] State transitions are validated server-side; a forged client transition is rejected.
- [ ] A rejected resolution returns the exception to the maker, not to a closed state.

## Idempotent posting

- [ ] Submitting the same approved correction twice produces **one** ledger adjustment.
- [ ] Each adjustment posting carries a unique idempotency key.

## Audit

- [ ] Every state transition writes an audit record (actor, action, entity, before/after, timestamp,
      correlation id).
- [ ] There is **no** code path that updates or deletes an audit record.
- [ ] The auditor role can read audit and reports and can change nothing.

## Reporting

- [ ] The daily summary reconciles to the file register (no claims unaccounted for).
- [ ] Open-exceptions ageing and maker/checker activity render from stored state.
- [ ] Any historical report can be regenerated and matches the original.

## Platform behaviour

- [ ] Core pages render from MSSQL alone; SFTP/core-banking being down does not block triage or
      reporting.
- [ ] List views do not materialise large file-body columns.
- [ ] No external service is called synchronously on the request path.
