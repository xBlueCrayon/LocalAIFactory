<#
.SYNOPSIS  R2-ACC-FINAL: verify a staged (or extracted) release package is complete and safe.
.DESCRIPTION Read-only. Checks the package contains app binaries, the 4 knowledge packs, database scripts,
             appsettings examples, docs/manuals, a manifest — and contains NO secrets, DB files, backups, or
             oversized stray artifacts. Exits non-zero on any failure.
.PARAMETER Stage  Path to a staged package folder (or an extracted package root).
#>
param([string]$Stage = "./.tmp-release/stage")

$ErrorActionPreference = "Continue"
if (-not (Test-Path $Stage)) { Write-Host "Stage not found: $Stage (run package-release.ps1 first)" -ForegroundColor Red; exit 1 }
$Stage = (Resolve-Path $Stage).Path
$fail = 0
function Ok($m){ Write-Host "  [ OK ] $m" -ForegroundColor Green }
function Bad($m){ Write-Host "  [FAIL] $m" -ForegroundColor Red; $script:fail++ }

Write-Host "== Required contents ==" -ForegroundColor Cyan
if (Test-Path "$Stage/app/LocalAIFactory.Web.dll") { Ok "app binaries present (LocalAIFactory.Web.dll)" } else { Bad "app binaries missing" }
$packs = Get-ChildItem "$Stage/knowledge-packs" -Directory -ErrorAction SilentlyContinue | Where-Object { Test-Path "$($_.FullName)/manifest.json" }
if ($packs.Count -ge 4) { Ok "knowledge packs present ($($packs.Count))" } else { Bad "expected >=4 knowledge packs, found $($packs.Count)" }
if ((Get-ChildItem "$Stage/database" -Filter *.ps1 -ErrorAction SilentlyContinue).Count -ge 5) { Ok "database scripts present" } else { Bad "database scripts missing" }
if ((Get-ChildItem "$Stage" -Recurse -Filter "appsettings*.example.json" -ErrorAction SilentlyContinue).Count -ge 1) { Ok "appsettings examples present" } else { Bad "appsettings examples missing" }
foreach ($m in @("User-Manual.md","Admin-Manual.md","Operator-Manual.md")) {
  if (Test-Path "$Stage/docs/$m") { Ok "manual: $m" } else { Bad "missing manual: $m" }
}
if (Test-Path "$Stage/RELEASE_MANIFEST.json") { Ok "release manifest present" } else { Bad "release manifest missing" }
if (Test-Path "$Stage/RELEASE_NOTES.md") { Ok "release notes present" } else { Bad "release notes missing" }

Write-Host "`n== Safety (no secrets / forbidden artifacts) ==" -ForegroundColor Cyan
$forbidden = Get-ChildItem $Stage -Recurse -File -ErrorAction SilentlyContinue |
  Where-Object { $_.Extension -in @(".pfx",".pem",".key",".mdf",".ldf",".bak",".gguf",".onnx",".safetensors") -or $_.Name -eq "secrets.json" }
if ($forbidden) { $forbidden | ForEach-Object { Bad "forbidden file in package: $($_.Name)" } } else { Ok "no secret/db/backup/model files in package" }
# Oversized stray non-binary assets (screenshots/docs should be small; app dlls are allowed).
$bigDocs = Get-ChildItem "$Stage/docs" -Recurse -File -ErrorAction SilentlyContinue | Where-Object { $_.Length -gt 5MB }
if ($bigDocs) { $bigDocs | ForEach-Object { Bad "oversized doc asset (>5MB): $($_.Name)" } } else { Ok "no oversized doc/screenshot assets" }

Write-Host "`n== Result ==" -ForegroundColor Cyan
if ($fail -eq 0) { Write-Host "VERIFY-RELEASE-PACKAGE: PASS" -ForegroundColor Green; exit 0 }
Write-Host "VERIFY-RELEASE-PACKAGE: FAIL ($fail issue(s))" -ForegroundColor Red; exit 1
