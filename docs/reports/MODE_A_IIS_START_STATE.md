# Mode A (IIS) — Start State

**Date:** 2026-06-21 · **Phase:** MODE-A-IIS-PROOF

| Item | Value |
|---|---|
| Branch | `ke-008-code-symbols` (not merged) |
| Starting commit | `9098fa2` — *DEPLOYMENT-HARDENING: execute pilot deployment proof and health checks* |
| Working tree at start | **clean** |
| Draft release | `v1.0.0-rc` — `isDraft=true`, `isPrerelease=true` (unchanged) |

## Process cleanup

`Get-Process dotnet,node,chrome,chromium,playwright,LocalAIFactory.Web` → **no repo-related processes**.
The previous report's "2 shells still running" were completed background wait-loops/build-server nodes;
nothing needed stopping. No unrelated user processes touched.

## Ports

- **8095: FREE** · **8080: FREE** — no leftover app hosting or file locks on `.tmp-release` / `.tmp-publish`.

## Mode C proof summary (prior phase, commit `9098fa2`)

Mode C = published-app binaries + **SQL Server Express 2022**, no IIS: fresh
`LocalAIFactory_DeploymentProof` DB, 14 migrations + 4 packs/438 items, 13 routes 200, 0 HTTP 500s,
`09-post-deploy-healthcheck` PASS, rollback proven, main LocalDB untouched.

## This phase

Attempt **Mode A — real IIS + SQL Express pilot**. The host has **IIS available to enable** (DISM:
`IIS-WebServerRole` = Disabled, not absent) and admin rights, but the **ASP.NET Core Hosting Bundle /
ANCM is absent** — it is the prerequisite being installed (winget `Microsoft.DotNet.HostingBundle.10`
10.0.9, version-matched to the installed runtime). See `MODE_A_IIS_PREREQUISITE_DISCOVERY.md` and
`MODE_A_IIS_ENABLEMENT_EVIDENCE.md`.
