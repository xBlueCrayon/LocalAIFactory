# Autonomous Approval Gates

Where a human MUST approve before the autonomous workspace changes any shared or production state.

## The gates (all enforced)

1. **Before applying a patch to the working tree** — patches are proposed as diffs; not applied without review.
2. **Before any commit** — `git commit` is `RequiresApproval`; the executor never commits (`Promoted` is always
   false). A human runs the commit after reviewing the evidence.
3. **Before any push** — `git push` is `RequiresApproval`; pushed only by a human, and **never merged**.
4. **Before any database schema change** — `dotnet ef migrations/database` is `RequiresApproval`.
5. **Before any deployment** — production deployment is denied/operator-gated; the autonomous path never deploys.

## What runs without a gate

Only allowlisted, non-mutating-of-shared-state commands: build, test, benchmark, and read-only git/file
commands. These produce the evidence (pass/fail) a human needs to make the approval decision.

## Failure handling

Execution **halts at the first failure**. A failed build/test means no promotion is offered — the run is
summarized as FAILED and stops. There is no "force through" path.

## Audit

Each planned/executed command is recorded (command, decision, executed?, exit code, output, duration) for the
review summary. Approval decisions and any subsequent human commit/push are captured by Git history and the
platform's append-only audit trail. **Proof to strengthen**: wire an `AuditEvent` for each autonomous run +
approval into the existing audit trail (currently the run record is in-memory/report-only).
