<# .SYNOPSIS R2-ACC-INDUSTRIAL: SQL Express demo install (create DB + migrate; seed on first app run + verify). #>
param([string]$Instance = ".\SQLEXPRESS", [string]$Database = "LocalAIFactory", [string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../..").Path)
$ErrorActionPreference = "Stop"
& "$RepoRoot/database/create-sqlexpress-db.ps1" -Instance $Instance -Database $Database; if ($LASTEXITCODE) { exit 1 }
& "$RepoRoot/database/seed-professional-knowledge-base.ps1" -Instance $Instance -Database $Database
exit $LASTEXITCODE
