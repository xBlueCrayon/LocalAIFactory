<#
.SYNOPSIS R2-ACC-20X: time the core gates (build / test / benchmark smoke) for a performance baseline.
.DESCRIPTION Read-only timing harness. Prints wall-clock for each gate so regressions in build/test/benchmark
  time are visible release-over-release. No changes made.
#>
$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
function Time($label, $block) {
  $sw = [System.Diagnostics.Stopwatch]::StartNew(); & $block | Out-Null; $sw.Stop()
  [pscustomobject]@{ Gate = $label; Seconds = [math]::Round($sw.Elapsed.TotalSeconds, 2) }
}
$rows = @()
$rows += Time "build (Release)"        { dotnet build "$repo/LocalAIFactory.sln" -c Release --nologo 2>&1 }
$rows += Time "test (full, no-build)"  { dotnet test "$repo/tests/LocalAIFactory.Tests/LocalAIFactory.Tests.csproj" -c Release --no-build --nologo 2>&1 }
$rows += Time "benchmark smoke"        { Push-Location "$repo/tools/LocalAIFactory.Benchmark"; dotnet run -c Release -- --inmemory --suite smoke 2>&1; Pop-Location }
$rows | Format-Table -AutoSize
Write-Host "PERFORMANCE-PROFILE: done" -ForegroundColor Green
