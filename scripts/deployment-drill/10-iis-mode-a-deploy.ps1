<#
.SYNOPSIS  Deployment drill 10 — MODE A: deploy the published app under IIS. DRY-RUN by default; -Execute applies.
.DESCRIPTION In dry-run it prints the plan. With -Execute it (idempotently): ensures the IIS app pool exists
             (No Managed Code, ApplicationPoolIdentity), backs up any existing physical folder, copies the
             published app, patches web.config with the deployment connection string + environment, grants the
             app-pool identity read/execute on the folder, ensures the IIS site + binding exist and use the pool,
             and starts the site. Uses appcmd (the WebAdministration PS module is unavailable on this host).
             Non-destructive: never deletes user data; backs up (never deletes) any prior app folder.
.PARAMETER SiteName / AppPoolName / PhysicalPath / Port / SqlServer / Database / PackageOrPublishPath / Environment
#>
param(
  [string]$SiteName = "LocalAIFactoryPilot",
  [string]$AppPoolName = "LocalAIFactoryPilotPool",
  [string]$PhysicalPath = "C:\inetpub\LocalAIFactoryPilot",
  [int]$Port = 8095,
  [string]$SqlServer = ".\SQLEXPRESS",
  [string]$Database = "LocalAIFactory_IISProof",
  [string]$PackageOrPublishPath = "",
  [string]$Environment = "Development",
  [switch]$Execute
)
$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$appcmd = Join-Path $env:windir "system32\inetsrv\appcmd.exe"
if (-not $PackageOrPublishPath) { $PackageOrPublishPath = Join-Path $repo ".tmp-publish" }
$conn = "Server=$SqlServer;Database=$Database;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"

Write-Host "== IIS Mode A deploy plan ==" -ForegroundColor Cyan
@(
  "Site        : $SiteName  (binding http://*:$Port)",
  "App pool    : $AppPoolName  (No Managed Code; ApplicationPoolIdentity)",
  "Physical    : $PhysicalPath  (published app copied from $PackageOrPublishPath)",
  "Database    : $SqlServer / $Database  (Trusted_Connection — app-pool identity authenticates)",
  "Environment : ASPNETCORE_ENVIRONMENT=$Environment (pilot posture)"
) | ForEach-Object { Write-Host "  $_" }

if (-not (Test-Path $appcmd)) { Write-Host "  appcmd not found — IIS not installed. Stop." -ForegroundColor Red; exit 1 }
if (-not (Test-Path (Join-Path $PackageOrPublishPath "LocalAIFactory.Web.dll"))) { Write-Host "  published app not found at $PackageOrPublishPath." -ForegroundColor Red; exit 1 }

if (-not $Execute) {
  Write-Host "`n  DRY-RUN. No IIS or filesystem changes made. Re-run elevated with -Execute to apply." -ForegroundColor Yellow
  Write-Host "  Pre-req: grant the app-pool identity least-privilege SQL access first:" -ForegroundColor Yellow
  Write-Host "    pwsh database/grant-iis-apppool-sql-access.ps1 -Server $SqlServer -Database $Database -AppPoolName $AppPoolName" -ForegroundColor Yellow
  return
}

function Appcmd { & $appcmd @args }

# 1. App pool (idempotent).
$pools = (Appcmd list apppool) -join "`n"
if ($pools -notmatch [regex]::Escape("APPPOOL `"$AppPoolName`"")) { Appcmd add apppool /name:$AppPoolName | Out-Null; Write-Host "  created app pool $AppPoolName" -ForegroundColor Green }
Appcmd set apppool $AppPoolName /managedRuntimeVersion: | Out-Null            # No Managed Code
Appcmd set apppool $AppPoolName /processModel.identityType:ApplicationPoolIdentity | Out-Null
Appcmd set apppool $AppPoolName /startMode:OnDemand | Out-Null

# 2. Physical folder: back up any existing, then copy published app.
if (Test-Path $PhysicalPath) {
  $bak = "$PhysicalPath.bak-$((Get-Date).ToString('yyyyMMddHHmmss'))"
  Rename-Item $PhysicalPath $bak
  Write-Host "  backed up existing folder -> $bak" -ForegroundColor Yellow
}
New-Item -ItemType Directory -Force -Path $PhysicalPath | Out-Null
Copy-Item (Join-Path $PackageOrPublishPath '*') $PhysicalPath -Recurse -Force
Write-Host "  copied published app -> $PhysicalPath" -ForegroundColor Green

# 3. Patch web.config: set ASPNETCORE_ENVIRONMENT + the deployment connection string as ANCM env vars.
$webConfig = Join-Path $PhysicalPath "web.config"
[xml]$xml = Get-Content $webConfig
$ancm = $xml.SelectSingleNode("//aspNetCore")
if ($ancm) {
  $envNode = $ancm.SelectSingleNode("environmentVariables")
  if (-not $envNode) { $envNode = $xml.CreateElement("environmentVariables"); [void]$ancm.AppendChild($envNode) }
  function SetEnv($name,$value){
    $n = $envNode.SelectSingleNode("environmentVariable[@name='$name']")
    if (-not $n) { $n = $xml.CreateElement("environmentVariable"); $n.SetAttribute("name",$name); [void]$envNode.AppendChild($n) }
    $n.SetAttribute("value",$value)
  }
  SetEnv "ASPNETCORE_ENVIRONMENT" $Environment
  SetEnv "ConnectionStrings__DefaultConnection" $conn
  $xml.Save($webConfig)
  Write-Host "  patched web.config (env=$Environment, connection -> $Database)" -ForegroundColor Green
} else { Write-Host "  WARN: no <aspNetCore> node in web.config" -ForegroundColor Yellow }

# 4. Folder ACL for the app-pool identity (read/execute).
& icacls $PhysicalPath /grant ("IIS AppPool\$AppPoolName" + ":(OI)(CI)RX") /T /Q | Out-Null
Write-Host "  granted IIS AppPool\$AppPoolName read/execute on $PhysicalPath" -ForegroundColor Green

# 5. Site (idempotent) + binding + assign pool.
$sites = (Appcmd list site) -join "`n"
$binding = "http/*:${Port}:"
if ($sites -notmatch [regex]::Escape("SITE `"$SiteName`"")) {
  Appcmd add site /name:$SiteName /physicalPath:$PhysicalPath /bindings:$binding | Out-Null
  Write-Host "  created site $SiteName" -ForegroundColor Green
} else {
  Write-Host "  site $SiteName already exists (binding/physical path left as-is)" -ForegroundColor Yellow
}
Appcmd set app "$SiteName/" /applicationPool:$AppPoolName | Out-Null
Appcmd start apppool $AppPoolName 2>$null | Out-Null
Appcmd start site $SiteName 2>$null | Out-Null
Write-Host "`n  Deployed. Site '$SiteName' on http://localhost:$Port (app pool '$AppPoolName')." -ForegroundColor Green
Write-Host "  Verify: pwsh scripts/deployment-drill/11-iis-mode-a-healthcheck.ps1 -AppUrl http://localhost:$Port -Database $Database" -ForegroundColor Yellow
Write-Host "  Rollback: pwsh scripts/deployment-drill/12-iis-mode-a-rollback-dryrun.ps1" -ForegroundColor Yellow
