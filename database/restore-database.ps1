<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: restore a backup into a NON-production verify database (safe by default).
.DESCRIPTION Refuses to overwrite the production database name. Restores into a clearly-named verify database
             so a restore can be validated without risking live data.
#>
param(
  [Parameter(Mandatory = $true)][string]$BackupFile,
  [string]$ServerInstance = "(localdb)\MSSQLLocalDB",
  [string]$ProductionDatabase = "LocalAIFactory",
  [string]$TargetDatabase = "LocalAIFactory_RestoreVerify"
)
$ErrorActionPreference = "Stop"
if ($TargetDatabase -eq $ProductionDatabase) { Write-Host "Refusing to restore over the production database [$ProductionDatabase]." -ForegroundColor Red; exit 1 }
if (-not (Test-Path $BackupFile)) { Write-Host "Backup not found: $BackupFile" -ForegroundColor Red; exit 1 }
Write-Host "Restoring [$BackupFile] -> [$TargetDatabase] (not production)" -ForegroundColor Cyan
sqlcmd -S "$ServerInstance" -E -C -b -Q "RESTORE DATABASE [$TargetDatabase] FROM DISK = N'$BackupFile' WITH REPLACE, RECOVERY;"
if ($LASTEXITCODE -eq 0) { Write-Host "RESTORE OK -> $TargetDatabase" -ForegroundColor Green; exit 0 } else { exit 1 }
