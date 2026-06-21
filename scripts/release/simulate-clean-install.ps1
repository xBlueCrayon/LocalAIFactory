<#
.SYNOPSIS  R2-ACC-FINAL: simulate a customer install from the release ZIP into a clean folder.
.DESCRIPTION Extracts the newest release ZIP into a fresh, git-ignored folder and runs the package verification
             against the extracted copy (proving the ZIP — not the repo — is self-sufficient). Read-only with
             respect to the repo; writes only into the throwaway clean folder. A true fresh-VM/server proof is
             out of scope on a dev box and is documented as the remaining external proof.
.PARAMETER Zip   Path to a release ZIP (default: newest under ./.tmp-release).
.PARAMETER Dest  Clean extraction folder (default ./.tmp-clean-install).
#>
param([string]$Zip = "", [string]$Dest = "./.tmp-clean-install")

$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
if (-not $Zip) {
  $Zip = (Get-ChildItem "$repo/.tmp-release/LocalAIFactory-release-*.zip" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1)?.FullName
}
if (-not $Zip -or -not (Test-Path $Zip)) { Write-Host "No release ZIP found — run build-release.ps1 then package-release.ps1." -ForegroundColor Red; exit 1 }

if (Test-Path $Dest) { Remove-Item $Dest -Recurse -Force }
New-Item -ItemType Directory -Force -Path $Dest | Out-Null
Write-Host "== Extracting $([IO.Path]::GetFileName($Zip)) -> $Dest ==" -ForegroundColor Cyan
Expand-Archive -Path $Zip -DestinationPath $Dest -Force

Write-Host "`n== Extracted top-level contents ==" -ForegroundColor Cyan
Get-ChildItem $Dest | ForEach-Object { "  $($_.Name)" }

Write-Host "`n== Verify the EXTRACTED package (not the repo) ==" -ForegroundColor Cyan
& (Join-Path $PSScriptRoot "verify-release-package.ps1") -Stage $Dest
$code = $LASTEXITCODE

Write-Host "`n== Clean-install simulation note ==" -ForegroundColor Yellow
Write-Host "  This proves the ZIP is self-contained (app + knowledge base + DB scripts + docs + scripts)."
Write-Host "  A TRUE clean-machine proof still requires extracting on a fresh Windows host with .NET 10 +"
Write-Host "  SQL Server/LocalDB, running database/setup-full-local-demo.ps1, and starting the app."
exit $code
