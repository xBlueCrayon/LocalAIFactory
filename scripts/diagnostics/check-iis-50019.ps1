<#
.SYNOPSIS  Diagnose IIS HTTP 500.19 (config/handler error — usually a missing/locked module or bad web.config). READ-ONLY.
#>
param([string]$PhysicalPath="C:\inetpub\LocalAIFactoryPilot")
Write-Host "== IIS 500.19 config diagnostic ==" -ForegroundColor Cyan
$wc = Join-Path $PhysicalPath "web.config"
if (Test-Path $wc) {
  Write-Host "  [ OK ] web.config present"
  try { [xml]$x = Get-Content $wc; $ancm = $x.SelectSingleNode("//aspNetCore"); if ($ancm) { Write-Host "  [ OK ] <aspNetCore> handler present (processPath=$($ancm.processPath))" -ForegroundColor Green } else { Write-Host "  [WARN] no <aspNetCore> node — ANCM handler missing" -ForegroundColor Yellow } }
  catch { Write-Host "  [FAIL] web.config is not valid XML -> 500.19" -ForegroundColor Red }
} else { Write-Host "  [FAIL] web.config missing at $PhysicalPath" -ForegroundColor Red }
$ancmMod = (& "$env:windir\system32\inetsrv\appcmd.exe" list module 2>$null | Out-String) -match 'AspNetCoreModuleV2'
Write-Host "  AspNetCoreModuleV2 registered globally: $(if($ancmMod){'yes'}else{'NO -> Hosting Bundle not installed'})" -ForegroundColor $(if($ancmMod){'Green'}else{'Red'})
Write-Host "  Common 500.19 causes: missing Hosting Bundle/ANCM, locked config section, malformed web.config, missing module." -ForegroundColor Yellow
