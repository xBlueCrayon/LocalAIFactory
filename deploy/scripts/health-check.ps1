<#
.SYNOPSIS  R2-ACC-CAP6: read-only health check of a running LocalAIFactory instance.
.DESCRIPTION GETs core pages and reports status. No changes; safe to run against any environment.
#>
param([string]$Url = "http://localhost:8080", [int]$TimeoutSec = 15)
$ErrorActionPreference = "Continue"
$paths = @("/", "/BaseKnowledge", "/Readiness", "/Models")
$fail = 0
foreach ($p in $paths) {
  try {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $code = (Invoke-WebRequest -UseBasicParsing -Uri "$Url$p" -TimeoutSec $TimeoutSec).StatusCode
    $sw.Stop()
    if ($code -eq 200 -or $code -eq 302) { Write-Host ("  OK   {0,-16} {1} ({2} ms)" -f $p, $code, $sw.ElapsedMilliseconds) -ForegroundColor Green }
    else { Write-Host ("  BAD  {0,-16} {1}" -f $p, $code) -ForegroundColor Red; $fail++ }
  } catch { Write-Host ("  DOWN {0,-16} {1}" -f $p, $_.Exception.Message) -ForegroundColor Red; $fail++ }
}
if ($fail -eq 0) { Write-Host "HEALTH: OK" -ForegroundColor Green; exit 0 } else { Write-Host "HEALTH: $fail issue(s)" -ForegroundColor Red; exit 1 }
