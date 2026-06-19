# Applies EF Core migrations to the configured database.
# The app ALSO migrates automatically on startup, so this is optional.
# Usage: ./scripts/migrate.ps1
$ErrorActionPreference = "Stop"
$root       = Split-Path -Parent $PSScriptRoot
$dataProj   = Join-Path $root "src/LocalAIFactory.Data/LocalAIFactory.Data.csproj"
$startupPrj = Join-Path $root "src/LocalAIFactory.Web/LocalAIFactory.Web.csproj"

Write-Host "Ensuring EF Core tools are installed..." -ForegroundColor Cyan
if (-not (dotnet tool list -g | Select-String "dotnet-ef")) {
    dotnet tool install --global dotnet-ef
}

Write-Host "Applying migrations (project=Data, startup=Web)..." -ForegroundColor Cyan
dotnet ef database update --project $dataProj --startup-project $startupPrj

Write-Host "Database is up to date." -ForegroundColor Green
