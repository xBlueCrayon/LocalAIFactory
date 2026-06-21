# Deployment IIS Execution Proof (Phase 4)

**Date:** 2026-06-21

## Outcome: IIS NOT available on this host — honestly recorded, not faked

| Check | Result |
|---|---|
| IIS service (`W3SVC`) | **NOT present** |
| IIS feature provider (`Get-WindowsOptionalFeature IIS-WebServerRole`) | **Not registered** ("Class not registered" — the DISM IIS provider is absent) |
| `iisreset` / IIS management | n/a (IIS not installed) |
| App pool created | **No** — IIS not installed |
| Site created | **No** — IIS not installed |

**A real IIS deployment was therefore NOT performed, and none is claimed.** Per the absolute rules, the
strongest *truthful* fallback was executed instead: **Mode C — published app + SQL Express** (see
`DEPLOYMENT_PUBLISHED_APP_PROOF.md`).

## What IIS would require (the documented path to Mode A)

To reach a real IIS deployment on this host, an operator would (elevated, approved):

1. Install the IIS Windows feature: `Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole -All`
   (plus `IIS-WebServer`, `IIS-ManagementConsole`).
2. Install the **ASP.NET Core 10 Hosting Bundle** (provides the ASP.NET Core Module / ANCM for IIS;
   without it IIS returns 500.31 / 502.5). Note: the ASP.NET Core **runtime** 10.0.9 is already present,
   but the **Hosting Bundle / ANCM** for IIS is a separate install.
3. Run the drill scripts in `-Execute` mode: `04-setup-iis-dryrun.ps1 -Execute` (app pool + site),
   `05-deploy-package-dryrun.ps1 -Execute` (copy published app), then `06`/`09` health checks.
4. Bind site `LocalAIFactoryPilot` → app pool `LocalAIFactoryPilotPool` (No Managed Code) → physical
   path `C:\inetpub\LocalAIFactoryPilot`, port 8095, with a least-privilege SQL login for the app-pool
   identity against SQL Express.

The drill pack (`scripts/deployment-drill/`) already scripts every one of these steps; only the IIS
feature + Hosting Bundle install is missing on this host. This is the single concrete blocker between the
current **Mode C** proof and a **Mode A** IIS proof.

## Honest statement

No IIS app pool, site, or binding exists. This phase's executable proof is the SQL-Express published-app
deployment, not IIS. Installing IIS + the Hosting Bundle on an operator-approved host is the exact next
action to upgrade this to a real IIS pilot.
