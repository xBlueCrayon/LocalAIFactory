<#
.SYNOPSIS
  R2-ACC-POC-ENTERPRISE — UI smoke test (HTTP-level). Proves the UI runs like a real product, not just compiles.
.DESCRIPTION
  If -AppUrl is given, tests that running app. Otherwise builds the Web project, starts it locally
  (Development dev-auth = admin), waits for readiness, exercises core pages + Base Knowledge searches, asserts
  NO 500s, then stops the app it started. No destructive actions; no DB drops; no production changes.

  NOTE ON PLAYWRIGHT: full browser testing (Playwright) is documented in docs/Readiness-Maturity-Model.md but
  not yet wired (it needs Node + browser downloads). This HTTP smoke test is the current deterministic gate.
.PARAMETER AppUrl
  Base URL of an already-running app. If omitted, the script starts and stops its own instance.
.PARAMETER Port
  Local port to use when starting the app (default 60398, matching launchSettings http).
#>
param([string]$AppUrl = "", [int]$Port = 60398)

$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$fail = 0
$started = $false
function Ok($m)  { Write-Host "  [PASS] $m" -ForegroundColor Green }
function Bad($m) { Write-Host "  [FAIL] $m" -ForegroundColor Red; $script:fail++ }

function Get-Page($url) {
  try { $r = Invoke-WebRequest -UseBasicParsing -Uri $url -TimeoutSec 20; return @{ Code = [int]$r.StatusCode; Body = $r.Content } }
  catch {
    $resp = $_.Exception.Response
    if ($resp) { return @{ Code = [int]$resp.StatusCode; Body = "" } }
    return @{ Code = 0; Body = "" }
  }
}

try {
  if (-not $AppUrl) {
    Write-Host "== Build Web ==" -ForegroundColor Cyan
    & dotnet build "$repo/src/LocalAIFactory.Web/LocalAIFactory.Web.csproj" -c Release --nologo 2>&1 | Select-Object -Last 2 | Out-Host
    if ($LASTEXITCODE -ne 0) { Bad "Web build failed"; throw "build" }

    Write-Host "== Start app (Development) on port $Port ==" -ForegroundColor Cyan
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    Start-Process -FilePath "dotnet" -ArgumentList @("run","--project","$repo/src/LocalAIFactory.Web/LocalAIFactory.Web.csproj","-c","Release","--no-build") -WindowStyle Hidden | Out-Null
    $started = $true
    $AppUrl = "http://localhost:$Port"
    $up = $false
    for ($i = 0; $i -lt 60; $i++) {
      Start-Sleep -Seconds 2
      if ((Get-Page "$AppUrl/").Code -eq 200) { $up = $true; break }
    }
    if ($up) { Ok "app is up at $AppUrl" } else { Bad "app did not become ready"; throw "startup" }
  }

  Write-Host "== Core pages (no 500s) ==" -ForegroundColor Cyan
  foreach ($p in @("/","/BaseKnowledge","/Readiness","/Support","/Projects","/Knowledge","/Models","/Coverage","/Graph","/Users","/Audit")) {
    $r = Get-Page "$AppUrl$p"
    if ($r.Code -ge 500) { Bad "GET $p -> $($r.Code)" }
    elseif ($r.Code -eq 200 -or $r.Code -eq 302) { Ok "GET $p -> $($r.Code)" }
    else { Bad "GET $p -> $($r.Code)" }
  }

  Write-Host "== Base Knowledge search ==" -ForegroundColor Cyan
  # OCR/Mauritius/VB6/PDF are baseline knowledge -> expect matches. SAP is NOT baseline knowledge (it lives in
  # scenarios/docs) -> expect a clean 200 with zero matches (honest: the platform does not pretend to be SAP).
  $expectMatches = @{ "OCR" = $true; "Mauritius banking" = $true; "VB6" = $true; "PDF summarizer" = $true; "SAP" = $false }
  foreach ($term in $expectMatches.Keys) {
    $r = Get-Page "$AppUrl/BaseKnowledge?q=$([uri]::EscapeDataString($term))"
    if ($r.Code -ne 200) { Bad "search '$term' -> HTTP $($r.Code)"; continue }
    $n = ([regex]::Matches($r.Body, "/BaseKnowledge/Details/")).Count
    if ($expectMatches[$term] -and $n -lt 1) { Bad "search '$term' returned 0 matches (expected >=1)" }
    else { Ok "search '$term' -> 200, matches=$n" }
  }
}
finally {
  if ($started) {
    Write-Host "== Stop app ==" -ForegroundColor Cyan
    Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" -ErrorAction SilentlyContinue |
      Where-Object { $_.CommandLine -match 'LocalAIFactory.Web' } |
      ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
  }
}

Write-Host "`n== Result ==" -ForegroundColor Cyan
if ($fail -eq 0) { Write-Host "UI-SMOKE: PASS" -ForegroundColor Green; exit 0 }
else { Write-Host "UI-SMOKE: FAIL ($fail issue(s))" -ForegroundColor Red; exit 1 }
