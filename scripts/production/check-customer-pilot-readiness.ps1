<#
.SYNOPSIS  Check customer-pilot readiness. READ-ONLY. A real pilot needs the customer + sanitized data + signoff.
#>
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
function Row($s,$d){ $c=switch($s){'PASS'{'Green'}'PARTIAL'{'Yellow'}default{'Magenta'}}; Write-Host ("  [{0,-18}] {1}" -f $s,$d) -ForegroundColor $c }
Write-Host "== Customer pilot readiness ==" -ForegroundColor Cyan
Row $(if(Test-Path "$repo/docs/Customer-Acceptance-Test.md"){'PASS'}else{'PARTIAL'}) "acceptance test defined"
Row $(if(Test-Path "$repo/docs/CUSTOMER_HANDOVER_WALKTHROUGH.md"){'PASS'}else{'PARTIAL'}) "handover walkthrough present"
Row $(if(Test-Path "$repo/operator-emulation/customer-pilot-signoff.example.json"){'PASS'}else{'PARTIAL'}) "pilot signoff EMULATED (placeholder)"
Row "BLOCKED_CUSTOMER" "sanitized estate data + signed acceptance NOT supplied (customer required)"
Write-Host "`n  Pilot is READY TO RUN once the customer provides sanitized data + signs acceptance. No real pilot executed." -ForegroundColor Yellow
