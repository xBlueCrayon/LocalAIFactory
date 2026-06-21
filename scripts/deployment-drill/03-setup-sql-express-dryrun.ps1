<#
.SYNOPSIS  Deployment drill 03 — set up the SQL Express database. DRY-RUN by default; -Execute creates the DB.
.DESCRIPTION In dry-run it shows the create + verify plan. With -Execute it calls database/create-sqlexpress-db.ps1
             (create-if-absent; never drops) and verifies. Express has no backup COMPRESSION (handled with the
             -Compress opt-in elsewhere).
.PARAMETER Server  SQL Express instance (default .\SQLEXPRESS). .PARAMETER Database  default LocalAIFactory.
#>
param([string]$Server = ".\SQLEXPRESS", [string]$Database = "LocalAIFactory", [switch]$Execute)
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
Write-Host "== SQL Express setup ($Server / $Database) ==" -ForegroundColor Cyan
if (-not $Execute) {
  Write-Host "  DRY-RUN. Would run:" -ForegroundColor Yellow
  Write-Host "    database/create-sqlexpress-db.ps1 -Instance '$Server' -Database '$Database'   (create-if-absent, never drops)"
  Write-Host "    then the app seeds all packs on first start; verify with database/verify-full-install.ps1"
  Write-Host "  Re-run with -Execute on an approved host to create the database." -ForegroundColor Yellow
  return
}
$create = Join-Path $repo "database/create-sqlexpress-db.ps1"
if (Test-Path $create) { & $create -Instance $Server -Database $Database } else { Write-Host "create-sqlexpress-db.ps1 not found" -ForegroundColor Red }
Write-Host "Now start the app once to seed, then run database/verify-full-install.ps1 -Server '$Server'." -ForegroundColor Cyan
