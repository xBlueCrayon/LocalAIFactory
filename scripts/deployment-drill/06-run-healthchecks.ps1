<#
.SYNOPSIS  Deployment drill 06 — post-deploy health checks. READ-ONLY.
.DESCRIPTION Probes the deployed app's core pages + the database + knowledge base. Changes nothing. Delegates to
             the existing post-install healthcheck where present.
.PARAMETER AppUrl  Base URL of the deployed app (e.g. http://host:8080). .PARAMETER Server  SQL instance.
#>
param([string]$AppUrl = "http://localhost:8080", [string]$Server = ".\SQLEXPRESS", [string]$Database = "LocalAIFactory")
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$fail = 0
function Ok($m){ Write-Host "  [ OK ] $m" -ForegroundColor Green }
function Bad($m){ Write-Host "  [FAIL] $m" -ForegroundColor Red; $script:fail++ }
Write-Host "== App health ($AppUrl) ==" -ForegroundColor Cyan
foreach ($p in @("/","/Support","/Readiness","/BaseKnowledge")) {
  try { $c = (Invoke-WebRequest -UseBasicParsing "$AppUrl$p" -TimeoutSec 20).StatusCode } catch { $c = 0 }
  if ($c -eq 200) { Ok "GET $p -> 200" } else { Bad "GET $p -> $c" }
}
Write-Host "`n== Database / knowledge base ==" -ForegroundColor Cyan
$vfi = Join-Path $repo "database/verify-full-install.ps1"
if (Test-Path $vfi) { & $vfi -Server $Server -Database $Database *> $null; if ($LASTEXITCODE -eq 0) { Ok "verify-full-install PASS" } else { Bad "verify-full-install FAILED" } }
$post = Join-Path $repo "scripts/release/post-install-healthcheck.ps1"
if (Test-Path $post) { Write-Host "`n== Delegating to post-install-healthcheck ==" -ForegroundColor Cyan; & $post -AppUrl $AppUrl -EA SilentlyContinue }
Write-Host "`nHEALTHCHECK: $([int]($fail -eq 0))" -ForegroundColor ($fail -eq 0 ? "Green":"Red")
exit ([int]($fail -ne 0))
