<#
.SYNOPSIS  Prepare a local SQL Express database for this ERP, then apply migrations.
.DESCRIPTION
  Checks for a reachable SQL Express instance, creates the target database if absent, and applies the
  committed EF migrations. Fails gracefully with exact setup guidance if SQL Express is not installed.
.PARAMETER Instance  e.g. ".\SQLEXPRESS" (default) or "(localdb)\MSSQLLocalDB".
.PARAMETER Database  Target database name (default LafErpGold).
#>
param([string]$Instance = ".\SQLEXPRESS", [string]$Database = "LafErpGold")
$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot/..").Path
$cs = "Server=$Instance;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True"
$master = "Server=$Instance;Database=master;Trusted_Connection=True;TrustServerCertificate=True"

function Test-Sql($connStr) {
    try { $c = New-Object System.Data.SqlClient.SqlConnection $connStr; $c.Open(); $c.Close(); return $true } catch { return $false }
}

if (-not (Test-Sql $master)) {
    Write-Host "SQL Express not reachable at '$Instance'." -ForegroundColor Yellow
    Write-Host "Setup required:" -ForegroundColor Yellow
    Write-Host "  1. Install SQL Server Express (https://www.microsoft.com/sql-server/sql-server-downloads)." -ForegroundColor Yellow
    Write-Host "  2. Ensure the instance name matches -Instance (default .\SQLEXPRESS)." -ForegroundColor Yellow
    Write-Host "  3. Re-run this script. (Portable SQLite mode needs none of this.)" -ForegroundColor Yellow
    exit 2
}
# Create database if it does not exist.
$c = New-Object System.Data.SqlClient.SqlConnection $master; $c.Open()
$cmd = $c.CreateCommand()
$cmd.CommandText = "IF DB_ID('$Database') IS NULL CREATE DATABASE [$Database];"
$cmd.ExecuteNonQuery() | Out-Null
$c.Close()
Write-Host "Database '$Database' ready on '$Instance'." -ForegroundColor Green
& (Join-Path $PSScriptRoot "apply-migrations.ps1") -Connection $cs
