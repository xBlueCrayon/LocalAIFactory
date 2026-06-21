<#
.SYNOPSIS  Deployment drill 09 — post-deploy health check against a running deployed/published endpoint. READ-ONLY.
.DESCRIPTION Probes the deployed app (HTTP), the database (knowledge-pack counts), and reports the deployment
             mode. Changes nothing. Returns non-zero if any health gate fails (HTTP non-200/500, DB unreachable,
             or pack/item counts below the expected baseline). Use after 05/Mode-C deploy to certify health.
.PARAMETER AppUrl   Base URL of the deployed app (e.g. http://localhost:8095).
.PARAMETER Server   SQL instance (e.g. .\SQLEXPRESS or (localdb)\MSSQLLocalDB).
.PARAMETER Database Deployment database (e.g. LocalAIFactory_DeploymentProof).
.PARAMETER Mode     Deployment mode label for the report (A/B/C/D/E).
.PARAMETER ExpectPacks  Expected installed pack count (default 4). .PARAMETER ExpectItems  Expected pack items (default 438).
#>
param(
  [string]$AppUrl = "http://localhost:8095",
  [string]$Server = ".\SQLEXPRESS",
  [string]$Database = "LocalAIFactory_DeploymentProof",
  [string]$Mode = "C",
  [int]$ExpectPacks = 4,
  [int]$ExpectItems = 438
)
$fail = 0
function Ok($m){ Write-Host "  [ OK ] $m" -ForegroundColor Green }
function Bad($m){ Write-Host "  [FAIL] $m" -ForegroundColor Red; $script:fail++ }

Write-Host "== Post-deploy health check (Mode $Mode) — $AppUrl / $Server / $Database ==" -ForegroundColor Cyan

# 1. HTTP pages (no 500s; 200 expected)
foreach ($p in @("/","/Support","/Readiness","/BaseKnowledge","/Coverage","/Graph")) {
  try { $c = (Invoke-WebRequest -UseBasicParsing "$AppUrl$p" -TimeoutSec 20).StatusCode } catch { $resp = $_.Exception.Response; $c = if ($resp) { [int]$resp.StatusCode } else { 0 } }
  if ($c -eq 200) { Ok "GET $p -> 200" } elseif ($c -ge 500) { Bad "GET $p -> $c (server error)" } else { Bad "GET $p -> $c" }
}

# 2. Base Knowledge search returns real matches
foreach ($t in @("OCR","Mauritius")) {
  try { $b = (Invoke-WebRequest -UseBasicParsing "$AppUrl/BaseKnowledge?q=$t" -TimeoutSec 20).Content } catch { $b = "" }
  $n = ([regex]::Matches($b, "/BaseKnowledge/Details/")).Count
  if ($n -ge 1) { Ok "search '$t' -> $n matches" } else { Bad "search '$t' -> 0 matches" }
}

# 3. DB connectivity + knowledge pack counts
$packs = (sqlcmd -S $Server -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.KnowledgePacks" -h -1 2>$null | Out-String).Trim()
$items = (sqlcmd -S $Server -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.KnowledgeItems WHERE KnowledgePackId IS NOT NULL" -h -1 2>$null | Out-String).Trim()
$migs  = (sqlcmd -S $Server -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.__EFMigrationsHistory" -h -1 2>$null | Out-String).Trim()
if ($packs -match '^\d+$' -and [int]$packs -ge $ExpectPacks) { Ok "DB reachable; installed packs = $packs (>= $ExpectPacks)" } else { Bad "DB packs = '$packs' (expected >= $ExpectPacks)" }
if ($items -match '^\d+$' -and [int]$items -ge $ExpectItems) { Ok "pack items = $items (>= $ExpectItems)" } else { Bad "pack items = '$items' (expected >= $ExpectItems)" }
if ($migs -match '^\d+$' -and [int]$migs -ge 1) { Ok "migrations applied = $migs" } else { Bad "no migrations history" }

# 4. App version + deployment context
$ver = $null
$verFile = Join-Path (Split-Path $PSScriptRoot -Parent | Split-Path -Parent) "VERSION.txt"
if (Test-Path $verFile) { $ver = (Get-Content $verFile -Raw).Trim() }
Write-Host "  app version: $(if ($ver) { $ver } else { '(VERSION.txt not found in repo root; bundled in package)' })"
Write-Host "  deployment mode: $Mode (C = published app + SQL Express, no IIS)"

# 5. Logs path presence (informational; logs are NOT committed)
Write-Host "  logs: app stdout/stderr captured to .tmp-deploy-app.*.log (git-ignored; not committed)"

Write-Host "`nPOST-DEPLOY-HEALTHCHECK (Mode $Mode): $(if ($fail -eq 0) { 'PASS' } else { "FAIL ($fail)" })" -ForegroundColor ($fail -eq 0 ? "Green":"Red")
exit ([int]($fail -ne 0))
