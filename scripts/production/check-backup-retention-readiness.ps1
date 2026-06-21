<#
.SYNOPSIS  Check backup/restore/retention readiness. READ-ONLY.
#>
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
function Row($s,$d){ $c=switch($s){'PASS'{'Green'}'PARTIAL'{'Yellow'}default{'Magenta'}}; Write-Host ("  [{0,-18}] {1}" -f $s,$d) -ForegroundColor $c }
Write-Host "== Backup / retention readiness ==" -ForegroundColor Cyan
$bk = Get-ChildItem $repo/scripts -Recurse -Filter *backup*.ps1 -EA SilentlyContinue
$rs = Get-ChildItem $repo/scripts -Recurse -Filter *restore*.ps1 -EA SilentlyContinue
Row $(if($bk){'PASS'}else{'PARTIAL'}) "backup script present ($($bk.Count))"
Row $(if($rs){'PASS'}else{'PARTIAL'}) "restore-verify script present ($($rs.Count))"
Row $(if(Test-Path "$repo/docs/Backup-and-Recovery.md"){'PASS'}else{'PARTIAL'}) "documented RPO/RTO + retention policy"
Row "BLOCKED_OPERATOR" "scheduled offsite backups + a real restore drill on the production host (operator)"
Write-Host "`n  Backup tooling + procedure READY; a scheduled job + verified restore drill is an operator task. No production backup taken." -ForegroundColor Yellow
