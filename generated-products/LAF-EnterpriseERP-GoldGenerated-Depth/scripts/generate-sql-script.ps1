<#
.SYNOPSIS  Generate an idempotent SQL DDL script from the committed EF Core migrations (for DBA review / manual apply).
.DESCRIPTION  Produces a script that can be inspected and run against SQL Server without the app. Does not touch any DB.
.PARAMETER Output  Target .sql path (default: ./db/laferp-gold-schema.sql, git-ignored).
#>
param([string]$Output = "")
$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot/..").Path
$data = Join-Path $repo "src/LafErp.Data/LafErp.Data.csproj"
$web  = Join-Path $repo "src/LafErp.Web/LafErp.Web.csproj"
if (-not $Output) { $dir = Join-Path $repo "db"; New-Item -ItemType Directory -Force -Path $dir | Out-Null; $Output = Join-Path $dir "laferp-gold-schema.sql" }
try {
    dotnet ef migrations script --idempotent --project $data --startup-project $web --context ErpDbContext --output $Output
    Write-Host "SQL script written: $Output" -ForegroundColor Green
} catch {
    Write-Host "Could not generate SQL script: $($_.Exception.Message)" -ForegroundColor Yellow
    exit 1
}
