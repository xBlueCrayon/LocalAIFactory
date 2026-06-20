# Scenario: Back-Office PL/SQL Modernization at Meridian Trust Bank

> Original synthetic scenario for the LocalAIFactory enterprise capability simulation suite.
> Inspired by the general domain of Oracle Database / PL/SQL legacy modernization toward
> .NET + MSSQL. This is a fictional institution. No vendor product is cloned, no manual is
> reproduced, and no claim of compatibility, equivalence, or certification with any commercial
> database or tooling is made or implied.

## Business Problem

Meridian Trust Bank runs its overnight back-office settlement and reconciliation on a 19-year-old
PL/SQL-centric platform called **NightLedger**. Business logic lives almost entirely inside the
database: 47 packages, ~610 stored procedures and functions, 130+ triggers, and a forest of
scheduled jobs. The original authors have retired. Each fiscal close requires "tribal knowledge"
that exists in three people's heads. Audit has flagged the platform as a key-person-risk and a
change-control risk: nobody can confidently say what a given proc touches before it runs.

The bank wants to (a) produce a defensible inventory and dependency map of the current logic, and
(b) migrate, in phased slices, toward a .NET service layer over MSSQL where business rules are
testable, versioned, and reviewable — without a risky big-bang cutover.

## Current-State Process

1. A nightly scheduler fires `PKG_SETTLE.RUN_CYCLE` at 22:30.
2. The package walks pending transactions, applies fee and interest rules, posts ledger entries,
   and flags exceptions into `BO_EXCEPTION_QUEUE`.
3. Reconciliation procedures compare internal balances against external clearing files.
4. Triggers cascade derived updates and write history rows on nearly every base table.
5. Operators watch a green-screen monitor; failures are diagnosed by reading raw proc source.

The logic is opaque: control flow spans packages, dynamic SQL is common, and transaction
boundaries are implicit. There is no automated test suite — correctness is "it matched yesterday".

## Target-State Process

- Business rules extracted into a documented, versioned **.NET service layer** (C#/EF Core over MSSQL).
- Heavy set-based data work remains as **T-SQL stored procedures** in MSSQL where appropriate, but
  each proc has a written contract, owner, and parity test.
- Orchestration (the nightly cycle) moves to an application-controlled workflow with explicit,
  observable transaction boundaries and structured logging.
- Every migrated rule carries a parity test proving it reproduces the legacy numeric result.

## Users and Roles

- **Back-Office Operator** — runs/monitors the nightly cycle, triages exceptions. Read + run.
- **Reconciliation Analyst** — investigates breaks, annotates exceptions. Read + comment.
- **Migration Engineer** — assesses legacy logic, authors target services/procs and tests. Read/write.
- **Release Manager** — approves promotion of a migrated slice. Approval authority.
- **Auditor** — read-only across inventory, dependency map, and change history. No write.

## Data Entities

- **StoredProcInventoryItem** — name, package, type (proc/func/trigger), LOC, last-modified, owner,
  risk tier, migration status.
- **DependencyEdge** — caller → callee (proc→proc, proc→table, trigger→table), edge kind.
- **DataTypeMapping** — source legacy type → proposed MSSQL type → nullability/precision notes.
- **TransactionScope record** — proc → commit/rollback semantics, isolation, autonomous flag.
- **ParityTestCase** — fixture inputs, expected legacy output, tolerance, status.
- **LedgerEntry / Transaction / ExceptionQueueItem** — the business data the logic operates on.

## Integrations

- External clearing-house settlement files (fixed-width and delimited) ingested nightly.
- A general-ledger system consuming posted entries via a batch export.
- An internal message bus notifying downstream of completed cycles (future target state).
- LocalAIFactory ingestion: the migration team imports the extracted source as a project for
  assessment, dependency mapping, and knowledge capture.

## Security and Audit Controls

- Role-based access (above); deny-by-default; project-scoped visibility.
- Append-only audit of every assessment action and every migration-status change.
- No production credentials or customer PII enter the assessment corpus; only DDL/source and
  synthetic fixtures are imported.
- Promotion of any migrated slice requires Release Manager approval (recorded, immutable).

## Reporting Requirements

- **Inventory report**: count by package, type, risk tier, and migration status.
- **Dependency report**: fan-in/fan-out per proc; identify high-blast-radius hubs.
- **Coverage report**: % of procs with a written contract and a passing parity test.
- **Gap report**: items that could not be parsed/assessed, with the reason (no silent blind spots).
- **Data-type mapping report**: every distinct legacy type with its proposed MSSQL mapping.

## Failure Modes

- Hidden dependency via dynamic SQL missed during mapping → broken cutover.
- Transaction-semantics mismatch (e.g., autonomous commit) silently changes rollback behavior.
- Numeric drift from precision/rounding differences between source and target types.
- Trigger-driven side effects not reproduced in the service layer.
- Incomplete inventory: a proc nobody knew about runs in production.

## Acceptance Criteria

See `acceptance-criteria.md` for the measurable checklist. In summary: a complete, gap-honest
inventory; a dependency map with identified hubs; a data-type mapping table; documented transaction
semantics per migrated proc; and parity tests for every migrated rule.

## Expected Architecture

Assessment-first, then phased modernization:

1. **Assess** the legacy source: inventory procs/functions/triggers, build the dependency graph,
   catalog data types and transaction boundaries.
2. **Map** legacy PL/SQL constructs to target equivalents: set-based logic → T-SQL stored procedures
   on MSSQL; rule/orchestration logic → a C#/EF Core service layer.
3. **Migrate** in risk-ordered slices, each with a parity test, behind Release Manager approval.

**Honest capability note (critical):** LocalAIFactory's current code extractor parses **MSSQL /
T-SQL** today. It does **NOT** parse Oracle PL/SQL. Oracle PL/SQL parsing is a **future extractor**
and is out of scope for current automated extraction. For this scenario, automated symbol/dependency
extraction applies to the **target-state T-SQL** artifacts; assessment of the **source PL/SQL** is
treated as a documented gap requiring the future extractor (or manual/assisted cataloging in the
interim). This boundary must be stated plainly in any output and must not be papered over.

## Expected Tests

- **Parity tests** per migrated rule: same fixture inputs produce matching numeric outputs within a
  declared tolerance (e.g., money to the cent, interest to declared precision).
- **Dependency-coverage tests**: every inventoried proc has at least one mapped edge or an explicit
  "leaf" annotation.
- **Transaction-semantics tests**: rollback on a forced mid-cycle failure leaves no partial posting.
- **Regression**: re-running a settled day reproduces the same ledger and exception set.

## Expected Deployment Concerns

- Phased, slice-by-slice cutover with a feature flag per migrated slice; no big-bang.
- Schema changes on MSSQL must be additive and backward-compatible during dual-run.
- Dual-run window where legacy and target both compute results and outputs are compared.
- Observability: structured logs and explicit timing around each cycle stage.

## Rollback Considerations

- Each slice is independently revertible via its feature flag without redeploy.
- Dual-run means the legacy path remains authoritative until a slice's parity is signed off.
- Additive-only schema changes guarantee the legacy path keeps working if a slice is reverted.
- Promotion and rollback events are both recorded in the append-only audit trail.

## CEO/CTO Summary

NightLedger is a key-person and audit risk: critical settlement logic is locked inside ~610 legacy
database routines no current employee fully understands. This program first makes that logic
**visible and inventoried**, then modernizes it in **small, individually reversible slices** into a
testable .NET-over-MSSQL platform — every migrated rule proven against the legacy result before it
goes live. We are explicit about tooling limits: automated extraction covers our **target** MSSQL
artifacts today; cataloging the **source Oracle PL/SQL** is a recognized gap addressed by a future
extractor and assisted manual review. The payoff is reduced key-person risk, audit-ready change
control, and a defensible, low-risk path off an unmaintainable platform.
