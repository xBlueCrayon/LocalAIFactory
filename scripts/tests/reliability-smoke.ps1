<#
.SYNOPSIS
  R2-ACC-20X: reliability smoke — run a fast, safe operation repeatedly and check it is stable.
.DESCRIPTION
  Repeats the in-memory unit-test fast subset N times and reports per-iteration timing + pass/fail. No external
  services, no destructive actions. Catches flakiness and gross timing drift. Safe to run on any dev machine.
#>
param([int]$Iterations = 5, [string]$Filter = "FullyQualifiedName~ChatLearningTests|FullyQualifiedName~LicensingTests|FullyQualifiedName~LocalFixLoopTests")

$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$proj = Join-Path $repo "tests/LocalAIFactory.Tests/LocalAIFactory.Tests.csproj"
& dotnet build $proj -c Release --nologo 2>&1 | Out-Null

$times = @(); $fail = 0
for ($i = 1; $i -le $Iterations; $i++) {
  $sw = [System.Diagnostics.Stopwatch]::StartNew()
  $out = & dotnet test $proj -c Release --no-build --nologo --filter $Filter 2>&1
  $sw.Stop()
  $ok = ($out | Select-String "Passed!")
  $times += $sw.Elapsed.TotalSeconds
  if ($ok) { Write-Host ("  iter {0}: PASS  {1:N2}s" -f $i, $sw.Elapsed.TotalSeconds) -ForegroundColor Green }
  else { Write-Host ("  iter {0}: FAIL  {1:N2}s" -f $i, $sw.Elapsed.TotalSeconds) -ForegroundColor Red; $fail++ }
}

$avg = ($times | Measure-Object -Average).Average
$min = ($times | Measure-Object -Minimum).Minimum
$max = ($times | Measure-Object -Maximum).Maximum
Write-Host ("`nIterations={0}  failures={1}  avg={2:N2}s  min={3:N2}s  max={4:N2}s" -f $Iterations, $fail, $avg, $min, $max)
if ($fail -eq 0) { Write-Host "RELIABILITY-SMOKE: PASS" -ForegroundColor Green; exit 0 }
Write-Host "RELIABILITY-SMOKE: FAIL ($fail iteration failure(s))" -ForegroundColor Red; exit 1
