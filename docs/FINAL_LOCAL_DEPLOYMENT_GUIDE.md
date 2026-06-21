# Final Local Deployment Guide — LocalAIFactory

The **fastest local path** to a running LocalAIFactory with its full knowledge base. This is the
LocalDB demo route: one create script, run, and the app **auto-seeds all 4 knowledge packs (438
items)** on first startup. Every command below is exact and uses committed scripts.

> For pilot/production engines see [`SQL-Express-Pilot-Deployment.md`](SQL-Express-Pilot-Deployment.md),
> [`Full-SQL-Server-Deployment.md`](Full-SQL-Server-Deployment.md), and
> [`Windows-Server-IIS-Deployment-Guide.md`](Windows-Server-IIS-Deployment-Guide.md). For mode
> selection see [`Deployment-Guide.md`](Deployment-Guide.md).

---

## 1. Prerequisites

| Requirement | Notes |
|---|---|
| **Windows** with **PowerShell 7+** | The scripts use PowerShell 7 syntax (`pwsh`). |
| **.NET 10 SDK** | Builds, runs, and applies `dotnet ef` migrations. |
| **SQL Server LocalDB** | Default instance `(localdb)\MSSQLLocalDB`. Ships with SQL Server tools / Visual Studio. |
| **`sqlcmd`** on PATH | Used by the create / verify scripts. |

Optional (off by default, not required for this path): Ollama (local inference), Qdrant (vectors),
a GPU. The platform runs fully without them — see [`Offline-Mode-Guide.md`](Offline-Mode-Guide.md).

---

## 2. Build

From the repository root:

```powershell
dotnet restore
dotnet build LocalAIFactory.sln -c Release
```

Expected: build succeeds with **0 errors** (the verified state this release).

---

## 3. Create the local database (create-if-absent, never drops)

```powershell
pwsh database/create-localdb.ps1 -Instance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory"
```

This is **CREATE-if-absent**: it checks `IF DB_ID('LocalAIFactory') IS NULL` and creates the database
only if it does not exist. If it already exists, it **migrates only** and never drops, truncates, or
overwrites. Re-running is safe.

Trusted (Windows/Integrated) connection is the default — no password is stored. SQL auth is supported
via `-User`/`-Password` but never forced; see [`SQL-Server-Deployment-Guide.md`](SQL-Server-Deployment-Guide.md) §2.

### One-command alternative

```powershell
pwsh scripts/release/install-localdb-demo.ps1
```

This runs create → seed → verify in sequence
(`create-localdb.ps1` → `seed-professional-knowledge-base.ps1` → `verify-knowledge-base.ps1`).

---

## 4. Configure (no secrets)

Copy the LocalDB example and edit if needed. It contains **no secrets** — trusted auth by default:

```powershell
Copy-Item database/appsettings.LocalDB.example.json src/LocalAIFactory.Web/appsettings.Development.json
```

The LocalDB example sets `Qdrant.Enabled=false`, `Ollama.Enabled=false`, and dev auth
(`Security.UseDevAuth=true`) for a frictionless local demo. **Data Protection keys** persist in
`./keys` (git-ignored); preserve this folder across restarts so encrypted secrets and auth cookies
survive.

---

## 5. Run — the knowledge base auto-seeds

```powershell
dotnet run --project src/LocalAIFactory.Web
```

On first startup the app **migrates, seeds, and installs ALL knowledge packs idempotently**:

- Controlled by `KnowledgePacks:InstallAllAtStartup` (**default `true`**).
- Installs `professional-base-v1` (**390 items**) plus the three domain packs
  (`financial-institution-operations-v1`, `kyc-aml-transaction-approval-v1`,
  `market-intelligence-forecasting-v1`, **16 each = 48**) for a total of **438 items**.
- **Idempotent and propose-never-overwrite** — re-running does **not** duplicate items and never
  overwrites curated knowledge. See [`FINAL_KNOWLEDGE_BASE_GUIDE.md`](FINAL_KNOWLEDGE_BASE_GUIDE.md).

The four core pages (Home, Projects, Knowledge, Models) must load on an empty DB, a seeded DB, and in
MSSQL-only mode. Default local URL: `http://localhost:5000`.

### Scripted seed (instead of leaving the app running)

```powershell
pwsh database/seed-professional-knowledge-base.ps1 -Instance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory"
```

This applies migrations, starts the app briefly to trigger the pack install, waits until the baseline
items are present (or times out), stops the app, and verifies. Re-running does not duplicate items.

---

## 6. Verify (read-only gates)

Run after seeding. Each is read-only and exits non-zero on failure, so they are deployment gates:

```powershell
# Knowledge base: pack row, baseline >= MinItems, unique Uids, all curated, provenance, source tags
pwsh database/verify-knowledge-base.ps1 -ServerInstance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory"

# Installation artifacts + knowledge base together
pwsh scripts/release/verify-installation.ps1 -Instance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory"

# Running-instance health: GETs core pages, asserts 200/302, changes nothing
pwsh scripts/release/post-install-healthcheck.ps1 -Url "http://localhost:5000"
```

Verified this release: the knowledge base verifies with all items curated and no duplicate Uids.

---

## 7. Run the validation gates (optional but recommended)

These reproduce the release-validation evidence locally:

```powershell
dotnet test tests/LocalAIFactory.Tests                                   # 235 / 235
cd tools/LocalAIFactory.Benchmark; dotnet run -c Release -- --inmemory --suite standard
pwsh scripts/poc/verify-poc.ps1                                          # artifacts + build + test + benchmark + hygiene
pwsh scripts/poc/ui-smoke-test.ps1                                       # starts app, asserts no 500s (11 pages)
pwsh scripts/security/security-audit.ps1                                 # secrets / dangerous-command / large-artifact scan
pwsh scripts/diagnostics/system-snapshot.ps1                             # CPU/RAM/disk/GPU snapshot
```

Verified this release: tests **235/235**, benchmark standard **PASS**, UI smoke **PASS** (11 pages
incl. `/Support`), `verify-poc` **PASS**.

---

## 8. Optional: enable local AI (Ollama)

AI is an optional accelerator for curation; it is **never** on the critical path. To enable it
locally, install Ollama, pull a model (e.g. `qwen2.5-coder:14b`), set `Ollama.Enabled=true` in your
config, and restart. With no reachable model the platform runs normally with no AI outputs. AI output
is always a **proposal**, never authoritative — see [`AI-Governance.md`](AI-Governance.md) and
[`AI-Output-Provenance-and-Approval.md`](AI-Output-Provenance-and-Approval.md).

---

## 9. Tear-down

LocalDB is per-user and disposable. There is **no destructive automation**: removing the demo means
dropping the LocalDB database yourself (an explicit operator action), and deleting the working
directory. The uninstall script (`scripts/release/uninstall-dryrun.ps1`) is a dry-run that removes
nothing and never drops the database. Back up first if you want to keep the curated knowledge — see
[`Backup-Restore-Runbook.md`](Backup-Restore-Runbook.md).
