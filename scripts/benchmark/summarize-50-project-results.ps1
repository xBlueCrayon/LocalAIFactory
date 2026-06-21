<#
.SYNOPSIS  Aggregate a public-project benchmark result file into a summary JSON + markdown table.
.PARAMETER Suite / ResultsFile
#>
param([string]$Suite = "public50", [string]$ResultsFile = "")
$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
if (-not $ResultsFile) { $ResultsFile = Join-Path $repo "benchmarks/results/$Suite-results.json" }
$data = Get-Content $ResultsFile -Raw | ConvertFrom-Json
$rows = $data.results
$passLike = @("Passed","PassedPartial","ValidationOnly")
$summary = [ordered]@{
  suite = $data.suite
  attempted = $rows.Count
  byStatus = @{}
  byLanguage = @{}
  bySizeTier = @{}
  cloned = ($rows | Where-Object { $_.sha }).Count
  passedFull = ($rows | Where-Object { $_.status -eq "Passed" }).Count
  passedPartial = ($rows | Where-Object { $_.status -eq "PassedPartial" }).Count
  validationOnly = ($rows | Where-Object { $_.status -eq "ValidationOnly" }).Count
  unsupported = ($rows | Where-Object { $_.status -in @("UnsupportedLanguage","NoSupportedFiles") }).Count
  failed = ($rows | Where-Object { $_.status -in @("CloneFailed","CheckoutFailed","TimedOut","Failed") }).Count
  totalSupportedFiles = ($rows | Measure-Object supportedFiles -Sum).Sum
  totalCSharpSymbols = ($rows | Measure-Object symbolsCSharp -Sum).Sum
  totalPythonSymbols = ($rows | Measure-Object symbolsPython -Sum).Sum
  totalSqlObjects = ($rows | Measure-Object sqlObjects -Sum).Sum
  totalLoc = ($rows | Measure-Object loc -Sum).Sum
  totalDurationSec = [math]::Round((($rows | Measure-Object durationSec -Sum).Sum),1)
}
foreach ($g in ($rows | Group-Object status)) { $summary.byStatus[$g.Name] = $g.Count }
foreach ($g in ($rows | Group-Object primaryLanguage)) { $summary.byLanguage[$g.Name] = $g.Count }
foreach ($g in ($rows | Group-Object sizeTier)) { $summary.bySizeTier[$g.Name] = $g.Count }
# crude average score: Passed=100, PassedPartial=75, ValidationOnly=50, Unsupported=25 (honest gap), failed=0
function ScoreOf($s){ switch($s){ "Passed"{100} "PassedPartial"{75} "ValidationOnly"{50} "UnsupportedLanguage"{25} "NoSupportedFiles"{25} default{0} } }
$summary.averageScore = [math]::Round((($rows | ForEach-Object { ScoreOf $_.status } | Measure-Object -Average).Average),1)
$largest = $rows | Sort-Object totalFiles -Descending | Select-Object -First 1
$summary.largestRepoAttempted = "$($largest.displayName) ($($largest.totalFiles) files, $($largest.status))"

$outJson = Join-Path $repo "benchmarks/results/$Suite-summary.json"
$summary | ConvertTo-Json -Depth 6 | Set-Content $outJson -Encoding UTF8
Write-Host "== $Suite summary ==" -ForegroundColor Cyan
Write-Host "  attempted=$($summary.attempted) cloned=$($summary.cloned) Passed=$($summary.passedFull) Partial=$($summary.passedPartial) ValidationOnly=$($summary.validationOnly) Unsupported=$($summary.unsupported) Failed=$($summary.failed)"
Write-Host "  supportedFiles=$($summary.totalSupportedFiles) C#symbols=$($summary.totalCSharpSymbols) PySymbols=$($summary.totalPythonSymbols) SQLobjects=$($summary.totalSqlObjects) LOC=$($summary.totalLoc)"
Write-Host "  averageScore=$($summary.averageScore)  largest=$($summary.largestRepoAttempted)"
Write-Host "Summary: $outJson" -ForegroundColor Green
