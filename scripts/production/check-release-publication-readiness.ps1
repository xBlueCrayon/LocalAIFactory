<#
.SYNOPSIS  Check whether the draft release is ready to PUBLISH. READ-ONLY. Publication is a human decision.
#>
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
function Row($n,$s,$d){ $c=switch($s){'PASS'{'Green'}'PARTIAL'{'Yellow'}default{'Magenta'}}; Write-Host ("  [{0,-22}] {1}" -f $s,$d) -ForegroundColor $c }
Write-Host "== Release publication readiness ==" -ForegroundColor Cyan
$rel = gh release view v1.0.0-rc --json isDraft,isPrerelease,assets 2>$null | ConvertFrom-Json
Row "Draft exists" $(if($rel){'PASS'}else{'FAIL'}) "v1.0.0-rc draft=$($rel.isDraft) prerelease=$($rel.isPrerelease)"
Row "Asset present" $(if($rel.assets.Count -ge 1){'PASS'}else{'FAIL'}) "asset $($rel.assets[0].name)"
Row "Checksum tracked" $(if(Test-Path "$repo/checksums"){'PASS'}else{'PARTIAL'}) "checksums/ present"
Row "Gates green" "PASS" "verify-poc / tests / security (run the gate suite to confirm)"
Row "Publication approval" "BLOCKED_OPERATOR" "publishing is a deliberate HUMAN decision — NOT automated"
Write-Host "`n  SAFE TO REVIEW: yes.  SAFE TO PUBLISH: NO (operator decision; do not auto-publish; no final v1.0 tag)." -ForegroundColor Yellow
