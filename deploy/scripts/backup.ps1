<#
.SYNOPSIS  R2-ACC-CAP6: back up the LocalAIFactory database (non-destructive).
.DESCRIPTION Creates a full backup via sqlcmd. Does NOT drop, overwrite, or truncate anything. Parameterised;
             no secrets embedded — pass credentials via -User/-Password or use -TrustedConnection.
#>
param(
  [string]$ServerInstance = "(localdb)\MSSQLLocalDB",
  [string]$Database = "LocalAIFactory",
  [string]$BackupDir = "./backups",
  [switch]$TrustedConnection = $true,
  [string]$User,
  [string]$Password
)
$ErrorActionPreference = "Stop"
if (-not (Test-Path $BackupDir)) { New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null }
$stamp = (Get-Date).ToString("yyyyMMdd-HHmmss")
$file = Join-Path (Resolve-Path $BackupDir) "$Database-$stamp.bak"
$auth = $TrustedConnection -and -not $User ? "-E" : "-U `"$User`" -P `"$Password`""
$sql = "BACKUP DATABASE [$Database] TO DISK = N'$file' WITH INIT, CHECKSUM, COMPRESSION;"
Write-Host "Backing up [$Database] on $ServerInstance -> $file"
$cmd = "sqlcmd -S `"$ServerInstance`" $auth -C -b -Q `"$sql`""
Invoke-Expression $cmd
if ($LASTEXITCODE -eq 0 -and (Test-Path $file)) { Write-Host "BACKUP OK: $file" -ForegroundColor Green; exit 0 }
else { Write-Host "BACKUP FAILED" -ForegroundColor Red; exit 1 }
