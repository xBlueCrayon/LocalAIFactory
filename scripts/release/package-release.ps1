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

New-Item -ItemType Directory -Force -Path "$stage/docs","$stage/docs/screenshots","$stage/scripts" | Out-Null

# App binaries + included knowledge base + database scripts + release-template (README/LICENSE/THIRD-PARTY/VERSION).
Copy-Item $PublishDir "$stage/app" -Recurse
Copy-Item "$RepoRoot/database" "$stage/database" -Recurse
Copy-Item "$RepoRoot/knowledge-packs" "$stage/knowledge-packs" -Recurse
Copy-Item "$RepoRoot/release-template/*" $stage -Recurse -ErrorAction SilentlyContinue

# Operator/verification scripts the customer needs (knowledge / diagnostics / security / release / poc / support).
foreach ($s in @("knowledge","diagnostics","security","release","poc","support","docs")) {
  if (Test-Path "$RepoRoot/scripts/$s") { Copy-Item "$RepoRoot/scripts/$s" "$stage/scripts/$s" -Recurse -ErrorAction SilentlyContinue }
}

# Curated docs set: deployment/runbooks + manuals + handover + knowledge base + known limitations + reports.
$docList = @(
  "README.md","FINAL_CUSTOMER_HANDOVER_INDEX.md","FINAL_RELEASE_OVERVIEW.md","FINAL_LOCAL_DEPLOYMENT_GUIDE.md",
  "FINAL_KNOWLEDGE_BASE_GUIDE.md","Included-Knowledge-Base-Catalog.md","Deployment-Guide.md",
  "Industrial-Installation-Guide.md","SQL-Server-Deployment-Guide.md","Windows-Server-IIS-Deployment-Guide.md",
  "SQL-Express-Pilot-Deployment.md","Full-SQL-Server-Deployment.md","Docker-Deployment-Guide.md","Offline-Mode-Guide.md",
  "Backup-Restore-Runbook.md","Upgrade-Rollback-Runbook.md","Database-Setup-Guide.md",
  "User-Manual.md","Admin-Manual.md","Operator-Manual.md","Developer-Manual.md","Quick-Start-With-Screenshots.md",
  "Support-Runbook.md","07-Troubleshooting.md","Supportability-Dashboard-Spec.md",
  "Known-Limitations.md","Security-Model.md","Secrets-Handling.md","AI-Governance.md",
  "Industrial-Ship-Readiness-Certificate.md","Final-Ship-Ready-Completion-Report.md","readiness-scorecard.json"
)
foreach ($d in $docList) { Copy-Item "$RepoRoot/docs/$d" "$stage/docs/" -ErrorAction SilentlyContinue }
Copy-Item "$RepoRoot/docs/screenshots/*.png" "$stage/docs/screenshots/" -ErrorAction SilentlyContinue
Copy-Item "$RepoRoot/RELEASE_NOTES.md" $stage -ErrorAction SilentlyContinue

# Release manifest (version, contents, checksums) + customer acceptance checklist.
& (Join-Path $PSScriptRoot "create-release-manifest.ps1") -Stage $stage -ErrorAction SilentlyContinue

$stamp = (Get-Date).ToString("yyyyMMdd-HHmmss")
$zip = Join-Path (Resolve-Path $OutDir) "LocalAIFactory-release-$stamp.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path "$stage/*" -DestinationPath $zip
Write-Host "PACKAGE OK: $zip ($([math]::Round((Get-Item $zip).Length/1MB,1)) MB)" -ForegroundColor Green
Write-Host "Stage retained at: $stage (for verify-release-package.ps1)" -ForegroundColor Cyan
exit 0
