<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: tiny safe inference test against an INSTALLED local model (no model pull).
.DESCRIPTION Runs one short, bounded generation to prove the local AI path works end-to-end. Never pulls a
             model; uses an installed one. AI output is NEVER authoritative — MSSQL remains the source of truth.
#>
param(
  [string]$BaseUrl = "http://localhost:11434",
  [string]$Model = "",          # default: first installed NON-reasoning model
  [int]$NumPredict = 64,
  [int]$TimeoutSec = 180
)
try { $tags = Invoke-RestMethod -Uri "$BaseUrl/api/tags" -TimeoutSec 8 } catch { Write-Host "Ollama not reachable — skipping (AI is optional)." -ForegroundColor Yellow; exit 0 }
$models = @($tags.models)
if ($models.Count -eq 0) { Write-Host "No installed models — skipping (do not pull large models)." -ForegroundColor Yellow; exit 0 }
if (-not $Model) {
  # Prefer a non-reasoning model: reasoning models (e.g. *-r1) spend the token budget on hidden <think> output.
  $nonReasoning = $models | Where-Object { $_.name -notmatch 'r1|reason|think' } | Select-Object -First 1
  $Model = ($nonReasoning ? $nonReasoning.name : $models[0].name)
}

Write-Host "Testing model '$Model' (num_predict=$NumPredict)..." -ForegroundColor Cyan
$body = @{ model = $Model; prompt = "Reply with exactly: OK"; stream = $false; options = @{ num_predict = $NumPredict } } | ConvertTo-Json
$sw = [System.Diagnostics.Stopwatch]::StartNew()
try { $r = Invoke-RestMethod -Uri "$BaseUrl/api/generate" -Method Post -Body $body -ContentType "application/json" -TimeoutSec $TimeoutSec }
catch { Write-Host "Inference call failed: $($_.Exception.Message)" -ForegroundColor Red; exit 1 }
$sw.Stop()
$resp = ($r.response ?? "").Trim()
Write-Host ("  model responded in {0:N1}s; {1} chars" -f ($sw.Elapsed.TotalSeconds), $resp.Length) -ForegroundColor Green
Write-Host ("  sample: {0}" -f ($resp.Substring(0, [Math]::Min(120, $resp.Length))))
if ($resp.Length -gt 0) { Write-Host "LOCAL-AI: working (optional accelerator)." -ForegroundColor Green; exit 0 }
else { Write-Host "Model returned empty output." -ForegroundColor Red; exit 1 }
