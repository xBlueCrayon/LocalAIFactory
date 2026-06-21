<#
.SYNOPSIS  Summarize IIS load-simulation result files into one rollup. READ-ONLY.
#>
param([string]$Suites = "iis-smoke-load,iis-search-load,iis-sustained-load")
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$res = Join-Path $repo "benchmarks/results"
$all = @()
foreach ($s in ($Suites -split ',')) {
  $f = Join-Path $res "$($s.Trim())-results.json"
  if (Test-Path $f) { $all += (Get-Content $f -Raw | ConvertFrom-Json) }
}
if (-not $all) { Write-Host "no load result files found." -ForegroundColor Yellow; exit 0 }
Write-Host "== Load simulation rollup ==" -ForegroundColor Cyan
foreach ($r in $all) {
  Write-Host ("  {0,-20} req={1} 200={2} 500={3} rps={4} p95={5}ms p99={6}ms pool={7}" -f $r.suite,$r.totalRequests,$r.http200,$r.http500,$r.rps,$r.p95ms,$r.p99ms,$r.appPoolAfter)
}
$totReq = ($all | Measure-Object totalRequests -Sum).Sum
$tot500 = ($all | Measure-Object http500 -Sum).Sum
@{ suites=$all; totalRequests=$totReq; totalHttp500=$tot500 } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $res "load-rollup-summary.json")
Write-Host "  TOTAL: requests=$totReq  http500=$tot500" -ForegroundColor Green
Write-Host "LOAD-SUMMARY: $(if($tot500 -eq 0){'PASS (0 HTTP 500s across suites)'}else{"REVIEW ($tot500 HTTP 500s)"})" -ForegroundColor ($tot500 -eq 0 ? "Green":"Yellow")
