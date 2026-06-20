<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: apply EF Core migrations to a target database (additive; never destructive).
.DESCRIPTION Runs `dotnet ef database update`. Creates the database if absent (the app/EF does this); never
             drops or truncates. Connection comes from -ConnectionString or the standard env var.
#>
param(
  [string]$ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=LocalAIFactory;Trusted_Connection=True;TrustServerCertificate=True",
  [string]$RepoRoot = (Resolve-Path "$PSScriptRoot/..").Path
)
$ErrorActionPreference = "Stop"
$env:ConnectionStrings__DefaultConnection = $ConnectionString
Write-Host "Applying migrations to: $($ConnectionString -replace 'Password=[^;]+','Password=***')" -ForegroundColor Cyan
& dotnet ef database update --project "$RepoRoot/src/LocalAIFactory.Data" --startup-project "$RepoRoot/src/LocalAIFactory.Web"
if ($LASTEXITCODE -eq 0) { Write-Host "MIGRATIONS: applied." -ForegroundColor Green; exit 0 }
else { Write-Host "MIGRATIONS: FAILED ($LASTEXITCODE)" -ForegroundColor Red; exit 1 }
