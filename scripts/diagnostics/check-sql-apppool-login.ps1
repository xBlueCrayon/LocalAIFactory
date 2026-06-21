<#
.SYNOPSIS  Diagnose the IIS app-pool SQL login + least-privilege (sysadmin check). READ-ONLY.
#>
param([string]$Server=".\SQLEXPRESS",[string]$Database="LocalAIFactory_IISProof",[string]$AppPool="LocalAIFactoryPilotPool")
$login="IIS APPPOOL\$AppPool"
Write-Host "== SQL app-pool login check ($login on $Server/$Database) ==" -ForegroundColor Cyan
$exists=(sqlcmd -S $Server -h -1 -Q "SET NOCOUNT ON; SELECT CASE WHEN SUSER_ID(N'$login') IS NULL THEN 0 ELSE 1 END" 2>$null|Out-String).Trim()
if($exists -eq "1"){ Write-Host "  [ OK ] login exists" -ForegroundColor Green } else { Write-Host "  [FAIL] login '$login' missing (fix: CREATE LOGIN [$login] FROM WINDOWS)" -ForegroundColor Red }
$user=(sqlcmd -S $Server -d $Database -h -1 -Q "SET NOCOUNT ON; SELECT CASE WHEN DATABASE_PRINCIPAL_ID(N'$login') IS NULL THEN 0 ELSE 1 END" 2>$null|Out-String).Trim()
if($user -eq "1"){ Write-Host "  [ OK ] db user mapped in $Database" -ForegroundColor Green } else { Write-Host "  [FAIL] user not mapped (fix: CREATE USER [$login] FOR LOGIN [$login] + grant datareader/datawriter/EXECUTE)" -ForegroundColor Red }
$sa=(sqlcmd -S $Server -h -1 -Q "SET NOCOUNT ON; SELECT IS_SRVROLEMEMBER('sysadmin',N'$login')" 2>$null|Out-String).Trim()
if($sa -eq "0"){ Write-Host "  [ OK ] NOT sysadmin (least privilege)" -ForegroundColor Green } else { Write-Host "  [WARN] login is sysadmin — over-privileged for a runtime account" -ForegroundColor Yellow }
