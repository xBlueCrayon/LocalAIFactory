# Scenario: Meridian Advisory Group — Client Relationship & Case Platform

> **Fictional scenario for capability simulation.** Meridian Advisory Group is an invented
> professional-services firm. This document describes a CRM / sales-pipeline / case-management
> platform *inspired by* the broad category of relationship-management systems. It does not clone,
> reproduce, or claim compatibility, equivalence, or certification with any commercial product.

## Business Problem

Meridian Advisory Group is a mid-sized management-consulting and managed-services firm (roughly 420
employees across four offices). Its commercial motion spans three loosely connected worlds:

1. **Business development** — partners and account directors chase new logos and expansion work.
2. **Delivery** — engagement managers run active client work and field client requests.
3. **Client support** — a service desk triages issues, change requests, and renewal questions.

Today these worlds live in disconnected tools: a spreadsheet-based pipeline, a shared mailbox for
client issues, and a separate billing system. The result is predictable: leads go cold because no
one owns follow-up, the same account is contacted by two partners unaware of each other, support
cases are lost in email threads, and leadership cannot answer simple questions like "what is our
weighted pipeline this quarter?" or "which accounts have open high-severity cases *and* an open
renewal opportunity?" without a manual data-gathering exercise that takes days.

The firm needs a single relationship-of-record system that ties **accounts, contacts, leads,
opportunities, cases, and activities** together, enforces ownership and access rules, and produces
trustworthy reporting.

## Current-State Process

- Sales pipeline tracked in a shared spreadsheet; stages are inconsistent between partners.
- Inbound leads arrive by web form into a generic inbox and are manually copy-pasted.
- Client issues ("cases") are handled in a shared mailbox with colour-coded flags.
- No common account record: the same client may appear three times with different spellings.
- Activity history (calls, meetings, emails) is tribal knowledge, not recorded.
- Reporting is a monthly hand-built deck; numbers are frequently disputed.
- Access control is "trust everyone with the spreadsheet"; no audit of who changed what.

## Target-State Process

- Every external organization is a single **Account** with deduplicated identity.
- Inbound interest becomes a **Lead**, qualified and converted into a **Contact + Opportunity**.
- Opportunities move through a defined, firm-wide **stage pipeline** with weighted forecast values.
- Post-sale client issues become **Cases** with severity, SLA target, and an owner.
- Every meaningful touch (call, meeting, email, note) is a logged **Activity** linked to the record.
- Ownership is explicit; reassignment is auditable.
- Leadership sees live dashboards instead of a disputed monthly deck.

## Users and Roles

| Role | Responsibilities | Typical access |
|------|------------------|----------------|
| **Sales Representative** | Owns leads and opportunities they create | Read/write own records; read team records |
| **Engagement Manager** | Runs delivery; owns cases for their accounts | Read/write cases + activities on assigned accounts |
| **Service Desk Agent** | Triages and resolves cases | Read/write cases queue; read account/contact |
| **Account Director** | Owns the overall account relationship | Read/write all records on owned accounts |
| **Sales Operations** | Maintains pipeline hygiene, stages, reporting | Read all; write pipeline configuration |
| **Administrator** | User, role, and security configuration | Full administrative access |
| **Executive (read-only)** | Consumes dashboards and forecasts | Read-only across all data |

## Data Entities

The core relational model (all persisted in MSSQL via EF Core):

- **Account** — an external organization. Fields: legal name, trading name, industry, tier, region,
  primary Account Director (owner), status (Prospect / Active / Dormant), dedup key.
- **Contact** — a person at an Account. Fields: name, title, email, phone, account reference,
  marketing-consent flag, primary-contact flag. Many contacts per account.
- **Lead** — unqualified inbound interest. Fields: source, raw company name, raw contact name,
  status (New / Working / Qualified / Disqualified), owner. Converts into Contact + Opportunity.
- **Opportunity** — a potential deal. Fields: account reference, name, stage, estimated value,
  probability (%), weighted value (derived), expected close date, owner, won/lost reason.
- **Case** — a post-sale client issue or request. Fields: account reference, contact reference,
  title, description, severity (1–4), status (Open / Pending / Resolved / Closed), SLA target,
  owner, resolution notes.
- **Activity** — a logged interaction. Fields: type (Call / Meeting / Email / Note / Task),
  subject, due/occurred timestamp, related entity (polymorphic: Account / Contact / Lead /
  Opportunity / Case), owner, completed flag.

Relationships: Account 1—* Contact; Account 1—* Opportunity; Account 1—* Case; Lead converts to
Contact + Opportunity; Activity *—1 owner and *—1 related entity.

## Integrations

All integrations are **optional and degrade gracefully** — the platform is fully usable with MSSQL
alone, consistent with the host project's local-first constraints.

- **Inbound web-form lead capture** — a posted form creates a Lead (idempotent on submission token).
- **Outbound email notification** — case assignment / SLA-breach alerts via an SMTP relay (queued,
  retried, never blocks the request path).
- **Billing system reference** — read-only account number lookup; absence does not block records.
- **Optional embedding/RAG enrichment** — case and account notes may be indexed for retrieval when
  a vector store is present; the system functions identically without it.

## Security and Audit Controls

- **Authentication** via the host platform's existing identity mechanism.
- **Role-based authorization** enforced server-side in controllers and services — never only in the UI.
- **Record-level ownership checks**: a Sales Rep cannot edit an Opportunity they do not own unless
  they hold an elevating role. Guard against insecure direct object reference (IDOR).
- **Append-only audit log** for create/update/delete/reassign on every core entity: who, what, when,
  before/after for sensitive fields.
- **Deny-by-default**: a request with no satisfied authorization rule is rejected.
- **Marketing-consent flag** on contacts is honoured before any outbound communication.

## Reporting Requirements

- **Weighted pipeline** by stage, owner, and region (sum of value × probability).
- **Win/loss rate** over a rolling period with loss-reason breakdown.
- **Open-case aging** by severity with SLA-breach count.
- **Account 360** — single account view showing contacts, open opportunities, open cases, recent
  activities, on one page that loads quickly.
- **Cross-cut alert**: accounts with both an open severity-1/2 case and an open renewal opportunity.
- All report queries must use lightweight projections and avoid materializing large text columns.

## Failure Modes

- **Duplicate accounts** from inconsistent inbound naming — mitigated by a dedup key + merge tool.
- **Orphaned activities** if a parent record is deleted — mitigated by soft-delete + cascade rules.
- **Lead conversion race** — two reps converting the same lead; mitigated by optimistic concurrency.
- **SLA timer drift** if the notification relay is down — queue persists; alerts catch up.
- **Report timeout** from naive aggregation — mitigated by indexed projections, separate counts.
- **Privilege creep** — periodic access review surfaced from the audit log.

## Acceptance Criteria

(See `acceptance-criteria.md` for the measurable checklist.) At a high level: all six entities are
CRUD-able with server-side authorization; lead conversion produces a linked Contact + Opportunity;
weighted pipeline and account-360 reports return correct numbers under one second on test data; every
mutating action writes an audit entry; and the system runs correctly with MSSQL only.

## Expected Architecture (ASP.NET Core MVC + MSSQL + EF Core)

- **ASP.NET Core MVC** controllers per aggregate (Accounts, Contacts, Leads, Opportunities, Cases,
  Activities) plus a Dashboard/Reports controller.
- **EF Core** `DbContext` with the six entities, a polymorphic Activity link, optimistic-concurrency
  row version on Lead and Opportunity, and migrations checked into source control.
- **Service layer** holding authorization + ownership checks and the lead-conversion transaction.
- **Append-only `AuditEntry`** table written by an interceptor or explicit service call.
- **Lightweight read models** (records) for list and report views — no large-text materialization.
- **Background queue** for outbound notifications, modelled on the host's hosted-service pattern.
- Local-first: no required external service on the request path; health probes cached.

## Expected Tests

- Unit tests for weighted-value derivation, SLA-target calculation, and dedup-key generation.
- Authorization tests: each role × each action, asserting allow/deny including IDOR attempts.
- Lead-conversion integration test: produces exactly one Contact and one Opportunity, links them,
  and is idempotent under a duplicate submission token.
- Concurrency test: simultaneous opportunity edits — one succeeds, one gets a concurrency error.
- Reporting tests: weighted pipeline and account-360 return expected figures on a seeded fixture.
- Audit tests: every mutating action appends exactly one immutable audit row.

## Expected Deployment Concerns

- Database migrations applied on startup (additive, backward-compatible).
- Connection strings and SMTP credentials supplied via environment / git-ignored override.
- Indexes on foreign keys, opportunity stage, case status/severity, and audit timestamp.
- Notification queue must survive a restart (persisted, not in-memory only).
- Capacity: index-backed reporting validated against a representative data volume.

## Rollback Considerations

- Schema changes are additive so a rollback to the prior app version remains compatible.
- Lead conversion runs in a single transaction — partial conversions cannot persist.
- Outbound notifications are queued and idempotent, so re-processing after rollback is safe.
- The audit log is append-only and never rewritten, so it remains valid across versions.
- A feature flag gates the merge/dedup tool so it can be disabled without redeploy.

## CEO/CTO Summary

Meridian replaces three disconnected tools — a pipeline spreadsheet, a support mailbox, and tribal
account knowledge — with one relationship-of-record platform. The CEO gets a trustworthy, live view
of pipeline and client health instead of a disputed monthly deck. The CTO gets a local-first
ASP.NET Core + MSSQL system with server-side security, an append-only audit trail, and graceful
degradation when optional services are absent. The measurable win: ownership is explicit and
auditable, no lead or case falls through the cracks, and leadership can answer cross-cutting
questions (e.g. "which key accounts are simultaneously at renewal and in trouble?") in seconds.
