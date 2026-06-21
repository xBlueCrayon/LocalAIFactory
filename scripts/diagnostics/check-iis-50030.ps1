<#
.SYNOPSIS  Diagnose IIS HTTP 500.30 (ASP.NET Core in-process startup failure). READ-ONLY.
.DESCRIPTION Reusable rule: "If IIS returns 500.30, check Event Viewer + stdout logs + appsettings + runtime
             bundle BEFORE changing code." Checks ANCM presence, recent ANCM/.NET error events, and the runtime.
#>
param([string]$Site="LocalAIFactoryPilot")
Write-Host "== IIS 500.30 startup diagnostic ($Site) ==" -ForegroundColor Cyan
$ancm = Test-Path "C:\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
Write-Host "  ANCM (AspNetCoreModuleV2): $(if($ancm){'present'}else{'ABSENT -> install the ASP.NET Core Hosting Bundle'})" -ForegroundColor $(if($ancm){'Green'}else{'Red'})
$rt = Get-ChildItem "C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App" -EA SilentlyContinue | Select-Object -ExpandProperty Name
Write-Host "  ASP.NET Core runtimes: $($rt -join ', ')"
Write-Host "  Recent ANCM / .NET Runtime error events (Application log, 30 min):"
Get-WinEvent -LogName Application -MaxEvents 100 -EA SilentlyContinue |
  Where-Object { $_.ProviderName -match 'IIS AspNetCore|\.NET Runtime|Application Error' -and $_.LevelDisplayName -in @('Error','Critical') -and $_.TimeCreated -gt (Get-Date).AddMinutes(-30) } |
  Select-Object -First 5 TimeCreated,ProviderName,Id | Format-Table -AutoSize
Write-Host "  Checklist: 1) Event Viewer (above)  2) stdout log (web.config stdoutLogEnabled)  3) appsettings/connection string  4) Hosting Bundle/ANCM  5) app-pool identity SQL login." -ForegroundColor Yellow
