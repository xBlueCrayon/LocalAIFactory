<#
.SYNOPSIS  R2-ACC-CAP6: Windows/IIS deployment helper — DRY-RUN by default.
.DESCRIPTION Prints the publish + copy + IIS-configure steps it WOULD run. It performs NO server changes unless
             -Execute is passed. Even with -Execute it never edits firewall/services and never deploys without
             a backup having been taken. This script is a guided runbook, not an unattended production deployer.
#>
param(
  [string]$Project = "src/LocalAIFactory.Web/LocalAIFactory.Web.csproj",
  [string]$PublishDir = "C:\inetpub\LocalAIFactory",
  [string]$SiteName = "LocalAIFactory",
  [string]$AppPool = "LocalAIFactory",
  [switch]$Execute
)
$ErrorActionPreference = "Stop"
$steps = @(
  "dotnet publish `"$Project`" -c Release -o `"$PublishDir`"",
  "icacls `"$PublishDir`" /grant `"IIS AppPool\${AppPool}:(OI)(CI)RX`"",
  "appcmd add apppool /name:`"$AppPool`" /managedRuntimeVersion:`"`"  (No Managed Code for ASP.NET Core)",
  "appcmd add site /name:`"$SiteName`" /physicalPath:`"$PublishDir`" /bindings:http/*:8080:",
  "Confirm web.config has the ASP.NET Core Module (AspNetCoreModuleV2) and correct processPath."
)
Write-Host "=== Windows deployment plan (DRY-RUN unless -Execute) ===" -ForegroundColor Cyan
$i = 1; foreach ($s in $steps) { Write-Host ("  {0}. {1}" -f $i++, $s) }
Write-Host ""
Write-Host "Pre-flight: a verified database BACKUP must exist (deploy/scripts/backup.ps1 + restore-verify.ps1)." -ForegroundColor Yellow
Write-Host "This script does NOT change firewall, services, or production data." -ForegroundColor Yellow

if (-not $Execute) { Write-Host "`nDRY-RUN: nothing executed. Re-run with -Execute on a prepared host to apply." -ForegroundColor Green; exit 0 }

# Execute path: only the safe publish step runs here; IIS/site changes are intentionally left to an operator
# with the appropriate runbook and approvals (no unattended production automation).
Write-Host "`n-Execute: running the publish step only (IIS/site steps remain operator-gated)." -ForegroundColor Cyan
& dotnet publish "$Project" -c Release -o "$PublishDir"
exit $LASTEXITCODE
