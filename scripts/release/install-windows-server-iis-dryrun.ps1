<# .SYNOPSIS R2-ACC-INDUSTRIAL: Windows/IIS install DRY-RUN (prints the plan; changes nothing). #>
param([string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../..").Path, [string]$PublishDir = "C:\inetpub\LocalAIFactory")
& "$RepoRoot/deploy/scripts/windows-deploy.ps1" -PublishDir $PublishDir   # dry-run by default (no -Execute)
exit $LASTEXITCODE
