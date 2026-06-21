# Mode A (IIS) — Enablement Evidence

**Date:** 2026-06-21 · **Host:** `DESKTOP-M1HANKN` (Windows 11 Pro, admin) · **No reboot required.**

IIS and the ASP.NET Core hosting prerequisites were enabled safely and reversibly. Exactly what changed:

## IIS Windows features (before → after)

| Feature | Before | After | How |
|---|---|---|---|
| `IIS-WebServerRole` (+ dependencies: WebServer, CommonHttpFeatures, StaticContent, DefaultDocument, HttpErrors, RequestFiltering, …) | Disabled | **Enabled** | `dism /online /enable-feature /featurename:IIS-WebServerRole /all /norestart` |
| `IIS-ManagementConsole` | Disabled | **Enabled** | `dism … /featurename:IIS-ManagementConsole /all /norestart` |
| `IIS-WindowsAuthentication` | Disabled | **Enabled** | `dism … /featurename:IIS-WindowsAuthentication /all /norestart` |

All three `dism` operations reported **"The operation completed successfully."** with **no restart**
required (`RebootPending = False`).

## Service + tooling state (after)

| Check | Result |
|---|---|
| `W3SVC` (World Wide Web Publishing Service) | **Running** |
| `appcmd.exe` | **Present** (`C:\Windows\System32\inetsrv\appcmd.exe`) |
| IIS management of sites/app pools | via **`appcmd.exe`** (the `WebAdministration` PS module is not loadable on this host due to the same COM quirk; `appcmd` is used instead) |

## ASP.NET Core Hosting Bundle / ANCM

| Check | Result |
|---|---|
| Package | **`Microsoft.DotNet.HostingBundle.10` 10.0.9** installed via **winget** (`--silent`, version-matched to the runtime; official Microsoft, MIT) |
| Installed program | "Microsoft .NET 10.0.9 - Windows Server Hosting" (10.0.9.26270) |
| **ANCM binary** | **Present** — `C:\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll` |
| **IIS global module** | **`AspNetCoreModuleV2` registered** (`appcmd list module` → present) |
| Reboot | **Not required** (`RebootPending = False`) |

> The Hosting Bundle places ANCM under `C:\Program Files\IIS\Asp.Net Core Module\V2\` and registers the
> `AspNetCoreModuleV2` global module — **not** directly under `system32\inetsrv`. (An initial check of the
> `inetsrv` path returned false; the correct location confirms ANCM is installed.)

## Classification result

**ReadyForModeA** ✅ — IIS is running, ANCM is registered, `appcmd` is available, SQL Express 2022 is
running, and the published app is built. No reboot, no unsupported OS state, no forced restart.

## Reversibility (documented)

- Hosting Bundle: `winget uninstall Microsoft.DotNet.HostingBundle.10`.
- IIS features: `dism /online /disable-feature /featurename:IIS-WebServerRole /norestart` (and the others).
- These are **not** performed automatically — IIS is left enabled for the deployment proof.
