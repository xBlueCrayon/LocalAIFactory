# Mode A — IIS Site & App Pool Proof (Phase 5)

**Date:** 2026-06-21 · Created via `appcmd` (the `WebAdministration` PS module is not loadable on this host)

| Item | Value |
|---|---|
| **Site** | `LocalAIFactoryPilot` — `appcmd list site` → `(id:2, bindings:http/*:8095:, state:Started)` |
| **App pool** | `LocalAIFactoryPilotPool` — `(MgdVersion:, MgdMode:Integrated, state:Started)` |
| App pool managed runtime | **No Managed Code** (`managedRuntimeVersion` = empty) — correct for ASP.NET Core / ANCM |
| App pool identity | **`ApplicationPoolIdentity`** → virtual account `IIS APPPOOL\LocalAIFactoryPilotPool` |
| Physical path | `C:\inetpub\LocalAIFactoryPilot` (published app) |
| Binding | `http://*:8095` |
| Folder ACL | `IIS AppPool\LocalAIFactoryPilotPool` granted **(OI)(CI)RX** (read/execute) via `icacls` |
| Environment | `ASPNETCORE_ENVIRONMENT=Development` (pilot posture, via web.config) |
| SQL connection | `Server=.\SQLEXPRESS;Database=LocalAIFactory_IISProof;Trusted_Connection=True` — app-pool identity authenticates with **least-privilege** roles |

## Hosting confirmation

- The app is served **through IIS**: the HTTP response carries **`Server: Microsoft-IIS/10.0`**.
- ANCM (`AspNetCoreModuleV2`) launches the app process under the app-pool identity — confirmed by the
  Application event log (`IIS AspNetCore Module V2`, Event 1032/1033, Information) on first request.

## Limitations

- The pilot uses `ApplicationPoolIdentity` (a local virtual account). A production deployment would more
  commonly use a **domain service account** with the same least-privilege SQL grant.
- The site binds plain HTTP on `:8095`. Production would add an HTTPS binding + certificate.
- This is a **pilot** site name and posture — not a production site.
