# Database Setup Guide — LocalAIFactory

MSSQL is the **authoritative memory store**; the platform must run with **only SQL Server present**.
This guide covers the three supported database targets — **LocalDB** (dev/demo), **SQL Express**
(pilot), and **full SQL Server** (server) — using the committed scripts under `database/`. All setup
scripts are **safe by default**: they detect an existing database and **migrate only** (never drop).

EF Core applies migrations and the app seeds the knowledge base on startup, so for most cases a
created database plus a single app run is all that is needed.

---

## 0. Prerequisites

- .NET 10 SDK (for `dotnet`, `dotnet ef`).
- A reachable SQL Server: LocalDB, SQL Express, or full SQL Server.
- `sqlcmd` on PATH for the verification scripts (present on this host).
- A connection string. Examples are committed:
  - `database/appsettings.LocalDB.example.json`
  - `database/appsettings.SqlExpress.example.json`
  - `database/appsettings.FullSqlServer.example.json`

Copy the relevant example to your environment's `appsettings` (or set
`ConnectionStrings__DefaultConnection`). Never commit a real connection string with credentials.

---

## 1. Fastest path — one command (LocalDB)

```powershell
pwsh database/setup-full-local-demo.ps1
```

This runs the full first-run sequence and is **idempotent** (create-if-absent, never drops):

1. **Create LocalDB** database if absent (`database/create-localdb.ps1`).
2. **Apply migrations + seed all knowledge packs** by booting the app briefly
   (`scripts/knowledge/install-all-knowledge-packs.ps1 -SkipCreate`).
3. **Verify** the full install (`database/verify-full-install.ps1`).

Expected result:

```
SETUP-FULL-LOCAL-DEMO: PASS — database ready, knowledge base seeded + verified.
```

This is the recommended path for a customer/operator first run on a developer or demo machine.

---

## 2. LocalDB (dev / demo) — step by step

```powershell
# 1. Create/prepare the database (starts LocalDB, migrates; never drops an existing DB)
pwsh database/create-localdb.ps1

# 2. Seed the knowledge base (idempotent) — or just run the app once
pwsh scripts/knowledge/install-all-knowledge-packs.ps1 -SkipCreate

# 3. Verify
pwsh database/verify-full-install.ps1
```

`create-localdb.ps1` detects an existing `LocalAIFactory` database and **migrates only**, printing
`Database [LocalAIFactory] already exists — will MIGRATE only (no drop).`

---

## 3. SQL Express (pilot)

```powershell
# Create/prepare on a SQL Express instance (adjust the instance name)
pwsh database/create-sqlexpress-db.ps1

# Apply migrations explicitly against an Express connection string if needed
pwsh database/apply-migrations.ps1 `
  -ConnectionString "Server=.\SQLEXPRESS;Database=LocalAIFactory;Trusted_Connection=True;TrustServerCertificate=True"

# Verify against the Express instance
pwsh database/verify-full-install.ps1 -Server ".\SQLEXPRESS" -Database "LocalAIFactory"
```

Use `database/appsettings.SqlExpress.example.json` as the connection-string template. See
`docs/SQL-Express-Pilot-Deployment.md` for the pilot deployment context.

> **Not host-verified here.** The SQL Express path is documented and uses the same scripts, but it was
> not exercised end-to-end on the build host. See `docs/Known-Limitations.md` §5.

---

## 4. Full SQL Server (server)

```powershell
# Create/prepare on a full SQL Server instance
pwsh database/create-full-mssql-db.ps1

# Apply migrations against the server connection string
pwsh database/apply-migrations.ps1 `
  -ConnectionString "Server=DBHOST\INST;Database=LocalAIFactory;Trusted_Connection=True;TrustServerCertificate=True"

# Verify
pwsh database/verify-full-install.ps1 -Server "DBHOST\INST" -Database "LocalAIFactory"
```

Use `database/appsettings.FullSqlServer.example.json` as the template. For SQL authentication, pass
credentials via the connection string / environment variables — never commit them. See
`docs/Full-SQL-Server-Deployment.md` and `docs/MSSQL-Production-Readiness.md`.

> **Not host-verified here.** A real full-SQL/production deployment has not been executed as part of
> this work (`docs/Known-Limitations.md` §5).

---

## 5. Verification (all targets)

```powershell
pwsh database/verify-full-install.ps1 -Server "<instance>" -Database "LocalAIFactory"
```

This is read-only and checks:

1. The database is reachable.
2. `__EFMigrationsHistory` has rows (migrations applied — **14** on this host).
3. The knowledge base verifies (`database/verify-knowledge-base.ps1` → `KNOWLEDGE-BASE: VERIFIED`,
   438 baseline items).
4. All source packs validate and match the live installed counts (4 packs / 438 items).

Expected: `VERIFY-FULL-INSTALL: PASS`.

---

## 6. What gets created

- The `LocalAIFactory` database (34-table schema) via EF Core migrations.
- The seeded knowledge base (4 packs / 438 items) on first app startup.

The app **migrates and seeds on startup**, so once the connection string points at a reachable server,
`dotnet run --project src/LocalAIFactory.Web` is sufficient for a first run.

## See also

- `docs/Database-Operations-Runbook.md` — day-2 operations.
- `docs/Database-Backup-Restore-Evidence.md` — backup/restore evidence.
- `docs/MSSQL-Production-Readiness.md` — production readiness assessment.
- `database/README.md` — the script reference.
