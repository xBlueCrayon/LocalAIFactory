# Windows Server / IIS Deployment Guide — LocalAIFactory

This guide deploys LocalAIFactory behind IIS using the real helper
`deploy/scripts/windows-deploy.ps1`. That script is **DRY-RUN by default** and is a *guided
runbook, not an unattended production deployer*. It never edits the firewall or services, and it
never deploys without a backup having been taken.

> **A real IIS PILOT was executed (Mode A, 2026-06-21).** On a Windows 11 + SQL Express host, IIS was
> enabled, the ASP.NET Core Hosting Bundle/ANCM installed, and the published app deployed under IIS (served
> through IIS — `Server: Microsoft-IIS/10.0`) against SQL Express with a **least-privilege** app-pool login,
> 0 HTTP 500s, rollback proven. The executable, re-runnable scripts are
> `scripts/deployment-drill/10-iis-mode-a-deploy.ps1` (+ `11` health, `12` rollback); evidence is in
> `docs/reports/MODE_A_IIS_*`. This guide remains the path for a Windows **Server** production deployment
> (HTTPS, domain service account, staged rollout), which is **not** yet executed.

---

## 1. Prerequisites on the target server

- **IIS** with the **ASP.NET Core Module v2** (`AspNetCoreModuleV2`) — installed by the
  **.NET Hosting Bundle**.
- For a **framework-dependent** deployment: the **.NET 10 ASP.NET Core Runtime** on the server.
- For a **self-contained** deployment: no runtime needed on the server (the runtime ships in the
  publish output).
- A reachable SQL Server instance with the database prepared (see `SQL-Server-Deployment-Guide.md`).
- A **verified database backup** taken before you deploy (see `Backup-Restore-Runbook.md`).

---

## 2. Publish the app

### Framework-dependent (default, smaller)

```powershell
pwsh scripts/release/build-release.ps1 -Output ".\.tmp-publish"
```

This runs `dotnet publish ... -c Release` and bundles the runnable app **plus the knowledge packs
and readiness scorecard**. Live-proven output here: **142 files, ~45 MB**.

### Self-contained (no runtime required on the server)

```powershell
pwsh scripts/release/build-release.ps1 -Output ".\.tmp-publish" -SelfContained -Runtime win-x64
```

This adds `--self-contained true -r win-x64 -p:PublishSingleFile=true`.

To assemble a shippable zip (app + database scripts + knowledge packs + appsettings examples +
docs) after publishing:

```powershell
pwsh scripts/release/package-release.ps1
```

---

## 3. Review the IIS deployment plan (DRY-RUN)

Run the dry-run to see exactly what would happen — it changes nothing:

```powershell
pwsh scripts/release/install-windows-server-iis-dryrun.ps1 -PublishDir "C:\inetpub\LocalAIFactory"
```

That wraps `deploy/scripts/windows-deploy.ps1` with no `-Execute`. The printed plan is:

1. `dotnet publish <project> -c Release -o "<PublishDir>"`
2. `icacls "<PublishDir>" /grant "IIS AppPool\<AppPool>:(OI)(CI)RX"`
3. `appcmd add apppool /name:"<AppPool>" /managedRuntimeVersion:""`  (No Managed Code)
4. `appcmd add site /name:"<SiteName>" /physicalPath:"<PublishDir>" /bindings:http/*:8080:`
5. Confirm `web.config` references `AspNetCoreModuleV2` with the correct `processPath`.

The script also reminds you, before anything runs, that a **verified database backup must exist**.

### Running the publish step only

```powershell
pwsh deploy/scripts/windows-deploy.ps1 -PublishDir "C:\inetpub\LocalAIFactory" -Execute
```

With `-Execute` the script runs **only the safe publish step**. The IIS site/app-pool steps are
**intentionally left operator-gated** — there is no unattended production automation.

---

## 4. Configure IIS (operator steps)

Run these as an administrator on a prepared host, after reviewing the dry-run plan:

```powershell
# App pool: No Managed Code (ASP.NET Core hosts its own runtime via the module)
appcmd add apppool /name:"LocalAIFactory" /managedRuntimeVersion:""

# Site bound to :8080, pointing at the publish folder
appcmd add site /name:"LocalAIFactory" /physicalPath:"C:\inetpub\LocalAIFactory" /bindings:http/*:8080:

# Bind the site to the app pool (operator step)
appcmd set app "LocalAIFactory/" /applicationPool:"LocalAIFactory"
```

**Why No Managed Code:** the ASP.NET Core Module v2 runs the app out-of-process/in-process using the
.NET runtime, not the legacy .NET Framework CLR — so the app pool's managed runtime version is empty.

---

## 5. File permissions (app-pool identity)

Grant the app-pool identity read/execute on the publish folder:

```powershell
icacls "C:\inetpub\LocalAIFactory" /grant "IIS AppPool\LocalAIFactory:(OI)(CI)RX"
```

The app-pool identity also needs:

- **SQL access** — if using Windows/Integrated auth, add a login for the app-pool identity (or use a
  dedicated service account) with rights on the `LocalAIFactory` database.
- **Write access to the Data Protection key folder** — see §6.

---

## 6. Data Protection key persistence across restarts

The app persists Data Protection keys to **`./keys`** (relative to the content root; git-ignored).
These keys encrypt API keys at rest and protect auth artifacts. **If the folder is wiped or not
writable, encrypted secrets become unreadable and users are logged out after a restart.**

For IIS deployments:

- Place `keys` on a path the app-pool identity can **read and write** (e.g. under the publish folder
  or a dedicated data directory).
- **Persist it across deployments** — do not delete it when you redeploy a new build.
- Back it up alongside the database before an upgrade (see `Upgrade-Rollback-Runbook.md`).
- Grant the app-pool identity modify rights, e.g.:

```powershell
icacls "C:\inetpub\LocalAIFactory\keys" /grant "IIS AppPool\LocalAIFactory:(OI)(CI)M"
```

---

## 7. Post-deployment health check

After the site is up, run the read-only health check:

```powershell
pwsh scripts/release/post-install-healthcheck.ps1 -Url "http://localhost:8080"
```

It GETs `/`, `/BaseKnowledge`, `/Readiness`, `/Models` and reports status + timing. It makes **no
changes** and exits non-zero if any page is not `200`/`302`.

---

## 8. Safety contract

- The deploy helper is **DRY-RUN unless `-Execute`**, and even then only publishes.
- It **never** edits firewall or services, and **never** touches production data.
- IIS site/app-pool changes are **operator-gated** with the appropriate approvals.
- Uninstall is a dry-run (`scripts/release/uninstall-dryrun.ps1`) that removes nothing and never
  drops the database.

---

## 9. Docker (reference only)

`deploy/Dockerfile`, `deploy/docker-compose.cpu.yml`, and `deploy/docker-compose.gpu.yml` exist as
references. **Docker is not installed on the build host used for these runbooks**, so the container
path is unverified here. Treat it as an alternative to validate in your own environment, not a
proven path.
