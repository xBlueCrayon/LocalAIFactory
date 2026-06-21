<#
.SYNOPSIS  Deployment drill 07 — roll back a deployment. DRY-RUN by default; -Execute is operator-gated.
.DESCRIPTION In dry-run it prints the rollback plan: stop the site, restore the previous app/ folder from the
             timestamped backup taken by 05, and (only if the operator confirms) restore the database from a
             SQL backup. It NEVER drops a database and NEVER deletes user data. The DB restore step is the most
             destructive action in the pack, so it stays manual and confirmation-gated even under -Execute.
.PARAMETER PhysicalPath  IIS physical path (default C:\inetpub\LocalAIFactory).
.PARAMETER PreviousApp   Path to the previous app/ backup to restore (default: newest *.bak-app under PhysicalPath).
.PARAMETER DbBackup      Optional SQL .bak to restore (operator must pass it explicitly; never auto-selected).
#>
param(
  [string]$PhysicalPath = "C:\inetpub\LocalAIFactory",
  [string]$PreviousApp = "",
  [string]$DbBackup = "",
  [switch]$Execute
)
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
Write-Host "== Rollback plan ($PhysicalPath) ==" -ForegroundColor Cyan
@(
  "1. Stop the IIS site / app pool so no requests hit a half-rolled-back app.",
  "2. Restore the previous published app/ from its timestamped backup (taken before 05 deployed).",
  "3. Restart the site and run 06-run-healthchecks.ps1 to confirm the previous version is healthy.",
  "4. (DB) ONLY if the new release ran a migration that must be reverted: restore the SQL backup taken in step 14",
  "   of the handover walkthrough. This is destructive and stays manual — pass -DbBackup and confirm at the prompt."
) | ForEach-Object { Write-Host "  $_" }

if (-not $PreviousApp) {
  $PreviousApp = (Get-ChildItem "$PhysicalPath" -Filter "*.bak-app" -Directory -EA SilentlyContinue | Sort-Object LastWriteTime -Desc | Select-Object -First 1)?.FullName
}
Write-Host "`n  App backup to restore : $(if ($PreviousApp) { $PreviousApp } else { '(none found — nothing to roll back to)' })"
Write-Host "  DB backup to restore  : $(if ($DbBackup) { $DbBackup } else { '(none supplied — DB left untouched)' })"

if (-not $Execute) {
  Write-Host "`n  DRY-RUN. No changes made. Re-run elevated with -Execute on the approved host to apply the app rollback." -ForegroundColor Yellow
  Write-Host "  The DB restore is never performed automatically; do it manually with database/restore-database.ps1 if required." -ForegroundColor Yellow
  return
}

if (-not $PreviousApp -or -not (Test-Path $PreviousApp)) {
  Write-Host "`n  -Execute requested but no previous app backup found — refusing to delete the current app. Aborting." -ForegroundColor Red
  exit 1
}
Write-Host "`n  -Execute requested: restoring app from '$PreviousApp'. Stop the site first, then this copies the backup back." -ForegroundColor Yellow
Copy-Item "$PreviousApp/*" $PhysicalPath -Recurse -Force
Write-Host "  App restored. Start the IIS site, then run 06-run-healthchecks.ps1." -ForegroundColor Green
if ($DbBackup) {
  Write-Host "`n  DB rollback is destructive and NOT performed by this drill. To restore the database, run, with the operator present:" -ForegroundColor Yellow
  Write-Host "    pwsh database/restore-database.ps1 -BackupFile '$DbBackup'" -ForegroundColor Yellow
}
