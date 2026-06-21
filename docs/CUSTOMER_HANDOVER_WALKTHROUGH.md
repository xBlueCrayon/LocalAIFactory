# Customer Handover Walkthrough — LocalAIFactory

A single, end-to-end walkthrough from **GitHub** to a **verified running local demo**, with
copy-paste commands. Every command references a committed script or a standard tool; nothing here
is aspirational. Where a step depends on optional tooling (Ollama, Node, Qdrant) it says so.

> LocalAIFactory is a private, local-first, **MSSQL-authoritative** AI software-engineering platform.
> MSSQL is the source of truth; Ollama (local inference), Qdrant (vectors), and a GPU are all
> **optional** and degrade gracefully when absent. This release makes **no** commercial-GA,
> vendor-certification, regulatory, financial, or fraud-proof claim. See
> [`Known-Limitations.md`](Known-Limitations.md) and [`readiness-scorecard.json`](readiness-scorecard.json).

**Verified state of this release (build host):** branch `ke-008-code-symbols`, final commit
`7a35961`; build **0 errors**; **235/235** tests; benchmark smoke + standard **PASS** (ERP/CRM Gold
6/6, core-banking Gold 6/6, KYC/AML→approval Gold 7/7); UI smoke **PASS** (11 pages incl.
`/Support`); `verify-poc` **PASS**; included knowledge base **4 packs / 438 items** auto-seeded at
startup. A draft prerelease `v1.0.0-rc` exists on GitHub; there is **no** final `v1.0` tag yet.

---

## Choose your path

Four paths share the same 21 numbered steps below. Pick the one that matches your goal and run only
the steps it lists.

| Path | Goal | Steps to run | Notes |
|---|---|---|---|
| **Developer quick path** | Build, run, iterate on the code | 1 → 2 → 3 → 4 → 5 → 6 → 9 → 10 | LocalDB; auto-seed; no release/packaging steps |
| **Operator demo path** | Stand up a clean local demo and prove it | 1 → 5 → 6 → 7 → 8 → 9 → 10 → 11 → 12 → 13 | Verifies KB + pages + UI smoke + benchmark |
| **Customer acceptance path** | Reproduce the release evidence on your own host | 1 → 5 → 8 → 9 → 11 → 12 → 13 → 14 → 15 → 16 → 18 → 19 → 20 | Adds backup/restore, screenshots, release verification, support bundle |
| **Production-style path** | Walk a production-shaped deploy (dry-run) | 1 → 4 → then the **deployment-drill pack** | See [`Production-Deployment-Drill-Pack.md`](Production-Deployment-Drill-Pack.md). **Dry-run by default; not yet executed on a real server.** |

> The Production-style path is **not** a proof of a fresh-server deployment. No representative
> production host, SQL Express, full SQL Server, or Docker deployment has been executed in this work
> (Docker is **not installed** on the build host). The drill pack is the executable procedure to run
> later on a real, operator-approved host — see step 21.

---

## Prerequisites at a glance

| Requirement | Needed for | Notes |
|---|---|---|
| **Windows** + **PowerShell 7+** (`pwsh`) | all paths | Scripts use PowerShell 7 syntax |
| **.NET 10 SDK** | build, run, `dotnet ef` | Builds and applies migrations |
| **SQL Server LocalDB** (or SQL Express / full SQL Server) | all paths | Default LocalDB instance `(localdb)\MSSQLLocalDB` |
| **`sqlcmd`** on PATH | create / verify scripts | Ships with SQL Server command-line tools |
| **Node + Playwright** (optional) | screenshots (step 16), some UI smoke | Only the screenshot/UI capture steps need it |
| **Ollama** (optional) | local AI proposals | Never on the critical path; the platform runs without it |

---

## Step 1 — Clone the repository

```powershell
git clone https://github.com/xBlueCrayon/LocalAIFactory.git
cd LocalAIFactory
git checkout ke-008-code-symbols
git log -1 --oneline   # expect 7a35961
```

Alternatively, download the draft prerelease asset from the GitHub Releases page
(`LocalAIFactory-release-20260621-040519.zip`, 16,997,982 bytes,
SHA256 `eac98e2cdef11d7a2958b7b2d5257e0caf00576f0fd12740888dcece22e6e63b`). Verify the checksum before
extracting:

```powershell
Get-FileHash .\LocalAIFactory-release-20260621-040519.zip -Algorithm SHA256
# expect: EAC98E2CDEF11D7A2958B7B2D5257E0CAF00576F0FD12740888DCECE22E6E63B
```

## Step 2 — Review the README

```powershell
Get-Content .\README.md | Select-Object -First 80
```

Read [`README.md`](../README.md) (documentation hub and role index) and this release's index,
[`FINAL_CUSTOMER_HANDOVER_INDEX.md`](FINAL_CUSTOMER_HANDOVER_INDEX.md). The authoritative source of
truth is `MASTER_VISION.md`; the operating contract is `CLAUDE.md`.

## Step 3 — Install prerequisites

Required: **.NET 10 SDK** and one **SQL Server engine** (LocalDB for the demo). Optional: **Ollama**
(local inference) and **Node + Playwright** (screenshots). Confirm what is present:

```powershell
dotnet --version            # expect 10.x
sqlcmd -?                   # confirms sqlcmd is on PATH
node --version              # optional, only for screenshots
ollama list                 # optional, only if you want local AI
```

## Step 4 — Configure appsettings (no secrets)

Point the connection string at your engine by copying the matching example from `database/`. For the
LocalDB demo (trusted auth, no secrets):

```powershell
Copy-Item database\appsettings.LocalDB.example.json src\LocalAIFactory.Web\appsettings.Development.json
```

Other engines: `database\appsettings.SqlExpress.example.json`,
`database\appsettings.FullSqlServer.example.json`. The LocalDB example sets `Qdrant.Enabled=false`,
`Ollama.Enabled=false`, and dev auth for a frictionless local demo. **Data Protection keys** persist
in `./keys` (git-ignored) — preserve that folder across restarts. Never commit connection strings
with credentials; use environment variables or a git-ignored override.

## Step 5 — Create the local database (create-if-absent, never drops)

```powershell
pwsh database\create-localdb.ps1 -Instance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory"
```

This checks `IF DB_ID('LocalAIFactory') IS NULL` and creates only if absent. If the database exists
it **migrates only** — it never drops, truncates, or overwrites. Re-running is safe.

> Full local demo in one command (create → seed → verify), if you prefer:
> ```powershell
> pwsh database\setup-full-local-demo.ps1
> ```

## Step 6 — Apply migrations

Migrations apply **automatically on first run** (step 9). To apply them explicitly first:

```powershell
pwsh database\apply-migrations.ps1 -Instance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory"
```

This release ships **14 EF migrations**. Applying them on an empty database is the expected first
state.

## Step 7 — Seed the included knowledge base

The knowledge base seeds **automatically at startup** (step 9), controlled by
`KnowledgePacks:InstallAllAtStartup` (**default `true`**). With it `true`, the app installs the base
pack plus the three domain packs (**4 packs / 438 items** total). Set it to `false` to seed only the
base pack. To run the install explicitly without leaving the app running:

```powershell
pwsh scripts\knowledge\install-all-knowledge-packs.ps1
```

The install is **idempotent and propose-never-overwrite** — re-running does not duplicate items and
never overwrites curated knowledge.

## Step 8 — Verify the knowledge packs (read-only gate)

```powershell
pwsh scripts\knowledge\verify-all-knowledge-packs.ps1
```

Expected on this release: **4 packs / 438 items**, all curated, unique Uids (live DB verified). The
script exits non-zero on failure, so it is a deployment gate. To export the catalog:

```powershell
pwsh scripts\knowledge\export-knowledge-catalog.ps1
```

## Step 9 — Run the app

```powershell
dotnet run --project src\LocalAIFactory.Web
```

On first startup the app **migrates, seeds, and installs all knowledge packs idempotently**, then
serves on `http://localhost:5000` by default. The four core pages (Home, Projects, Knowledge, Models)
must load on an empty DB, a seeded DB, and in MSSQL-only mode.

## Step 10 — Open the dashboard

Browse to `http://localhost:5000/`. Confirm the dashboard renders quickly and the navigation works.
A quick scripted check:

```powershell
curl -s -o NUL -w "%{http_code} %{time_total}s`n" http://localhost:5000/
```

## Step 11 — Open `/Support`

Browse to `http://localhost:5000/Support`. This is a **read-only** running-instance health page
(it changes nothing). Confirm it loads and shows the instance status at a glance.

## Step 12 — Run the UI smoke test

```powershell
pwsh scripts\poc\ui-smoke-test.ps1
```

Starts the app, GETs the core pages, and asserts there are no 500s. Verified this release: **PASS**
across **11 pages including `/Support`**.

## Step 13 — Run the benchmark smoke (and standard)

```powershell
cd tools\LocalAIFactory.Benchmark
dotnet run -c Release -- --inmemory --suite smoke
dotnet run -c Release -- --inmemory --suite standard
cd ..\..
```

Verified this release: smoke + standard **PASS** (ERP/CRM Gold 6/6, core-banking Gold 6/6,
KYC/AML→approval Gold 7/7). The full validation gate that combines build + test + benchmark + hygiene
is `pwsh scripts\poc\verify-poc.ps1` (verified **PASS**).

## Step 14 — Back up the database

```powershell
pwsh database\backup-database.ps1 -Instance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory"
```

Take a backup before any later upgrade. See [`Backup-Restore-Runbook.md`](Backup-Restore-Runbook.md).

## Step 15 — Verify the restore

```powershell
pwsh database\restore-verify-database.ps1 -Instance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory"
```

Restores the backup into a verification target and confirms the knowledge base is intact, proving the
backup is recoverable (not just written).

## Step 16 — Capture screenshots (optional — needs Node + Playwright)

```powershell
pwsh scripts\docs\capture-screenshots.ps1
```

Drives the running app with Playwright to capture UI screenshots. This release includes **11 real
screenshots**. Requires Node + Playwright (step 3); skip if not installed.

## Step 17 — Build the release

```powershell
pwsh scripts\release\build-release.ps1
```

Produces the publish output that the package step assembles. Expected: build **0 errors**.

## Step 18 — Verify the release package

```powershell
pwsh scripts\release\package-release.ps1
pwsh scripts\release\verify-release-package.ps1
```

`package-release.ps1` assembles the deployable package from the publish output (never the `.git`
source tree); `verify-release-package.ps1` asserts its contents. Verified this release: package
**16.2 MB / 277 files**, `verify-release-package` **PASS**. A manifest can be produced with
`pwsh scripts\release\create-release-manifest.ps1`.

## Step 19 — Simulate a clean install

```powershell
pwsh scripts\release\simulate-clean-install.ps1
pwsh scripts\release\customer-acceptance-check.ps1
```

`simulate-clean-install.ps1` exercises the package as a fresh install would; `customer-acceptance-check.ps1`
runs the acceptance gate. Verified this release: clean-install simulation **PASS**, customer
acceptance **ACCEPTED**. See [`Customer-Acceptance-Test.md`](Customer-Acceptance-Test.md). This is a
**simulation on the build host** — it is not a fresh-server proof.

## Step 20 — Export a support bundle

```powershell
pwsh scripts\support\export-support-bundle.ps1
```

Collects diagnostics, health, and environment info into a single bundle to attach to a support
request. Run the security audit too if you want the scan evidence:

```powershell
pwsh scripts\security\security-audit.ps1
```

Verified this release: security audit **0 HIGH** findings.

## Step 21 — Known blockers for commercial GA (read before relying on this)

This release is a **controlled, operator-assisted demo / pilot** scoped to the proven core. It is
**not** commercial general availability. Honest blockers, each with a stated proof-to-close in
[`Known-Limitations.md`](Known-Limitations.md) and a procedure in
[`ROADMAP_TO_TRUE_20_OF_20.md`](ROADMAP_TO_TRUE_20_OF_20.md):

- **No executed production / IIS / Docker / SQL Express / full-SQL-Server deployment.** Docker is
  **not installed** on the build host; those paths are documented and scripted but **not** host-verified.
  The executable drill is in [`Production-Deployment-Drill-Pack.md`](Production-Deployment-Drill-Pack.md).
- **No enterprise SSO / IdP.** Windows/Negotiate (and guarded dev) auth only — see
  [`SSO_ENTRA_ID_PROOF_PACK.md`](SSO_ENTRA_ID_PROOF_PACK.md).
- **No external penetration test / security review.** Internal audit only (0 HIGH).
- **No signed customer pilot** on sanitized estate data.
- **No trained OCR / CV model** — document and cheque paths are prototypes only.
- **Autonomous fix loop proven on synthetic workspace only** — not on a real repo.
- **No cross-repository estate model.**
- **No licensing enforcement.**

Readiness is reported honestly (mean ~61.2%, max 88, **none at 100**) on the `/Readiness` page and in
[`readiness-scorecard.json`](readiness-scorecard.json). The pilot dimension scores 8/10; local / repo
/ package readiness ~19/20.

---

## Next

- Reproduce the evidence: [`FINAL_LOCAL_DEPLOYMENT_GUIDE.md`](FINAL_LOCAL_DEPLOYMENT_GUIDE.md)
- Production-shaped dry run: [`Production-Deployment-Drill-Pack.md`](Production-Deployment-Drill-Pack.md)
- Path to closing the last gaps: [`ROADMAP_TO_TRUE_20_OF_20.md`](ROADMAP_TO_TRUE_20_OF_20.md)
- SSO proof pack: [`SSO_ENTRA_ID_PROOF_PACK.md`](SSO_ENTRA_ID_PROOF_PACK.md)
