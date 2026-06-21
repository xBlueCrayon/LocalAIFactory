<#
.SYNOPSIS  Production-readiness gate V2 — richer classification incl. operator-emulation + integration + pullable repo.
.DESCRIPTION Builds on the V1 gate (30 areas) and adds 12 closure dimensions, then computes one of:
             NOT_READY | PILOT_READY | LOCAL_PRODUCTION_LIKE_READY | OPERATOR_EMULATION_READY |
             PRODUCTION_READY_WHEN_EXTERNAL_PROOFS_SUPPLIED | FULL_PRODUCTION_READY | COMMERCIAL_GA_READY.
             FULL/COMMERCIAL require REAL external production evidence (never produced from this host). If all
             local + emulation + integration + pullable gates pass and only external gates remain (emulated),
             returns PRODUCTION_READY_WHEN_EXTERNAL_PROOFS_SUPPLIED. READ-ONLY.
#>
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
function Has($rel){ Test-Path (Join-Path $repo $rel) }
function RunPass($rel,$marker){ try { $o = & pwsh -NoProfile -File (Join-Path $repo $rel) 2>&1 | Out-String; return ($o -match $marker) } catch { return $false } }

Write-Host "== Production-readiness gate V2 ==" -ForegroundColor Cyan
# V1 gate
$v1 = & pwsh -NoProfile -File (Join-Path $repo "scripts/production/verify-production-readiness.ps1") 2>&1 | Out-String
$v1Pilot = ($v1 -match "FINAL CLASSIFICATION: PILOT_READY")
$v1NotReady = ($v1 -match "FINAL CLASSIFICATION: NOT_READY")

$dims = [ordered]@{
  "1 Code complete"                     = (Has 'LocalAIFactory.sln') -and (Has 'tests/LocalAIFactory.Tests/PocReadinessTests.cs')
  "2 Local production-like proof"       = $v1Pilot
  "3 Operator emulation completeness"   = (Has 'operator-emulation') -and (RunPass 'scripts/production/run-operator-emulation-tests.ps1' 'OPERATOR-EMULATION-TESTS: PASS')
  "4 Integration expectation library"   = (Has 'benchmarks/integration-expectations') -and (RunPass 'scripts/integration/validate-integration-expectations.ps1' 'VALIDATE-INTEGRATION-EXPECTATIONS: PASS')
  "5 Public-system understanding"       = Has 'benchmarks/results/public-systems-understanding-summary.json'
  "6 Knowledge packs"                   = RunPass 'scripts/knowledge/verify-all-knowledge-packs.ps1' 'VERIFY-ALL-KNOWLEDGE-PACKS: PASS'
  "7 Local-LLM governance"              = (Has 'docs/Local-LLM-Reasoning-Governance.md') -and (Has 'docs/reports/LOCAL_LLM_REASONING_PROOF.md')
  "8 Workflow code-gen standard"        = Has 'docs/Workflow-Code-Generation-Standard.md'
  "9 Security mappings (ASVS/SSDF)"     = (Has 'docs/security/OWASP-ASVS-Mapping.md') -and (Has 'docs/security/NIST-SSDF-Mapping.md')
  "10 Load tests"                       = Has 'benchmarks/results/iis-smoke-load-results.json'
  "11 Pullable repo proof"              = Has 'docs/reports/FRESH_CLONE_PULLABLE_REPO_PROOF.md'
  "12 Release draft proof"              = Has 'docs/reports/FINAL_DRAFT_RELEASE_STATUS.md'
}
$pass = 0; $miss = @()
foreach ($k in $dims.Keys) { if ($dims[$k]) { $pass++; Write-Host "  [PASS] $k" -ForegroundColor Green } else { $miss += $k; Write-Host "  [MISS] $k" -ForegroundColor Yellow } }

# external gates are EMULATED only (never real from this host)
$externalEmulated = (Has 'operator-emulation/entra-oidc-inputs.example.json') -and (Has 'operator-emulation/external-security-review.example.json') -and (Has 'operator-emulation/customer-pilot-signoff.example.json')

if ($v1NotReady -or $miss.Count -gt 0) { $final = "NOT_READY" }
elseif ($pass -eq $dims.Count -and $externalEmulated) { $final = "PRODUCTION_READY_WHEN_EXTERNAL_PROOFS_SUPPLIED" }
elseif ($v1Pilot) { $final = "OPERATOR_EMULATION_READY" }
else { $final = "PILOT_READY" }

@{ gateVersion=2; dimensionsPassed=$pass; dimensionsTotal=$dims.Count; externalGatesEmulatedNotReal=$true; finalClassification=$final;
   note="FULL_PRODUCTION_READY / COMMERCIAL_GA_READY require REAL external production evidence (Windows Server, CA TLS, real Entra tenant, external pen-test, signed customer pilot) — NOT produced from this host." } |
  ConvertTo-Json -Depth 4 | Set-Content (Join-Path $repo "benchmarks/results/production-readiness-gate-v2.json")
Write-Host "`n  V2 dimensions: $pass/$($dims.Count) pass.  External gates: EMULATED only (not real)." -ForegroundColor Cyan
Write-Host "FINAL CLASSIFICATION (V2): $final" -ForegroundColor $(if($final -match 'NOT_READY'){'Red'}else{'Green'})
Write-Host "  (FULL_PRODUCTION_READY / COMMERCIAL_GA_READY are NOT claimable from this host — they need real external proofs.)" -ForegroundColor Yellow
exit ([int]($final -eq "NOT_READY"))
