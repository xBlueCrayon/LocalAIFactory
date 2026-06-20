<#
.SYNOPSIS
  R2-ACC-20X: capture product screenshots for the manuals using Playwright (if available).
.DESCRIPTION
  Drives a running LocalAIFactory instance and saves PNGs of the key pages into docs/screenshots/.
  SAFE: read-only navigation only (GET pages); it never submits forms or changes data. If Playwright (Node)
  is not installed, it prints the exact install commands and exits 0 (documented blocker, not a failure).
.PARAMETER BaseUrl  Base URL of a RUNNING app (e.g. http://localhost:5000).
#>
param([string]$BaseUrl = "http://localhost:5000")

$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$outDir = Join-Path $repo "docs/screenshots"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# Pages to capture (controller routes -> file name). Read-only GETs only.
$pages = @(
  @{ Path="/";                Name="01-home-dashboard" },
  @{ Path="/Readiness";       Name="03-readiness" },
  @{ Path="/Support";         Name="15-support-health" },
  @{ Path="/BaseKnowledge";   Name="04-base-knowledge" },
  @{ Path="/Projects";        Name="06-projects" },
  @{ Path="/Graph";           Name="09-graph-explorer" },
  @{ Path="/Benchmarks";      Name="11-benchmarks" },
  @{ Path="/Audit";           Name="13-audit" }
)

$node = Get-Command node -ErrorAction SilentlyContinue
$npx  = Get-Command npx  -ErrorAction SilentlyContinue
if (-not $node -or -not $npx) {
  Write-Host "Playwright/Node not installed — screenshots not captured (documented blocker)." -ForegroundColor Yellow
  Write-Host "To enable, on a machine with Node 18+:" -ForegroundColor Cyan
  Write-Host "  npm init -y; npm i -D playwright; npx playwright install chromium"
  Write-Host "  then re-run:  scripts/docs/capture-screenshots.ps1 -BaseUrl $BaseUrl"
  Write-Host "Pages that would be captured:"
  $pages | ForEach-Object { "  $($_.Name)  <- $BaseUrl$($_.Path)" }
  exit 0
}

# Emit a tiny Playwright script and run it.
$js = @"
const { chromium } = require('playwright');
(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const page = await ctx.newPage();
  const pages = $(($pages | ConvertTo-Json -Compress));
  for (const p of pages) {
    try {
      await page.goto('$BaseUrl' + p.Path, { waitUntil: 'networkidle', timeout: 15000 });
      await page.screenshot({ path: '$($outDir -replace '\\','/')/' + p.Name + '.png', fullPage: true });
      console.log('captured', p.Name);
    } catch (e) { console.log('skip', p.Name, e.message); }
  }
  await browser.close();
})();
"@
$tmp = Join-Path $env:TEMP "laf-shots-$(Get-Random).js"
$js | Set-Content -Path $tmp -Encoding UTF8
& node $tmp
Remove-Item $tmp -Force -ErrorAction SilentlyContinue
Write-Host "Screenshots written to docs/screenshots/ (where the app was reachable)." -ForegroundColor Green
