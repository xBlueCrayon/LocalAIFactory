<#
.SYNOPSIS  Back up the ERP database. Portable SQLite -> file copy; SQL Server -> guidance for BACKUP DATABASE.
.PARAMETER To  Optional target path for the SQLite backup.
#>
param([string]$To = "")
& (Join-Path $PSScriptRoot "backup-restore-health.ps1") -Action backup -To $To
Write-Host "SQL Server: run  sqlcmd -Q `"BACKUP DATABASE [LafErpGold] TO DISK='C:\\backups\\LafErpGold.bak' WITH INIT`"" -ForegroundColor DarkGray
