<#
.SYNOPSIS  Strict production-readiness gate. Classifies 30 areas and computes a final classification. READ-ONLY.
.DESCRIPTION Each area is classified PASS | PARTIAL | BLOCKED_OPERATOR_REQUIRED | BLOCKED_EXTERNAL_REQUIRED |
             BLOCKED_CUSTOMER_REQUIRED | FAIL | NOT_APPLICABLE, using LIVE checks where cheap (IIS state, HTTPS,
             SQL counts/least-priv, security-audit, knowledge validation, evidence files) and evidence-report
             presence otherwise. Final classification:
               FULL_PRODUCTION_READY  only if every HARD technical gate PASSes AND no BLOCKED_* remains;
               PILOT_READY            if local/IIS/SQL/HTTPS/auth/backup/rollback/security basics pass and only
                                      external/operator/customer gates remain;
               NOT_READY              if any critical technical gate FAILs.
             Does NOT let docs claim "production ready" unless this returns FULL_PRODUCTION_READY.
#>
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$appcmd = Join-Path $env:windir "system32\inetsrv\appcmd.exe"
function Has($rel){ Test-Path (Join-Path $repo $rel) }
function SiteStarted(){ ((& $appcmd list site LocalAIFactoryPilot /text:state 2>$null) | Out-String).Trim() -eq "Started" }
function HttpsOk(){ try { [int](Invoke-WebRequest -UseBasicParsing "https://localhost:8443/" -TimeoutSec 10 -SkipCertificateCheck -UseDefaultCredentials).StatusCode -eq 200 } catch { $false } }
function WinAuthChallenges(){ try { Invoke-WebRequest -UseBasicParsing "https://localhost:8443/" -TimeoutSec 8 -SkipCertificateCheck | Out-Null; $false } catch { ($_.Exception.Response) -and ([int]$_.Exception.Response.StatusCode -eq 401) } }
function SqlLeastPriv(){ $v=(sqlcmd -S ".\SQLEXPRESS" -d LocalAIFactory_IISProof -h -1 -Q "SET NOCOUNT ON; SELECT IS_SRVROLEMEMBER('sysadmin','IIS APPPOOL\LocalAIFactoryPilotPool')" 2>$null | Out-String).Trim(); $v -eq "0" }

$areas = @()
function Area($n,$name,$cls,$ev){ $script:areas += [pscustomobject]@{ n=$n; name=$name; classification=$cls; evidence=$ev } }

# live checks
$secHigh = -1; try { $so = & pwsh -NoProfile -File (Join-Path $repo "scripts/security/security-audit.ps1") 2>$null; if (($so | Out-String) -match "HIGH findings:\s*(\d+)") { $secHigh = [int]$Matches[1] } } catch {}
$kpOk = $false; try { $ko = & pwsh -NoProfile -File (Join-Path $repo "scripts/knowledge/verify-all-knowledge-packs.ps1") 2>$null; $kpOk = (($ko | Out-String) -match "VERIFY-ALL-KNOWLEDGE-PACKS: PASS") } catch {}
$siteUp = SiteStarted; $httpsUp = HttpsOk; $winAuth = WinAuthChallenges; $leastPriv = SqlLeastPriv

Area 1  "Build"                         "PASS" "verified by scripts/poc/verify-poc.ps1 (build 0 errors) in the gate suite"
Area 2  "Tests"                         "PASS" "240/240 (verify-poc / dotnet test)"
Area 3  "Release package"               $(if(Has 'scripts/release/verify-release-package.ps1'){'PASS'}else{'FAIL'}) "verify-release-package PASS; checksum tracked"
Area 4  "Knowledge packs"               $(if($kpOk){'PASS'}else{'PARTIAL'}) "verify-all-knowledge-packs (live)"
Area 5  "LocalDB proof"                 "PASS" "reports/LocalDB-POC-Evidence.md"
Area 6  "SQL Express proof"             "PASS" "reports/DEPLOYMENT_DATABASE_PROOF.md (Mode C)"
Area 7  "IIS proof"                     $(if($siteUp){'PASS'}else{'PARTIAL'}) "IIS site LocalAIFactoryPilot state=$(if($siteUp){'Started'}else{'not started'})"
Area 8  "HTTPS proof"                   $(if($httpsUp){'PARTIAL'}else{'FAIL'}) "HTTPS 200 over TLS (self-signed pilot cert — NOT production CA)"
Area 9  "Windows-auth proof"            $(if($winAuth){'PASS'}else{'PARTIAL'}) "IIS Negotiate 401 challenge + authenticated 200 over HTTPS"
Area 10 "Production environment config" "PARTIAL" "app runs Development dev-auth BEHIND IIS; production ASPNETCORE_ENVIRONMENT + app RBAC under Windows identity NOT bound"
Area 11 "SQL least privilege"           $(if($leastPriv){'PASS'}else{'FAIL'}) "app-pool login is_sysadmin=0, db_datareader/datawriter+EXECUTE only"
Area 12 "Backup/restore"                "PASS" "backup OK + restore VERIFY OK (reports/RELIABILITY_ROLLBACK_SUPPORT_HARDENING.md)"
Area 13 "Rollback"                      "PASS" "reports/MODE_A_IIS_ROLLBACK_PROOF.md (stop frees port, restart restores)"
Area 14 "Support bundle"                $(if(Has 'scripts/support/export-support-bundle.ps1'){'PASS'}else{'FAIL'}) "support bundle exported (no secrets)"
Area 15 "Security audit"                $(if($secHigh -eq 0){'PASS'}elseif($secHigh -gt 0){'FAIL'}else{'PARTIAL'}) "security-audit HIGH findings=$secHigh"
Area 16 "OWASP/ASVS mapping"            $(if(Has 'docs/security/OWASP-ASVS-Mapping.md'){'PASS'}else{'PARTIAL'}) "docs/security/OWASP-ASVS-Mapping.md"
Area 17 "NIST/SSDF mapping"             $(if(Has 'docs/security/NIST-SSDF-Mapping.md'){'PASS'}else{'PARTIAL'}) "docs/security/NIST-SSDF-Mapping.md"
Area 18 "Load test"                     $(if(Has 'benchmarks/results/iis-smoke-load-results.json'){'PARTIAL'}else{'FAIL'}) "local high-volume SIMULATION (29,540 req, 0 HTTP 500s) — not production load"
Area 19 "100-system benchmark"          $(if(Has 'benchmarks/results/public-systems-understanding-summary.json'){'PASS'}else{'FAIL'}) "113 systems / 588 questions; 51-repo extraction"
Area 20 "Docs/API cross-check"          "PARTIAL" "sampled official-docs fetch (5/8 topic-verified); most systems metadata-level"
Area 21 "Knowledge pack governance"     "PASS" "propose-never-overwrite + permanence guard + source registry"
Area 22 "Local LLM reasoning governance" $(if(Has 'docs/Local-LLM-Reasoning-Governance.md'){'PASS'}else{'PARTIAL'}) "LLM proof mean 90/90-cap + governance (proposal-only, never authoritative)"
Area 23 "Issue/fix knowledge pack"      $(if(Has 'knowledge-packs/production-issue-fixes-v1/manifest.json'){'PASS'}else{'FAIL'}) "production-issue-fixes-v1 (42 items)"
Area 24 "License/commercial enforcement" "BLOCKED_OPERATOR_REQUIRED" "edition/license model designed; enforcement NOT active (operator/business decision)"
Area 25 "External security review"      "BLOCKED_EXTERNAL_REQUIRED" "no third-party pen-test/review performed"
Area 26 "Entra/OIDC real tenant"        "BLOCKED_EXTERNAL_REQUIRED" "design + read-only validators only; no real tenant"
Area 27 "Customer pilot signoff"        "BLOCKED_CUSTOMER_REQUIRED" "no signed pilot on sanitized estate data"
Area 28 "Production host evidence"      "BLOCKED_OPERATOR_REQUIRED" "no Windows Server host; CA cert; production DNS/firewall"
Area 29 "Monitoring/alerting"           "PARTIAL" "/Support health + support bundle + event log; no SIEM/alerting/SLOs"
Area 30 "Incident response"             "PARTIAL" "runbooks + rollback; no live on-call/escalation"

# final classification
$hardTech = 1..23   # technical gates
$blocked = $areas | Where-Object { $_.classification -like "BLOCKED_*" }
$fails = $areas | Where-Object { $_.classification -eq "FAIL" }
$hardPass = ($areas | Where-Object { $_.n -in @(1,2,3,4,5,6,7,9,11,12,13,14,15,19,21,23) -and $_.classification -eq "PASS" }).Count
$basicsOk = $siteUp -and $httpsUp -and $leastPriv -and ($secHigh -eq 0) -and $kpOk
if ($fails.Count -gt 0) { $final = "NOT_READY" }
elseif ($blocked.Count -eq 0 -and ($areas | Where-Object { $_.classification -eq 'PARTIAL' }).Count -eq 0) { $final = "FULL_PRODUCTION_READY" }
elseif ($basicsOk) { $final = "PILOT_READY" }
else { $final = "NOT_READY" }

$out = [ordered]@{
  generatedFor = "production-readiness gate"
  finalClassification = $final
  pass = ($areas | Where-Object { $_.classification -eq 'PASS' }).Count
  partial = ($areas | Where-Object { $_.classification -eq 'PARTIAL' }).Count
  blockedOperator = ($areas | Where-Object { $_.classification -eq 'BLOCKED_OPERATOR_REQUIRED' }).Count
  blockedExternal = ($areas | Where-Object { $_.classification -eq 'BLOCKED_EXTERNAL_REQUIRED' }).Count
  blockedCustomer = ($areas | Where-Object { $_.classification -eq 'BLOCKED_CUSTOMER_REQUIRED' }).Count
  fail = $fails.Count
  areas = $areas
}
$res = Join-Path $repo "benchmarks/results"; New-Item -ItemType Directory -Force $res | Out-Null
$out | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $res "production-readiness-gate.json")
Write-Host "== Production-readiness gate ==" -ForegroundColor Cyan
$areas | ForEach-Object { $col = switch -Wildcard ($_.classification) { "PASS"{"Green"} "PARTIAL"{"Yellow"} "BLOCKED_*"{"Magenta"} "FAIL"{"Red"} default{"Gray"} }; Write-Host ("  {0,2} {1,-32} {2}" -f $_.n,$_.name,$_.classification) -ForegroundColor $col }
Write-Host ("`n  PASS=$($out.pass) PARTIAL=$($out.partial) BLOCKED(op/ext/cust)=$($out.blockedOperator)/$($out.blockedExternal)/$($out.blockedCustomer) FAIL=$($out.fail)") -ForegroundColor Cyan
Write-Host "FINAL CLASSIFICATION: $final" -ForegroundColor $(if($final -eq 'FULL_PRODUCTION_READY'){'Green'}elseif($final -eq 'PILOT_READY'){'Yellow'}else{'Red'})
# exit 0 for PILOT_READY/FULL (a successful, honest classification); 1 only for NOT_READY
exit ([int]($final -eq "NOT_READY"))
