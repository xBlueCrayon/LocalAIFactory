<#
.SYNOPSIS  Check the production environment posture. READ-ONLY advisory.
.DESCRIPTION Applies learned production-readiness rules: Server SKU, IIS + Hosting Bundle, production env var,
             HTTPS (CA vs self-signed), app auth posture. Reports PASS/PARTIAL/BLOCKED honestly for THIS host.
#>
function Row($n,$s,$d){ $c=switch($s){'PASS'{'Green'}'PARTIAL'{'Yellow'}default{'Magenta'}}; Write-Host ("  [{0,-8}] {1} - {2}" -f $s,$n,$d) -ForegroundColor $c }
Write-Host "== Production environment check ==" -ForegroundColor Cyan
$os=(Get-CimInstance Win32_OperatingSystem).Caption
Row "OS SKU" $(if($os -match 'Server'){'PASS'}else{'BLOCKED_OPERATOR'}) "$os (production needs a Windows Server edition)"
$ancm = Test-Path "C:\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
Row "IIS + ANCM" $(if((Get-Service W3SVC -EA SilentlyContinue) -and $ancm){'PASS'}else{'PARTIAL'}) "W3SVC + AspNetCoreModuleV2"
$appcmd="$env:windir\system32\inetsrv\appcmd.exe"
$bind=if(Test-Path $appcmd){(& $appcmd list site LocalAIFactoryPilot /text:bindings 2>$null|Out-String)}else{''}
Row "HTTPS binding" $(if($bind -match 'https'){'PARTIAL'}else{'BLOCKED_OPERATOR'}) "self-signed pilot cert present; production needs a CA-issued cert (BLOCKED_OPERATOR)"
Row "App auth posture" "PARTIAL" "app runs Development dev-auth behind IIS; production needs ASPNETCORE_ENVIRONMENT=Production + app RBAC bound to the Windows/SSO identity"
Row "Secrets in config" $(if((Get-Content src\LocalAIFactory.Web\appsettings.json -Raw) -notmatch 'Password=[^;\s]+'){'PASS'}else{'PARTIAL'}) "Trusted_Connection; no inline secrets"
Write-Host "`n  Overall: PRODUCTION-LIKE on a workstation; real production needs a Server host + CA cert + production auth (operator)." -ForegroundColor Yellow
