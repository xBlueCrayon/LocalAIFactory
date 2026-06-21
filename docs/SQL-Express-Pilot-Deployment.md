# SQL Express Pilot Deployment — LocalAIFactory

The specifics of deploying LocalAIFactory on **SQL Server Express** for a small, controlled pilot.
This page covers only what differs from the shared database contract; for auth, encryption, and
migrations see [`SQL-Server-Deployment-Guide.md`](SQL-Server-Deployment-Guide.md).

> **Update (2026-06-21): SQL Express HAS now been exercised (Mode C).** The published app was deployed
> against **SQL Server Express 2022** (`MSSQL$SQLEXPRESS`) with a fresh `LocalAIFactory_DeploymentProof`
> database — it migrated (14), seeded 4 packs / 438 items, and served 13 HTTP routes (all 200, 0 HTTP
> 500s); `09-post-deploy-healthcheck` PASS; rollback proven. See
> [`reports/DEPLOYMENT_DATABASE_PROOF.md`](reports/DEPLOYMENT_DATABASE_PROOF.md) and
> [`reports/DEPLOYMENT_PUBLISHED_APP_PROOF.md`](reports/DEPLOYMENT_PUBLISHED_APP_PROOF.md). What is still
> **not** proven: SQL Express **behind IIS** (IIS is not installed on the host) and a production posture.
>
> Original honest note (still true for the IIS path): the validation host used LocalDB for the dev demo.
> The commands and scripts below are committed and follow the same safe contract, but
> Express itself is documented, not host-verified here ([`Known-Limitations.md`](Known-Limitations.md) §5).

---

## 1. When to choose Express

- A **small, controlled pilot** on a single host, where you want a real SQL Server engine (not
  LocalDB) but do not yet need a full managed instance.
- Multiple local users / a persistent service, rather than a developer demo.

If the estate is larger, or you need backup compression, larger databases, more RAM, or SQL Agent,
move to [`Full-SQL-Server-Deployment.md`](Full-SQL-Server-Deployment.md).

---

## 2. Express limits to plan around

- **Database size and RAM are capped** by the Express edition. For larger imports/estates, use a full
  MSSQL edition ([`Hardware-Profiles.md`](Hardware-Profiles.md) §5).
- **No backup `COMPRESSION`.** `WITH COMPRESSION` is **unsupported** on Express (and LocalDB), so the
  backup script leaves it **off by default** for portability. Do **not** pass `-Compress` on Express —
  it is only valid on a full edition that supports it ([`Backup-Restore-Runbook.md`](Backup-Restore-Runbook.md) §1).
- **No SQL Agent** for scheduled jobs — schedule backups via Windows Task Scheduler instead.

---

## 3. Prerequisites

- **SQL Server Express** installed; default instance `.\SQLEXPRESS`.
- **.NET 10 SDK** (or the ASP.NET Core 10 runtime for a framework-dependent host).
- **`sqlcmd`** on PATH.
- **Windows authentication** available for the account that will run the app (trusted auth is the
  default and stores no password).

---

## 4. Create the database (trusted auth, create-if-absent)

```powershell
pwsh database/create-sqlexpress-db.ps1 -Instance ".\SQLEXPRESS" -Database "LocalAIFactory"
```

- **Trusted (Windows/Integrated) connection by default** — no password stored.
- **CREATE-if-absent**: creates the database only if it does not exist; otherwise **migrates only**
  and prints `exists (no drop)`. It never drops, truncates, or overwrites.

### One-command install (create + seed)

```powershell
pwsh scripts/release/install-sqlexpress-demo.ps1 -Instance ".\SQLEXPRESS" -Database "LocalAIFactory"
```

This runs `create-sqlexpress-db.ps1` then `seed-professional-knowledge-base.ps1`.

SQL authentication is supported but **never forced and never stored**; pass `-User`/`-Password` only
when integrated auth is impossible, and inject the full connection string from an environment variable
or secret store at deploy time — never committed config
([`SQL-Server-Deployment-Guide.md`](SQL-Server-Deployment-Guide.md) §2).

---

## 5. Configure

Copy the Express example (no secrets; trusted auth):

```powershell
Copy-Item database/appsettings.SqlExpress.example.json src/LocalAIFactory.Web/appsettings.Production.json
```

The Express example sets `Qdrant.Enabled=false`, `Ollama.Enabled=false`, and
`Security.UseDevAuth=false` (i.e. **real Windows authentication**, not dev auth). Persist the Data
Protection key folder `./keys` across restarts so encrypted secrets and auth cookies survive.

---

## 6. Run and first-run seeding

```powershell
dotnet run --project src/LocalAIFactory.Web
```

On first startup the app **migrates, seeds, and installs all 4 knowledge packs idempotently**
(`KnowledgePacks:InstallAllAtStartup`, default `true`) → **438 items** (390 base + 48 domain).
Re-running does not duplicate items.

For an unattended seed step instead of leaving the app running:

```powershell
pwsh database/seed-professional-knowledge-base.ps1 -Instance ".\SQLEXPRESS" -Database "LocalAIFactory"
```

---

## 7. Verify (read-only gates)

```powershell
pwsh database/verify-knowledge-base.ps1 -ServerInstance ".\SQLEXPRESS" -Database "LocalAIFactory"
pwsh scripts/release/verify-installation.ps1 -Instance ".\SQLEXPRESS" -Database "LocalAIFactory"
pwsh scripts/release/post-install-healthcheck.ps1 -Url "http://localhost:5000"
```

Each is read-only and exits non-zero on failure (deployment gate).

---

## 8. Backups on Express (no compression)

```powershell
# Note: NO -Compress on Express (COMPRESSION unsupported)
pwsh database/backup-database.ps1 -ServerInstance ".\SQLEXPRESS" -Database "LocalAIFactory" -BackupDir "./backups"

# Verify (RESTORE VERIFYONLY — non-destructive)
$bak = (Get-ChildItem ./backups/*.bak | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
pwsh database/restore-verify-database.ps1 -BackupFile $bak
```

Back up the `./keys` folder separately — it is not in the `.bak`. Full drill in
[`Backup-Restore-Runbook.md`](Backup-Restore-Runbook.md) §4.

---

## 9. Hosting

For a persistent pilot behind IIS, follow [`Windows-Server-IIS-Deployment-Guide.md`](Windows-Server-IIS-Deployment-Guide.md)
and grant the app-pool identity a SQL login on the Express instance with rights on the
`LocalAIFactory` database, plus modify rights on the `./keys` folder.
