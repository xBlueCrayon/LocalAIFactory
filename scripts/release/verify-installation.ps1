<# .SYNOPSIS R2-ACC-INDUSTRIAL: verify an installation (artifacts + knowledge base). Read-only. #>
param([string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../..").Path, [string]$Instance = "(localdb)\MSSQLLocalDB", [string]$Database = "LocalAIFactory")
$ErrorActionPreference = "Continue"
& "$RepoRoot/scripts/poc/verify-poc.ps1" -Fast
& "$RepoRoot/database/verify-knowledge-base.ps1" -ServerInstance $Instance -Database $Database
exit $LASTEXITCODE
