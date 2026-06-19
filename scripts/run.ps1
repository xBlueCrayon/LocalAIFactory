# Runs the LocalAIFactory web application (migrates + seeds the DB on startup).
# Usage: ./scripts/run.ps1 [-Configuration Debug]
param([string]$Configuration = "Debug")

$ErrorActionPreference = "Stop"
$root    = Split-Path -Parent $PSScriptRoot
$webProj = Join-Path $root "src/LocalAIFactory.Web/LocalAIFactory.Web.csproj"

Write-Host "Starting LocalAIFactory.Web ($Configuration)..." -ForegroundColor Cyan
Write-Host "The app applies EF migrations and seed data on startup." -ForegroundColor DarkGray
dotnet run --project $webProj -c $Configuration
