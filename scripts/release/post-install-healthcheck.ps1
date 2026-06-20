<# .SYNOPSIS R2-ACC-INDUSTRIAL: post-install health check against a running instance (read-only). #>
param([string]$Url = "http://localhost:8080", [string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../..").Path)
& "$RepoRoot/deploy/scripts/health-check.ps1" -Url $Url
exit $LASTEXITCODE
