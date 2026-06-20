# Upgrade / Rollback Runbook — LocalAIFactory

This runbook describes how to move LocalAIFactory to a new build and how to roll back safely. It
relies on two facts proven by the committed scripts:

1. **EF migrations are additive and never destructive** — upgrades do not drop or rewrite data.
2. The app **migrates, seeds, and installs the knowledge pack idempotently on startup**, so deploying
   a new build and starting it converges the schema and content automatically.

Every destructive action (restoring over production, removing files) is **operator-gated**. There is
no unattended destructive automation.

---

## 1. Upgrade policy: additive migrations only

- New builds ship **additive, backward-compatible** EF migrations.
- `dotnet ef database update` (via `database/apply-migrations.ps1`, or automatically on app startup)
  **creates the database if absent and never drops or truncates**.
- **No destructive migration** (drop/rename column or table, lossy type change, data deletion) ships
  without explicit human approval. Because migrations are additive, the **previous build remains
  compatible with the upgraded schema** — which is what makes the rollback in §4 safe.

---

## 2. Pre-upgrade checklist

From `Backup-Restore-Runbook.md` §7:

```powershell
# 1. Fresh backup
pwsh database/backup-database.ps1 -BackupDir "./backups"

# 2. Verify it is restorable (non-destructive)
$bak = (Get-ChildItem ./backups/*.bak | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
pwsh database/restore-verify-database.ps1 -BackupFile $bak     # -> VERIFY OK
```

Also:

- Back up the **Data Protection keys** folder `./keys` (git-ignored) so encrypted secrets survive.
- **Record the current build** (commit/tag and the published artifact) so you can redeploy it on rollback.

Do not proceed on an unverified backup.

---

## 3. Upgrade steps

```powershell
# 1. (Pre-flight) Backup + verify — see §2.

# 2. Build/publish the new release
pwsh scripts/release/build-release.ps1 -Output ".\.tmp-publish"
#    (add -SelfContained -Runtime win-x64 for a self-contained build)

# 3. Deploy the new build
#    - IIS: stop the app pool, replace the publish folder, KEEP ./keys, restart.
#           Review deploy/scripts/windows-deploy.ps1 (dry-run) first; site changes are operator-gated.
#    - Console: replace the deployed folder and restart `dotnet run --project src/LocalAIFactory.Web`.

# 4. Start the app — it auto-migrates, seeds, and installs the knowledge pack idempotently on startup.
#    (To do the migration as an explicit step instead:)
pwsh database/apply-migrations.ps1 `
  -ConnectionString "Server=YOUR_SQL_HOST;Database=LocalAIFactory;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true"

# 5. Verify
pwsh database/verify-knowledge-base.ps1 -ServerInstance "YOUR_SQL_HOST" -Database "LocalAIFactory"
pwsh scripts/release/post-install-healthcheck.ps1 -Url "http://localhost:8080"
```

A successful upgrade ends with `KNOWLEDGE-BASE: VERIFIED` and `HEALTH: OK`.

> **Preserve `./keys` across the deployment.** If it is wiped, encrypted API keys become unreadable
> and users are logged out. See the IIS guide §6.

---

## 4. Rollback

Rollback is **validate-then-swap**, never a blind overwrite of production.

```powershell
# 1. Restore the last VERIFIED pre-upgrade backup into a VERIFY database (never production)
pwsh database/restore-database.ps1 `
  -BackupFile "./backups/LocalAIFactory-<pre-upgrade-stamp>.bak" `
  -ProductionDatabase "LocalAIFactory" `
  -TargetDatabase "LocalAIFactory_RestoreVerify"
#    The script REFUSES to restore over the production database name.

# 2. Validate the restored copy (read-only)
pwsh database/verify-knowledge-base.ps1 -Database "LocalAIFactory_RestoreVerify"
#    -> KNOWLEDGE-BASE: VERIFIED

# 3. Operator-approved swap (manual, gated):
#    - Stop the app / app pool.
#    - Take the verify DB to production by an approved DBA procedure
#      (e.g. rename, or restore the verified .bak over production under change control).
#      This is intentionally NOT scripted — it is the one destructive step and requires sign-off.

# 4. Redeploy the PRIOR build recorded in §2 (the prior build is compatible with the additive schema).

# 5. Re-verify
pwsh database/verify-knowledge-base.ps1 -Database "LocalAIFactory"
pwsh scripts/release/post-install-healthcheck.ps1 -Url "http://localhost:8080"
```

Why this is safe:

- The restore scripts **refuse to target production** — you always validate a copy first.
- Because the migration was additive, **the prior build still runs against the upgraded schema**, so
  rolling the application back does not require a destructive schema downgrade.

---

## 5. Knowledge pack: idempotent re-install

You do **not** need a separate step to reinstall the knowledge pack after an upgrade or rollback —
the app installs it idempotently on startup, and `verify-knowledge-base.ps1` enforces:

- no duplicate `Uid`s,
- baseline count ≥ minimum (default 100; live-proven baseline here is **390 items**).

If you want to force the install/verify as an explicit scripted step:

```powershell
pwsh database/seed-professional-knowledge-base.ps1 -Instance "YOUR_SQL_HOST" -Database "LocalAIFactory"
```

Re-running it does **not** duplicate items.

---

## 6. No-destructive-migration policy (summary)

- Default to **additive, backward-compatible** schema changes.
- Dropping/renaming columns or tables, lossy type changes, or data deletion require **explicit human
  approval** before they ship.
- The create scripts are **CREATE-if-absent, never drop**; migrations **never truncate**.
- Restores **never target production**; the production swap is a separate operator-approved step.
- Uninstall is a **dry-run** (`scripts/release/uninstall-dryrun.ps1`) that removes nothing and never
  drops the database — data removal is always a separate, explicit, operator-approved action.
