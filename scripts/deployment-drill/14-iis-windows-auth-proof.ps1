<#
.SYNOPSIS  Deployment drill 14 — MODE A: prove the IIS Windows/Negotiate auth round-trip over HTTPS. -Execute gated.
.DESCRIPTION In dry-run it prints the plan. With -Execute it enables Windows Authentication + disables Anonymous
             on the pilot site, then probes over HTTPS: (a) without credentials -> expects 401 (Negotiate
             challenge), (b) with the current Windows credentials -> expects 200 (authenticated). With -Revert it
             restores Anonymous (Windows auth off). Over HTTPS the client CAN send default Windows credentials
             (unlike plain HTTP). No secrets printed. JSON evidence to .tmp-*.
.PARAMETER SiteName / HttpsUrl / Execute / Revert
#>
param(
  [string]$SiteName = "LocalAIFactoryPilot",
  [string]$HttpsUrl = "https://localhost:8443",
  [switch]$Execute,
  [switch]$Revert
)
$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$appcmd = Join-Path $env:windir "system32\inetsrv\appcmd.exe"

function SetAuth($win,$anon){
  & $appcmd unlock config /section:windowsAuthentication 2>$null | Out-Null
  & $appcmd unlock config /section:anonymousAuthentication 2>$null | Out-Null
  & $appcmd set config $SiteName /section:windowsAuthentication /enabled:$win /commit:apphost 2>$null | Out-Null
  & $appcmd set config $SiteName /section:anonymousAuthentication /enabled:$anon /commit:apphost 2>$null | Out-Null
  Start-Sleep 2
}

if ($Revert) { SetAuth "false" "true"; Write-Host "  Reverted: Anonymous ON, Windows auth OFF for $SiteName." -ForegroundColor Yellow; return }

Write-Host "== IIS Windows/Negotiate auth proof ($SiteName over $HttpsUrl) ==" -ForegroundColor Cyan
if (-not $Execute) {
  Write-Host "  DRY-RUN. Would: enable Windows auth + disable Anonymous, then probe HTTPS without/with credentials." -ForegroundColor Yellow
  Write-Host "  Re-run with -Execute. Use -Revert to restore Anonymous." -ForegroundColor Yellow
  return
}

SetAuth "true" "false"
Write-Host "  Windows auth ENABLED, Anonymous DISABLED on $SiteName."

# (a) without credentials -> expect 401
$noCred = 0
try { $r = Invoke-WebRequest -UseBasicParsing "$HttpsUrl/" -TimeoutSec 15 -SkipCertificateCheck; $noCred = [int]$r.StatusCode }
catch { $resp = $_.Exception.Response; $noCred = if ($resp) { [int]$resp.StatusCode } else { 0 } }
Write-Host "  probe WITHOUT credentials -> HTTP $noCred  $(if($noCred -eq 401){'(Negotiate challenge enforced)'}else{''})" -ForegroundColor $(if($noCred -eq 401){'Green'}else{'Red'})

# (b) with current Windows credentials over HTTPS -> expect 200
$withCred = 0
try { $r = Invoke-WebRequest -UseBasicParsing "$HttpsUrl/" -TimeoutSec 15 -SkipCertificateCheck -UseDefaultCredentials; $withCred = [int]$r.StatusCode }
catch { $resp = $_.Exception.Response; $withCred = if ($resp) { [int]$resp.StatusCode } else { 0 } }
Write-Host "  probe WITH Windows credentials -> HTTP $withCred  $(if($withCred -eq 200){'(authenticated round-trip OK)'}else{''})" -ForegroundColor $(if($withCred -eq 200){'Green'}else{'Red'})

@{ site=$SiteName; httpsUrl=$HttpsUrl; noCredStatus=$noCred; withCredStatus=$withCred; whoami=(whoami) } | ConvertTo-Json |
  Set-Content (Join-Path $repo ".tmp-iis-winauth-evidence.json")
$pass = ($noCred -eq 401 -and $withCred -eq 200)
Write-Host "`nWINDOWS-AUTH-PROOF: $(if($pass){'PASS (401 challenge -> authenticated 200 over HTTPS)'}else{"PARTIAL (noCred=$noCred withCred=$withCred)"})" -ForegroundColor $(if($pass){'Green'}else{'Yellow'})
Write-Host "  The site is left in the Windows-auth posture. Restore anonymous with: -Revert" -ForegroundColor Yellow
exit ([int](-not $pass))
