# Database Operations Runbook — LocalAIFactory

Day-2 operations for the MSSQL store that backs LocalAIFactory. Every script referenced here is
committed under `database/` and is **non-destructive by default**: setup migrates (never drops),
backups never overwrite the live DB, restores refuse the production database name, and the
index-reset is **dry-run by default**.

MSSQL is the source of truth. Qdrant and Ollama are optional and rebuildable and are **not** covered
by these procedures.

---

## 1. Apply migrations (additive)

Migrations are **additive and never destructive**. The app also applies them on startup.

```powershell
# Via the wrapper (LocalDB default connection string)
pwsh database/apply-migrations.ps1

# Against a specific target
pwsh database/apply-migrations.ps1 `
  -ConnectionString "Server=<instance>;Database=LocalAIFactory;Trusted_Connection=True;TrustServerCertificate=True"

# Or directly via EF
dotnet ef database update --project src/LocalAIFactory.Data --startup-project src/LocalAIFactory.Web
```

On this host, **14 EF migrations** are applied (`__EFMigrationsHistory`). Expected:
`MIGRATIONS: applied.`

> Add new migrations only after entity changes **and** approval (CLAUDE.md §6). Default to additive,
> backward-compatible changes; regenerate the `ModelSnapshot` through EF.

---

## 2. Seed / verify the knowledge base

```powershell
# Seed (idempotent) — or just run the app once; startup installs all packs
pwsh scripts/knowledge/install-all-knowledge-packs.ps1 -SkipCreate

# Verify the seeded base (read-only)
pwsh database/verify-knowledge-base.ps1
#   -> KNOWLEDGE-BASE: VERIFIED  (438 baseline items, all curated, 438 provenance, 17 src: tags)

# Full-install verify (migrations + KB + source-pack reconciliation)
pwsh database/verify-full-install.ps1
#   -> VERIFY-FULL-INSTALL: PASS
```

See `docs/Knowledge-Pack-Install-Runbook.md` for details. Seeding is propose-never-overwrite, so it
is safe to re-run.

---

## 3. Backup (non-destructive)

```powershell
pwsh database/backup-database.ps1 `
  -ServerInstance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory" -BackupDir "./backups"
```

- Runs `BACKUP DATABASE ... WITH INIT, CHECKSUM` via `sqlcmd`. Does **not** drop/overwrite/truncate.
- Output is timestamped: `./backups/LocalAIFactory-yyyyMMdd-HHmmss.bak`.
- **No compression on Express / LocalDB** (`COMPRESSION` is unsupported there). It is off by default;
  on a full edition that supports it, pass `-Compress`.
- Live-proven on this host: a **69.5 MB** backup was produced.

Full procedure and evidence: `docs/Database-Backup-Restore-Evidence.md`,
`docs/Backup-Restore-Runbook.md`.

---

## 4. Restore-verify (never targets production)

```powershell
# RESTORE VERIFYONLY — touches no live DB, only checks the backup is restorable
pwsh database/restore-verify-database.ps1 -BackupFile "./backups/LocalAIFactory-<stamp>.bak"
#   -> VERIFY OK  (live-proven)

# Optional: restore into a clearly-named verify DB (never production)
pwsh database/restore-database.ps1 `
  -BackupFile "./backups/LocalAIFactory-<stamp>.bak" `
  -TargetDatabase "LocalAIFactory_RestoreVerify"
```

`restore-database.ps1` **refuses** to restore over the production database name — if
`-TargetDatabase` equals `-ProductionDatabase` it exits with an error and changes nothing. Restoring
*over* production is intentionally a manual, operator-gated swap (see `docs/Upgrade-Rollback-Runbook.md`).

---

## 5. Reset derived structural indexes (dry-run by default)

When structural data (code symbols / edges / references) needs rebuilding, reset only the
**regenerable** derived tables. They rebuild on the next import/consolidation.

```powershell
# DRY-RUN (default): reports counts, clears nothing
pwsh database/reset-derived-indexes.ps1
#   -> Derived structural rows: CodeEdges=.. CodeSymbolReferences=.. CodeSymbols=..
#   -> DRY-RUN: nothing cleared. Re-run with -Execute to clear.

# Execute (clears ONLY CodeEdges, CodeSymbolReferences, CodeSymbols)
pwsh database/reset-derived-indexes.ps1 -Execute
```

**Protected and never touched:** `KnowledgeItems`, `BusinessRules`, `ApprovedCodeSnippets`,
`AuditEvents`, `ProvenanceEvents`, `ImportedFiles`. There is no `DROP DATABASE`.

---

## 6. Health checks

```powershell
# Database reachable + migrations applied + KB verified
pwsh database/verify-full-install.ps1

# Knowledge base only (read-only)
pwsh database/verify-knowledge-base.ps1

# All knowledge packs (offline + live counts)
pwsh scripts/knowledge/verify-all-knowledge-packs.ps1
```

In the running app, service health (Qdrant/Ollama) is read from a **cached snapshot**
(`IServiceHealthCache`) populated by `HealthMonitorService` — never call those services synchronously
on the request path (CLAUDE.md §5). The `/Support` page surfaces these snapshots; see
`docs/Supportability-Dashboard-Guide.md`.

---

## 7. Routine schedule (suggested)

| Task | Frequency | Command |
|---|---|---|
| Backup | Daily (tune to policy) | `database/backup-database.ps1` |
| Restore-verify (VERIFYONLY) | After each backup / weekly | `database/restore-verify-database.ps1` |
| Full restore drill into verify DB | Periodic + before upgrades | `database/restore-database.ps1 -TargetDatabase LocalAIFactory_RestoreVerify` |
| KB / install verify | After deploy + ad hoc | `database/verify-full-install.ps1` |
| Pre-upgrade backup + verify + keys backup | Before every upgrade | see `docs/Backup-Restore-Runbook.md` §7 |

## See also

- `docs/Database-Setup-Guide.md`
- `docs/Database-Backup-Restore-Evidence.md`
- `docs/Backup-Restore-Runbook.md`, `docs/Upgrade-Rollback-Runbook.md`
- `database/README.md`
