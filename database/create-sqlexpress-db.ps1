<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: create/prepare the LocalAIFactory database on SQL Server Express.
.DESCRIPTION Safe by default: connects with a trusted connection, CREATEs the database only if it does not
             already exist (never drops), then applies migrations. SQL authentication is supported by passing
             -User/-Password but is never forced and never stored.
#>
param(
  [string]$Instance = ".\SQLEXPRESS",
  [string]$Database = "LocalAIFactory",
  [string]$User,
  [string]$Password
)
$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot/..").Path
$auth = ($User) ? "-U `"$User`" -P `"$Password`"" : "-E"
$cs = ($User) `
  ? "Server=$Instance;Database=$Database;User Id=$User;Password=$Password;MultipleActiveResultSets=true;TrustServerCertificate=True" `
  : "Server=$Instance;Database=$Database;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"

Write-Host "Checking SQL Express [$Instance] reachability..." -ForegroundColor Cyan
$ping = Invoke-Expression "sqlcmd -S `"$Instance`" $auth -C -h -1 -W -Q `"SET NOCOUNT ON; SELECT 1;`" 2>`$null"
if (-not $ping) { Write-Host "Cannot reach $Instance. Is SQL Express installed and running?" -ForegroundColor Red; exit 1 }

# Create the database ONLY if it does not exist (no drop, ever).
Invoke-Expression "sqlcmd -S `"$Instance`" $auth -C -b -Q `"IF DB_ID('$Database') IS NULL BEGIN CREATE DATABASE [$Database]; PRINT 'created'; END ELSE PRINT 'exists (no drop)';`""
if ($LASTEXITCODE -ne 0) { Write-Host "Database create/check failed." -ForegroundColor Red; exit 1 }

& "$PSScriptRoot/apply-migrations.ps1" -ConnectionString $cs -RepoRoot $repo
if ($LASTEXITCODE -ne 0) { exit 1 }
Write-Host "SQL Express database ready." -ForegroundColor Green
exit 0
