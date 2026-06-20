# Test Questions — VB6-to-ASP.NET Migration Scenario

Evaluation questions for the ClaimDesk modernization scenario. Each question lists what a **strong
answer must contain**. Questions that probe platform limits expect an honest "no."

---

### Q1. Can LocalAIFactory automatically migrate the ClaimDesk VB6 application to ASP.NET Core?

**Strong answer must contain:** a clear **no**. The platform has no VB6/VB.NET parser; it does not read
`.frm`/`.bas`/`.cls`/`.vbp` or Crystal `.rpt`, cannot transpile VB6/ADO/DAO to C#/EF Core, and cannot
auto-recover the rules. Its value is the migration playbook, risk assessment, target design, and test
strategy — not code transformation.

### Q2. What migration pattern should we use, and why not a big-bang rewrite?

**Strong answer must contain:** the **strangler-fig** pattern; incremental capability-by-capability
replacement; the shared MSSQL database as the seam enabling old/new coexistence; and an explicit
rejection of big-bang because it would freeze a business-critical claims process.

### Q3. In what order should capabilities be migrated?

**Strong answer must contain:** lowest-risk-first sequencing — read-only views and reporting first,
then FNOL/new-claim entry, then reserve and settlement (financial, rule-bearing) flows last, only after
golden-master parity is proven.

### Q4. The settlement and reserve rules live only in `modBusinessRules.bas`. How do we handle that?

**Strong answer must contain:** treat the module as a **black box**; recover its behaviour by
characterizing real inputs/outputs (golden master) rather than blind transpilation; document and
version the recovered rules; have **Compliance review** them before any code replaces them; test
boundary cases around each cap.

### Q5. How do we guarantee the new system pays the same amounts as the old one?

**Strong answer must contain:** **golden-master / behavioural-parity** testing against historical
claims; rules unit tests with at/just-below/just-above boundary cases; an honest caveat that parity is
demonstrated against the test set, **not guaranteed absolutely**.

### Q6. The current app approves settlements by typing initials. What should the target do?

**Strong answer must contain:** real **authentication** (Windows/AD), **role-based authorization**
enforced server-side and deny-by-default, **separation of duties** (no self-approval; admins cannot
edit financials), and an **append-only audit** capturing approver identity and before/after values.

### Q7. How do we replace the Crystal Reports loss-run and settlement reports?

**Strong answer must contain:** **server-side report generation** (parameterized, PDF/CSV), and
**report-parity** validation comparing totals and row counts against legacy Crystal output for fixed
parameters — because the regulator-facing numbers must match.

### Q8. What schema changes are acceptable while both apps run together?

**Strong answer must contain:** **additive, backward-compatible** changes only, so the VB6 app keeps
working; new tables (e.g., explicit audit, rules/cap matrix) added without breaking existing columns;
no destructive migrations during coexistence.

### Q9. The legacy sync uses "last write wins" between Access and MSSQL. What's the risk and fix?

**Strong answer must contain:** identification of **silent data loss** from last-write-wins; removal of
the Access/JET cache; single-transaction writes directly to MSSQL via EF Core; and **conflict-aware**
concurrency handling (e.g., optimistic concurrency) instead of silent overwrite.

### Q10. What are the top failure modes for this migration?

**Strong answer must contain:** hidden business rules; silent last-write-wins data loss;
encoding/locale drift on money/dates; report divergence from Crystal; authorization gaps if "initials"
approvals are ported naively; and big-bang freeze risk — each with a mitigation.

### Q11. If a migrated slice misbehaves in production, how do we roll back?

**Strong answer must contain:** **feature flags** per slice; because the database is shared and schema
changes are additive, disable the web slice and **fall back to ClaimDesk** for that capability with no
data divergence; a documented fall-back runbook; pilot-branch rollout before estate-wide enablement.

### Q12. What can LocalAIFactory concretely contribute to this project right now?

**Strong answer must contain:** curated, approvable **knowledge** — the strangler-fig playbook, the
risk catalogue with mitigations, the reasoned target architecture, and the parity test strategy —
injected first into prompts; plus an explicit restatement that it does **not** parse VB6 or auto-migrate,
so humans still read the source and extract golden-master data.
