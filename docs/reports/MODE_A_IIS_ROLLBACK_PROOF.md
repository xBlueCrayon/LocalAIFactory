# Mode A — IIS Rollback / Cleanup Proof (Phase 8)

**Date:** 2026-06-21

Rollback was proven **without destroying the evidence** — the site is left running for the final gates, but
the stop→restart cycle demonstrates a real, reversible rollback.

## Rollback script

`scripts/deployment-drill/12-iis-mode-a-rollback-dryrun.ps1` — dry-run by default; `-StopOnly` stops the
site+pool (reversible); `-Execute` removes the site+pool and restores the previous folder backup. It never
drops the database.

## Executed (stop → verify → restart cycle)

| Step | Result |
|---|---|
| `12 -StopOnly` | site = **Stopped**, app pool = **Stopped**, **port 8095 FREE** |
| Probe while stopped | app **not reachable** ("connection actively refused") — expected ✅ |
| Restart (`appcmd start apppool` + `start site`) | HTTP **200** restored, site = **Started** ✅ |

This proves the rollback is real (stopping frees the port and takes the app offline) and reversible
(restarting restores service).

## Full teardown (documented, not executed)

```powershell
# Remove the pilot site + app pool and offer the previous folder backup:
pwsh scripts\deployment-drill\12-iis-mode-a-rollback-dryrun.ps1 -Execute
# Drop the disposable deployment DB (isolated; LocalDB untouched):
sqlcmd -S .\SQLEXPRESS -Q "DROP DATABASE [LocalAIFactory_IISProof]"
# Optionally remove the physical folder:
Remove-Item C:\inetpub\LocalAIFactoryPilot -Recurse -Force
# Optionally uninstall the Hosting Bundle / disable IIS:
winget uninstall Microsoft.DotNet.HostingBundle.10
dism /online /disable-feature /featurename:IIS-WebServerRole /norestart
```

## Current state

The pilot site `LocalAIFactoryPilot` is **left Started** on `http://localhost:8095` as durable evidence
(re-runnable by `11-iis-mode-a-healthcheck.ps1`). The disposable `LocalAIFactory_IISProof` DB is retained;
**LocalDB is untouched**. The IIS physical folder is **not** committed.
