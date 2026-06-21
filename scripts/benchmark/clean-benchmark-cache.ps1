<#
.SYNOPSIS  Remove the public-project benchmark clone cache (git-ignored). Safe.
.PARAMETER CacheDir
#>
param([string]$CacheDir = ".tmp-benchmark-repos")
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$cache = Join-Path $repo $CacheDir
if (Test-Path $cache) {
  $sz = [math]::Round(((Get-ChildItem $cache -Recurse -File -Force -EA SilentlyContinue | Measure-Object Length -Sum).Sum/1MB),1)
  Remove-Item $cache -Recurse -Force -EA SilentlyContinue
  Write-Host "Removed $cache (~${sz} MB)." -ForegroundColor Green
} else { Write-Host "$cache not present (nothing to clean)." -ForegroundColor Yellow }
