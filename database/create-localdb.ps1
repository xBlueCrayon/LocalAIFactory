<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: create/prepare the LocalAIFactory database on SQL Server LocalDB (dev/demo).
.DESCRIPTION Safe by default: detects an existing database (never drops it), ensures the LocalDB instance is
             started, then applies migrations. Seeding + knowledge-pack install happen on first app startup
             (or run seed-professional-knowledge-base.ps1). Trusted connection; no passwords.
#>
param([string]$Instance = "(localdb)\MSSQLLocalDB", [string]$Database = "LocalAIFactory")
$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot/..").Path

Write-Host "Ensuring LocalDB instance is available..." -ForegroundColor Cyan
try { & sqllocaldb start MSSQLLocalDB 2>$null | Out-Null } catch { }

$exists = (sqlcmd -S "$Instance" -E -C -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID('$Database') IS NULL THEN 0 ELSE 1 END;" 2>$null | Select-Object -First 1).Trim()
if ($exists -eq "1") { Write-Host "Database [$Database] already exists — will MIGRATE only (no drop)." -ForegroundColor Yellow }
else { Write-Host "Database [$Database] not found — it will be created by EF migrations." -ForegroundColor Cyan }

& "$PSScriptRoot/apply-migrations.ps1" -ConnectionString "Server=$Instance;Database=$Database;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True" -RepoRoot $repo
if ($LASTEXITCODE -ne 0) { exit 1 }
Write-Host "LocalDB database ready. Run seed-professional-knowledge-base.ps1 then verify-knowledge-base.ps1." -ForegroundColor Green
exit 0
