# Mode A — SQL Express IIS Database Proof (Phase 3)

**Date:** 2026-06-21

| Item | Value |
|---|---|
| SQL instance | **`.\SQLEXPRESS`** — SQL Server Express 2022 (16.0.1) |
| Database | **`LocalAIFactory_IISProof`** (fresh; separate from the Mode C `LocalAIFactory_DeploymentProof`) |
| Migrations | **14** (`__EFMigrationsHistory`) |
| Installed packs | **4** |
| Pack items | **438** (distinct Uids 438, **0 duplicates**) |
| Setup script | `database/setup-iis-sqlexpress-proof.ps1` → created + migrated + seeded; **PASS** |
| Verify script | `database/verify-iis-sqlexpress-proof.ps1` → **PASS** |

## Migration-time vs runtime identity (least-privilege split)

This is the enterprise-correct split the prompt asks for:

| Identity | Who | Privilege | Used for |
|---|---|---|---|
| **Migration-time** | current admin (`desktop-m1hankn\admin`, trusted) | full (creates DB + DDL) | one-time `CREATE DATABASE` + `Database.Migrate()` (14) + initial pack seed, via the published app on a temporary port |
| **Runtime** | **`IIS APPPOOL\LocalAIFactoryPilotPool`** (the IIS app-pool virtual account) | **least privilege** | the live IIS site's SQL connection |

The runtime login was granted via `database/grant-iis-apppool-sql-access.ps1`:

```
CREATE LOGIN [IIS APPPOOL\LocalAIFactoryPilotPool] FROM WINDOWS;
CREATE USER  [IIS APPPOOL\LocalAIFactoryPilotPool] ...;
ALTER ROLE db_datareader ADD MEMBER ...;   -- read
ALTER ROLE db_datawriter ADD MEMBER ...;   -- write
GRANT EXECUTE ...;                          -- stored procedures
-- NOT db_owner, NOT sysadmin, NOT control.
```

Verified roles: **`db_datareader, db_datawriter`** (+ EXECUTE) — confirmed by query. **No db_owner / sysadmin.**

## Least-privilege proof

The IIS-hosted app **cold-started successfully under this least-privilege runtime login** and served HTTP
200 (the app's startup `Database.Migrate()` is a no-op because migrations are already applied, and pack
install is idempotent — neither needs DDL). This demonstrates that, with the migration/runtime split, the
runtime identity needs only `db_datareader + db_datawriter + EXECUTE`.

## Production recommendation

Keep the split: run migrations under a dedicated migration account (DDL rights) in a controlled release
step; grant the app-pool runtime account only `db_datareader + db_datawriter + EXECUTE`. Never give the
runtime app-pool identity `db_owner`/`sysadmin`. The disposable `LocalAIFactory_IISProof` DB is isolated
and can be dropped with `DROP DATABASE [LocalAIFactory_IISProof]`; **LocalDB is untouched**.
