# Removes build output and local runtime artifacts. Does NOT touch source or committed config.
# Usage: ./scripts/clean.ps1
$ErrorActionPreference = "SilentlyContinue"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "dotnet clean..." -ForegroundColor Cyan
dotnet clean (Join-Path $root "LocalAIFactory.sln") | Out-Null

Write-Host "Removing bin/obj/.vs and local runtime artifacts..." -ForegroundColor Cyan
$dirNames  = @("bin","obj",".vs","keys","_incoming","uploads","workspaces","logs")
foreach ($d in $dirNames) {
    Get-ChildItem -Path $root -Recurse -Directory -Filter $d -Force |
        ForEach-Object { Remove-Item $_.FullName -Recurse -Force }
}
$filePatterns = @("*.user","*.log","*.mdf","*.ldf","*.bak")
foreach ($p in $filePatterns) {
    Get-ChildItem -Path $root -Recurse -File -Filter $p -Force |
        ForEach-Object { Remove-Item $_.FullName -Force }
}

Write-Host "Clean complete. (Backup ZIPs and secrets are git-ignored, not auto-deleted.)" -ForegroundColor Green
