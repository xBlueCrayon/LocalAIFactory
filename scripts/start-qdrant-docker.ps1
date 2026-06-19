# Starts a local Qdrant vector database in Docker on port 6333.
# Qdrant is OPTIONAL: without it, LocalAIFactory automatically falls back to MSSQL keyword search.

$ErrorActionPreference = "Stop"

$docker = Get-Command docker -ErrorAction SilentlyContinue
if (-not $docker) {
    Write-Warning "Docker is not installed or not on PATH. Install Docker Desktop, or skip Qdrant (keyword search will be used)."
    return
}

$name = "localaifactory-qdrant"
$existing = docker ps -a --filter "name=$name" --format "{{.Names}}"
if ($existing -eq $name) {
    Write-Host "Starting existing container '$name'..." -ForegroundColor Cyan
    docker start $name | Out-Null
} else {
    Write-Host "Creating and starting Qdrant container '$name'..." -ForegroundColor Cyan
    docker run -d --name $name -p 6333:6333 -p 6334:6334 `
        -v localaifactory_qdrant_storage:/qdrant/storage `
        qdrant/qdrant
}

Write-Host "Qdrant REST API: http://localhost:6333" -ForegroundColor Green
Write-Host "Dashboard:       http://localhost:6333/dashboard" -ForegroundColor DarkGray
