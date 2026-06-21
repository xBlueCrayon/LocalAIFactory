<#
.SYNOPSIS  MODE-A: create + migrate + seed the IIS-proof database on SQL Server Express. Idempotent, non-destructive.
.DESCRIPTION Creates the deployment database (if absent) on SQL Express, then has the PUBLISHED app migrate it and
             install all knowledge packs by launching the published Web.dll briefly under an ADMIN (trusted)
             identity on a temporary port — this is the "migration-time identity". It then stops the app. Runtime
             least-privilege access for the IIS app-pool identity is granted separately by
             grant-iis-apppool-sql-access.ps1. Never drops or truncates; never touches LocalDB.
.PARAMETER Server   SQL instance (default .\SQLEXPRESS). .PARAMETER Database  default LocalAIFactory_IISProof.
.PARAMETER PublishPath  published app folder (default ./.tmp-publish). .PARAMETER SeedPort  temp port for the seed run.
#>
param(
  [string]$Server = ".\SQLEXPRESS",
  [string]$Database = "LocalAIFactory_IISProof",
  [string]$PublishPath = "",
  [int]$SeedPort = 8096
)
$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot/..").Path
if (-not $PublishPath) { $PublishPath = Join-Path $repo ".tmp-publish" }
function Ok($m){ Write-Host "  [ OK ] $m" -ForegroundColor Green }
function Info($m){ Write-Host "  [INFO] $m" -ForegroundColor Cyan }

Write-Host "== Setup IIS-proof DB ($Server / $Database) ==" -ForegroundColor Cyan
if (-not (Test-Path (Join-Path $PublishPath "LocalAIFactory.Web.dll"))) {
  Write-Host "  Published app not found at $PublishPath — run scripts/release/build-release.ps1 first." -ForegroundColor Red; exit 1
}

# 1. Create the database if absent (additive; never drops).
$exists = (sqlcmd -S $Server -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID('$Database') IS NULL THEN 0 ELSE 1 END" -h -1 2>$null | Out-String).Trim()
if ($exists -ne "1") { sqlcmd -S $Server -Q "IF DB_ID('$Database') IS NULL CREATE DATABASE [$Database];" | Out-Null; Ok "created database $Database" }
else { Info "database $Database already exists (left intact)" }

# 2. Migrate + seed by launching the published app (trusted admin identity) briefly.
$conn = "Server=$Server;Database=$Database;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
$env:ConnectionStrings__DefaultConnection = $conn
$env:ASPNETCORE_ENVIRONMENT = "Development"
$base = "http://localhost:$SeedPort"
Info "launching published app on $base to migrate + seed (migration-time identity = current admin)..."
$p = Start-Process -FilePath "dotnet" -ArgumentList @("LocalAIFactory.Web.dll","--urls",$base) -WorkingDirectory $PublishPath `
     -RedirectStandardOutput (Join-Path $repo ".tmp-iisproof-seed.out.log") -RedirectStandardError (Join-Path $repo ".tmp-iisproof-seed.err.log") -PassThru
try {
  $ready = $false
  for ($i = 0; $i -lt 60; $i++) { Start-Sleep -Seconds 2; try { if ((Invoke-WebRequest -UseBasicParsing "$base/" -TimeoutSec 8).StatusCode -eq 200) { $ready = $true; break } } catch {} }
  if (-not $ready) { Write-Host "  app did not become ready; see .tmp-iisproof-seed.err.log" -ForegroundColor Red; exit 1 }
  # wait for pack install to complete
  for ($i = 0; $i -lt 20; $i++) { $c = (sqlcmd -S $Server -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.KnowledgePacks" -h -1 2>$null | Out-String).Trim(); if ($c -match '^\d+$' -and [int]$c -ge 4) { break }; Start-Sleep -Seconds 3 }
  Ok "app migrated + seeded $Database"
}
finally { if ($p -and -not $p.HasExited) { Stop-Process -Id $p.Id -Force -EA SilentlyContinue } }

# 3. Report counts.
$migs  = (sqlcmd -S $Server -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.__EFMigrationsHistory" -h -1 2>$null | Out-String).Trim()
$packs = (sqlcmd -S $Server -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.KnowledgePacks" -h -1 2>$null | Out-String).Trim()
$items = (sqlcmd -S $Server -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.KnowledgeItems WHERE KnowledgePackId IS NOT NULL" -h -1 2>$null | Out-String).Trim()
Write-Host "  migrations=$migs  packs=$packs  packItems=$items" -ForegroundColor Green
Write-Host "Next: grant-iis-apppool-sql-access.ps1 (runtime least-privilege), then deploy under IIS." -ForegroundColor Yellow
