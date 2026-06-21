<#
.SYNOPSIS  Deployment drill 04 — configure IIS site + app pool. DRY-RUN by default; -Execute is operator-gated.
.DESCRIPTION In dry-run it prints the IIS plan and delegates to the existing dry-run installer when present. It
             does NOT change IIS unless -Execute is passed (and even then, defers destructive site changes to the
             operator). Key real-world risks (documented): the ASP.NET Core Hosting Bundle must be installed, and
             the app-pool identity needs its own least-privilege SQL login (Windows-auth / trusted connection).
.PARAMETER SiteName  default LocalAIFactory. .PARAMETER Port  default 8080. .PARAMETER PhysicalPath  publish dir.
#>
param([string]$SiteName = "LocalAIFactory", [int]$Port = 8080, [string]$PhysicalPath = "C:\inetpub\LocalAIFactory", [switch]$Execute)
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
Write-Host "== IIS site plan ($SiteName :$Port -> $PhysicalPath) ==" -ForegroundColor Cyan
@(
  "1. Install ASP.NET Core 10 Hosting Bundle (else IIS 500.31/502.5).",
  "2. Create app pool '$SiteName' (No Managed Code; identity = a domain/service account with a least-privilege SQL login).",
  "3. Create site '$SiteName' bound to :$Port with physical path '$PhysicalPath' (the published app).",
  "4. Grant the app-pool identity read/execute on the path and a SQL login mapped to db_datareader/db_datawriter + EXECUTE.",
  "5. Point appsettings connection string at the target SQL instance (Trusted_Connection or a dedicated login)."
) | ForEach-Object { Write-Host "  $_" }
$iisDry = Join-Path $repo "scripts/release/install-windows-server-iis-dryrun.ps1"
if (Test-Path $iisDry) { Write-Host "`nDelegating to existing dry-run installer:" -ForegroundColor Cyan; & $iisDry }
if (-not $Execute) { Write-Host "`nDRY-RUN only. Re-run elevated with -Execute on an approved host to apply." -ForegroundColor Yellow }
else { Write-Host "`n-Execute requested: apply the steps above with the operator present; this drill does not silently reconfigure IIS." -ForegroundColor Yellow }
