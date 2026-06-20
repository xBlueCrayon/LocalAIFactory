# Customer Onboarding Guide

This guide walks a pilot customer from zero to first value: install, verify, load a real project, and see
deterministic understanding, impact analysis, coverage, and the Professional Base Knowledge Pack working on
their own hardware. It is written for an admin doing the install with a domain contact available for sign-off.

LocalAIFactory is **local-first and MSSQL-authoritative**. It runs fully offline in MSSQL-only mode — no GPU,
no internet, no Ollama, no Qdrant required.

---

## 1. Prerequisites

- **Windows** (workstation or Windows Server) with the **.NET 10 runtime**.
- **SQL Server** reachable — one of:
  - **LocalDB** (`(localdb)\MSSQLLocalDB`) — fastest for a single-machine demo.
  - **SQL Express** — small shared instance.
  - **Full SQL Server** — for a realistic pilot footprint.
- An admin Windows identity (`DOMAIN\user`) to bootstrap.
- One or more **sanitized** customer repositories (C#/.NET, T-SQL, and/or Python) to import.
- Optional and **not required**: Ollama (verified `qwen2.5-coder` / `deepseek-r1`), Qdrant, a GPU.

See `docs/SQL-Server-Deployment-Guide.md`, `docs/Industrial-Installation-Guide.md`, and
`deploy/docs/hardware-sizing-guide.md` for sizing.

---

## 2. Install the database

Pick the SQL flavour and run the matching `database/*` script from the repo root.

```powershell
# LocalDB (single-machine demo)
./database/create-localdb.ps1

# OR SQL Express
./database/create-sqlexpress-db.ps1

# OR full SQL Server
./database/create-full-mssql-db.ps1

# Apply schema migrations
./database/apply-migrations.ps1
```

The app also **auto-migrates and seeds on startup**, so a first `dotnet run` is enough once the connection
string points at a reachable SQL Server — the scripts above are for a controlled, explicit setup.

---

## 3. Seed and verify the knowledge pack

```powershell
# Install the Professional Base Knowledge Pack (390 curated items, 22 categories, source registry)
./database/seed-professional-knowledge-base.ps1

# Verify it (read-only; exits non-zero on any failure)
./database/verify-knowledge-base.ps1
```

Expected: `KNOWLEDGE-BASE: VERIFIED`. The verifier checks the pack row, baseline item count, unique Uids, all
items curated, pack-origin provenance, and that the source registry is referenced (`src:` tags).

---

## 4. Start the app and confirm it's healthy

```powershell
dotnet run --project src/LocalAIFactory.Web
```

In a separate shell, confirm core pages return quickly (MSSQL-only; none should hang):

```powershell
./scripts/poc/ui-smoke-test.ps1
# or against a running instance:
./scripts/release/post-install-healthcheck.ps1 -Url http://localhost:8080
```

Open the dashboard in a browser. Home, Projects, Base Knowledge, Readiness, and Admin must all load in under a
second.

---

## 5. Set up admins and users

1. Set the bootstrap admin in configuration before first login:

   ```json
   "Security": { "BootstrapAdmin": "DOMAIN\\your-admin" }
   ```

   That identity is provisioned **Admin** on first login (and re-asserted every login for recovery).
2. All other first-seen users are **deny-by-default**: role **Viewer**, **no project access**.
3. As Admin, go to **/Users** to assign roles (Viewer / Analyst / Admin) and grant per-project access.
4. Every grant/revoke and role change is written to the **append-only audit trail** at **/Audit**.

See `docs/08-Security.md` and `docs/Admin-Guide.md` for the full RBAC model.

---

## 6. Load a project (import wizard)

1. Go to the **Import** wizard in the UI (Admin-only; the action is audited).
2. Upload a repository ZIP of a sanitized customer project.
3. The ingestion pipeline runs: it detects binary content, decodes text honestly (BOM/UTF-8/Latin-1), and
   processes each file in a bulletproof loop — **a bad file is recorded as a skip, never an aborted job, and a
   bad repository never crashes the platform.**
4. When the job reaches **Completed**, open the project to see its coverage/gap report.

Imperfect repositories are expected and tolerated. Skips are bucketed (binary, oversized, non-UTF-8, malformed)
and reported honestly — there are no silent zeros.

---

## 7. First value

With a project imported, demonstrate the three core proofs:

- **Coverage & gap** — open the project's coverage page. Every file's extraction outcome is recorded; gaps are
  reported, not hidden. C#/T-SQL/Python are supported; other languages show as honest gaps.
- **Graph & impact analysis** — open the graph explorer, pick a symbol, table, or stored procedure, and run
  **"what breaks if this changes."** The C#↔SQL and Python↔SQL bridges give blast radius in both directions,
  with confidence and evidence.
- **Base Knowledge** — search the 390-item pack (e.g. payments, leasing, IFRS, Mauritius banking, security).
  Each item shows its category, sources (`src:` tags), and an explicit limitation note.

---

## 8. Success checkpoints

Onboarding is complete when the customer can confirm all of these on their own instance:

- [ ] Database created and migrated (`database/*` scripts exit 0).
- [ ] `KNOWLEDGE-BASE: VERIFIED` from `database/verify-knowledge-base.ps1`.
- [ ] Core pages load in <1s, MSSQL-only (`ui-smoke-test.ps1` / health check pass).
- [ ] Bootstrap admin works; at least one Analyst and one Viewer provisioned with project grants.
- [ ] At least one customer repository imported to **Completed** with a coverage/gap report.
- [ ] An impact-analysis "what breaks if X changes" scenario returns correct blast radius.
- [ ] Base Knowledge search returns relevant, source-attributed results.
- [ ] `scripts/poc/verify-poc.ps1` passes on the customer instance.

---

## 9. Honest limits to set expectations at kickoff

- C#/T-SQL/Python only; other languages are reported gaps. Understanding is **syntactic**, not a full semantic
  model.
- OCR / cheque / PDF and forecasting are **prototypes/design**, not shipped engines.
- **No SSO** (Windows auth only); **no cross-repository estate model**; **no proven production deployment**.
- The autonomous workspace is **dry-run + allowlisted** only — it executes nothing destructive.
- Domain knowledge is advisory/awareness — **not legal, regulatory, tax, audit, or financial advice**, and
  not a compliance or certification claim.
