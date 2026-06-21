<#
.SYNOPSIS  Local high-volume SIMULATION against the IIS HTTPS/Windows-auth pilot. Read-only GETs.
.DESCRIPTION Fires concurrent HTTPS GETs (current Windows credentials, self-signed cert accepted) at the pilot for
             a bounded duration, capturing latency percentiles (p50/p95/p99), error count, HTTP 500 count, and
             app-pool state before/after. This is a LOCAL workstation simulation — NOT a production high-volume
             claim. Safe (read-only, bounded). Writes JSON to .tmp-* and benchmarks/results.
.PARAMETER AppUrl / Paths / Concurrency / DurationSeconds / Suite
#>
param(
  [string]$AppUrl = "https://localhost:8443",
  [string[]]$Paths = @("/","/Support","/Readiness","/BaseKnowledge","/BaseKnowledge?q=OCR","/BaseKnowledge?q=Mauritius","/BaseKnowledge?q=market","/Benchmarks"),
  [int]$Concurrency = 25,
  [int]$DurationSeconds = 60,
  [string]$Suite = "iis-smoke-load"
)
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$res = Join-Path $repo "benchmarks/results"; New-Item -ItemType Directory -Force $res | Out-Null
$appcmd = Join-Path $env:windir "system32\inetsrv\appcmd.exe"
$poolBefore = (& $appcmd list apppool LocalAIFactoryPilotPool /text:state 2>$null | Out-String).Trim()

Add-Type -AssemblyName System.Net.Http
$handler = New-Object System.Net.Http.HttpClientHandler
$handler.UseDefaultCredentials = $true
$handler.ServerCertificateCustomValidationCallback = [System.Net.Http.HttpClientHandler]::DangerousAcceptAnyServerCertificateValidator
$client = New-Object System.Net.Http.HttpClient($handler)
$client.Timeout = [TimeSpan]::FromSeconds(30)

Write-Host "== IIS load simulation ($Suite): $Concurrency concurrent, ${DurationSeconds}s, $AppUrl ==" -ForegroundColor Cyan
Write-Host "  (LOCAL workstation simulation — NOT a production high-volume claim)" -ForegroundColor Yellow
$latencies = New-Object System.Collections.Concurrent.ConcurrentBag[double]
$codes = New-Object System.Collections.Concurrent.ConcurrentBag[int]
$deadline = (Get-Date).AddSeconds($DurationSeconds)
$ri = 0
while ((Get-Date) -lt $deadline) {
  $tasks = @()
  for ($c = 0; $c -lt $Concurrency; $c++) {
    $path = $Paths[$ri % $Paths.Count]; $ri++
    $url = "$AppUrl$path"
    $tasks += [pscustomobject]@{ sw=[Diagnostics.Stopwatch]::StartNew(); task=$client.GetAsync($url) }
  }
  foreach ($t in $tasks) {
    try { $resp = $t.task.GetAwaiter().GetResult(); $t.sw.Stop(); $latencies.Add($t.sw.Elapsed.TotalMilliseconds); $codes.Add([int]$resp.StatusCode) }
    catch { $t.sw.Stop(); $codes.Add(0) }
  }
}
$client.Dispose()
$poolAfter = (& $appcmd list apppool LocalAIFactoryPilotPool /text:state 2>$null | Out-String).Trim()

$lat = $latencies.ToArray() | Sort-Object
$codeArr = $codes.ToArray()
function Pct($arr,$p){ if($arr.Count -eq 0){return 0}; $arr[[Math]::Min($arr.Count-1,[int][Math]::Floor($p/100.0*$arr.Count))] }
$total = $codeArr.Count
$ok200 = ($codeArr | Where-Object { $_ -eq 200 }).Count
$err500 = ($codeArr | Where-Object { $_ -ge 500 }).Count
$errOther = ($codeArr | Where-Object { $_ -eq 0 -or ($_ -ge 400 -and $_ -lt 500) }).Count
$summary = [ordered]@{
  suite=$Suite; appUrl=$AppUrl; concurrency=$Concurrency; durationSec=$DurationSeconds
  totalRequests=$total; http200=$ok200; http500=$err500; otherErrors=$errOther
  rps=[math]::Round($total/$DurationSeconds,1)
  p50ms=[math]::Round((Pct $lat 50),1); p95ms=[math]::Round((Pct $lat 95),1); p99ms=[math]::Round((Pct $lat 99),1)
  minMs=[math]::Round(($lat | Select-Object -First 1),1); maxMs=[math]::Round(($lat | Select-Object -Last 1),1)
  appPoolBefore=$poolBefore; appPoolAfter=$poolAfter
}
$summary | ConvertTo-Json | Set-Content (Join-Path $res "$Suite-results.json")
Write-Host ("  requests=$total  200=$ok200  500=$err500  otherErr=$errOther  rps=$($summary.rps)") -ForegroundColor Green
Write-Host ("  latency ms: p50=$($summary.p50ms)  p95=$($summary.p95ms)  p99=$($summary.p99ms)  max=$($summary.maxMs)") -ForegroundColor Green
Write-Host ("  app pool: before=$poolBefore after=$poolAfter") -ForegroundColor Green
Write-Host "IIS-LOAD ($Suite): $(if($err500 -eq 0 -and $poolAfter -eq 'Started'){'PASS (0 HTTP 500s, pool healthy)'}else{"REVIEW (500s=$err500 pool=$poolAfter)"})" -ForegroundColor ($(if($err500 -eq 0 -and $poolAfter -eq 'Started'){'Green'}else{'Yellow'}))
