# Deployment Rollback Proof (Phase 7)

**Date:** 2026-06-21 · **Mode:** C (published-app fallback)

The deployment was **not** broken to prove rollback. Rollback readiness was demonstrated safely.

## Executed (published-app teardown)

| Step | Result |
|---|---|
| Stop the deployed app | Stopped PID **30164** (the process listening on 8095) |
| Verify port free | Port **8095 → FREE** after stop |
| Verify no app process left | `Get-Process LocalAIFactory.Web` → **0** |
| Verify repo clean | No deployment artifacts in the working tree (publish/temp/logs are git-ignored) |
| No locked files | Publish folder and DB released; subsequent gates ran cleanly |

## Dry-run rollback script

`scripts/deployment-drill/07-run-rollback-dryrun.ps1` ran in **dry-run** (no changes): it prints the
app-restore plan (restore previous `app/` from a timestamped backup) and keeps the DB restore **manual and
confirmation-gated**. For the published-app mode, rollback is simply "stop the process + repoint the
connection string", which is what was exercised above.

## Disposable deployment database

`LocalAIFactory_DeploymentProof` on SQL Express was **left in place** as durable evidence (it is isolated
and does not affect the main LocalDB `LocalAIFactory` database). It can be removed at any time with the
operator-approved, scoped command:

```sql
DROP DATABASE LocalAIFactory_DeploymentProof;   -- isolated disposable deployment-test DB
```

## If this had been an IIS deployment (documented)

- Stop the site / app pool (`Stop-Website LocalAIFactoryPilot`, `Stop-WebAppPool LocalAIFactoryPilotPool`).
- Restore the previous published `app/` from its timestamped backup (`07 -Execute`).
- Repoint the connection string back to the previous database.
- Remove the pilot site/app pool if abandoning (`Remove-Website` / `Remove-WebAppPool`).
- The DB restore stays manual via `database/restore-database.ps1`.

## Result

Rollback readiness **proven**: the deployment stops cleanly, the port frees, no processes or locks remain,
the repo stays clean, and the disposable DB is isolated and removable. No destructive action was taken.
