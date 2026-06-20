<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: build a release publish of the web app (framework-dependent by default).
.DESCRIPTION Runs `dotnet publish` to an output folder (default ./.tmp-publish, git-ignored). Produces the
             runnable app + bundled knowledge packs + readiness scorecard. Does not deploy. Self-contained
             single-file builds are available via -SelfContained -Runtime <rid>.
#>
param(
  [string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../..").Path,
  [string]$Output = "./.tmp-publish",
  [switch]$SelfContained,
  [string]$Runtime = "win-x64"
)
$ErrorActionPreference = "Stop"
$args = @("publish", "$RepoRoot/src/LocalAIFactory.Web/LocalAIFactory.Web.csproj", "-c", "Release", "-o", $Output, "--nologo")
if ($SelfContained) { $args += @("--self-contained", "true", "-r", $Runtime, "-p:PublishSingleFile=true") }
Write-Host "Publishing -> $Output (selfContained=$SelfContained)" -ForegroundColor Cyan
& dotnet @args
if ($LASTEXITCODE -ne 0) { Write-Host "PUBLISH FAILED" -ForegroundColor Red; exit 1 }
$files = (Get-ChildItem $Output -Recurse -File).Count
Write-Host "PUBLISH OK: $files files in $Output" -ForegroundColor Green
exit 0
