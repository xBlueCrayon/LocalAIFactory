# Quick Start (10-minute path)

The fastest path from a clean checkout to a running instance with seeded knowledge, an imported
repository, an answered question, and a verified database. Commands are PowerShell, run from the
repository root.

> **Screenshots:** the images referenced below (`docs/screenshots/<name>.png`) are **generated
> separately** by the capture script and are **not committed fabricated images**. On this host
> Node/Playwright is not installed, so the screenshot set has not yet been generated — see
> `docs/screenshots/README.md` for the exact enabling commands and the current blocker. Where a
> screenshot is referenced, the surrounding text describes what you should see so the guide is
> complete without the image.

---

## 0. Prerequisites (1 min)

- **.NET 10 SDK**
- **SQL Server** — LocalDB or SQL Express is fine for this walkthrough
- Optional (for Chat): **Ollama** with a model such as `qwen2.5-coder:14b`

The platform runs fully in MSSQL-only mode; you can complete steps 1–6 and 8–9 without any model.

---

## 1. Create the database (1 min)

```powershell
database/create-localdb.ps1
```

Safe by default — it never drops an existing database; it applies migrations only.

---

## 2. Run the app (1 min)

```powershell
dotnet build LocalAIFactory.sln -c Release
dotnet run --project src/LocalAIFactory.Web
```

The app migrates, seeds, and installs the professional base knowledge pack automatically on startup.
Open `http://localhost:5000/`.

> Screenshot: `docs/screenshots/01-home-dashboard.png` — the Home dashboard. You should see DB
> counts, the environment **mode** badge (Minimal / Standard / FullAi), recent activity, and
> navigation into the knowledge and project areas.

---

## 3. Confirm seeded knowledge (1 min)

Open **Base Knowledge** in the left nav. You should see the curated professional summaries grouped by
category, each with sources, jurisdiction (where relevant), and a limitation note.

> Screenshot: `docs/screenshots/04-base-knowledge.png` — the Base Knowledge browser.

To confirm from the database instead of the UI:

```powershell
database/verify-knowledge-base.ps1 -ServerInstance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory"
```

Optionally install an add-on pack (Admin-only) — e.g. `financial-institution-operations-v1` — via the
Admin pack install action; it is idempotent.

---

## 4. Import a repository (2 min)

1. Open **Import Project** and upload a repository ZIP (an Analyst or Admin role is required).
2. Watch progress on the **Imports** page.
3. When complete, open the project from **Projects** to see its summary, imported files, and the
   **coverage / gap** report.

C#/.NET, T-SQL, and Python are structurally understood; other languages are imported and reported as
honest **gaps** — never silently counted as understood.

> Screenshot: `docs/screenshots/06-projects.png` — the Projects list and a project overview.

---

## 5. Ask a question (1 min, needs a model)

Open **Chat**, select the project (or global) context, and ask a plain-language question. The answer
is grounded in approved knowledge first and arrives with sources and a confidence indication. If no
model is configured, skip this step — every other page still works.

---

## 6. View impact — "what breaks if X changes" (1 min)

1. Open **Explore Graph** (or the project graph).
2. Pick a symbol, table, or stored procedure.
3. Run **impact analysis** to see its blast radius across the C#↔SQL bridge, in both directions, with
   confidence and evidence on each result.

This is deterministic and MSSQL-only — no model needed.

> Screenshot: `docs/screenshots/09-graph-explorer.png` — the graph explorer with an impact result.

---

## 7. Check the Support dashboard (30 s)

Open **Support** to see build/version, edition/license, cached service health, DB counts, last
import/audit, disk, and any warnings — the operations view at a glance.

> Screenshot: `docs/screenshots/15-support-health.png` — the Support / health dashboard.

---

## 8. Verify the database directly (30 s)

```powershell
scripts/diagnostics/sql-health-check.ps1
```

Confirms SQL connectivity and database size without altering anything.

---

## 9. Run tests and the benchmark (1 min)

```powershell
dotnet test LocalAIFactory.sln -c Release    # current status: 235/235 pass
scripts/poc/verify-poc.ps1                    # validation harness: PASS
```

> Screenshot: `docs/screenshots/11-benchmarks.png` — the Benchmarks page showing harness status.

---

## What you have now

A running, MSSQL-authoritative instance with curated knowledge, an imported project, structural
understanding with impact analysis, and a passing build/test/benchmark — all local, all reproducible.
For deeper operations see the Operator Manual; for administration see the Admin Manual.
