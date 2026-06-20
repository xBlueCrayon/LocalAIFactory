<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: create/prepare the LocalAIFactory database on a full SQL Server instance.
.DESCRIPTION Same safe contract as the Express script: trusted connection by default, CREATE-if-absent (never
             drop), then apply migrations. SQL auth supported via -User/-Password (never forced, never stored).
#>
param(
  [string]$Instance = "localhost",
  [string]$Database = "LocalAIFactory",
  [string]$User,
  [string]$Password
)
$ErrorActionPreference = "Stop"
& "$PSScriptRoot/create-sqlexpress-db.ps1" -Instance $Instance -Database $Database -User $User -Password $Password
exit $LASTEXITCODE
