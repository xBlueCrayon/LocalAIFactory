<#
.SYNOPSIS  Deployment drill 00 — check prerequisites for a Windows Server / SQL deployment. READ-ONLY.
.DESCRIPTION Verifies the tools a real deployment needs are present. Changes nothing. Part of the operator-gated
             production-deployment drill pack (see docs/Production-Deployment-Drill-Pack.md).
#>
$ErrorActionPreference = "Continue"
$ok = 0; $miss = 0
function Check($name, $present, $hint) {
  if ($present) { Write-Host "  [ OK ] $name" -ForegroundColor Green; $script:ok++ }
  else { Write-Host "  [MISS] $name — $hint" -ForegroundColor Yellow; $script:miss++ }
}
Write-Host "== Deployment prerequisites (read-only) ==" -ForegroundColor Cyan
Check ".NET SDK/runtime" ([bool](Get-Command dotnet -EA SilentlyContinue)) "install .NET 10 runtime + ASP.NET Core Hosting Bundle"
Check "sqlcmd" ([bool](Get-Command sqlcmd -EA SilentlyContinue)) "install SQL command-line tools"
Check "PowerShell 5.1+/7" ($PSVersionTable.PSVersion.Major -ge 5) "use Windows PowerShell 5.1+ or pwsh 7"
$admin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
Check "running as Administrator (needed for -Execute later)" $admin "re-run an elevated shell for the -Execute steps"
$drive = (Get-PSDrive C -EA SilentlyContinue)
Check "disk free >= 5 GB on C:" ($drive -and ($drive.Free/1GB) -ge 5) "free up disk before deploying"
Check "IIS feature manager (Get-WindowsOptionalFeature)" ([bool](Get-Command Get-WindowsOptionalFeature -EA SilentlyContinue)) "Windows Server / Win client with IIS optional features"
Write-Host "`nPrerequisites: $ok present, $miss missing." -ForegroundColor Cyan
Write-Host "This is a dry-run check; it installs nothing. Proceed to 01-check-host.ps1." -ForegroundColor Yellow
