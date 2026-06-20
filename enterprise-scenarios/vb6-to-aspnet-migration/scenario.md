# Scenario: Modernizing "ClaimDesk" — A Legacy VB6 Claims-Processing Application

> Synthetic enterprise scenario for the LocalAIFactory enterprise capability simulation suite.
> Fictional company, fictional system. No vendor product is cloned and no certification is implied.

**Company:** Meridian Mutual Indemnity (fictional), a mid-size regional insurer.
**System under study:** *ClaimDesk*, an internal Visual Basic 6.0 desktop application used by adjusters and clerks to register, triage, and settle property-damage claims.

---

## Business Problem

ClaimDesk was written in 1999 and last meaningfully changed in 2011. It is a thick-client VB6
application installed on ~140 Windows desktops across three branch offices. It is business-critical:
every property claim the company pays passes through it.

The pain is now acute and compounding:

- The original developers have left. No one fully understands the `modBusinessRules.bas` module that
  computes reserve amounts and settlement caps.
- It depends on a 32-bit Microsoft Access JET / DAO data layer for local caches and an aging ADO
  connection to a shared MSSQL 2008 database. The 32-bit OCX controls (a third-party grid and a date
  picker) no longer have supported vendors.
- Crystal Reports 8.5 generates the regulator-facing loss-run and settlement reports. The runtime is
  unsupported and breaks on newer Windows builds.
- Deployment is "copy the EXE to a share and hope." There is no build server, no source of truth, and
  two slightly different EXEs are in production right now.
- It cannot be used remotely. During the last office closure, claims processing stopped for four days.

Leadership wants a **web application** that any authorized employee can use from a managed browser,
with a maintainable codebase, real reporting, and an auditable settlement trail — without a risky
"big bang" rewrite that freezes the business for a year.

---

## Current-State Process

1. A clerk receives a First Notice of Loss (FNOL) by phone or fax and keys it into the ClaimDesk
   "New Claim" form (`frmNewClaim`).
2. The form writes to a local Access cache via DAO, then synchronizes to the shared MSSQL database via
   ADO when the clerk clicks **Commit**. Sync conflicts are resolved "last write wins," silently.
3. An adjuster opens the claim, inspects damage, and enters a reserve estimate. `modBusinessRules.bas`
   applies opaque caps based on policy type and branch.
4. A supervisor approves settlements over a threshold by typing initials into a text box (no real
   authentication of the approver).
5. At month end, a clerk runs four Crystal Reports for the state regulator and emails PDFs.

The process is undocumented, the rules live only in code, and there is no reliable audit of who
changed what.

---

## Target-State Process

1. FNOL is entered in a **browser form** served by an ASP.NET Core MVC controller; data is written
   directly to MSSQL through EF Core in a single transaction — no local Access cache, no silent
   "last write wins."
2. Reserve and settlement rules are **extracted into an explicit, versioned rules service** with unit
   tests, so the logic is readable and changeable.
3. Settlement approvals require **authenticated, role-checked** sign-off, recorded in an append-only
   audit log (who, what, when, previous/new value).
4. Reporting is regenerated as **server-side reports** (the loss-run and settlement summaries),
   parameterized and exported to PDF/CSV without the Crystal runtime.
5. The application is reachable from managed browsers in all branches; remote work no longer halts
   claims processing.

---

## Users and Roles

- **Claims Clerk** — registers FNOL, edits draft claims, runs standard reports.
- **Adjuster** — owns a claim, sets reserves, proposes settlements.
- **Supervisor** — approves settlements above threshold, reassigns claims.
- **Compliance Officer** — read-only across all claims, exports regulator reports, reads audit trail.
- **System Administrator** — manages users/roles; cannot edit claim financials (separation of duties).

---

## Data Entities

- **Claim** — claim number, policy number, FNOL date, status, branch, owning adjuster.
- **Policy** — policy number, holder, coverage type, coverage limits.
- **Claimant / PolicyHolder** — name, contact, relationship to policy.
- **ReserveEntry** — claim, amount, currency, set-by, set-at, reason.
- **Settlement** — claim, proposed amount, approved amount, approver, approval timestamp, status.
- **Document** — scanned FNOL, photos, correspondence (metadata + blob/file reference).
- **AuditEvent** — actor, action, entity, entity id, before/after, timestamp (append-only).
- **LookupTables** — branch, coverage type, settlement-cap matrix (currently buried in VB6 code).

---

## Integrations

- **Shared MSSQL database** (currently 2008; target a supported MSSQL version).
- **Regulator reporting handoff** — today, emailed Crystal PDFs; target, generated reports exported
  to PDF/CSV and dropped to a monitored folder or portal.
- **Document store** — scanned images currently on a file share; target, a referenced blob/file store
  with metadata in MSSQL.
- **Active Directory / Windows accounts** — for authenticating users (the current app has none).

There are no external payment or third-party-API integrations in scope for the first cut.

---

## Security and Audit Controls

- **Authentication** for every user (Windows/AD-backed), replacing the "type your initials" pattern.
- **Role-based authorization** enforced server-side on every controller action (deny-by-default).
- **Separation of duties:** administrators cannot alter claim financials; approvers cannot approve
  their own proposed settlements.
- **Append-only audit** of all financial and status changes, including the approver identity for
  every settlement.
- **No secrets in source.** Connection strings move to environment/config outside the repo.
- **Input validation and parameterized queries** throughout (the ADO layer used string-concatenated
  SQL in places — an injection risk to remove during migration).

---

## Reporting Requirements

- **Monthly Loss Run** — all open/closed claims with reserves and paid amounts for a period and branch.
- **Settlement Summary** — settlements by approver, with approval timestamps, for compliance review.
- **Open Reserves** — current reserve exposure by branch and coverage type.
- **Audit Extract** — filtered audit events for a date range (compliance/regulator on request).

Each report must be parameterized, reproducible, and exportable to PDF and CSV. Numbers must match the
legacy Crystal Reports for the same inputs (this is a behavioural-parity requirement, not just visual).

---

## Failure Modes

- **Hidden business rules:** the settlement-cap matrix and reserve logic exist only in
  `modBusinessRules.bas`; a naive rewrite risks changing payouts.
- **Silent data loss:** "last write wins" sync between Access and MSSQL has already merged conflicting
  edits without warning.
- **Encoding/locale drift:** legacy data may contain mixed code pages; money and dates can be
  misinterpreted on import.
- **Report divergence:** regenerated reports could subtly differ from Crystal output and fail
  regulator review.
- **Authorization gaps:** porting "initials" approvals naively would reproduce the lack of real
  approver identity.
- **Big-bang risk:** a full cutover freezes claims processing — unacceptable to the business.

---

## Acceptance Criteria

(Measurable list maintained in `acceptance-criteria.md`; summary here.)

- A documented, versioned statement of the reserve and settlement-cap rules exists and is reviewed by
  Compliance before any code replaces them.
- New web FNOL, reserve, and settlement flows produce **identical financial outcomes** to ClaimDesk
  for an agreed golden-master set of historical claims.
- Every settlement approval is authenticated, role-checked, and audited.
- Each regulator report matches legacy output for the same parameters within agreed tolerances.
- Migration is incremental (strangler-fig): the business never experiences a multi-day freeze.

---

## Expected Architecture (Strangler-Fig Migration to ASP.NET Core MVC + EF Core + MSSQL)

The migration follows the **strangler-fig** pattern: stand up the new system around the old one and
move capabilities slice by slice until the legacy app can be retired.

1. **Characterize before changing.** Treat ClaimDesk as a black box. Capture inputs/outputs for the
   reserve and settlement flows from real historical data to build a **golden master** (see Expected
   Tests). The rules are recovered as explicit specifications reviewed by Compliance — not transpiled.

2. **Establish the target shell.** ASP.NET Core MVC for the UI, EF Core over the existing MSSQL schema
   (introspect the current tables; add new tables only additively), authentication via Windows/AD, and
   deny-by-default role authorization. Reporting becomes server-side report generation, not Crystal.

3. **Shared database as the seam.** Both ClaimDesk and the new app point at the **same MSSQL database**
   during transition. New code reads/writes the same tables, so a claim created in either system is
   visible in both. This is what makes incremental cutover possible without data migration up front.

4. **Slice migration order (lowest risk first):**
   - *Read-only views* (claim search, report viewing) in the web app first — no write risk.
   - *Reporting* moved to server-side generation and validated against Crystal output.
   - *FNOL entry* (new claims) routed to the web app; clerks stop using `frmNewClaim`.
   - *Reserve and settlement* flows last, only after golden-master parity is proven, because they are
     financial and rule-bearing.

5. **Retire the legacy app** once every slice is migrated and the JET/DAO/Access cache and Crystal
   runtime are no longer referenced by any active workflow.

**Honest constraint:** LocalAIFactory cannot parse VB6/VB.NET source today (see
`expected-capabilities.md`). The architecture above is recovered by reading code manually and by
characterizing behaviour from data — the platform contributes the *playbook, risk assessment, target
design, and test strategy*, not an automatic VB6 transpiler.

---

## Expected Tests (Behavioural Parity, Golden-Master)

- **Golden-master tests:** for a curated set of historical claims, record the legacy reserve and
  settlement outputs, then assert the new rules service produces the same outputs for the same inputs.
- **Rules unit tests:** each recovered cap/threshold rule gets explicit unit tests with boundary cases
  (at, just below, just above each cap).
- **Report parity tests:** run each regulator report for fixed parameters in both systems and compare
  totals and row counts within agreed tolerances.
- **Authorization tests:** every role/action pair is asserted (allowed vs. denied), including
  self-approval denial and admin-cannot-edit-financials.
- **Data integrity tests:** concurrent edits are handled with explicit conflict detection, not silent
  last-write-wins.

---

## Expected Deployment Concerns

- Run the new ASP.NET Core app on IIS (or Kestrel behind IIS) in the existing Windows estate.
- Both old and new apps share one MSSQL instance during transition; schema changes must be **additive
  and backward-compatible** so the VB6 app keeps working until retired.
- No GPU, no internet dependency; the system must function inside the bank's closed network.
- Connection strings and secrets supplied via environment/config, never committed.
- Per-branch rollout with a pilot office before estate-wide enablement.

---

## Rollback Considerations

- Because the database is shared, a problematic web slice can be **switched off** and clerks fall back
  to ClaimDesk for that capability — the data remains consistent.
- All schema changes are additive, so rolling back the web app never requires reversing a destructive
  migration.
- Feature flags gate each migrated slice (read views, reporting, FNOL, settlements) so any one can be
  disabled independently.
- A documented "fall back to VB6 for capability X" runbook exists for each slice until it is fully
  trusted.

---

## CEO/CTO Summary

ClaimDesk is a 25-year-old VB6 desktop app that the business cannot afford to lose and cannot safely
maintain. The plan is not a risky rewrite: we wrap the old system with a modern ASP.NET Core + MSSQL
web application and move one capability at a time, proving each new flow produces the **same financial
results** as the legacy app before switching it on. The hidden settlement rules get documented and
tested; approvals become authenticated and audited; reporting is rebuilt to match the regulator output;
and at every step we can fall back to the old app because both share one database. LocalAIFactory does
not auto-convert VB6 — it provides the migration playbook, the risk and parity test strategy, and the
target architecture so the team executes this with eyes open rather than guessing.
