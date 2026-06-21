<#
.SYNOPSIS  R2-ACC-FINAL: run the customer acceptance checklist against a release package + the live system.
.DESCRIPTION Read-only. Maps to docs/Customer-Acceptance-Test.md: (1) package is complete + safe, (2) database
             verifies, (3) knowledge base seeded (4 packs / 438 items), (4) optional app health. Prints a clear
             ACCEPTED / NOT-ACCEPTED result. No destructive actions.
.PARAMETER Stage   Staged/extracted package to check (default ./.tmp-release/stage).
.PARAMETER AppUrl  Optional running-app URL for a live health check.
#>
param([string]$Stage = "./.tmp-release/stage", [string]$AppUrl = "")

$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$fail = 0
function Item($name, $ok) { if ($ok) { Write-Host "  [PASS] $name" -ForegroundColor Green } else { Write-Host "  [FAIL] $name" -ForegroundColor Red; $script:fail++ } }

Write-Host "== Customer Acceptance Checklist ==" -ForegroundColor Cyan

# 1. Package complete + safe
& (Join-Path $PSScriptRoot "verify-release-package.ps1") -Stage $Stage *> $null
Item "Release package complete and safe (verify-release-package)" ($LASTEXITCODE -eq 0)

# 2. Knowledge base verifies (offline + live if DB reachable)
& (Join-Path $repo "scripts/knowledge/verify-all-knowledge-packs.ps1") *> $null
Item "Knowledge base validates (4 packs, 438 items, no UID collisions)" ($LASTEXITCODE -eq 0)

# 3. Database full-install verify (skipped gracefully if DB not reachable)
$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if ($sqlcmd) {
  & (Join-Path $repo "database/verify-full-install.ps1") *> $null
  Item "Database full install verifies (migrations + KB)" ($LASTEXITCODE -eq 0)
} else { Write-Host "  [SKIP] DB verify (sqlcmd not present on this host)" -ForegroundColor Yellow }

# 4. Optional live app health
if ($AppUrl) {
  try { $code = (Invoke-WebRequest -UseBasicParsing "$AppUrl/Support" -TimeoutSec 15).StatusCode } catch { $code = 0 }
  Item "Support dashboard reachable ($AppUrl/Support -> 200)" ($code -eq 200)
}

Write-Host "`n== Result ==" -ForegroundColor Cyan
if ($fail -eq 0) { Write-Host "CUSTOMER-ACCEPTANCE: ACCEPTED" -ForegroundColor Green; exit 0 }
Write-Host "CUSTOMER-ACCEPTANCE: NOT ACCEPTED ($fail item(s) failed)" -ForegroundColor Red; exit 1
