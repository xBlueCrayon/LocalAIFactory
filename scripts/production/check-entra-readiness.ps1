<#
.SYNOPSIS  Check Entra ID / OIDC readiness by reusing the SSO config + claims-mapping validators. READ-ONLY.
#>
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
function Row($s,$d){ $c=switch($s){'PASS'{'Green'}'PARTIAL'{'Yellow'}default{'Magenta'}}; Write-Host ("  [{0,-18}] {1}" -f $s,$d) -ForegroundColor $c }
Write-Host "== Entra ID / OIDC readiness ==" -ForegroundColor Cyan
$cfg = "$repo/scripts/sso/check-oidc-config.ps1"; $map = "$repo/scripts/sso/validate-claims-mapping.ps1"
Row $(if(Test-Path $cfg){'PASS'}else{'PARTIAL'}) "OIDC config validator present"
Row $(if(Test-Path $map){'PASS'}else{'PARTIAL'}) "claims->UserRole mapping validator present"
Row $(if(Test-Path "$repo/operator-emulation/entra-oidc-inputs.example.json"){'PASS'}else{'PARTIAL'}) "Entra inputs EMULATED (placeholder tenant)"
Row "BLOCKED_EXTERNAL" "a real Entra tenant + app registration + client secret/cert (operator/identity)"
Write-Host "`n  OIDC plumbing + validators READY; binding to a real tenant is an operator task. No live Entra sign-in performed." -ForegroundColor Yellow
