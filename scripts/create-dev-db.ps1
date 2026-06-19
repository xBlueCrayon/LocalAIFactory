# Creates/updates the LocalAIFactory database by running EF Core migrations.
# Requires the .NET 10 SDK and the EF tools (installed automatically if missing).
# The app ALSO migrates automatically on startup, so this script is optional.

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$webProj = Join-Path $root "src/LocalAIFactory.Web/LocalAIFactory.Web.csproj"

Write-Host "Ensuring EF Core tools are installed..." -ForegroundColor Cyan
$hasEf = (dotnet tool list -g | Select-String "dotnet-ef")
if (-not $hasEf) { dotnet tool install --global dotnet-ef }

Write-Host "Applying migrations to the database..." -ForegroundColor Cyan
dotnet ef database update --project $webProj --startup-project $webProj

Write-Host "Database is up to date." -ForegroundColor Green
