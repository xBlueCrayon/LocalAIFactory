# Mode A — IIS Package Preparation (Phase 4)

**Date:** 2026-06-21 · Commit `9098fa2`

| Step | Command | Result |
|---|---|---|
| Build | `dotnet build LocalAIFactory.sln -c Release` | 0 errors |
| Test | `dotnet test` | 240 / 240 pass |
| Publish | `scripts/release/build-release.ps1` | 151 files → `./.tmp-publish` (framework-dependent; runs on the installed ASP.NET Core 10.0.9 runtime via ANCM) |
| Package | `scripts/release/package-release.ps1` | release ZIP (16.2 MB) — git-ignored |
| Verify | `scripts/release/verify-release-package.ps1` | PASS |

## IIS physical folder

| Item | Value |
|---|---|
| Path | **`C:\inetpub\LocalAIFactoryPilot`** (**not** committed — git-ignored deployment target) |
| Source | the published app from `./.tmp-publish` (incl. `web.config` with the ANCM handler + `knowledge-packs/`) |
| Backup | `10-iis-mode-a-deploy.ps1` renames any existing folder to `…\.bak-<timestamp>` before copying (never deletes) |

## web.config (ANCM) — patched for the pilot

`10-iis-mode-a-deploy.ps1` adds two `environmentVariables` to the `<aspNetCore>` section (no secrets — a
trusted SQL connection):

```xml
<aspNetCore processPath="dotnet" arguments=".\LocalAIFactory.Web.dll" hostingModel="...">
  <environmentVariables>
    <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Development" />
    <environmentVariable name="ConnectionStrings__DefaultConnection"
      value="Server=.\SQLEXPRESS;Database=LocalAIFactory_IISProof;Trusted_Connection=True;..." />
  </environmentVariables>
</aspNetCore>
```

- **No secrets** are written (Windows trusted auth; the app-pool identity authenticates to SQL).
- `ASPNETCORE_ENVIRONMENT=Development` is the **pilot** posture (dev-auth for full page reachability —
  see the HTTP/auth report for the production-posture note). Logs, if enabled, go to a folder under the
  physical path and are **not** committed.

## Deploy script

`scripts/deployment-drill/10-iis-mode-a-deploy.ps1` — dry-run by default; `-Execute` performs the deploy.
Parameters: `SiteName, AppPoolName, PhysicalPath, Port, SqlServer, Database, PackageOrPublishPath,
Environment`. It is idempotent (app pool/site created only if absent), backs up any prior folder, grants
the app-pool identity read/execute on the folder, and never makes destructive global IIS changes.
