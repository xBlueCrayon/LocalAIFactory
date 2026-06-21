<#
.SYNOPSIS  MODE-A: grant the IIS app-pool identity LEAST-PRIVILEGE runtime access to the deployment DB. Idempotent.
.DESCRIPTION Creates a Windows SQL login for the IIS application-pool virtual account
             (e.g. "IIS APPPOOL\LocalAIFactoryPilotPool"), maps it into the deployment database, and grants only
             db_datareader + db_datawriter + EXECUTE — NOT sysadmin, NOT db_owner. This is the RUNTIME identity;
             schema migrations are run separately by an admin (the migration-time identity in
             setup-iis-sqlexpress-proof.ps1). Additive and reversible (drop user/login documented below). Never
             touches LocalDB; never drops the database.
.PARAMETER Server / Database  SQL target. .PARAMETER AppPoolName  IIS app pool whose virtual account gets access.
#>
param(
  [string]$Server = ".\SQLEXPRESS",
  [string]$Database = "LocalAIFactory_IISProof",
  [string]$AppPoolName = "LocalAIFactoryPilotPool"
)
$ErrorActionPreference = "Stop"
$login = "IIS APPPOOL\$AppPoolName"
Write-Host "== Grant least-privilege SQL access to '$login' on $Server / $Database ==" -ForegroundColor Cyan

$tsql = @"
SET NOCOUNT ON;
IF SUSER_ID(N'$login') IS NULL
    CREATE LOGIN [$login] FROM WINDOWS;
USE [$Database];
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$login')
    CREATE USER [$login] FOR LOGIN [$login];
ALTER ROLE db_datareader ADD MEMBER [$login];
ALTER ROLE db_datawriter ADD MEMBER [$login];
GRANT EXECUTE TO [$login];
-- Explicitly NOT granting db_owner / control / sysadmin (least privilege for runtime).
"@
$tsql | sqlcmd -S $Server -b 2>&1 | Out-Host
if ($LASTEXITCODE -ne 0) { Write-Host "  GRANT FAILED (exit $LASTEXITCODE)" -ForegroundColor Red; exit 1 }

# Verify role membership (read-only).
$roles = sqlcmd -S $Server -d $Database -h -1 -W -Q @"
SET NOCOUNT ON;
SELECT r.name FROM sys.database_role_members m
JOIN sys.database_principals r ON r.principal_id = m.role_principal_id
JOIN sys.database_principals u ON u.principal_id = m.member_principal_id
WHERE u.name = N'$login' ORDER BY r.name;
"@ 2>$null
Write-Host "  '$login' database roles: $((($roles | Where-Object { $_ -and $_ -notmatch 'rows affected' }) -join ', ').Trim())" -ForegroundColor Green
Write-Host "  Least privilege: db_datareader + db_datawriter + EXECUTE only (no db_owner / sysadmin)." -ForegroundColor Green
Write-Host "  Reverse with: DROP USER [$login] (in $Database); DROP LOGIN [$login];" -ForegroundColor Yellow
