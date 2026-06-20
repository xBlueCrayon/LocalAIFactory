<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: summarize an autonomous run for human review/approval (read-only).
.DESCRIPTION Prints a concise pass/fail summary from a results JSON (as written by run-approved-local-checks or
             a controlled execution). No side effects; produces the artifact a human approves before promotion.
#>
param([string]$ResultsJson)
if ($ResultsJson -and (Test-Path $ResultsJson)) {
  $r = Get-Content $ResultsJson -Raw | ConvertFrom-Json
  Write-Host "== Autonomous run summary ==" -ForegroundColor Cyan
  $r | ForEach-Object { Write-Host ("  {0,-12} {1}" -f $_.check, ($_.passed ? "PASS" : "FAIL")) }
} else {
  Write-Host "No results file supplied. Use run-approved-local-checks.ps1 -Execute, then pass its summary here." -ForegroundColor Yellow
}
Write-Host "`nGate: promotion (commit/push/merge/deploy) requires explicit human approval." -ForegroundColor Yellow
exit 0
