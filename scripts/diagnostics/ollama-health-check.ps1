<#
.SYNOPSIS R2-ACC-20X: Ollama health (read-only, optional). MSSQL-only mode works without it.
#>
param([string]$Url = "http://localhost:11434")
try {
  $tags = Invoke-RestMethod -Uri "$Url/api/tags" -TimeoutSec 4 -ErrorAction Stop
  $models = @($tags.models | ForEach-Object { $_.name })
  Write-Host "Ollama reachable at $Url" -ForegroundColor Green
  if ($models.Count) { Write-Host ("Models: " + ($models -join ", ")) } else { Write-Host "No models installed." -ForegroundColor Yellow }
  Write-Host "OLLAMA-HEALTH: ONLINE" -ForegroundColor Green
} catch {
  Write-Host "Ollama not reachable at $Url — optional. The platform runs in MSSQL-only mode without it." -ForegroundColor Yellow
  Write-Host "OLLAMA-HEALTH: OFFLINE (optional)" -ForegroundColor Yellow
}
