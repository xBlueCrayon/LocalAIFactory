<#
.SYNOPSIS  R2-ACC-CAP6: post-deployment smoke test (read-only) against a deployed URL.
.DESCRIPTION Asserts core pages return 200/302 and that none return 5xx. No changes are made. For local UI
             coverage use scripts/poc/ui-smoke-test.ps1; this is the deployment-target variant.
#>
param([Parameter(Mandatory = $true)][string]$Url, [int]$TimeoutSec = 20)
$ErrorActionPreference = "Continue"
$paths = @("/", "/BaseKnowledge", "/Readiness", "/Projects", "/Models", "/Coverage")
$fail = 0
foreach ($p in $paths) {
  try {
    $code = (Invoke-WebRequest -UseBasicParsing -Uri "$Url$p" -TimeoutSec $TimeoutSec).StatusCode
    if ($code -ge 500) { Write-Host "  5xx $p -> $code" -ForegroundColor Red; $fail++ }
    elseif ($code -eq 200 -or $code -eq 302) { Write-Host "  OK  $p -> $code" -ForegroundColor Green }
    else { Write-Host "  ??? $p -> $code" -ForegroundColor Yellow; $fail++ }
  } catch {
    $resp = $_.Exception.Response
    if ($resp -and [int]$resp.StatusCode -ge 500) { Write-Host "  5xx $p" -ForegroundColor Red; $fail++ }
    else { Write-Host "  ERR $p -> $($_.Exception.Message)" -ForegroundColor Red; $fail++ }
  }
}
if ($fail -eq 0) { Write-Host "DEPLOY-SMOKE: PASS" -ForegroundColor Green; exit 0 } else { Write-Host "DEPLOY-SMOKE: FAIL ($fail)" -ForegroundColor Red; exit 1 }
