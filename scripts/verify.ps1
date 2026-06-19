# Build + runtime smoke test: confirms the solution builds and the core pages respond quickly.
# Core pages must NEVER hang. Requires a reachable SQL Server for the runtime portion.
# Usage: ./scripts/verify.ps1 [-Port 5000] [-SkipRun]
param([int]$Port = 5000, [switch]$SkipRun)

$ErrorActionPreference = "Stop"
$root    = Split-Path -Parent $PSScriptRoot
$sln     = Join-Path $root "LocalAIFactory.sln"
$webProj = Join-Path $root "src/LocalAIFactory.Web/LocalAIFactory.Web.csproj"

Write-Host "[1/2] Building..." -ForegroundColor Cyan
dotnet build $sln -c Release
Write-Host "Build OK." -ForegroundColor Green

if ($SkipRun) { Write-Host "Skipping runtime smoke test (-SkipRun)." -ForegroundColor Yellow; return }

Write-Host "[2/2] Runtime smoke test on http://localhost:$Port ..." -ForegroundColor Cyan
$env:ASPNETCORE_URLS = "http://localhost:$Port"
$proc = Start-Process dotnet -ArgumentList "run --project `"$webProj`" -c Release" -PassThru -NoNewWindow

try {
    # Wait for the app to come up (max ~60s).
    $up = $false
    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep -Seconds 1
        try { Invoke-WebRequest "http://localhost:$Port/" -TimeoutSec 3 -UseBasicParsing | Out-Null; $up = $true; break }
        catch { }
    }
    if (-not $up) { throw "Application did not start within 60s." }

    $pages = @("/","/Projects","/Knowledge","/Models")
    $allOk = $true
    foreach ($p in $pages) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        try {
            $r = Invoke-WebRequest "http://localhost:$Port$p" -TimeoutSec 10 -UseBasicParsing
            $sw.Stop()
            $flag = if ($sw.Elapsed.TotalSeconds -gt 1) { "SLOW" } else { "ok" }
            if ($r.StatusCode -ne 200) { $allOk = $false }
            Write-Host ("  {0,-12} {1}  {2:N3}s  [{3}]" -f $p, $r.StatusCode, $sw.Elapsed.TotalSeconds, $flag)
        } catch {
            $sw.Stop(); $allOk = $false
            Write-Host ("  {0,-12} FAILED/HUNG after {1:N3}s" -f $p, $sw.Elapsed.TotalSeconds) -ForegroundColor Red
        }
    }
    if ($allOk) { Write-Host "All core pages responded 200." -ForegroundColor Green }
    else        { Write-Host "One or more pages failed or were slow." -ForegroundColor Red; exit 1 }
}
finally {
    if ($proc -and -not $proc.HasExited) { Stop-Process -Id $proc.Id -Force }
}
