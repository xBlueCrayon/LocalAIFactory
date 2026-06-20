<#
.SYNOPSIS
  R2-ACC-20X: load smoke — run the benchmark smoke suite repeatedly and check timing consistency.
.DESCRIPTION
  Safe, bounded load: runs the in-memory benchmark smoke suite N times and reports per-run timing + PASS/FAIL.
  No network, no destructive actions, no production load. Documents the host's repeatable throughput.
#>
param([int]$Iterations = 3)

$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$bench = Join-Path $repo "tools/LocalAIFactory.Benchmark"
Push-Location $bench
& dotnet build -c Release --nologo 2>&1 | Out-Null

$times = @(); $fail = 0
for ($i = 1; $i -le $Iterations; $i++) {
  $sw = [System.Diagnostics.Stopwatch]::StartNew()
  $out = & dotnet run -c Release --no-build -- --inmemory --suite smoke 2>&1
  $sw.Stop()
  $times += $sw.Elapsed.TotalSeconds
  if ($out | Select-String "Result: PASS") { Write-Host ("  run {0}: PASS  {1:N2}s" -f $i, $sw.Elapsed.TotalSeconds) -ForegroundColor Green }
  else { Write-Host ("  run {0}: FAIL  {1:N2}s" -f $i, $sw.Elapsed.TotalSeconds) -ForegroundColor Red; $fail++ }
}
Pop-Location

$avg = ($times | Measure-Object -Average).Average
Write-Host ("`nRuns={0}  failures={1}  avg={2:N2}s" -f $Iterations, $fail, $avg)
if ($fail -eq 0) { Write-Host "LOAD-SMOKE: PASS" -ForegroundColor Green; exit 0 }
Write-Host "LOAD-SMOKE: FAIL" -ForegroundColor Red; exit 1
