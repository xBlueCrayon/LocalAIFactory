# Industrial Installation Guide — LocalAIFactory

This guide installs LocalAIFactory on a Windows host using the **real, committed scripts**
under `scripts/release/` and `database/`. Every command below is taken from those scripts.
Nothing here drops data, and nothing changes a server without an operator running it.

> Source of truth: **MSSQL**. Qdrant and Ollama are **optional** and gated behind config
> flags (`Qdrant.Enabled`, `Ollama.Enabled`) — the app runs fully without them.

---

## 1. Prerequisites

Required:

- **Windows** (PowerShell 7+ recommended; the scripts use PowerShell ternary syntax).
- **.NET 10 SDK** — needed to build, publish, and run `dotnet ef` migrations.
  (A framework-dependent deployment needs the **.NET 10 ASP.NET Core Runtime** on the
  target; a self-contained publish does not — see the IIS guide.)
- **One** SQL Server engine:
  - **SQL Server LocalDB** — `(localdb)\MSSQLLocalDB` — developer/demo.
  - **SQL Server Express** — `.\SQLEXPRESS` — pilot.
  - **Full SQL Server** — pilot/production.
- **`sqlcmd`** on PATH (ships with SQL Server tools) — used by the create/backup/verify scripts.

Optional (not installed by these scripts; the build host here has **no Docker**):

- **Docker** — `deploy/Dockerfile`, `deploy/docker-compose.cpu.yml`, `deploy/docker-compose.gpu.yml`
  exist as references. They are not exercised by this installer and require Docker to be installed.
- **Ollama** (local inference) and **Qdrant** (vector store) — both optional, both off by default.

---

## 2. Get the database engine ready

Pick the engine that matches your environment. Each create script is **CREATE-if-absent**:
it detects an existing database and **migrates only — it never drops**.

### LocalDB (one-command demo)

The single command below creates the database, seeds the Professional Base Knowledge Pack,
and verifies it:

```powershell
pwsh scripts/release/install-localdb-demo.ps1
```

Internally this runs `database/create-localdb.ps1` → `database/seed-professional-knowledge-base.ps1`
→ `database/verify-knowledge-base.ps1`.

### SQL Express (pilot)

```powershell
pwsh scripts/release/install-sqlexpress-demo.ps1 -Instance ".\SQLEXPRESS" -Database "LocalAIFactory"
```

This runs `database/create-sqlexpress-db.ps1` (trusted connection by default; CREATE-if-absent)
then `database/seed-professional-knowledge-base.ps1`.

### Full SQL Server

```powershell
pwsh database/create-full-mssql-db.ps1 -Instance "YOUR_SQL_HOST" -Database "LocalAIFactory"
```

Same safe contract: trusted connection by default, CREATE-if-absent, then `dotnet ef database update`.
SQL authentication is supported via `-User`/`-Password` but is **never forced and never stored**.
See `SQL-Server-Deployment-Guide.md` for auth and encryption detail.

---

## 3. First-run behaviour (auto-migrate, seed, pack install)

On startup the app **migrates, seeds, and installs the knowledge pack idempotently**.
Running it once against a prepared database is enough — re-running does **not** duplicate items.

From the repo root:

```powershell
dotnet run --project src/LocalAIFactory.Web
```

`database/seed-professional-knowledge-base.ps1` automates exactly this: it applies migrations,
starts the app briefly to trigger the pack install, waits until at least 100 pack items exist
(or times out after `-TimeoutSec`, default 180), stops the app, and verifies.

---

## 4. Verify the installation

Read-only checks — safe against any environment:

```powershell
# Knowledge base (KnowledgePack row, baseline count, unique Uids, provenance, source tags)
pwsh database/verify-knowledge-base.ps1 -ServerInstance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory"

# Artifacts + knowledge base together
pwsh scripts/release/verify-installation.ps1 -Instance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory"

# Running-instance health (GETs core pages, no changes)
pwsh scripts/release/post-install-healthcheck.ps1 -Url "http://localhost:8080"
```

Live-proven on this host: the knowledge base verified at **390 items**, all curated, no duplicate
Uids. `verify-knowledge-base.ps1` enforces a minimum baseline (`-MinItems`, default 100) and exits
non-zero on any failed check.

---

## 5. Configuration and key locations

Copy the matching example into `appsettings.Development.json` (or environment-specific config) and
edit. **The examples contain no secrets** — trusted (Windows/Integrated) auth is the default:

- `database/appsettings.LocalDB.example.json`
- `database/appsettings.SqlExpress.example.json`
- `database/appsettings.FullSqlServer.example.json`

Each example sets `Qdrant.Enabled=false` and `Ollama.Enabled=false`. The LocalDB example enables
dev auth (`Security.UseDevAuth=true`); the Express/Full examples set `UseDevAuth=false` for real
Windows authentication.

**Data Protection keys** persist in `./keys` (git-ignored). These encrypt API keys at rest and
sign auth cookies; **preserve this folder across restarts and deployments** (see the IIS guide).

> Never commit credentials. If SQL auth is mandated, inject the full connection string from an
> environment variable or secret store at deploy time — see the comment in the FullSqlServer example.

---

## 6. Operator-gating note

The install/uninstall flow is deliberately **operator-gated and non-destructive**:

- The IIS installer (`scripts/release/install-windows-server-iis-dryrun.ps1`) is a **dry-run** —
  it prints the plan and changes nothing.
- The uninstaller (`scripts/release/uninstall-dryrun.ps1`) is a **dry-run** — it removes nothing,
  **never drops the database**, and preserves Data Protection keys and backups.

There is **no unattended destructive automation**. Anything that removes files or data is a separate,
explicit, operator-approved step. See `Upgrade-Rollback-Runbook.md` and `Backup-Restore-Runbook.md`.
