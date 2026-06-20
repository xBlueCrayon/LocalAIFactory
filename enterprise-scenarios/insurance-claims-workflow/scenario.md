# Scenario: Zephyr Mutual Assurance — Claims Workflow Platform

> Fictional scenario for the LocalAIFactory enterprise capability simulation suite.
> Zephyr Mutual Assurance is an invented insurer used to reason about a solution design.
> This document is **not** legal or insurance advice and makes **no** compliance or
> certification claim. References to Mauritius / the Financial Services Commission (FSC)
> are **awareness-only** context, not an assertion of regulatory conformance.

## Business Problem

Zephyr Mutual Assurance, a mid-sized general insurer, runs its short-term claims on a mix
of spreadsheets, shared mailboxes, and a legacy desktop tool. Claims handlers re-key policy
data, assessor reports arrive as loose attachments, and settlement approvals happen over
email. The result is slow cycle times, inconsistent reserving, weak traceability, and an
audit trail that has to be reconstructed by hand. Leadership wants a single workflow platform
that moves a claim from first notification to settled payment with controlled hand-offs,
enforced separation of duties, and a complete record of who did what and when.

## Current-State Process

1. A claimant reports a loss by phone or email; a handler opens a spreadsheet row.
2. Policy details are copied manually from the policy admin export.
3. The handler emails an assessor; the assessor replies with a report and a quantum estimate.
4. A reserve figure is typed into the spreadsheet with no version history.
5. Settlement is approved by a reply-all email from a team lead.
6. Payment is requested by emailing finance a beneficiary name and amount.
7. Month-end reporting is a manual reconciliation of spreadsheets and the bank feed.

Pain points: duplicate data entry, no enforced approval thresholds, reserves drifting from
exposure, and no defensible audit trail.

## Target-State Process

1. **Intake (FNOL):** A claim is registered against a verified policy; the system snapshots
   policy cover, limits, and excess at the moment of registration.
2. **Triage:** The claim is validated, categorised, and routed by type and value band.
3. **Assessment:** An assessor records findings, a quantum estimate, and supporting evidence.
4. **Reserve:** A reserve is set and revised with a full change history and reason codes.
5. **Decision:** Approve, decline, or request more information against threshold-based authority.
6. **Settlement:** An approved payment instruction is raised, dual-controlled, and released.
7. **Closure:** The claim is closed, reserves released, and the file locked for audit.

Every transition is an explicit, logged state change with an actor, timestamp, and reason.

## Users and Roles

- **Claims Handler** — registers and progresses claims; cannot approve own settlements.
- **Assessor** — records assessments and quantum; cannot set reserves or approve payment.
- **Reserving Analyst** — sets and revises reserves; cannot approve settlement.
- **Claims Approver / Manager** — approves decisions and settlements within authority limits.
- **Finance / Payments Officer** — releases dual-controlled payment instructions.
- **Auditor (read-only)** — reads any claim, reserve history, and the audit log; changes nothing.
- **Administrator** — manages users, roles, and authority thresholds (no claim-data edits).

## Data Entities

- **Policy** — policy number, product, insured party, cover, sum insured, excess, status, period.
- **Claim** — reference, linked policy snapshot, loss date/type, status, assigned handler.
- **Claimant** — party making the claim; identity reference, contact, relationship to policy.
- **Assessment** — assessor, findings, quantum estimate, evidence references, recommendation.
- **Reserve** — current and historical reserve amounts, currency, reason code, set-by, set-at.
- **Payment** — beneficiary, amount, instruction status, raised-by, approved-by, released-by.
- **AuditEvent** — append-only record of every state change and authority decision.

Relationships: a Policy has many Claims; a Claim has one Claimant, many Assessments, a Reserve
history, and zero-or-more Payments; every entity links to AuditEvents.

## Integrations

- **Policy administration system** — read-only lookup to register claims against real policies
  and snapshot cover. Designed to degrade to manual entry if the feed is unavailable.
- **Payments / banking file export** — produces a payment instruction file for finance; the
  platform never moves money itself.
- **Document store** — assessment evidence and correspondence held by reference, not inline.
- **Identity / directory** — role assignment sourced from the corporate directory where present.

All integrations are optional at the boundary: the platform remains usable with MSSQL alone and
manual data entry, consistent with the local-first constraint.

## Security and Audit Controls

- **Separation of Duties (SoD):** the actor who raises a settlement cannot approve it; the
  approver cannot release the payment. Authority thresholds gate approval by claim value band.
- **KYC/AML awareness:** claimant identity is captured by reference and beneficiary changes are
  flagged for review. This is operational awareness only — **not** a sanctions-screening or
  AML-compliance product, and no regulatory claim is made.
- **Audit:** an append-only `AuditEvent` log captures actor, role, action, before/after state,
  timestamp, and reason. Audit records are never edited or deleted in normal operation.
- **Access control:** role-based; least privilege; the Auditor role is strictly read-only.
- **FSC / Mauritius awareness:** the design notes that an insurer in this market operates under
  a regulator (FSC). This is contextual awareness only; the platform asserts no conformance.

## Reporting Requirements

- Open claims by status, age band, and handler.
- Reserve movement (opened, increased, decreased, released) over a period.
- Settlement throughput and average cycle time from FNOL to payment.
- Exceptions: claims breaching SLA, reserves over threshold, declined-then-reopened claims.
- An audit extract for any claim showing the full ordered event history.

## Failure Modes

- **Policy feed down** — fall back to manual policy entry; flag the claim as unverified.
- **Duplicate FNOL** — detect likely duplicates by policy + loss date before registration.
- **Reserve/exposure drift** — surface claims where reserve diverges sharply from quantum.
- **Approval bypass attempt** — block and log any attempt to self-approve or exceed authority.
- **Payment file rejected by finance** — keep the instruction in a recoverable failed state.
- **Concurrent edits** — optimistic concurrency to prevent silent overwrite of reserves.

## Acceptance Criteria

See `acceptance-criteria.md` for the measurable checklist. In summary, a claim can be taken
end to end through every state with SoD enforced, reserves fully versioned, settlements
dual-controlled, and a complete append-only audit trail produced on demand.

## Expected Architecture

- **ASP.NET Core MVC** UI with controllers per aggregate (Claims, Assessments, Reserves,
  Payments) and Razor views; no blocking external calls on the request path.
- **MSSQL + EF Core** as the system of record; migrations are additive and backward-compatible.
- **Approval / exception handling:** state transitions run through a guarded service that checks
  role, authority threshold, and SoD before committing, raising a typed exception on violation.
- Reserve changes are append-only history rows, not in-place updates.
- AuditEvent writes occur in the same transaction as the state change they describe.
- Integrations sit behind interfaces with no-op / manual fallbacks so MSSQL-only mode works.

## Expected Tests

- Unit tests for the authority-and-SoD guard (approve-own-settlement is rejected).
- Reserve history tests proving every change is versioned with a reason code.
- State-machine tests for legal vs illegal claim transitions.
- Audit tests proving an event is written for each transition, in the same transaction.
- Concurrency test proving optimistic concurrency blocks a stale reserve overwrite.
- Fallback test proving claim registration works with the policy feed unavailable.

## Expected Deployment Concerns

- Runs on IIS with MSSQL; migrates and seeds on startup.
- No dependency on internet, GPU, or external AI services to render core pages.
- Authority thresholds and role mappings are configuration, changeable without redeploy.
- Backups must include the append-only audit table; restore must preserve event ordering.

## Rollback Considerations

- All schema changes are additive, so a rollback to the prior build leaves data readable.
- A failed payment-file run is recoverable and re-runnable without double payment.
- Because reserves are append-only, a bad reserve entry is corrected by a new compensating
  entry with a reason code, never by deleting history.
- Feature-flag new transition rules so they can be disabled without a migration.

## CEO/CTO Summary

Zephyr Mutual replaces a fragile spreadsheet-and-email claims process with a single workflow
platform that enforces separation of duties, versions every reserve, dual-controls every
payment, and produces a defensible audit trail on demand. It runs locally on MSSQL, degrades
gracefully when upstream systems are offline, and treats regulatory context (FSC/Mauritius) as
awareness only. The outcome is faster, more controlled, more traceable claims handling — built
to be evolved, not a black box.
