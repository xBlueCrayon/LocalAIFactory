# Test Questions — Oracle PL/SQL Modernization Scenario

> Evaluation prompts for the capability simulation. Each question lists what a **strong answer must
> contain**. Inspired-by the PL/SQL-to-.NET/MSSQL domain; answers must avoid vendor text and any
> compatibility/equivalence/certification claims.

## Q1. Can the platform automatically extract the inventory of the NightLedger PL/SQL packages today?

A strong answer must:
- State clearly **no** — Oracle PL/SQL parsing is not yet supported (gap-only).
- Note that automated extraction today covers **MSSQL / T-SQL** only.
- Describe the interim path: manual/assisted cataloging plus gap-honest import reporting.

## Q2. How would you build a defensible, gap-honest inventory of ~610 legacy routines?

A strong answer must:
- Import the source as a project and rely on coverage + gap reporting (no silent blind spots).
- List name, package, type, LOC, owner, risk tier per item.
- Reconcile assessed + gapped = total, and report unparseable items with reasons.

## Q3. Map the proposed migration of a `PKG_SETTLE` proc from PL/SQL toward the target architecture.

A strong answer must:
- Split set-based data work (→ T-SQL stored procedures on MSSQL) from rule/orchestration logic
  (→ C#/EF Core service layer).
- Require a written contract, owner, and parity test for the migrated unit.
- Acknowledge that target T-SQL artifacts are extractable today; source PL/SQL is not.

## Q4. How are data types mapped from the legacy database to MSSQL, and what are the risks?

A strong answer must:
- Produce a per-type mapping with nullability and precision/scale notes.
- Flag money/interest precision and an explicit rounding rule.
- Identify numeric-drift risk from precision differences as a failure mode.

## Q5. How do you preserve transaction semantics during migration?

A strong answer must:
- Document commit/rollback behavior and isolation per migrated proc.
- Explicitly flag autonomous/implicit-commit behavior where present.
- Require an atomicity test: forced mid-cycle failure leaves no partial posting.

## Q6. What does "parity" mean here and how is it proven?

A strong answer must:
- Define parity as identical numeric output for identical fixture inputs within a declared tolerance.
- Require money parity to the cent and re-run reproducibility of ledger + exception set.
- Tie parity to a coverage report (% of in-scope procs with contract + passing test).

## Q7. A trigger silently updates history rows on every base-table write. How do you handle it?

A strong answer must:
- Treat trigger-driven side effects as in-scope behavior to reproduce in the service layer.
- Note that trigger side effects are a named failure mode if missed.
- Add dependency edges (trigger→table) and a parity test covering the derived history rows.

## Q8. Dynamic SQL hides a dependency. How is the blast radius controlled?

A strong answer must:
- Identify hidden dynamic-SQL dependencies as a top failure mode for cutover.
- Use dependency fan-in/fan-out and hub flagging; annotate manually-derived edges as such.
- Recommend dual-run comparison before promoting the affected slice.

## Q9. Describe a safe, phased cutover with rollback for one migrated slice.

A strong answer must:
- Use an independent feature flag per slice; no big-bang.
- Keep schema changes additive/backward-compatible during a dual-run window.
- Make the slice independently revertible, with promotion and rollback both audited.

## Q10. Who can do what, and how is it audited?

A strong answer must:
- Enumerate roles: Operator, Analyst, Engineer, Release Manager, Auditor with correct permissions.
- State deny-by-default, project-scoped access, and read-only Auditor.
- Require append-only audit of every assessment action and migration-status change.

## Q11. Can this run with only SQL Server present?

A strong answer must:
- Confirm local-first operation with no GPU, internet, Qdrant, or Ollama required.
- Note that optional services degrade gracefully and never block assessment.

## Q12. Does the platform certify the migration as equivalent to the legacy system?

A strong answer must:
- State plainly **no** — no compatibility, equivalence, or certification is claimed.
- Reframe assurance as evidence: parity tests, dual-run comparison, and audited approvals.
- Reiterate the source-PL/SQL extraction gap versus target-T-SQL support.
