# Acceptance Criteria — Meridian CRM Scenario

A measurable, checkable list. Each item is pass/fail. "Test data" refers to a seeded fixture of
representative accounts, contacts, leads, opportunities, cases, and activities.

## Data Model & Persistence

- [ ] All six entities (Account, Contact, Lead, Opportunity, Case, Activity) exist as EF Core
      entities with migrations checked into source control.
- [ ] Account → Contact, Account → Opportunity, Account → Case relationships are enforced by FK.
- [ ] Activity links polymorphically to exactly one parent (Account/Contact/Lead/Opportunity/Case).
- [ ] Opportunity and Lead carry an optimistic-concurrency row version.
- [ ] Opportunity weighted value = estimated value × probability, computed correctly on save.

## Functional Behaviour

- [ ] Each entity supports create, read, update, and delete via an MVC controller.
- [ ] Converting a qualified Lead produces exactly one Contact and one Opportunity, both linked to
      the resolved Account, in a single transaction.
- [ ] A duplicate lead-conversion submission (same token) does not create duplicate records.
- [ ] Two concurrent edits to the same Opportunity result in one success and one concurrency error.
- [ ] Case severity 1 or 2 sets the shorter SLA target per the documented rule.

## Security & Audit

- [ ] Authorization is enforced server-side; removing UI controls does not grant access.
- [ ] A Sales Rep cannot edit an Opportunity they do not own (IDOR attempt returns 403/404).
- [ ] A request matching no authorization rule is denied (deny-by-default).
- [ ] Every create/update/delete/reassign writes exactly one append-only audit row with actor,
      timestamp, entity, action, and before/after for sensitive fields.
- [ ] Audit rows are never updated or deleted by application code.
- [ ] Outbound notification respects the contact marketing-consent flag.

## Reporting & Performance

- [ ] Weighted pipeline by stage/owner/region returns correct totals on test data.
- [ ] Win/loss report shows rate and loss-reason breakdown over a rolling window.
- [ ] Open-case aging report groups by severity and counts SLA breaches.
- [ ] Account-360 page renders contacts, open opportunities, open cases, and recent activities and
      returns in under one second on test data.
- [ ] Cross-cut report lists accounts with both an open severity-1/2 case and an open renewal
      opportunity.
- [ ] List and report queries use lightweight projections; no large text column is materialized in
      a list view.

## Local-First & Resilience

- [ ] The full scenario runs with MSSQL only — no Ollama, no vector store, no internet.
- [ ] When the notification relay is unavailable, requests still succeed and alerts queue.
- [ ] No controller action or view performs a synchronous call to an external service.
- [ ] Migrations are additive and backward-compatible (rollback-safe).

## Tests Present

- [ ] Authorization matrix tests (role × action, including IDOR).
- [ ] Lead-conversion integration + idempotency test.
- [ ] Optimistic-concurrency test.
- [ ] Reporting correctness tests (weighted pipeline, account-360).
- [ ] Audit-append test (one row per mutating action).
