# Database & Knowledge-Base Shipping

Repeatable, **safe-by-default** MSSQL creation, migration, seed, verification, and backup/restore for
LocalAIFactory. MSSQL is the source of truth; the Professional Base Knowledge Pack ships as source-controlled
JSON (`knowledge-packs/`) and is **installed into MSSQL** on first app startup, audited via provenance, and
verifiable by script — distinct from imported project knowledge.

## Safety contract

- **No `DROP DATABASE`, ever.** Create scripts CREATE-if-absent and otherwise migrate only.
- **No hard-coded passwords.** Trusted (Integrated) connection by default; SQL auth is supported via
  `-User/-Password` but never forced and never stored in committed config.
- **Existing-DB detection** before any change. Migrations are additive.
- **Backups/restores** verify before trusting; restores target a non-production verify database by default.
- **Derived data reset** is dry-run by default and never touches curated knowledge, audit, or provenance.

## Scripts

| Script | Purpose |
|---|---|
| `create-localdb.ps1` | Prepare the DB on LocalDB (dev/demo) — create-if-absent + migrate. |
| `create-sqlexpress-db.ps1` | Prepare the DB on SQL Express (pilot). |
| `create-full-mssql-db.ps1` | Prepare the DB on a full SQL Server. |
| `apply-migrations.ps1` | `dotnet ef database update` against a target connection (additive). |
| `seed-professional-knowledge-base.ps1` | Migrate + start the app to install the knowledge pack + verify. |
| `verify-knowledge-base.ps1` | Read-only checks: pack row, item count, unique Uids, curated, provenance, source tags. |
| `backup-database.ps1` | Full backup (non-destructive). |
| `restore-database.ps1` | Restore into a **verify** database (refuses to overwrite production). |
| `restore-verify-database.ps1` | `RESTORE VERIFYONLY` (a backup is unproven until verified). |
| `reset-derived-indexes.ps1` | Dry-run report / `-Execute` clear of regenerable structural rows only. |

`appsettings.LocalDB.example.json`, `appsettings.SqlExpress.example.json`, `appsettings.FullSqlServer.example.json`
are connection templates (no secrets).

## Typical first install (LocalDB demo)

```powershell
./database/create-localdb.ps1
./database/seed-professional-knowledge-base.ps1   # migrate + install pack + verify
./database/verify-knowledge-base.ps1              # re-verify any time (read-only)
```

## Backup / restore drill

```powershell
./database/backup-database.ps1 -BackupDir ./backups
./database/restore-verify-database.ps1 -BackupFile ./backups/LocalAIFactory-<stamp>.bak
```

See `docs/Database-and-KnowledgeBase-Ship-Proof.md` for exact commands and observed outputs.
