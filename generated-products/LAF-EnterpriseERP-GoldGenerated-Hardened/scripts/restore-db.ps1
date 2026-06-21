<#
.SYNOPSIS  Restore the ERP database. Portable SQLite -> file copy; SQL Server -> guidance for RESTORE DATABASE.
.PARAMETER From  Path to the SQLite backup to restore.
#>
param([Parameter(Mandatory=$true)][string]$From)
& (Join-Path $PSScriptRoot "backup-restore-health.ps1") -Action restore -To $From
Write-Host "SQL Server: run  sqlcmd -Q `"RESTORE DATABASE [LafErpGold] FROM DISK='C:\\backups\\LafErpGold.bak' WITH REPLACE`"" -ForegroundColor DarkGray
