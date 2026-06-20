# Acceptance Criteria — Oracle PL/SQL Modernization Scenario

> Measurable checklist. Each item is pass/fail. Inspired-by the PL/SQL-to-.NET/MSSQL domain; no
> vendor text, no compatibility/equivalence/certification claims.

## A. Honesty and scope

- [ ] Output explicitly states Oracle PL/SQL parsing is **not yet supported** (gap-only).
- [ ] Output explicitly states automated extraction currently covers **MSSQL / T-SQL only**.
- [ ] No claim of compatibility, equivalence, or certification with any commercial product appears.
- [ ] The source-vs-target extraction boundary is stated in any assessment summary.

## B. Inventory (gap-honest)

- [ ] An inventory exists listing every legacy proc/function/trigger discovered in the corpus.
- [ ] Each inventory item records: name, package, type, LOC, owner (or "unknown"), risk tier.
- [ ] Items that could not be assessed are listed in a **gap report** with a stated reason.
- [ ] Inventory totals reconcile: assessed + gapped = total discovered (no missing remainder).

## C. Dependency mapping

- [ ] A dependency map links caller → callee (proc→proc, proc→table, trigger→table) where derivable.
- [ ] Fan-in / fan-out is reported per proc; high-blast-radius hubs are flagged.
- [ ] Every inventoried proc has at least one mapped edge or an explicit "leaf" annotation.
- [ ] For source PL/SQL, edges derived by manual/assisted means are labeled as such (not as automated).

## D. Data-type mapping

- [ ] Every distinct legacy data type in scope has a proposed MSSQL mapping.
- [ ] Each mapping records nullability and precision/scale notes.
- [ ] Money/interest types declare an explicit precision and rounding rule.

## E. Transaction semantics

- [ ] Each migrated proc documents commit/rollback behavior and isolation.
- [ ] Autonomous/implicit-commit behaviors are flagged explicitly where present.
- [ ] A forced mid-cycle failure test shows no partial posting (atomicity preserved).

## F. Parity and tests

- [ ] Every migrated rule has at least one parity test with fixture inputs and expected output.
- [ ] Numeric parity holds within the declared tolerance (money to the cent).
- [ ] Re-running a settled day reproduces the same ledger entries and exception set.
- [ ] Coverage report shows % of in-scope procs with a contract and a passing parity test.

## G. Security, audit, and access

- [ ] RBAC enforced: Operator, Analyst, Engineer, Release Manager, Auditor roles behave per scenario.
- [ ] Auditor access is read-only across inventory, dependency map, and history.
- [ ] No production credentials or customer PII present in the assessment corpus.
- [ ] Every assessment action and migration-status change is in the append-only audit trail.

## H. Deployment and rollback

- [ ] Each migrated slice is behind an independent feature flag.
- [ ] Schema changes are additive and backward-compatible during dual-run.
- [ ] A documented dual-run + comparison window exists before any slice is promoted.
- [ ] Each slice is independently revertible; promotion and rollback are both audited.

## I. Local-first

- [ ] The scenario is operable with only SQL Server present (no GPU, internet, Qdrant, or Ollama).
