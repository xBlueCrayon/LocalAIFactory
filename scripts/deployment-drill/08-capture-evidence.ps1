<#
.SYNOPSIS  Deployment drill 08 — capture deployment evidence into a bundle. READ-ONLY.
.DESCRIPTION Collects the evidence an operator needs to sign off a deployment: host facts, page health, DB/KB
             verification output, and (if present) the support bundle. Writes everything under an evidence folder.
             Changes nothing on the host or in the database.
.PARAMETER AppUrl  Base URL of the deployed app. .PARAMETER Server / Database  SQL target. .PARAMETER OutDir  evidence root.
#>
param(
  [string]$AppUrl = "http://localhost:8080",
  [string]$Server = ".\SQLEXPRESS",
  [string]$Database = "LocalAIFactory",
  [string]$OutDir = ""
)
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
if (-not $OutDir) { $OutDir = Join-Path $repo ".tmp-deployment-evidence" }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
Write-Host "== Capture deployment evidence -> $OutDir (read-only) ==" -ForegroundColor Cyan

# 1. Host facts
$hostInfo = [ordered]@{
  capturedUtc   = (Get-Date).ToUniversalTime().ToString("o")
  computerName  = $env:COMPUTERNAME
  os            = (Get-CimInstance Win32_OperatingSystem -EA SilentlyContinue).Caption
  dotnet        = (& dotnet --version 2>$null)
  appUrl        = $AppUrl
  sqlServer     = $Server
  database      = $Database
}
$hostInfo | ConvertTo-Json | Set-Content (Join-Path $OutDir "host.json")
Write-Host "  [ OK ] host.json"

# 2. Page health
$pages = @("/","/Support","/Readiness","/BaseKnowledge")
$rows = foreach ($p in $pages) {
  try { $c = (Invoke-WebRequest -UseBasicParsing "$AppUrl$p" -TimeoutSec 20).StatusCode } catch { $c = 0 }
  [pscustomobject]@{ path = $p; status = $c }
}
$rows | ConvertTo-Json | Set-Content (Join-Path $OutDir "page-health.json")
$rows | ForEach-Object { Write-Host "  page $($_.path) -> $($_.status)" }

# 3. DB / knowledge-base verification (delegates to the existing read-only verifier)
$vfi = Join-Path $repo "database/verify-full-install.ps1"
if (Test-Path $vfi) {
  & $vfi -Server $Server -Database $Database *> (Join-Path $OutDir "verify-full-install.txt")
  Write-Host "  [ OK ] verify-full-install.txt (exit $LASTEXITCODE)"
}

# 4. Support bundle (if the support exporter is present)
$sb = Join-Path $repo "scripts/support/export-support-bundle.ps1"
if (Test-Path $sb) {
  Write-Host "  Running support-bundle exporter (read-only)..." -ForegroundColor Cyan
  & $sb *> (Join-Path $OutDir "support-bundle.txt")
  Write-Host "  [ OK ] support-bundle.txt"
}

Write-Host "`nEvidence captured under: $OutDir" -ForegroundColor Green
Write-Host "Attach this folder to the deployment sign-off record. Nothing on the host was modified." -ForegroundColor Yellow
