<#
.SYNOPSIS
  R2-ACC-FINAL: one-command local demo setup — create LocalDB, apply migrations, seed the full knowledge base,
  and verify. Safe + idempotent (create-if-absent; never drops). Intended for a customer/operator first run.
.DESCRIPTION
  Steps: (1) create the LocalDB database if absent; (2) seed ALL knowledge packs by booting the app briefly
  (startup applies migrations + installs every pack idempotently); (3) verify the full install. No data is
  dropped at any step.
#>
param([int]$Port = 60398)

$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/..").Path

Write-Host "== Step 1/3: create LocalDB (if absent) ==" -ForegroundColor Cyan
& (Join-Path $repo "database/create-localdb.ps1") | Out-Host

Write-Host "`n== Step 2/3: apply migrations + seed all knowledge packs (app startup) ==" -ForegroundColor Cyan
& (Join-Path $repo "scripts/knowledge/install-all-knowledge-packs.ps1") -Port $Port -SkipCreate | Out-Host

Write-Host "`n== Step 3/3: verify full install ==" -ForegroundColor Cyan
& (Join-Path $repo "database/verify-full-install.ps1")
$code = $LASTEXITCODE
Write-Host "`n== Result ==" -ForegroundColor Cyan
if ($code -eq 0) { Write-Host "SETUP-FULL-LOCAL-DEMO: PASS — database ready, knowledge base seeded + verified." -ForegroundColor Green }
else { Write-Host "SETUP-FULL-LOCAL-DEMO: FAIL — see messages above." -ForegroundColor Red }
exit $code
