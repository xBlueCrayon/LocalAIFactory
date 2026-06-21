<#
.SYNOPSIS  R2-ACC-FINAL: collect a read-only support bundle (diagnostics snapshots) into a single folder/zip.
.DESCRIPTION Runs the read-only diagnostics scripts and captures their output + key environment facts into a
             git-ignored .tmp-release/support-bundle folder, then zips it. Contains NO secrets, NO database
             contents, NO source — only environment/health snapshots an operator can send to support.
.PARAMETER OutDir  Output root (default ./.tmp-release).
#>
param([string]$OutDir = "./.tmp-release")

$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$stamp = "support-bundle"
$dest = Join-Path $OutDir $stamp
if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
New-Item -ItemType Directory -Force -Path $dest | Out-Null

function Capture($name, $script, $argList) {
  $p = Join-Path $repo $script
  if (-not (Test-Path $p)) { return }
  Write-Host "  capturing $name..." -ForegroundColor Cyan
  try { & $p @argList *>&1 | Out-File (Join-Path $dest "$name.txt") -Encoding UTF8 } catch { "ERROR: $($_.Exception.Message)" | Out-File (Join-Path $dest "$name.txt") }
}

Capture "system-snapshot"   "scripts/diagnostics/system-snapshot.ps1"   @()
Capture "gpu-health"        "scripts/diagnostics/gpu-health-check.ps1"   @()
Capture "ollama-health"     "scripts/diagnostics/ollama-health-check.ps1" @()
Capture "sql-health"        "scripts/diagnostics/sql-health-check.ps1"    @()
Capture "process-monitor"   "scripts/diagnostics/process-monitor.ps1"     @()
Capture "knowledge-verify"  "scripts/knowledge/verify-all-knowledge-packs.ps1" @()
Capture "security-audit"    "scripts/security/security-audit.ps1"        @()

# Environment facts (no secrets).
[ordered]@{
  timestampUtc = (Get-Date).ToUniversalTime().ToString("s") + "Z"
  machine = $env:COMPUTERNAME
  os = (Get-CimInstance Win32_OperatingSystem).Caption
  dotnet = (& dotnet --version 2>$null)
  gitCommit = (& git -C $repo rev-parse --short HEAD 2>$null)
  gitBranch = (& git -C $repo rev-parse --abbrev-ref HEAD 2>$null)
} | ConvertTo-Json | Out-File (Join-Path $dest "environment.json") -Encoding UTF8

$zip = Join-Path (Resolve-Path $OutDir) "LocalAIFactory-support-bundle.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path "$dest/*" -DestinationPath $zip
Write-Host "SUPPORT-BUNDLE: $zip ($([math]::Round((Get-Item $zip).Length/1KB,0)) KB)" -ForegroundColor Green
