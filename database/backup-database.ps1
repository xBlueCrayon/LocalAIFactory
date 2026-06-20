<# .SYNOPSIS R2-ACC-INDUSTRIAL: full database backup (non-destructive). Thin wrapper over deploy/scripts/backup.ps1. #>
param([string]$ServerInstance = "(localdb)\MSSQLLocalDB", [string]$Database = "LocalAIFactory", [string]$BackupDir = "./backups")
& "$PSScriptRoot/../deploy/scripts/backup.ps1" -ServerInstance $ServerInstance -Database $Database -BackupDir $BackupDir
exit $LASTEXITCODE
