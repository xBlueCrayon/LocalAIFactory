<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: check the optional local AI runtime (Ollama) — read-only.
.DESCRIPTION Reports whether Ollama is reachable and which models are installed. The platform is MSSQL-only by
             default; this check never pulls models and never makes AI authoritative. Exit 0 if reachable.
#>
param([string]$BaseUrl = "http://localhost:11434", [int]$TimeoutSec = 8)
try {
  $tags = Invoke-RestMethod -Uri "$BaseUrl/api/tags" -TimeoutSec $TimeoutSec
  $models = @($tags.models)
  Write-Host "Ollama: REACHABLE at $BaseUrl" -ForegroundColor Green
  if ($models.Count -eq 0) { Write-Host "  (no models installed)" -ForegroundColor Yellow }
  else { $models | ForEach-Object { "  {0}  ({1:N1} GB)" -f $_.name, ($_.size / 1GB) } }
  Write-Host "Note: local AI is OPTIONAL; LocalAIFactory runs MSSQL-only without it." -ForegroundColor Cyan
  exit 0
} catch {
  Write-Host "Ollama: NOT reachable at $BaseUrl (this is fine — local AI is optional)." -ForegroundColor Yellow
  exit 0   # not an error: AI is optional
}
