<#
.SYNOPSIS  Check monitoring/observability readiness. READ-ONLY.
.DESCRIPTION Confirms the built-in observability surface (request-timing, health cache, /Support bundle, structured
             logs) and reports what an external SIEM/alerting pipeline still needs from the operator.
#>
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
function Row($s,$d){ $c=switch($s){'PASS'{'Green'}'PARTIAL'{'Yellow'}default{'Magenta'}}; Write-Host ("  [{0,-18}] {1}" -f $s,$d) -ForegroundColor $c }
Write-Host "== Monitoring readiness ==" -ForegroundColor Cyan
Row $(if(Get-ChildItem $repo/src -Recurse -Filter RequestTimingMiddleware.cs -EA SilentlyContinue){'PASS'}else{'PARTIAL'}) "request-timing middleware emits per-request latency/status"
Row $(if(Get-ChildItem $repo/src -Recurse -Filter *.cs -EA SilentlyContinue | Select-String -Pattern 'IServiceHealthCache' -List -EA SilentlyContinue){'PASS'}else{'PARTIAL'}) "health cache snapshot (Qdrant/Ollama/SQL)"
Row $(if(Get-ChildItem $repo/src -Recurse -Filter SupportController.cs -EA SilentlyContinue){'PASS'}else{'PARTIAL'}) "/Support diagnostic bundle endpoint"
Row "BLOCKED_OPERATOR" "external SIEM/alerting sink (Seq/App Insights/ELK) + on-call alert NOT wired (operator)"
Write-Host "`n  In-app observability READY; production alerting needs an operator-supplied sink + alert rule. No live alert fired." -ForegroundColor Yellow
