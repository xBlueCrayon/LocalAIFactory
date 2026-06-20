<# .SYNOPSIS R2-ACC-INDUSTRIAL: verify a backup is restorable (RESTORE VERIFYONLY — non-destructive). Wrapper. #>
param([Parameter(Mandatory = $true)][string]$BackupFile, [string]$ServerInstance = "(localdb)\MSSQLLocalDB", [switch]$FullRestore)
& "$PSScriptRoot/../deploy/scripts/restore-verify.ps1" -BackupFile $BackupFile -ServerInstance $ServerInstance -FullRestore:$FullRestore
exit $LASTEXITCODE
