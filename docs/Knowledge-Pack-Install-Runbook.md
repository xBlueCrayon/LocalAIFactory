# Knowledge-Pack Install Runbook — LocalAIFactory

How the included knowledge base is installed into MSSQL, how to install it on demand, how to verify
it, and how to regenerate the human-readable catalog. All scripts here are **idempotent** and
**non-destructive** — they never drop or overwrite curated knowledge.

The included base is **4 packs / 438 items**:

| Pack (folder) | Version | Items |
|---|---|---:|
| Professional Base Knowledge Pack (`professional-base-v1`) | 1.2.0 | 390 |
| Financial Institution Operations v1 (`financial-institution-operations-v1`) | 1.0.0 | 16 |
| KYC AML Transaction Approval v1 (`kyc-aml-transaction-approval-v1`) | 1.0.0 | 16 |
| Market Intelligence and Forecasting v1 (`market-intelligence-forecasting-v1`) | 1.0.0 | 16 |

---

## 1. How packs install (startup auto-install)

The application installs **every** pack under `knowledge-packs/` **on startup**, controlled by the
config flag `KnowledgePacks:InstallAllAtStartup` (**default `true`**). Installation is:

- **Idempotent.** Re-running startup does not duplicate items; already-installed packs are recognized
  and skipped. UIDs are stable GUIDs, so re-seeding is a no-op for existing items.
- **Propose-never-overwrite.** The installer does not overwrite curated knowledge already in the
  database. New content is added; existing curated rows are left intact. Permanence rules protect
  curated knowledge from being silently replaced.
- **Additive.** No pack install drops or truncates anything.

For most deployments, **just run the app once** against a reachable SQL Server and the full base is
seeded automatically. The scripts below exist for operators who want to seed without leaving the app
running, or to verify on demand.

---

## 2. Install on demand (without leaving the app running)

```powershell
# Ensures the LocalDB database exists, boots the app just long enough for startup
# seeding to run, stops it, then verifies the installed counts. Idempotent.
pwsh scripts/knowledge/install-all-knowledge-packs.ps1

# If the database already exists, skip the create step:
pwsh scripts/knowledge/install-all-knowledge-packs.ps1 -SkipCreate
```

What it does:

1. (Unless `-SkipCreate`) runs `database/create-localdb.ps1` to create the database if absent
   (it never drops an existing one).
2. Builds the Web app and starts it hidden; startup applies EF migrations and installs all packs.
3. Waits for the app to answer, then stops the process.
4. Runs `scripts/knowledge/verify-all-knowledge-packs.ps1` and exits with its status.

Expected tail:

```
== Verify installed packs ==
Packs: 4 | items: 438 | distinct UIDs: 438
  [ OK ] DB has 4 installed pack(s), 438 pack item(s)
VERIFY-ALL-KNOWLEDGE-PACKS: PASS
```

> For a complete first-run path (create DB → migrate → seed → verify) in one command, use
> `database/setup-full-local-demo.ps1` (see `docs/Database-Setup-Guide.md`).

---

## 3. Verify the install

### Offline pack validation (no DB)

```powershell
pwsh scripts/knowledge/verify-all-knowledge-packs.ps1 -Server ""
```

Validates manifests, UIDs, no within/cross-pack collisions, and limitation+tags. See
`docs/Knowledge-Pack-Validation-Report.md` for the full result set.

### Live database verification

```powershell
pwsh database/verify-knowledge-base.ps1
#   -> KNOWLEDGE-BASE: VERIFIED  (438 baseline items, all curated, 438 provenance, 17 src: tags)

pwsh database/verify-full-install.ps1
#   -> VERIFY-FULL-INSTALL: PASS (14 migrations, KB verified, source packs match live counts)
```

---

## 4. Regenerate the human-readable catalog

```powershell
pwsh scripts/knowledge/export-knowledge-catalog.ps1
```

This is **read-only against the repo's source packs** (no database needed). It reads each
`knowledge-packs/<pack>/manifest.json` plus its category files and writes:

- `docs/Included-Knowledge-Base-Catalog.md` — a Markdown catalog: a summary line
  (`Packs: 4 · Total items: 438`), a packs table (name, version, item count, review status), and a
  per-pack per-category breakdown.
- `docs/Included-Knowledge-Base-Catalog.json` — the same data machine-readable
  (`packCount`, `totalItems`, and per-pack categories).

Expected tail:

```
Catalog written:
  .../docs/Included-Knowledge-Base-Catalog.md
  .../docs/Included-Knowledge-Base-Catalog.json
Packs: 4 | Total items: 438
```

---

## 5. Configuration reference

| Setting | Default | Effect |
|---|---|---|
| `KnowledgePacks:InstallAllAtStartup` | `true` | Install every pack under `knowledge-packs/` on app startup (idempotent). Set `false` to suppress automatic seeding and install on demand via the script in §2. |

The packs themselves live under `knowledge-packs/` and are shipped in the release package
(see `docs/Published-Package-Contents.md`).

---

## 6. Honesty notes

- Installation is **additive and idempotent**; it protects existing curated knowledge
  (propose-never-overwrite). It is safe to re-run.
- The packs contain original professional summaries with explicit limitation notes and make **no**
  regulatory, financial, or fraud-detection claim.

## See also

- `docs/Knowledge-Pack-Validation-Report.md`
- `docs/Database-Setup-Guide.md`
- `docs/Knowledge-Pack-Authoring-Guide.md`
