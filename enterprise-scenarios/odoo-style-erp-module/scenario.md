# Scenario: Equipment Subscription & Leasing Module for a Modular ERP

> Original synthetic scenario authored for the LocalAIFactory enterprise capability
> simulation suite. Inspired by the *idea* of pluggable, community-style ERP modules.
> It does not reproduce, clone, or claim compatibility with any vendor product, and
> makes no equivalence or certification claims.

## Business Problem

"Northwind Equipment Co." rents and leases industrial machinery (forklifts, generators,
floor scrubbers) to commercial customers. Their existing ERP handles sales, inventory,
and accounting well, but has no first-class concept of a recurring *subscription lease*:
a contract that bills on a schedule, accrues usage charges, and manages the lifecycle of
a physical asset that is loaned rather than sold. Today the team improvises with manual
invoices and spreadsheets. This causes missed billing cycles, disputed usage charges,
and no reliable view of which asset is at which customer site.

They want a new ERP module — installable alongside their existing modules — that adds a
clean "Leasing & Subscriptions" capability without forking the core platform.

## Current-State Process

- A salesperson creates a one-off quote in the Sales module for the rental.
- An accountant manually raises an invoice each month from a recurring reminder.
- Asset whereabouts are tracked in a shared spreadsheet, updated inconsistently.
- Usage (engine hours, meter readings) is emailed in and keyed by hand.
- End-of-lease return, damage assessment, and final billing are ad hoc.

## Target-State Process

- A lease agreement is created from a customer and one or more leasable assets.
- A billing schedule is generated automatically (monthly/quarterly, with proration).
- Recurring invoices are drafted on schedule and posted to the Accounting module.
- Usage readings are captured and converted into metered charges per agreed rate.
- Asset lifecycle states (Available → Reserved → On Lease → Returned → Maintenance)
  are tracked, with each transition audited.
- Lease end triggers a return checklist, condition assessment, and final settlement.

## Users and Roles

- **Leasing Agent** — creates and amends lease agreements, records usage.
- **Billing Clerk** — reviews draft invoices, posts them, handles disputes.
- **Asset Manager** — manages the asset catalogue and lifecycle states.
- **Finance Approver** — approves write-offs, credit notes, early-termination fees.
- **Read-Only Auditor** — views agreements, invoices, and the full audit trail.
- **System Administrator** — configures rate cards, billing calendars, and roles.

## Data Entities

- **LeaseAgreement** — customer, start/end dates, status, terms, billing frequency.
- **LeasableAsset** — asset code, category, serial, current lifecycle state, location.
- **LeaseLine** — links an agreement to a specific asset and its rate.
- **RateCard** — base recurring price plus metered rates (e.g. per engine hour).
- **BillingSchedule** — generated dated billing periods for an agreement.
- **UsageReading** — meter type, value, reading date, captured-by, source.
- **DraftInvoice / InvoiceLine** — proposed charges before posting to Accounting.
- **AssetStateTransition** — from-state, to-state, timestamp, actor, reason.
- **AuditEvent** — append-only record of every consequential action.

## Integrations (module boundaries, shared services)

- **Accounting module (boundary):** the leasing module never writes ledger entries
  directly. It produces *draft invoices* and calls a published Accounting service
  interface to post them. Posting is the Accounting module's responsibility.
- **Inventory/Asset module (boundary):** leasable assets reference master asset records
  by stable ID; the leasing module owns only lease-specific state, not the master record.
- **Customer/CRM module (boundary):** customers are referenced by ID; the module does
  not duplicate customer master data.
- **Shared services:** identity/authentication, notification (email), and a shared
  audit sink are consumed through platform-provided interfaces, not reimplemented.
- All cross-module calls go through explicit, versioned application service contracts —
  no direct foreign-key reach-ins across module schemas.

## Security and Audit Controls

- Role-based authorization on every command (create/amend/post/approve).
- Deny-by-default: an action with no matching role grant is rejected.
- Finance-sensitive actions (credit notes, early termination) require Finance Approver.
- Append-only `AuditEvent` log; entries are never updated or deleted in place.
- Every asset state transition and every invoice posting is audited with actor + reason.
- Server-side enforcement of project/tenant scoping; no trust in client-supplied IDs.
- Sensitive configuration (rate cards) is change-controlled and version-stamped.

## Reporting Requirements

- Active leases by customer, asset category, and region.
- Upcoming billing (next 30/60/90 days) and overdue invoices.
- Asset utilization: percentage of fleet on lease vs. available vs. maintenance.
- Revenue recognized vs. billed per period.
- Audit report: all finance-sensitive approvals in a date range.

## Failure Modes

- **Double billing:** schedule generates duplicate periods → idempotency key per period.
- **Orphaned asset state:** lease cancelled but asset stuck "On Lease" → compensating
  transition with audit reason.
- **Accounting unavailable:** posting fails → invoice stays "draft/pending", retried,
  never silently dropped.
- **Clock/proration errors:** month-boundary and leap-day proration must be deterministic
  and unit-tested.
- **Concurrent amendment:** two agents edit the same agreement → optimistic concurrency.
- **Partial usage import:** bad reading row must not abort the whole batch.

## Acceptance Criteria

See `acceptance-criteria.md` for the measurable checklist. In summary: the module
installs without modifying core schemas, generates correct prorated schedules, drafts
invoices idempotently, enforces role-based approval, and records a complete audit trail.

## Expected Architecture (modular ASP.NET Core MVC + MSSQL)

- A self-contained module project (e.g. `Leasing`) depending only on shared `Core`
  abstractions, exposing controllers, application services, and its own EF Core entities.
- The module owns its own MSSQL tables (schema-prefixed, e.g. `leasing.*`) and migrations;
  it does not alter other modules' tables.
- Cross-module communication via injected service interfaces (`IAccountingPosting`,
  `IAssetCatalog`, `ICustomerDirectory`) resolved through DI — clean boundaries, no cycles.
- MSSQL is the primary store; the module must function with only SQL Server present.
- No blocking external calls on the request path; scheduling and posting run in a
  background worker that drains a queue.

## Expected Tests

- Unit tests for proration, schedule generation, and metered-charge calculation.
- Idempotency tests: regenerating a schedule produces no duplicate billing periods.
- Authorization tests: each role can/cannot perform each command (deny-by-default).
- Audit tests: every consequential command writes exactly one append-only audit entry.
- Integration test: draft invoice → Accounting posting contract (with a fake/posting stub).
- Concurrency test: simultaneous amendments are resolved by optimistic concurrency.

## Expected Deployment Concerns

- Additive, backward-compatible migrations only; module install runs its own migrations.
- Feature-flag the module so it can be enabled per environment.
- Configuration (rate cards, billing calendar) seeded but overridable per deployment.
- Background worker must be idempotent and safe to restart mid-run.

## Rollback Considerations

- Module disable must be non-destructive: data retained, UI/endpoints gated off.
- Migrations reversible where feasible; destructive changes require explicit approval.
- Draft (unposted) invoices can be discarded; posted invoices are reversed only via the
  Accounting module's credit-note flow, never by deleting ledger data.
- A failed schedule run leaves the system in a consistent, resumable state.

## CEO/CTO Summary

This module adds recurring-lease and subscription billing to an existing modular ERP
without forking its core. It turns today's spreadsheet-and-manual-invoice process into a
governed, auditable lifecycle — correct prorated billing, tracked asset states, and
role-gated financial approvals — while respecting strict module boundaries so the core
platform stays upgradeable. It is designed to run on SQL Server alone, degrade gracefully,
and be enabled or disabled per environment with no destructive impact.
