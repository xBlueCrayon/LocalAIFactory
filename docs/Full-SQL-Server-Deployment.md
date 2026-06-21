# Full SQL Server Deployment — LocalAIFactory

Production-style deployment against a **full SQL Server edition** (a separate, managed instance) with
a least-privilege service account, TLS in transit, a real backup/restore schedule, and additive
migrations. This page covers what is specific to full SQL Server; the shared contract is in
[`SQL-Server-Deployment-Guide.md`](SQL-Server-Deployment-Guide.md).

> Honest note: **no production deployment was executed in this work.** The scripts are committed and
> follow the safe contract, and the validation host used LocalDB; a representative production deploy
> is documented, not demonstrated here ([`Known-Limitations.md`](Known-Limitations.md) §5). MSSQL is
> the source of truth; Ollama/Qdrant/GPU remain optional.

---

## 1. When to choose full SQL Server

- A **pilot or production** deployment on a managed SQL estate, separate from the app host.
- Larger imports/estates than SQL Express can hold; backup **compression**; SQL Agent scheduling; HA
  options provided by your DBA team.

---

## 2. Separate instance and service account (least privilege)

- Run the app host and the SQL instance **separately**; do not co-locate the database with the app
  unless your estate requires it.
- Create a **dedicated service account** (a domain account or the IIS app-pool identity) for the app.
  Grant it the **minimum** rights on the `LocalAIFactory` database — enough to run the app and apply
  migrations, not server-wide admin. Avoid `sysadmin`.
- Prefer **Windows/Integrated authentication** so no password is stored. If SQL auth is mandated,
  inject the full connection string from a secret store at deploy time — never committed config
  (see §5).

---

## 3. Create the database (create-if-absent, never drops)

```powershell
pwsh database/create-full-mssql-db.ps1 -Instance "YOUR_SQL_HOST" -Database "LocalAIFactory"
```

Same safe contract as the other engines: trusted connection by default, **CREATE-if-absent**
(`exists (no drop)` when present), then `dotnet ef database update`. SQL auth via `-User`/`-Password`
is supported but **never forced and never stored**.

If your estate standardises on a **specific collation**, create the database with that collation
**before** running migrations — the scripts will then migrate the existing database rather than create
one ([`SQL-Server-Deployment-Guide.md`](SQL-Server-Deployment-Guide.md) §5).

---

## 4. Apply additive migrations

```powershell
pwsh database/apply-migrations.ps1 `
  -ConnectionString "Server=YOUR_SQL_HOST;Database=LocalAIFactory;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=True;TrustServerCertificate=False"
```

Policy:

- Migrations are **additive and backward-compatible**; EF creates the database if absent and **never
  drops or truncates**.
- **No destructive schema change** (drop/rename column or table, lossy type change, data deletion)
  ships without explicit human approval.
- The migration log masks any `Password=...` token before printing the target connection.

---

## 5. Encryption in transit and secret handling

- Use **`Encrypt=True;TrustServerCertificate=False`** with a properly issued, trusted certificate on
  the SQL host — TLS on, server certificate validated. This is the recommended production posture.
- `MultipleActiveResultSets=true` is required (the app relies on MARS).
- **Never commit credentials.** Supply the connection string via the
  `ConnectionStrings__DefaultConnection` environment variable or a git-ignored local override:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=YOUR_SQL_HOST;Database=LocalAIFactory;User Id=app_user;Password=<from-secret-store>;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true"
```

The example config `database/appsettings.FullSqlServer.example.json` defaults to trusted auth and
contains no secrets.

---

## 6. First-run seeding and verification

On first startup the app **migrates, seeds, and installs all 4 knowledge packs idempotently**
(`KnowledgePacks:InstallAllAtStartup`, default `true`) → **438 items**. Then verify (read-only gates):

```powershell
pwsh database/verify-knowledge-base.ps1 -ServerInstance "YOUR_SQL_HOST" -Database "LocalAIFactory"
pwsh scripts/release/verify-installation.ps1 -Instance "YOUR_SQL_HOST" -Database "LocalAIFactory"
pwsh scripts/release/post-install-healthcheck.ps1 -Url "http://<app-host>:8080"
```

---

## 7. Backup / restore (compression supported here)

Full SQL Server supports backup compression, so you may pass `-Compress`:

```powershell
# Backup WITH COMPRESSION (full edition only)
pwsh database/backup-database.ps1 -ServerInstance "YOUR_SQL_HOST" -Database "LocalAIFactory" -BackupDir "\\backup-share\LAF" -Compress

# Verify (RESTORE VERIFYONLY — non-destructive)
$bak = (Get-ChildItem \\backup-share\LAF\*.bak | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
pwsh database/restore-verify-database.ps1 -BackupFile $bak
```

Restores **never target production**: `restore-database.ps1` refuses if the target equals the
production database name, and restoring *over* production is an operator-gated swap performed only
after a verify-database copy is validated. Back up the `./keys` folder separately. Full drill and
retention guidance in [`Backup-Restore-Runbook.md`](Backup-Restore-Runbook.md); upgrades in
[`Upgrade-Rollback-Runbook.md`](Upgrade-Rollback-Runbook.md).

---

## 8. Hosting behind IIS

For a production-style web host, deploy behind IIS per
[`Windows-Server-IIS-Deployment-Guide.md`](Windows-Server-IIS-Deployment-Guide.md): No Managed Code
app pool, the app-pool identity granted a SQL login with rights on `LocalAIFactory`, and modify rights
on the persistent `./keys` folder. The IIS deploy helper is dry-run by default; site/app-pool changes
are operator-gated.

---

## 9. Honest status to close production-grade

The full-SQL path is documented and scripted but **not demonstrated on a representative production
host** in this work. Close it by running the create → seed → verify → smoke → benchmark sequence on a
production-class SQL instance and host, with the output captured in logs
([`Known-Limitations.md`](Known-Limitations.md) §5).
