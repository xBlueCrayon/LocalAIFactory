# Deployment Hardening — Start State

**Date:** 2026-06-21 · **Phase:** DEPLOYMENT-HARDENING

| Item | Value |
|---|---|
| Branch | `ke-008-code-symbols` (historical working branch, **not** merged) |
| Starting commit | `3d5523a` — *R2-ACC-POC-COMPLETE: full local proof of concept validation…* |
| Working tree at start | **clean** |
| Draft release | `v1.0.0-rc` — `isDraft=true`, `isPrerelease=true` (unchanged; **not** published) |

## Process cleanup

`Get-Process dotnet,node,chrome,chromium,playwright,LocalAIFactory.Web` returned **no repo-related
processes**. The previous report's "1 shell still running" was the `verify-poc` background gate, which
completed (exit 0) before this phase. **Nothing needed to be stopped.** No unrelated user processes were
touched.

## Deployment capability detected (summary; detail in DEPLOYMENT_ENVIRONMENT_DISCOVERY.md)

- Admin rights: **Yes** (`desktop-m1hankn\admin`, elevated).
- Windows 11 Pro.
- **IIS: NOT installed** (no `W3SVC` service; IIS feature provider not registered).
- ASP.NET Core shared runtime **10.0.9 present** → a framework-dependent published app runs via `dotnet …dll`.
- **SQL Server Express RUNNING** — `MSSQL$SQLEXPRESS`, **SQL Server 2022 (16.0.1)**; `sqlcmd` present.
- SQL LocalDB also present (holds the existing `LocalAIFactory` DB — left untouched).

## Chosen deployment mode

**Mode C — Published app + SQL Express executed, no IIS.** IIS is genuinely unavailable on this host, so
Mode A/B are not possible (documented, not faked). Mode C is the strongest *truthful* proof available: it
runs the **published binaries** (not `dotnet run` from source) against a **real server SQL engine**
(SQL Server Express 2022, not LocalDB) using a **fresh, isolated deployment database**
(`LocalAIFactory_DeploymentProof`). The existing LocalDB `LocalAIFactory` database is not modified.
