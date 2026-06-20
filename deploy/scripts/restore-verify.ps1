<#
.SYNOPSIS  R2-ACC-CAP6: verify a database backup is restorable (a backup is unproven until verified).
.DESCRIPTION Default mode runs RESTORE VERIFYONLY — NON-DESTRUCTIVE (it touches no live database). Use
             -FullRestore to restore into a clearly-named *verify* database (never the production database);
             it refuses to target the production database name.
#>
param(
  [Parameter(Mandatory = $true)][string]$BackupFile,
  [string]$ServerInstance = "(localdb)\MSSQLLocalDB",
  [string]$ProductionDatabase = "LocalAIFactory",
  [switch]$FullRestore,
  [string]$VerifyDatabase = "LocalAIFactory_RestoreVerify"
)
$ErrorActionPreference = "Stop"
if (-not (Test-Path $BackupFile)) { Write-Host "Backup file not found: $BackupFile" -ForegroundColor Red; exit 1 }
if ($VerifyDatabase -eq $ProductionDatabase) { Write-Host "Refusing to restore over the production database." -ForegroundColor Red; exit 1 }

Write-Host "RESTORE VERIFYONLY on $BackupFile"
sqlcmd -S "$ServerInstance" -E -C -b -Q "RESTORE VERIFYONLY FROM DISK = N'$BackupFile';"
if ($LASTEXITCODE -ne 0) { Write-Host "VERIFY FAILED" -ForegroundColor Red; exit 1 }
Write-Host "VERIFY OK" -ForegroundColor Green

if ($FullRestore) {
  Write-Host "Restoring into verify database [$VerifyDatabase] (NOT production)"
  $sql = "RESTORE DATABASE [$VerifyDatabase] FROM DISK = N'$BackupFile' WITH REPLACE, RECOVERY;"
  sqlcmd -S "$ServerInstance" -E -C -b -Q "$sql"
  if ($LASTEXITCODE -eq 0) { Write-Host "RESTORE-TO-VERIFY OK: $VerifyDatabase" -ForegroundColor Green } else { exit 1 }
}
exit 0
