# Controlled Autonomous Engineering — Runbook

LocalAIFactory includes a **controlled** autonomous engineering capability. It is local-only, dry-run by
default, gated by a hard command policy, and **cannot commit, push, merge, or deploy without explicit human
approval**. It is a force-multiplier for an engineer, not an unattended production agent.

## What exists (implemented + tested)

- **Command policy** (`CommandPolicy`) — allow/deny/approval classification (`AutonomousExecutorTests`,
  `CapabilityPrototypeTests`).
- **Dry-run planner** (`AutonomousWorkflowPlanner`) — builds a reviewable plan; executes nothing.
- **Controlled executor** (`ControlledExecutor`) — dry-run by default; in execute mode runs **only allowlisted**
  build/test/read commands, captures output, halts on first failure, and **never promotes** (commit/push are
  never performed). Proven by tests: dry-run runs nothing; denied/approval commands never run; failure halts.
- **Scripts**: `scripts/auto/plan-change.ps1` (print dry-run plan), `run-approved-local-checks.ps1` (run
  allowlisted checks with `-Execute`), `summarize-run.ps1` (review summary).

## The loop (human stays in control)

1. **Intake** a change request (title/description/target).
2. **Plan** — `plan-change.ps1` prints the dry-run plan and each command's policy decision. Nothing runs.
3. **Propose patch** — generated as a diff; not applied to the working tree without approval.
4. **Run approved checks** — `run-approved-local-checks.ps1 -Execute` builds, tests, benchmarks (allowlisted
   only). It **halts on the first failure** and never commits.
5. **Summarize** — `summarize-run.ps1` produces the evidence a human reviews.
6. **Human approval gate** — only a human may commit/push (and never merge). The tooling refuses to do it.

## Hard safety properties (enforced in code)

- Default mode is dry-run; execution requires an explicit flag.
- Denied commands (`reset --hard`, `clean -fdx`, `drop/truncate`, force-push, `merge`, `iisreset`, firewall,
  `rm -rf`, …) are never planned and never executed.
- `commit`/`push`/`tag`/`ef migrations`/unknown commands are `RequiresApproval` and never run autonomously.
- Execution halts at the first non-zero exit (no promotion past a failure).
- The executor's `Promoted` flag is always false — it structurally cannot commit/push/deploy.

## Honest limitations (proof to close)

- The executor runs commands but does **not yet** apply patches into an isolated worktree and prove a real
  fix→test→rollback cycle on a real repository. **Proof for next level**: a real change applied in a worktree,
  built+tested, then reverted, with an audit trail and a human-approval gate exercised end-to-end.
- No production deployment path; that remains operator-gated by design.

See `Autonomous-Command-Policy.md` and `Autonomous-Approval-Gates.md` for the policy and gates in detail.
