# Mode A (IIS) — Prerequisite Discovery

**Date:** 2026-06-21 · **Host:** `DESKTOP-M1HANKN` (Windows 11 Pro)

| Capability | Result (before enablement) | Source |
|---|---|---|
| Admin rights | **Yes** (`desktop-m1hankn\admin`, elevated) | WindowsPrincipal check |
| `W3SVC` service | **Absent** (before) | `Get-Service W3SVC` |
| **IIS-WebServerRole feature** | **Disabled** (i.e. *available to enable*, not absent) | `dism /online /get-featureinfo /featurename:IIS-WebServerRole` |
| IIS-WebServer / CommonHttpFeatures / ManagementConsole / WindowsAuthentication | **Disabled** (available) | `dism /online /get-features` |
| `appcmd.exe` | Absent before (appears after IIS install) | `Test-Path inetsrv\appcmd.exe` |
| `WebAdministration` / `IISAdministration` modules | Absent before | `Get-Module -ListAvailable` |
| **ASP.NET Core Hosting Bundle / ANCM** | **ABSENT** (`aspnetcorev2.dll` not present; no ANCM registry) | filesystem + registry |
| ASP.NET Core shared runtime | **10.0.9 present** (enough for Kestrel self-host, **not** for IIS hosting) | `dotnet/shared/Microsoft.AspNetCore.App` |
| SQL Server Express | **Running** — `MSSQL$SQLEXPRESS`, SQL Server 2022 (16.0.1) | `Get-Service` |
| Internet / winget source | winget present; source index downloaded; `Microsoft.DotNet.HostingBundle.10` 10.0.9 **available** | `winget search/show` |

## Why the earlier "Class not registered"

The PowerShell `Get-WindowsOptionalFeature` cmdlet returned "Class not registered" on this host (a
CBS/DISM COM-registration quirk in the PS host). The **`dism.exe` command-line tool works correctly** and
reports the real feature state (Disabled = enableable). All IIS enablement in this phase therefore uses
`dism.exe`, not the PowerShell cmdlet.

## Classification

**NeedsIisEnablement + NeedsHostingBundle** → both are satisfiable on this host:

- IIS features can be enabled via `dism /online /enable-feature … /norestart` (no reboot observed).
- The ASP.NET Core Hosting Bundle (ANCM) can be installed via winget
  (`Microsoft.DotNet.HostingBundle.10`, version **10.0.9** — exactly matching the installed runtime),
  which is the official, MIT-licensed, reversible Microsoft package.

This host is therefore **ReadyForModeA after enablement + Hosting Bundle install** — not blocked. (Had the
Hosting Bundle been unavailable or required a reboot, this phase would have stopped at "Mode C+" with the
blocker documented, per the safety rules.)
