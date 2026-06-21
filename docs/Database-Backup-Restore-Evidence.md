# Database Backup / Restore Evidence — LocalAIFactory

This document records the **observed evidence** from a real backup/restore exercise on the build
host, the **edition caveat** for compression, and **how to reproduce** each result. It complements the
procedural `docs/Backup-Restore-Runbook.md` with the concrete outputs that were captured.

Guiding rule: *a backup is unproven until it has been verified, and restores never target the
production database.*

---

## 1. Evidence summary

| Step | Script | Observed result |
|---|---|---|
| Backup | `database/backup-database.ps1` | **BACKUP OK — 69.5 MB** `.bak` produced |
| Restore-verify (VERIFYONLY) | `database/restore-verify-database.ps1` | **VERIFY OK** against the 69.5 MB backup |
| Knowledge-base re-check on restored copy | `database/verify-knowledge-base.ps1` | **KNOWLEDGE-BASE: VERIFIED** |

All steps are non-destructive. `RESTORE VERIFYONLY` touches no live database; the full-restore path
(when used) targets a clearly-named **verify** database and refuses the production name.

---

## 2. Backup — 69.5 MB (live-proven)

```powershell
pwsh database/backup-database.ps1 `
  -ServerInstance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory" -BackupDir "./backups"
```

- Wraps `BACKUP DATABASE ... WITH INIT, CHECKSUM` via `sqlcmd`. No drop/overwrite/truncate of the
  live database.
- Output is timestamped: `./backups/LocalAIFactory-yyyyMMdd-HHmmss.bak`.
- **Observed on this host: a 69.5 MB backup was produced successfully.**

---

## 3. Restore-verify — VERIFYONLY OK (live-proven)

```powershell
$bak = (Get-ChildItem ./backups/*.bak | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
pwsh database/restore-verify-database.ps1 -BackupFile $bak
```

- Default mode runs `RESTORE VERIFYONLY` — it only checks that the backup is restorable and **touches
  no live database**.
- **Observed: VERIFY OK** against the 69.5 MB backup.

Optionally, a full restore into a non-production verify database:

```powershell
pwsh database/restore-database.ps1 -BackupFile $bak -TargetDatabase "LocalAIFactory_RestoreVerify"
pwsh database/verify-knowledge-base.ps1 -Database "LocalAIFactory_RestoreVerify"
#   -> KNOWLEDGE-BASE: VERIFIED
```

`restore-database.ps1` refuses to restore over the production database name (if `-TargetDatabase`
equals `-ProductionDatabase`, it exits with an error and changes nothing).

---

## 4. Compression caveat (Express / LocalDB)

**Express and LocalDB editions do not support `WITH COMPRESSION`.** Because the build/verification
host uses LocalDB, compression is **off by default** for portability — the 69.5 MB figure above is an
**uncompressed** backup.

- On those editions, leave compression off (the default). The backup options are `INIT, CHECKSUM`.
- On a full SQL Server edition that supports it, pass **`-Compress`** to add `COMPRESSION`; the
  options become `INIT, CHECKSUM, COMPRESSION` and the resulting `.bak` is smaller.

This is handled cleanly via the opt-in `-Compress` switch — the script does not assume an edition
that supports compression.

---

## 5. Reproduce the full drill

```powershell
# 1. Back up
pwsh database/backup-database.ps1 -BackupDir "./backups"
#    -> BACKUP OK   (live-proven: 69.5 MB, uncompressed on LocalDB)

# 2. Newest backup path
$bak = (Get-ChildItem ./backups/*.bak | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName

# 3. Verify (VERIFYONLY — non-destructive)
pwsh database/restore-verify-database.ps1 -BackupFile $bak
#    -> VERIFY OK   (live-proven)

# 4. Restore into a verify DB (never production) and re-check the KB
pwsh database/restore-database.ps1 -BackupFile $bak -TargetDatabase "LocalAIFactory_RestoreVerify"
pwsh database/verify-knowledge-base.ps1 -Database "LocalAIFactory_RestoreVerify"
#    -> KNOWLEDGE-BASE: VERIFIED
```

If any step exits non-zero, **stop and investigate before trusting the backup**.

---

## 6. What the backup covers — and what it does not

The `.bak` covers the **MSSQL database** (the source of truth: knowledge items, packs, projects,
provenance, audit). It does **not** include:

- **Data Protection keys** (`./keys`, git-ignored) — back these up separately, or encrypted API keys
  cannot be decrypted after restore to a different host.
- Qdrant / Ollama state — optional and rebuildable; not part of the source of truth.

---

## 7. Honesty notes

- The 69.5 MB / VERIFY OK figures are from a **real run on the LocalDB build host**. They are not a
  benchmark and do not represent any other edition or dataset size.
- Compression effectiveness on a full SQL Server edition has **not** been measured here; only the
  opt-in switch is provided.
- This is backup/restore **evidence**, not a tested production HA/DR plan. See
  `docs/MSSQL-Production-Readiness.md`.

## See also

- `docs/Backup-Restore-Runbook.md` — full procedure + retention guidance.
- `docs/Database-Operations-Runbook.md` — day-2 operations.
- `docs/Upgrade-Rollback-Runbook.md` — pre-upgrade backup gate.

## MULTI-AGENT-HARDENING evidence (2026-06-21, SQL Express)

The backup/restore path was re-verified against the **SQL Express** deployment DB used by the IIS pilot
(`LocalAIFactory_IISProof`) — a disposable, isolated DB (the main LocalDB was untouched):

| Step | Command | Result |
|---|---|---|
| Backup | `database/backup-database.ps1 -Server .\SQLEXPRESS -Database LocalAIFactory_IISProof` | **OK** — 1,890 pages → `./backups/*.bak` (git-ignored) |
| Restore verify | `database/restore-verify-database.ps1 -BackupFile <bak>` | **VERIFY OK** — backup set valid/restorable (`RESTORE VERIFYONLY`) |

`backups/` is **git-ignored**; `.bak` files are never committed. No destructive restore was performed over
an active database.
