<#
.SYNOPSIS  Deployment drill 05 — deploy the release package to the target path. DRY-RUN by default.
.DESCRIPTION In dry-run it shows what would be copied. With -Execute it extracts the newest release ZIP and copies
             app/ to the target path (it stops the site first if it exists). It never deletes user data and never
             touches the database. Run 03 (DB) before this.
.PARAMETER PhysicalPath  IIS physical path (default C:\inetpub\LocalAIFactory). .PARAMETER Zip  release ZIP.
#>
param([string]$PhysicalPath = "C:\inetpub\LocalAIFactory", [string]$Zip = "", [switch]$Execute)
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
if (-not $Zip) { $Zip = (Get-ChildItem "$repo/.tmp-release/LocalAIFactory-release-*.zip" -EA SilentlyContinue | Sort-Object LastWriteTime -Desc | Select-Object -First 1)?.FullName }
Write-Host "== Deploy package ==" -ForegroundColor Cyan
if (-not $Zip -or -not (Test-Path $Zip)) { Write-Host "  No release ZIP found — run scripts/release/build-release.ps1 + package-release.ps1." -ForegroundColor Red; return }
Write-Host "  Package: $([IO.Path]::GetFileName($Zip))"
Write-Host "  Target : $PhysicalPath\app"
if (-not $Execute) {
  Write-Host "  DRY-RUN. Would: stop site (if any) -> extract ZIP -> copy app/ to '$PhysicalPath' -> start site." -ForegroundColor Yellow
  Write-Host "  Re-run elevated with -Execute on an approved host." -ForegroundColor Yellow
  return
}
$tmp = Join-Path $env:TEMP "laf-deploy-$(Get-Random)"
Expand-Archive -Path $Zip -DestinationPath $tmp -Force
New-Item -ItemType Directory -Force -Path $PhysicalPath | Out-Null
Copy-Item "$tmp/app/*" $PhysicalPath -Recurse -Force
Remove-Item $tmp -Recurse -Force -EA SilentlyContinue
Write-Host "  Copied app to $PhysicalPath. Start the IIS site, then run 06-run-healthchecks.ps1." -ForegroundColor Green
