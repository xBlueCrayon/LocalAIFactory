<# .SYNOPSIS R2-ACC-INDUSTRIAL: one-command LocalDB demo install (create DB + seed knowledge pack + verify). #>
param([string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../..").Path)
$ErrorActionPreference = "Stop"
& "$RepoRoot/database/create-localdb.ps1"; if ($LASTEXITCODE) { exit 1 }
& "$RepoRoot/database/seed-professional-knowledge-base.ps1"; if ($LASTEXITCODE) { exit 1 }
& "$RepoRoot/database/verify-knowledge-base.ps1"
exit $LASTEXITCODE
