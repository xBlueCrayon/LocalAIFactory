<#
.SYNOPSIS  R2-ACC-FINAL: read-only process monitor — top processes by memory/CPU + any running LocalAIFactory.
.DESCRIPTION Snapshot only; changes nothing. Useful in a support bundle to see what's consuming resources.
#>
param([int]$Top = 8)

$ErrorActionPreference = "SilentlyContinue"
Write-Host "== Top $Top processes by working set ==" -ForegroundColor Cyan
Get-Process | Sort-Object WorkingSet64 -Descending | Select-Object -First $Top |
  ForEach-Object { "{0,-26} pid {1,-7} {2,7:N0} MB" -f $_.ProcessName, $_.Id, ($_.WorkingSet64/1MB) }

Write-Host "`n== LocalAIFactory processes ==" -ForegroundColor Cyan
$laf = Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" | Where-Object { $_.CommandLine -match 'LocalAIFactory' }
if ($laf) { $laf | ForEach-Object { "  pid {0}  {1} MB" -f $_.ProcessId, [math]::Round($_.WorkingSetSize/1MB,0) } }
else { Write-Host "  none running" }

Write-Host "`n== dotnet processes ==" -ForegroundColor Cyan
(Get-Process dotnet -ErrorAction SilentlyContinue | Measure-Object).Count | ForEach-Object { "  $_ dotnet process(es)" }
Write-Host "PROCESS-MONITOR: OK" -ForegroundColor Green
