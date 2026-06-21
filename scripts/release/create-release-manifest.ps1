<#
.SYNOPSIS  R2-ACC-FINAL: write a release manifest (contents + counts + checksums) into a staged package.
.DESCRIPTION Read-only over the repo; writes RELEASE_MANIFEST.json + RELEASE_MANIFEST.md into the stage folder.
#>
param([Parameter(Mandatory)][string]$Stage)

$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$version = (Test-Path "$repo/release-template/VERSION") ? (Get-Content "$repo/release-template/VERSION" -Raw).Trim() : "1.0.0-rc"

$files = Get-ChildItem $Stage -Recurse -File
$packs = Get-ChildItem (Join-Path $Stage "knowledge-packs") -Directory -ErrorAction SilentlyContinue |
  Where-Object { Test-Path (Join-Path $_.FullName "manifest.json") }
# checksum a few headline files
$key = @("app/LocalAIFactory.Web.dll") | ForEach-Object {
  $p = Join-Path $Stage $_
  if (Test-Path $p) { [ordered]@{ file=$_; sha256=(Get-FileHash $p -Algorithm SHA256).Hash; bytes=(Get-Item $p).Length } }
}

$manifest = [ordered]@{
  product = "LocalAIFactory"
  version = $version
  kind    = "customer-handover-package"
  fileCount = $files.Count
  totalBytes = ($files | Measure-Object Length -Sum).Sum
  knowledgePacks = ($packs | ForEach-Object { $_.Name })
  topLevel = (Get-ChildItem $Stage | Select-Object -ExpandProperty Name)
  keyChecksums = $key
  note = "MSSQL is the runtime source of truth; JSON knowledge packs are the seed format. No DB file, backup, secret, or model file is included."
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content "$Stage/RELEASE_MANIFEST.json" -Encoding UTF8

$md = @"
# LocalAIFactory Release Manifest

- **Product:** LocalAIFactory
- **Version:** $version (customer-handover package)
- **Files:** $($files.Count)  ·  **Total size:** $([math]::Round(($files|Measure-Object Length -Sum).Sum/1MB,1)) MB
- **Knowledge packs:** $($packs.Count) ($([string]::Join(', ', ($packs|ForEach-Object{$_.Name}))))

## Top-level contents
$([string]::Join("`n", ((Get-ChildItem $Stage | ForEach-Object { "- $($_.Name)" }))))

This package contains source-controlled scripts, the published app, the included knowledge base (JSON packs),
database setup scripts, docs and manuals. It contains **no** database file, backup, secret, key, or model file.
"@
$md | Set-Content "$Stage/RELEASE_MANIFEST.md" -Encoding UTF8
Write-Host "Manifest written: $Stage/RELEASE_MANIFEST.{json,md} (version $version, $($files.Count) files)" -ForegroundColor Green
