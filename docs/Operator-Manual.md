# Operator Manual

For the operator who installs, runs, monitors, backs up, and upgrades a LocalAIFactory instance. All
commands below are PowerShell, run from the repository root unless noted. Scripts referenced here are
**read-only or non-destructive by default**; destructive operations are called out explicitly.

The platform is **local-first and MSSQL-authoritative**. It must remain fully operable on MSSQL alone
— no GPU, no internet, no Ollama, no Qdrant required.

---

## 1. Prerequisites

- **.NET 10 SDK** (build/run) — or the published runtime for a deployed instance.
- **SQL Server** — LocalDB or SQL Express for dev/demo; full SQL Server for production.
- Optional: **Ollama** + a model (e.g. `qwen2.5-coder:14b`) and **Qdrant** for AI/vector features.
  Both are optional and gated behind config.

Proven host this sprint (illustrative, not a requirement): AMD Ryzen 7 9800X3D (8c/16t), 31 GB RAM,
NVIDIA RTX 5070 Ti 16 GB (driver 596.36), Ollama online with `qwen2.5-coder:14b` + `deepseek-r1:14b`,
MSSQL via LocalDB.

---

## 2. Create and seed the database

Pick the script that matches your SQL host. Each is **safe by default** — it detects an existing
database and **never drops it**, applying migrations only.

```powershell
# Dev / demo on LocalDB
database/create-localdb.ps1

# Demo on SQL Express
database/create-sqlexpress-db.ps1

# Full SQL Server
database/create-full-mssql-db.ps1
```

The app **migrates and seeds on startup**, and installs the professional base knowledge pack
idempotently. To pre-seed and verify without browsing the app:

```powershell
database/seed-professional-knowledge-base.ps1   # applies migrations, triggers install, verifies
database/verify-knowledge-base.ps1 -ServerInstance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory"
```

Example `appsettings` templates are provided: `database/appsettings.LocalDB.example.json`,
`appsettings.SqlExpress.example.json`, `appsettings.FullSqlServer.example.json`.

---

## 3. Run the app

```powershell
dotnet restore
dotnet build LocalAIFactory.sln -c Release
dotnet run --project src/LocalAIFactory.Web
```

A first run against a reachable SQL Server is enough — the app migrates, seeds, and installs the base
pack automatically. PowerShell helpers exist in `scripts/`: `build.ps1`, `run.ps1`, `migrate.ps1`,
`clean.ps1`, `verify.ps1`.

For an IIS / Windows Server deployment see `docs/Windows-Server-IIS-Deployment-Guide.md` and
`docs/SQL-Server-Deployment-Guide.md`.

---

## 4. Health checks

### 4.1 Core-page smoke (must never hang)

With the app running, each core page must return quickly:

```powershell
curl -s -o NUL -w "%{http_code} %{time_total}s`n" http://localhost:5000/
curl -s -o NUL -w "%{http_code} %{time_total}s`n" http://localhost:5000/Projects
curl -s -o NUL -w "%{http_code} %{time_total}s`n" http://localhost:5000/Knowledge
curl -s -o NUL -w "%{http_code} %{time_total}s`n" http://localhost:5000/Models
```

`RequestTimingMiddleware` logs `-> {path} started` and `<- {path} {status} in {ms} ms`. A hung
endpoint logs "started" with no matching "completed" line — use that to locate a stall.

### 4.2 Read-only diagnostics

All non-destructive; safe to run on a live host. They degrade gracefully when an optional service is
absent.

```powershell
scripts/diagnostics/system-snapshot.ps1      # CPU / RAM / disk / GPU / top processes (-Json for machine output)
scripts/diagnostics/sql-health-check.ps1     # SQL connectivity + DB size (never alters anything)
scripts/diagnostics/ollama-health-check.ps1  # Ollama tags/availability (optional)
scripts/diagnostics/gpu-health-check.ps1     # NVIDIA GPU; exits cleanly when no GPU/driver
scripts/diagnostics/performance-profile.ps1  # wall-clock for build / test / benchmark gates
```

---

## 5. The Support dashboard (/Support)

The **Support** page is the at-a-glance operations view. It answers "is this install healthy and
what version is it?" and **always renders** (empty DB, seeded DB, or MSSQL-only). Tiles:

- **Build / version** — assembly version, informational version, framework, OS, machine, server
  time (UTC), process uptime.
- **Edition / license** — effective edition and license status (demo-safe: missing/expired ⇒
  Community core).
- **Service health** — Qdrant / Ollama / embeddings status and chat availability, read from the
  **cached snapshot only** (never a synchronous probe), plus the last-checked timestamp.
- **DB counts** — projects, knowledge items, knowledge packs, code symbols, imported files, chat
  messages, audit events. Each count is independently guarded; a DB hiccup degrades a tile to
  "unavailable" rather than failing the page.
- **Last import / last audit** — most recent ingestion job and audit event.
- **Disk** — free/total on the content-root drive (best effort).
- **Warnings** — DB unreachable, license grace/expired/invalid, low disk (<5 GB), or first health
  probe pending.

Full spec: `docs/Supportability-Dashboard-Spec.md`.

---

## 6. Diagnostics bundle for support

When raising a support case, collect a bundle:

1. `scripts/diagnostics/system-snapshot.ps1 -Json` (host resources, GPU)
2. `scripts/diagnostics/sql-health-check.ps1` (DB connectivity/size)
3. `scripts/diagnostics/ollama-health-check.ps1` and `gpu-health-check.ps1` (optional AI)
4. A screenshot or copy of the `/Support` page tiles and warnings
5. Recent app logs (look for the `RequestTimingMiddleware` start/finish lines)

See `docs/Support-Runbook.md` and `docs/Troubleshooting-Guide.md` for triage steps.

---

## 7. Backup and restore

MSSQL holds everything authoritative; derived stores (vectors, graph) are rebuildable.

```powershell
database/backup-database.ps1            # back up MSSQL
database/restore-database.ps1           # restore from a backup
database/restore-verify-database.ps1    # verify the restored database
database/reset-derived-indexes.ps1      # rebuild derived indexes after restore
```

Full runbook: `docs/Backup-Restore-Runbook.md`.

---

## 8. Upgrade and rollback

- Apply migrations on upgrade with `database/apply-migrations.ps1` (or the app does it on startup).
- Schema changes are **additive and backward-compatible** by default; no destructive change ships
  without explicit approval.
- Rollback strategy and procedure: `docs/Upgrade-Rollback-Runbook.md`.

---

## 9. Reliability / load smoke

A light reliability check before handover:

1. Run the core-page smoke (§4.1) and confirm every page returns well under one second.
2. Run `scripts/diagnostics/performance-profile.ps1` to baseline build/test/benchmark timing.
3. Run the release verification (`scripts/release/verify-installation.ps1`) and the running-instance
   health check (`scripts/release/post-install-healthcheck.ps1`).

These are smoke-level checks, not a formal load test. Large-scale / concurrency load testing is **not
yet performed** — see `docs/Known-Limitations.md`.

---

## 10. Operational rules

- Never make a page depend on an external service to render. Health is read from the cached snapshot.
- Keep secrets out of the repo: connection strings with credentials go in environment variables or a
  git-ignored override; Data Protection keys live in a git-ignored `keys/`.
- No destructive DB change without explicit approval.
