<#
.SYNOPSIS
  R2-ACC-FINAL: install ALL included knowledge packs into the database.
.DESCRIPTION
  The application installs every pack under knowledge-packs/ at startup (idempotent; propose-never-overwrite;
  KnowledgePacks:InstallAllAtStartup=true by default). This script seeds them WITHOUT leaving the app running:
  it ensures the database exists, starts the Web app just long enough for startup seeding to complete, stops it,
  then verifies the installed counts. Idempotent and safe to re-run. No data is dropped.
.PARAMETER Port  Local port to boot on (default 60398).
.PARAMETER SkipCreate  Skip the create-localdb step (assume DB already exists).
#>
param([int]$Port = 60398, [switch]$SkipCreate)

$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path

if (-not $SkipCreate) {
  $create = Join-Path $repo "database/create-localdb.ps1"
  if (Test-Path $create) { Write-Host "== Ensure database (create-if-absent) ==" -ForegroundColor Cyan; & $create | Out-Host }
}

Write-Host "== Seed packs via app startup (idempotent) ==" -ForegroundColor Cyan
$env:ASPNETCORE_ENVIRONMENT = "Development"
& dotnet build "$repo/src/LocalAIFactory.Web/LocalAIFactory.Web.csproj" -c Release --nologo 2>&1 | Out-Null
Start-Process dotnet -ArgumentList @("run","--project","$repo/src/LocalAIFactory.Web/LocalAIFactory.Web.csproj","-c","Release","--no-build") -WindowStyle Hidden
$up = $false
for ($i = 0; $i -lt 60; $i++) {
  Start-Sleep 2
  try { if ((Invoke-WebRequest -UseBasicParsing "http://localhost:$Port/" -TimeoutSec 5).StatusCode -eq 200) { $up = $true; break } } catch {}
}
if ($up) { Write-Host "  app started; startup seeding ran." -ForegroundColor Green; Start-Sleep 4 }
else { Write-Host "  app did not become ready — check the connection string / LocalDB." -ForegroundColor Red }

Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" -ErrorAction SilentlyContinue |
  Where-Object { $_.CommandLine -match 'LocalAIFactory.Web' } |
  ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
Write-Host "  app stopped."

Write-Host "`n== Verify installed packs ==" -ForegroundColor Cyan
& (Join-Path $PSScriptRoot "verify-all-knowledge-packs.ps1")
exit $LASTEXITCODE
