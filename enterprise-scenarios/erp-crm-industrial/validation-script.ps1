<# R2-ACC-INDUSTRIAL: validate the ERP/CRM capability by running the benchmark fixture (the live proof). #>
param([string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../..").Path)
$ErrorActionPreference = "Stop"
Push-Location "$RepoRoot/tools/LocalAIFactory.Benchmark"
try { $out = & dotnet run -c Release -- --inmemory --suite standard 2>&1 } finally { Pop-Location }
$out | Select-String "ErpCrmIndustrial" | ForEach-Object { Write-Host $_ }
if ($out | Select-String "ErpCrmIndustrial\s+Gold\s+.*pov=6/6") { Write-Host "ERP/CRM: Gold 6/6 — capability proven." -ForegroundColor Green; exit 0 }
else { Write-Host "ERP/CRM fixture did not reach Gold 6/6." -ForegroundColor Red; exit 1 }
