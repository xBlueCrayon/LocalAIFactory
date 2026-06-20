# Acceptance Criteria — Measurable Checklist

Each item is binary (met / not met) and independently verifiable.

## Modularity & Boundaries

- [ ] The module lives in its own project depending only on shared `Core` abstractions.
- [ ] The module owns its own schema-prefixed MSSQL tables (e.g. `leasing.*`).
- [ ] No migration in this module alters another module's tables or columns.
- [ ] Cross-module access is only via injected service interfaces (no cross-schema FKs).
- [ ] No project-reference cycles are introduced.

## Billing Correctness

- [ ] A monthly schedule for a mid-month start produces correct prorated first period.
- [ ] Quarterly and monthly frequencies both generate the expected number of periods.
- [ ] Leap-day and month-end (28/30/31) proration are deterministic and unit-tested.
- [ ] Metered charges = base recurring + (usage delta × agreed rate), verified by test.
- [ ] Regenerating a schedule produces zero duplicate billing periods (idempotency key).

## Invoicing & Posting

- [ ] Invoices are created as **drafts** and never post to Accounting directly.
- [ ] Posting goes through the `IAccountingPosting` contract; failure leaves draft "pending".
- [ ] A failed post is retryable and never silently drops the invoice.

## Asset Lifecycle

- [ ] Asset state machine enforces legal transitions only (no skipping illegal states).
- [ ] Cancelling a lease drives the asset back to an available/return state via a
      compensating transition with a recorded reason.
- [ ] Every transition writes one `AssetStateTransition` row.

## Security & Audit

- [ ] Every command is role-gated; an ungranted role is denied (deny-by-default).
- [ ] Finance-sensitive actions require the Finance Approver role.
- [ ] Each consequential action writes exactly one append-only `AuditEvent`.
- [ ] Audit entries are never updated or deleted in place.
- [ ] Tenant/project scoping is enforced server-side; client-supplied IDs are not trusted.

## Reliability & Concurrency

- [ ] Concurrent amendments to one agreement are resolved by optimistic concurrency.
- [ ] A partial usage-import batch skips bad rows without aborting the whole batch.
- [ ] The scheduling worker is idempotent and safe to restart mid-run.

## Runtime & Deployment

- [ ] The module functions with only SQL Server present (no external dependency required).
- [ ] No blocking external-service call occurs on the request path.
- [ ] The module can be feature-flagged on/off per environment.
- [ ] Disabling the module is non-destructive (data retained, endpoints gated off).

## Testing

- [ ] Unit tests cover proration, schedule generation, and metered charges.
- [ ] Authorization tests cover allow/deny for each role × command.
- [ ] Audit tests assert one audit entry per consequential command.
- [ ] An integration test exercises draft → posting against an Accounting stub.
