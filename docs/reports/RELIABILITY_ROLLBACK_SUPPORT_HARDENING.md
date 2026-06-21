# Reliability / Rollback / Support Hardening (Phase 4)

**Date:** 2026-06-21 · Verified live against the Mode A IIS pilot + SQL Express deployment DB.

| Capability | Result | Evidence |
|---|---|---|
| DB backup | **OK** — `LocalAIFactory_IISProof` backed up (1,890 pages) to `./backups/*.bak` (git-ignored) | `database/backup-database.ps1 -Server .\SQLEXPRESS -Database LocalAIFactory_IISProof` |
| Restore verify | **VERIFY OK** — backup set valid/restorable (`RESTORE VERIFYONLY`) | `database/restore-verify-database.ps1` |
| IIS rollback | **PASS** — stop frees port 8095, restart restores 200 | `12-iis-mode-a-rollback-dryrun.ps1 -StopOnly` + restart |
| Support bundle | exported (~3 KB, git-ignored): ollama/sql/process/knowledge/security sections, no secrets | `scripts/support/export-support-bundle.ps1` |
| Event log | ANCM (`IIS AspNetCore Module V2`) process-start events; **no Application Errors** | `Get-WinEvent Application` |
| Process monitor | no stray app processes; IIS worker under W3SVC | `Get-Process` |
| SQL health | SQL Express reachable; deployment DB intact (4 packs/438) | `09`/`11` healthchecks |
| Ollama (optional) | reachable; **off the critical path** | support bundle |
| Deployment healthcheck | Mode A `11` PASS; production-posture `15` PASS (HTTPS+WinAuth) | drill scripts |
| App restart behaviour | app cold-starts under ANCM on first request (200) | `MODE_A_IIS_*` |
| Site stop/start | clean (port freed on stop, restored on start) | rollback proof |

## Honest scope

- Backups/restore proven on the **disposable** `LocalAIFactory_IISProof` DB (RESTORE VERIFYONLY; no
  destructive restore over an active DB). The main LocalDB and the deployment DB are untouched.
- `backups/` is **git-ignored** — backup `.bak` files are never committed.
- No app-pool **recycle-under-load** or sustained-operations test was performed (single-host pilot). A
  scheduled backup job, log rotation, alerting/SLOs, and operations-over-time remain production items.
