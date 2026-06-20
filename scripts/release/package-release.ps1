<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: assemble a release package (zip) from a publish + ship assets.
.DESCRIPTION Combines the published app, database scripts, knowledge packs, appsettings examples, docs, and
             checks into a single timestamped zip under a git-ignored output dir. Commits NOTHING. The package
             is a deployable artifact, not source-controlled.
#>
param(
  [string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../..").Path,
  [string]$PublishDir = "./.tmp-publish",
  [string]$OutDir = "./.tmp-release"
)
$ErrorActionPreference = "Stop"
if (-not (Test-Path $PublishDir)) { Write-Host "Run build-release.ps1 first ($PublishDir missing)." -ForegroundColor Red; exit 1 }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$stage = Join-Path $OutDir "stage"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stage | Out-Null

Copy-Item $PublishDir "$stage/app" -Recurse
Copy-Item "$RepoRoot/database" "$stage/database" -Recurse
Copy-Item "$RepoRoot/knowledge-packs" "$stage/knowledge-packs" -Recurse
Copy-Item "$RepoRoot/release-template/*" $stage -Recurse -ErrorAction SilentlyContinue
@("Industrial-Installation-Guide.md","SQL-Server-Deployment-Guide.md","Windows-Server-IIS-Deployment-Guide.md","Backup-Restore-Runbook.md","Upgrade-Rollback-Runbook.md") |
  ForEach-Object { Copy-Item "$RepoRoot/docs/$_" "$stage/docs/" -ErrorAction SilentlyContinue }

$stamp = (Get-Date).ToString("yyyyMMdd-HHmmss")
$zip = Join-Path (Resolve-Path $OutDir) "LocalAIFactory-release-$stamp.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path "$stage/*" -DestinationPath $zip
Write-Host "PACKAGE OK: $zip ($([math]::Round((Get-Item $zip).Length/1MB,1)) MB)" -ForegroundColor Green
exit 0
