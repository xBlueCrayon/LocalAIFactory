<#
.SYNOPSIS
  R2-ACC-FINAL: verify a full local install — DB reachable, migrations applied, knowledge base seeded.
.DESCRIPTION
  Read-only. Confirms (1) the database is reachable, (2) the EF migrations history table exists and has rows,
  (3) the knowledge base verifies, and (4) all source packs validate + match the live installed counts.
  Exits non-zero on any failure. No writes, no drops.
.PARAMETER Server / Database  Target instance (default LocalDB / LocalAIFactory).
#>
param([string]$Server = "(localdb)\MSSQLLocalDB", [string]$Database = "LocalAIFactory")

$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/..").Path
$fail = 0
function Bad($m){ Write-Host "  [FAIL] $m" -ForegroundColor Red; $script:fail++ }
function Ok($m){ Write-Host "  [ OK ] $m" -ForegroundColor Green }

Write-Host "== Database reachability + migrations ==" -ForegroundColor Cyan
$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmd) { Bad "sqlcmd not found"; }
else {
  $mig = & sqlcmd -S $Server -d $Database -E -b -h -1 -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM __EFMigrationsHistory;" 2>&1
  if ($LASTEXITCODE -ne 0) { Bad "database '$Database' not reachable on $Server" }
  else {
    $n = ($mig | Where-Object { $_ -match '^\s*\d+\s*$' } | Select-Object -First 1)
    if ([int]$n -gt 0) { Ok "migrations applied ($([int]$n) in __EFMigrationsHistory)" } else { Bad "no migrations recorded" }
  }
}

Write-Host "`n== Knowledge base verification ==" -ForegroundColor Cyan
& (Join-Path $repo "database/verify-knowledge-base.ps1") -ServerInstance $Server -Database $Database 2>&1 | Out-Host
if ($LASTEXITCODE -ne 0) { Bad "verify-knowledge-base failed" } else { Ok "knowledge base verified" }

Write-Host "`n== All packs (offline + live counts) ==" -ForegroundColor Cyan
& (Join-Path $repo "scripts/knowledge/verify-all-knowledge-packs.ps1") -Server $Server -Database $Database 2>&1 | Out-Host
if ($LASTEXITCODE -ne 0) { Bad "verify-all-knowledge-packs failed" } else { Ok "all packs verified" }

Write-Host "`n== Result ==" -ForegroundColor Cyan
if ($fail -eq 0) { Write-Host "VERIFY-FULL-INSTALL: PASS" -ForegroundColor Green; exit 0 }
Write-Host "VERIFY-FULL-INSTALL: FAIL ($fail issue(s))" -ForegroundColor Red; exit 1
