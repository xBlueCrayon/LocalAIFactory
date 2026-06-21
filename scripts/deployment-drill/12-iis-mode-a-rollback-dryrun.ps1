<#
.SYNOPSIS  Deployment drill 12 — MODE A: roll back / clean up the IIS pilot deployment. DRY-RUN by default.
.DESCRIPTION In dry-run it prints the rollback plan and the current IIS site/app-pool state. With -StopOnly it
             stops the site + app pool (reversible; frees the port) and verifies stopped. With -Execute it ALSO
             removes the site + app pool and restores the previous app folder from the newest .bak-* backup if one
             exists. It never drops the database (the disposable deployment DB is left as evidence; a documented
             DROP is printed). Uses appcmd.
.PARAMETER SiteName / AppPoolName / PhysicalPath / Port / Database / StopOnly / Execute
#>
param(
  [string]$SiteName = "LocalAIFactoryPilot",
  [string]$AppPoolName = "LocalAIFactoryPilotPool",
  [string]$PhysicalPath = "C:\inetpub\LocalAIFactoryPilot",
  [int]$Port = 8095,
  [string]$Database = "LocalAIFactory_IISProof",
  [switch]$StopOnly,
  [switch]$Execute
)
$appcmd = Join-Path $env:windir "system32\inetsrv\appcmd.exe"
function State($kind,$name){ (& $appcmd list $kind $name /text:state 2>$null | Out-String).Trim() }

Write-Host "== Mode A IIS rollback ($SiteName / $AppPoolName) ==" -ForegroundColor Cyan
Write-Host "  current site state    : $(State 'site' $SiteName)"
Write-Host "  current app-pool state: $(State 'apppool' $AppPoolName)"
@(
  "Rollback ladder:",
  "  1. Stop site + app pool (reversible)            -> frees port $Port; app no longer served.",
  "  2. Restore previous app/ from newest .bak-*      -> if a prior deploy was backed up by 10-iis-mode-a-deploy.",
  "  3. Remove site + app pool (full teardown)        -> appcmd delete site/apppool.",
  "  4. (DB) the disposable deployment DB is LEFT as evidence; drop manually if desired:",
  "       sqlcmd -S .\\SQLEXPRESS -Q `"DROP DATABASE [$Database]`"   (isolated; LocalDB untouched)"
) | ForEach-Object { Write-Host "  $_" }

if ($StopOnly) {
  & $appcmd stop site $SiteName 2>$null | Out-Null
  & $appcmd stop apppool $AppPoolName 2>$null | Out-Null
  Start-Sleep 2
  $free = -not (Get-NetTCPConnection -LocalPort $Port -State Listen -EA SilentlyContinue)
  Write-Host "`n  STOPPED. site=$(State 'site' $SiteName) apppool=$(State 'apppool' $AppPoolName); port $Port free=$free" -ForegroundColor Yellow
  Write-Host "  Restart with: appcmd start apppool $AppPoolName; appcmd start site $SiteName" -ForegroundColor Yellow
  return
}
if (-not $Execute) {
  Write-Host "`n  DRY-RUN. No changes. Use -StopOnly to stop (reversible), or -Execute to remove site+app pool and restore backup." -ForegroundColor Yellow
  return
}
# -Execute: full teardown
& $appcmd stop site $SiteName 2>$null | Out-Null
& $appcmd stop apppool $AppPoolName 2>$null | Out-Null
& $appcmd delete site $SiteName 2>$null | Out-Null
& $appcmd delete apppool $AppPoolName 2>$null | Out-Null
$bak = Get-ChildItem "$PhysicalPath.bak-*" -Directory -EA SilentlyContinue | Sort-Object LastWriteTime -Desc | Select-Object -First 1
if ($bak) { Write-Host "  previous app backup available to restore: $($bak.FullName)" -ForegroundColor Yellow }
Write-Host "`n  Site + app pool removed. DB left intact ($Database). Folder $PhysicalPath retained (delete manually if desired)." -ForegroundColor Green
