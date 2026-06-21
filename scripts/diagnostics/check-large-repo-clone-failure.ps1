<#
.SYNOPSIS  Diagnose large-repo shallow-clone failures (timeout / partial dir / scale). READ-ONLY advisory.
.DESCRIPTION Firsthand rule from the 50/100-repo benchmark: "If clone retry says 'destination exists', delete the
             partial clone dir before retrying." Also: xlarge monorepos exceed a workstation per-repo budget.
#>
param([string]$CacheDir=".tmp-benchmark-repos")
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$cache = Join-Path $repo $CacheDir
Write-Host "== Large-repo clone-failure diagnostic ==" -ForegroundColor Cyan
if (Test-Path $cache) {
  $partial = Get-ChildItem $cache -Directory -EA SilentlyContinue | Where-Object { -not (Test-Path (Join-Path $_.FullName ".git")) }
  if ($partial) { Write-Host "  [WARN] partial clone dirs (no .git) found — delete before retry:" -ForegroundColor Yellow; $partial | ForEach-Object { Write-Host "    $($_.FullName)" } }
  else { Write-Host "  [ OK ] no partial clone dirs in cache" -ForegroundColor Green }
} else { Write-Host "  [ OK ] clone cache empty/absent" -ForegroundColor Green }
Write-Host "  Rules: (1) remove the destination dir before EVERY clone attempt (fixed in run-50-project-benchmark.ps1)."
Write-Host "         (2) xlarge monorepos (dotnet/runtime, elasticsearch, kubernetes) need a longer budget / server host / incremental extraction." -ForegroundColor Yellow
