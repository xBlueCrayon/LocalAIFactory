<# R2-ACC-INDUSTRIAL: validate core-banking integration capability via the benchmark fixture (live proof). #>
param([string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../..").Path)
$ErrorActionPreference = "Stop"
Push-Location "$RepoRoot/tools/LocalAIFactory.Benchmark"
try { $out = & dotnet run -c Release -- --inmemory --suite standard 2>&1 } finally { Pop-Location }
$out | Select-String "CoreBankingIntegration" | ForEach-Object { Write-Host $_ }
if ($out | Select-String "CoreBankingIntegration\s+Gold\s+.*pov=6/6") { Write-Host "Core-banking: Gold 6/6 — capability proven." -ForegroundColor Green; exit 0 }
else { Write-Host "Core-banking fixture did not reach Gold 6/6." -ForegroundColor Red; exit 1 }
