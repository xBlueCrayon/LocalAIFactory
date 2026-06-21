# Deployment Environment Discovery

**Date:** 2026-06-21 · **Host:** `DESKTOP-M1HANKN`

Captured live. Determines the strongest **truthful** deployment mode this host supports.

| Capability | Result | Evidence |
|---|---|---|
| Windows edition | Windows 11 Pro | `Get-CimInstance Win32_OperatingSystem` |
| Admin rights | **Yes** (`desktop-m1hankn\admin`, elevated) | `whoami`; WindowsPrincipal admin check |
| PowerShell execution policy | `Bypass` | `Get-ExecutionPolicy` |
| **IIS (W3SVC service)** | **NOT present** | `Get-Service W3SVC` → not found |
| **IIS feature provider** | **Not registered** | `Get-WindowsOptionalFeature IIS-WebServerRole` → "Class not registered" (DISM IIS provider absent) |
| IIS management / `iisreset` | n/a (IIS not installed) | — |
| .NET SDK | 10.0.301 | `dotnet --version` |
| **ASP.NET Core shared runtime** | **10.0.9 present** | `C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.9` |
| ANCM / Hosting Bundle (for IIS) | n/a (no IIS) — but the ASP.NET Core runtime above lets a published app self-host via Kestrel | — |
| SQL LocalDB | Present (`MSSQLLocalDB` v17) — holds existing `LocalAIFactory` DB | prior phases |
| **SQL Server Express** | **RUNNING** — `MSSQL$SQLEXPRESS`, **SQL Server 2022 (16.0.1)** | `Get-Service MSSQL$SQLEXPRESS` (Running); `sqlcmd -S .\SQLEXPRESS -Q "SELECT @@VERSION"` |
| Full SQL Server | Not present (Express only) | service list |
| `sqlcmd` | Present | `C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE` |
| Deployment DB `LocalAIFactory_DeploymentProof` | **Absent** (will be created fresh on SQL Express) | `SELECT DB_ID(...)` → ABSENT |
| Chosen port `8095` | available (verified at run time) | — |
| Docker | Not installed (not required) | prior phases |
| GitHub release asset | `v1.0.0-rc` draft asset present | `gh release view` |

## Deployment mode classification

| Mode | Definition | Available here? |
|---|---|---|
| A | Real IIS + SQL Express/full SQL executed | ❌ No IIS |
| B | Real IIS + LocalDB executed | ❌ No IIS |
| **C** | **Published app + SQL Express/full SQL executed, no IIS** | ✅ **YES — selected** |
| D | Published app + LocalDB executed, no IIS | (superseded by C) |
| E | Dry-run only, blocker documented | (not needed — C is executable) |

**Selected: Mode C.** SQL Server Express 2022 is running, so the proof uses a **real server database
engine** (not LocalDB) with the **published application binaries**. IIS is honestly recorded as
unavailable; installing the IIS Windows feature is the documented next step to reach Mode A.

## Honest limitation

Mode C is a **published-app + SQL-Express pilot proof**, **not** an IIS production proof and **not**
commercial GA. The auth posture for HTTP reachability is documented in
`DEPLOYMENT_PUBLISHED_APP_PROOF.md`.
