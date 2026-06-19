# Pulls the local models LocalAIFactory uses by default.
# Safe to re-run. Warns (does not fail) if Ollama is not installed.

$ErrorActionPreference = "Stop"

Write-Host "Checking for Ollama..." -ForegroundColor Cyan
$ollama = Get-Command ollama -ErrorAction SilentlyContinue
if (-not $ollama) {
    Write-Warning "Ollama is not installed or not on PATH. Install it from https://ollama.com/download and re-run this script."
    Write-Warning "LocalAIFactory still runs without Ollama, but local chat/embeddings will be unavailable until a model is configured."
    return
}

$chatModel  = "qwen2.5-coder:14b"
$embedModel = "nomic-embed-text"

Write-Host "Pulling chat model: $chatModel (this can take a while)..." -ForegroundColor Cyan
ollama pull $chatModel

Write-Host "Pulling embedding model: $embedModel..." -ForegroundColor Cyan
ollama pull $embedModel

Write-Host "Done. Ollama should be serving on http://localhost:11434" -ForegroundColor Green
Write-Host "Tip: run 'ollama list' to confirm the installed models." -ForegroundColor DarkGray
