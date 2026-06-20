<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: run the APPROVED local checks (build/test/benchmark) — never commits/pushes.
.DESCRIPTION DRY-RUN by default (prints what it would run). With -Execute it runs only the allowlisted
             build/test/benchmark commands, stops on the first failure, and writes a summary. It NEVER commits,
             pushes, merges, deploys, or runs a destructive command.
#>
param([string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../..").Path, [switch]$Execute)
$checks = @(
  @{ name = "build";     cmd = { dotnet build "$RepoRoot/LocalAIFactory.sln" -c Release --nologo } },
  @{ name = "test";      cmd = { dotnet test "$RepoRoot/tests/LocalAIFactory.Tests/LocalAIFactory.Tests.csproj" -c Release --nologo } },
  @{ name = "benchmark"; cmd = { Push-Location "$RepoRoot/tools/LocalAIFactory.Benchmark"; try { dotnet run -c Release -- --inmemory } finally { Pop-Location } } }
)
if (-not $Execute) {
  Write-Host "DRY-RUN: would run (allowlisted only): build, test, benchmark. No commit/push/deploy." -ForegroundColor Yellow
  exit 0
}
$results = @()
foreach ($c in $checks) {
  Write-Host "== $($c.name) ==" -ForegroundColor Cyan
  & $c.cmd | Select-Object -Last 3 | Out-Host
  $ok = ($LASTEXITCODE -eq 0)
  $results += [pscustomobject]@{ check = $c.name; passed = $ok }
  if (-not $ok) { Write-Host "$($c.name) FAILED — halting (no promotion)." -ForegroundColor Red; break }
}
$results | Format-Table | Out-Host
$allPass = ($results | Where-Object { -not $_.passed }).Count -eq 0 -and $results.Count -eq $checks.Count
Write-Host ("RESULT: {0}. Promotion (commit/push) requires explicit human approval and is NOT performed here." -f ($allPass ? "ALL PASSED" : "FAILED")) -ForegroundColor ($allPass ? "Green" : "Red")
exit ($allPass ? 0 : 1)
