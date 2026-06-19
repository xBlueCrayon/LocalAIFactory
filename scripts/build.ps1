# Restores and builds the full LocalAIFactory solution in Release.
# Usage: ./scripts/build.ps1 [-Configuration Debug]
param([string]$Configuration = "Release")

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$sln  = Join-Path $root "LocalAIFactory.sln"

Write-Host "Restoring packages..." -ForegroundColor Cyan
dotnet restore $sln

Write-Host "Building $sln ($Configuration)..." -ForegroundColor Cyan
dotnet build $sln -c $Configuration --no-restore

Write-Host "Build succeeded." -ForegroundColor Green
