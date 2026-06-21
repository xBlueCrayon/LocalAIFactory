# MSSQL Production Readiness — LocalAIFactory

An honest assessment of what is **ready** for the MSSQL store and what **remains** before a production
deployment can be claimed. MSSQL is the authoritative memory store; the platform is designed to run
with **only SQL Server present**. This document makes **no** production-certification or commercial-GA
claim — it states the readiness facts and the open items, with the proofs that would close them.

---

## 1. What is ready (observed on the build host)

### Additive, non-destructive migrations
- **14 EF Core migrations** applied (`__EFMigrationsHistory`), schema of 34 tables.
- Migrations are **additive and backward-compatible** by policy; destructive changes require explicit
  approval (CLAUDE.md §6). `apply-migrations.ps1` runs `dotnet ef database update` and never drops or
  truncates.
- The `ModelSnapshot` is kept consistent through EF (no hand-edited drift).

### Backup and restore
- `database/backup-database.ps1`: `BACKUP DATABASE ... WITH INIT, CHECKSUM`; **69.5 MB** backup
  produced (live-proven, uncompressed on LocalDB).
- `database/restore-verify-database.ps1`: `RESTORE VERIFYONLY` → **VERIFY OK** (live-proven).
- `database/restore-database.ps1`: refuses to restore over the production database name.
- Opt-in `-Compress` for full editions that support `COMPRESSION` (off by default for Express/LocalDB).
- Evidence: `docs/Database-Backup-Restore-Evidence.md`.

### Seed + verify integrity
- Knowledge base seeds idempotently (4 packs / 438 items); `verify-knowledge-base.ps1` →
  `KNOWLEDGE-BASE: VERIFIED`; `verify-full-install.ps1` → `VERIFY-FULL-INSTALL: PASS`.

### Repo hygiene / secrets
- Security audit (`scripts/security/security-audit.ps1`): **0 HIGH**, no tracked `bin/obj/db/model/keys`
  artifacts, no tracked file >5 MB, no hardcoded secrets. Connection strings with credentials live in
  environment variables / git-ignored overrides, never committed.

---

## 2. Least-privilege service-account guidance

For a server deployment, run the app under a dedicated, least-privilege identity:

- **Use a dedicated service account** (Windows service account or SQL login) scoped to the
  `LocalAIFactory` database only.
- **Grant the minimum the app needs at runtime:** `db_datareader` + `db_datawriter` on the
  application database (plus `EXECUTE` on any procedures used). Day-to-day operation does not require
  `sysadmin`.
- **Separate the migration/DDL identity from the runtime identity.** Schema migrations
  (`dotnet ef database update`) need DDL rights (e.g. `db_ddladmin` / `db_owner`); apply migrations
  under that identity, then run the app under the constrained read/write account. (Note the
  startup-migrate caveat in §4 if the runtime identity also performs startup migrations.)
- **Backup rights** (`db_backupoperator`) belong to the backup job identity, not the app.
- **Trusted connection by default.** For SQL auth, supply credentials via environment variables or a
  git-ignored override — never in committed config (CLAUDE.md §10).
- Keep the `./keys` Data Protection folder out of source control and backed up separately
  (`docs/Database-Backup-Restore-Evidence.md` §6).

---

## 3. What remains for production (open items)

| Area | Status | Proof required to close |
|---|---|---|
| Executed production / IIS deployment | **Not done** | A deploy to a representative IIS/server host with core-page smoke checks passing on that host (`docs/Known-Limitations.md` §5). |
| Full SQL Server / SQL Express exercised | **Not host-verified** | The setup + verify + benchmark run on Express and full SQL Server, captured in logs. |
| High availability / disaster recovery | **Not designed/tested here** | An HA topology (e.g. Always On / failover) and a tested DR runbook with RPO/RTO targets. |
| Scaled-out / multi-instance | **Single-instance assumption** | Migration-lock strategy validated across instances (see §4); load test on the target topology. |
| External penetration test | **Not performed** | A completed third-party pen-test + remediation (`docs/Final-Security-Audit-Report.md`). |

---

## 4. The startup-migrate caveat (and the scaled-out path)

The application **applies EF migrations on startup**. This is:

- **Acceptable for the single-instance, local-first deployment** this platform targets. EF Core takes
  a **migration lock**, so even with a brief overlap two app instances will not corrupt the schema —
  one acquires the lock and the other waits.

For a **scaled-out / multi-instance** deployment, prefer **decoupling migrations from app startup**:

- Apply migrations as a **separate, gated step** before rolling app instances, using the committed
  idempotent script:

  ```powershell
  pwsh database/apply-migrations.ps1 `
    -ConnectionString "Server=<instance>;Database=LocalAIFactory;Trusted_Connection=True;TrustServerCertificate=True"
  ```

  or generate an **idempotent SQL script** and run it under the DDL identity:

  ```powershell
  dotnet ef migrations script --idempotent `
    --project src/LocalAIFactory.Data --startup-project src/LocalAIFactory.Web -o migrate.sql
  ```

- This removes startup races entirely and keeps the runtime identity at least privilege (no DDL
  rights on the app account).

The shipped scripts are idempotent and safe to re-run, which is the precondition for this path.

---

## 5. Honesty statement

The MSSQL store is **structurally sound and operationally exercised on a local LocalDB host**
(migrations, seed/verify, backup, restore-verify). It is **not** a certified production deployment:
no production/IIS/Express/full-SQL deploy has been executed here, no HA/DR has been tested, and no
external pen-test has been performed. **Commercial GA is blocked** until those proofs exist. This
document is a readiness assessment, not a production sign-off.

## See also

- `docs/Database-Setup-Guide.md`, `docs/Database-Operations-Runbook.md`
- `docs/Database-Backup-Restore-Evidence.md`
- `docs/Final-Security-Audit-Report.md`, `docs/Known-Limitations.md`
