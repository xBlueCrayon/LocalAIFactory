# Backup / Restore Runbook — LocalAIFactory

This runbook uses the committed, **non-destructive** scripts under `database/` (thin wrappers over
`deploy/scripts/`). The guiding rule: **a backup is unproven until it has been verified**, and
**restores never target the production database**.

---

## 1. Take a backup (non-destructive)

```powershell
pwsh database/backup-database.ps1 `
  -ServerInstance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory" -BackupDir "./backups"
```

This wraps `deploy/scripts/backup.ps1`, which runs
`BACKUP DATABASE ... WITH INIT, CHECKSUM` via `sqlcmd`. It **does not drop, overwrite, or truncate**
anything. The output file is timestamped: `./backups/LocalAIFactory-yyyyMMdd-HHmmss.bak`.

- **Trusted connection by default** (`-E`). For SQL auth, pass `-User`/`-Password` (never stored).
- **No compression on Express / LocalDB.** `WITH COMPRESSION` is unsupported on those editions, so it
  is **off by default** for portability. On a full SQL Server edition that supports it, pass
  `-Compress` to add `COMPRESSION` (the options become `INIT, CHECKSUM, COMPRESSION`).

Live-proven on this host: a backup of **69.5 MB** was produced successfully.

---

## 2. Verify the backup (RESTORE VERIFYONLY — non-destructive)

A backup you haven't verified is not a backup you can rely on:

```powershell
pwsh database/restore-verify-database.ps1 -BackupFile "./backups/LocalAIFactory-20260620-120000.bak"
```

This wraps `deploy/scripts/restore-verify.ps1`, whose **default mode runs `RESTORE VERIFYONLY`** —
it touches no live database and only checks that the backup is restorable.

Live-proven on this host: **VERIFY OK** against the 69.5 MB backup.

Add `-FullRestore` to also restore into a clearly-named **verify** database (never production):

```powershell
pwsh database/restore-verify-database.ps1 `
  -BackupFile "./backups/LocalAIFactory-20260620-120000.bak" -FullRestore
```

The verify database defaults to `LocalAIFactory_RestoreVerify`. The script **refuses to target the
production database name**.

---

## 3. Full restore into a verify database (never production)

```powershell
pwsh database/restore-database.ps1 `
  -BackupFile "./backups/LocalAIFactory-20260620-120000.bak" `
  -ServerInstance "(localdb)\MSSQLLocalDB" `
  -ProductionDatabase "LocalAIFactory" `
  -TargetDatabase "LocalAIFactory_RestoreVerify"
```

This runs `RESTORE DATABASE [<TargetDatabase>] ... WITH REPLACE, RECOVERY` and **refuses to restore
over the production database** — if `-TargetDatabase` equals `-ProductionDatabase`, it exits with an
error and changes nothing.

> Restoring *over* production is intentionally **not** a scripted action. It is an operator-gated swap
> performed only after the restored verify database has been validated. See
> `Upgrade-Rollback-Runbook.md` §4.

---

## 4. Backup / restore drill (with live-proven outputs)

Run this drill periodically to prove your backups are restorable end to end.

```powershell
# 1. Back up
pwsh database/backup-database.ps1 -BackupDir "./backups"
#    -> BACKUP OK: ./backups/LocalAIFactory-<stamp>.bak   (live-proven: 69.5 MB)

# 2. Capture the newest backup path
$bak = (Get-ChildItem ./backups/*.bak | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName

# 3. Verify (VERIFYONLY — non-destructive)
pwsh database/restore-verify-database.ps1 -BackupFile $bak
#    -> VERIFY OK   (live-proven)

# 4. Restore into a verify database (never production)
pwsh database/restore-database.ps1 -BackupFile $bak -TargetDatabase "LocalAIFactory_RestoreVerify"
#    -> RESTORE OK -> LocalAIFactory_RestoreVerify

# 5. Validate the restored copy (read-only knowledge-base checks)
pwsh database/verify-knowledge-base.ps1 -Database "LocalAIFactory_RestoreVerify"
#    -> KNOWLEDGE-BASE: VERIFIED   (live-proven baseline: 390 items)
```

If any step exits non-zero, **stop and investigate before trusting the backup**.

---

## 5. What is in the backup — and what is not

The `.bak` covers the **MSSQL database** (the source of truth: knowledge items, packs, projects,
provenance, audit). It does **not** include:

- **Data Protection keys** (`./keys`, git-ignored). Back these up separately — without them, encrypted
  API keys cannot be decrypted after a restore to a different host.
- Qdrant/Ollama state — both are optional and rebuildable; they are not part of the source of truth.

---

## 6. Retention guidance

- Keep enough timestamped backups to cover your recovery window (e.g. daily for 2 weeks, weekly for a
  quarter) — tune to your estate's policy. Filenames are sortable by timestamp.
- Store at least one **verified** copy off the host.
- Always keep the **last verified backup** that immediately precedes any upgrade.
- Verify backups on a schedule (§4), not only when you need to restore.

---

## 7. Before any upgrade

1. Take a fresh backup (§1).
2. **Verify it** with `RESTORE VERIFYONLY` (§2) — do not proceed on an unverified backup.
3. Back up the `./keys` folder (§5).
4. Record the current build/version so you can redeploy it on rollback.

These four steps are the precondition for the upgrade flow in `Upgrade-Rollback-Runbook.md`.
