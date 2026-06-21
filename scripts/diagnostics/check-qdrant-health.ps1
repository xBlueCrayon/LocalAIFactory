<#
.SYNOPSIS  Diagnose optional Qdrant vector store. READ-ONLY. Optional component (default disabled).
#>
param([string]$BaseUrl="http://localhost:6333")
Write-Host "== Qdrant health check ($BaseUrl) ==" -ForegroundColor Cyan
try{ $r=Invoke-WebRequest -UseBasicParsing "$BaseUrl/healthz" -TimeoutSec 5; Write-Host "  [ OK ] Qdrant reachable -> $([int]$r.StatusCode)" -ForegroundColor Green }
catch{ Write-Host "  [INFO] Qdrant not reachable (OPTIONAL — Qdrant.Enabled=false by default; app runs MSSQL-only)" -ForegroundColor Yellow }
