<#
.SYNOPSIS  Apply the committed EF Core migrations to a SQL Server / SQL Express database.
.DESCRIPTION
  Non-destructive: `dotnet ef database update` only applies pending migrations; it never drops data.
  Requires the dotnet-ef tool (`dotnet tool install --global dotnet-ef`) and a reachable SQL Server.
  SQLite portable mode does NOT use this script (it uses EnsureCreated at startup).
.PARAMETER Connection  SQL Server connection string. If omitted, the design-time default (.\ LafErpGold) is used.
#>
param([string]$Connection = "")
$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot/..").Path
$data = Join-Path $repo "src/LafErp.Data/LafErp.Data.csproj"
$web  = Join-Path $repo "src/LafErp.Web/LafErp.Web.csproj"
if ($Connection) { $env:LAFERP_MIGRATION_CONNECTION = $Connection; $env:ConnectionStrings__Default = $Connection }
if (-not (Get-Command dotnet-ef -ErrorAction SilentlyContinue) -and -not (dotnet tool list --global 2>$null | Select-String 'dotnet-ef')) {
    Write-Host "dotnet-ef not found. Install with: dotnet tool install --global dotnet-ef" -ForegroundColor Yellow
    exit 2
}
try {
    dotnet ef database update --project $data --startup-project $web --context ErpDbContext
    Write-Host "Migrations applied." -ForegroundColor Green
} catch {
    Write-Host "Could not apply migrations (is SQL Server reachable?). Detail: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "Setup required: a running SQL Server/Express instance and a valid connection string." -ForegroundColor Yellow
    exit 1
}
