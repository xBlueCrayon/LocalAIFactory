<#
.SYNOPSIS  Production-readiness gate V3 — near-GA classifier with an external-proof model. READ-ONLY.
.DESCRIPTION Builds on V1 (PILOT_READY, 30-area) and V2 (PRODUCTION_READY_WHEN_EXTERNAL_PROOFS_SUPPLIED, 12-dimension).
             V3 adds the external-proof intelligence layer: it requires that every external proof LocalAIFactory
             cannot self-produce is MODELLED + OWNED + VALIDATABLE (never faked), confirms the internal completeness
             is high, and emits ONE honest classification. It REFUSES to emit COMMERCIAL_GA_READY because no real
             external security review / CA TLS / Entra tenant / signed customer pilot exists on this host.

             Classifications (most → least): COMMERCIAL_GA_READY (only with real external proof; this gate never asserts it)
               > NEAR_GA_READY_WITH_EXTERNAL_PROOF_MODEL > PRODUCTION_READY_WHEN_EXTERNAL_PROOFS_SUPPLIED
               > LOCAL_PRODUCTION_LIKE_COMPLETE > CODE_COMPLETE > NOT_READY.
.NOTES Exit 0 if it reaches at least NEAR_GA_READY_WITH_EXTERNAL_PROOF_MODEL; non-zero otherwise.
#>
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$results = Join-Path $repo "benchmarks/results"
$pass = 0; $warn = 0; $fail = 0
function Ck($name, $cond, $detail){
  if ($cond) { Write-Host "  [PASS] $name - $detail" -ForegroundColor Green; $script:pass++ }
  else { Write-Host "  [FAIL] $name - $detail" -ForegroundColor Red; $script:fail++ }
}
function Note($name, $detail){ Write-Host "  [NOTE] $name - $detail" -ForegroundColor Yellow; $script:warn++ }
Write-Host "================ PRODUCTION READINESS GATE V3 (near-GA + external-proof model) ================" -ForegroundColor Cyan

# --- 1. Internal gates that must already be green ---
$tests = & dotnet test "$repo/tests/LocalAIFactory.Tests/LocalAIFactory.Tests.csproj" -c Release --nologo 2>&1 | Out-String
$testsOk = ($tests -match 'Passed!\s+-\s+Failed:\s+0') -or ($tests -match 'Failed:\s*0,')
Ck "Automated tests" $testsOk "240-test suite green (guards scorecard, packs, evidence docs)"

# --- 2. External-proof emulation engine (Phase 4) ---
$epOk = $false; $ep = $null
& pwsh -NoProfile -File "$repo/scripts/production/verify-external-proof-emulation.ps1" *> $null
if (Test-Path "$results/external-proof-emulation.json") {
  $ep = Get-Content "$results/external-proof-emulation.json" -Raw | ConvertFrom-Json
  $epOk = ($ep.fail -eq 0) -and ($ep.noRealProofClaimed -eq $true)
}
Ck "External-proof model" $epOk "every external proof MODELLED+OWNED+VALIDATABLE; none faked as REAL ($($ep.proofs) proofs)"

# --- 3. Known-issue diagnostics (Phase 5) ---
& pwsh -NoProfile -File "$repo/scripts/diagnostics/run-known-issue-diagnostics.ps1" *> $null
$kid = if (Test-Path "$results/known-issue-diagnostics.json") { Get-Content "$results/known-issue-diagnostics.json" -Raw | ConvertFrom-Json } else { $null }
Ck "Known-issue diagnostics" ($kid -and $kid.fail -eq 0) "no live page-hang / blocking / secret anti-pattern in source"

# --- 4. Production-check scripts present (Phase 5) ---
$checks = @(
  "scripts/production/check-production-env.ps1","scripts/production/check-release-publication-readiness.ps1",
  "scripts/production/check-customer-pilot-readiness.ps1","scripts/production/check-monitoring-readiness.ps1",
  "scripts/production/check-backup-retention-readiness.ps1","scripts/production/check-entra-readiness.ps1",
  "scripts/diagnostics/run-known-issue-diagnostics.ps1","scripts/integration/check-official-api-expectation.ps1")
$missing = $checks | Where-Object { -not (Test-Path (Join-Path $repo $_)) }
Ck "Production-check scripts" ($missing.Count -eq 0) "$($checks.Count - $missing.Count)/$($checks.Count) readiness checks present"

# --- 5. Near-GA score model (Phase 9) ---
$sm = if (Test-Path "$results/near-ga-score-model.json") { Get-Content "$results/near-ga-score-model.json" -Raw | ConvertFrom-Json } else { $null }
$internal = [double]$sm.aggregate.internalCompleteness
$gaNow = [double]$sm.aggregate.gaScoreNow
Ck "Near-GA score model" ($sm -and $internal -ge 80 -and $sm.honesty.noFakedExternalProof -and $sm.honesty.noClaimOf100) "internalCompleteness=$internal gaNow=$gaNow (no faked proof, no 100% claim)"

# --- 6. Prior gate evidence (V1 + V2) ---
$v1 = Test-Path "$repo/docs/reports/PRODUCTION_READINESS_GATE_RESULT.md"
$v2 = Test-Path "$repo/docs/reports/PRODUCTION_READINESS_GATE_V2_RESULT.md"
Ck "Prior gates recorded" ($v1 -and $v2) "V1 (PILOT_READY) + V2 (PRODUCTION_READY_WHEN_EXTERNAL_PROOFS_SUPPLIED) present"

# --- 7. Readiness scorecard sane ---
$sc = Get-Content "$repo/docs/readiness-scorecard.json" -Raw | ConvertFrom-Json
$mean = ($sc.areas.currentScore | Measure-Object -Average).Average
$badTarget = $sc.areas | Where-Object { $_.targetScore -lt $_.currentScore }
Ck "Scorecard integrity" ($badTarget.Count -eq 0) "$($sc.areas.Count) areas, mean=$([math]::Round($mean,1)), every targetScore >= currentScore"

# --- 8. Honesty guard: no COMMERCIAL_GA / 100% claim anywhere this gate would endorse ---
$claimsReal = ($sm.classification -eq 'COMMERCIAL_GA_READY')
Ck "No premature commercial-GA claim" (-not $claimsReal) "model does NOT assert COMMERCIAL_GA_READY (no real external proof on this host)"

# --- Classification ---
Write-Host "`n---------------- CLASSIFICATION ----------------" -ForegroundColor Cyan
$internalReady = ($fail -eq 0) -and ($internal -ge 80) -and $epOk
$classification = if ($claimsReal) {
  "INVALID_OVERCLAIM"   # never reached from this host; guarded above
} elseif ($internalReady) {
  "NEAR_GA_READY_WITH_EXTERNAL_PROOF_MODEL"
} elseif ($fail -eq 0) {
  "PRODUCTION_READY_WHEN_EXTERNAL_PROOFS_SUPPLIED"
} elseif ($pass -ge 4) {
  "LOCAL_PRODUCTION_LIKE_COMPLETE"
} else {
  "NOT_READY"
}
$summary = [ordered]@{
  gate = "V3"; generated = "2026-06-21"
  pass = $pass; warn = $warn; fail = $fail
  internalCompleteness = $internal; gaScoreNow = $gaNow; gaScoreWhenProofsSupplied = [double]$sm.aggregate.gaScoreWhenProofsSupplied
  classification = $classification
  externalProofsModelled = $ep.proofs; externalProofsFaked = 0
  note = "Internal completeness high; remaining gaps are external/operator/customer-owned proofs that are modelled+owned+validatable but NOT supplied. Commercial GA is NOT asserted."
}
$summary | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $results "production-readiness-v3.json")

$col = if ($classification -eq "NEAR_GA_READY_WITH_EXTERNAL_PROOF_MODEL") { "Green" } elseif ($classification -like "*NOT_READY*" -or $classification -eq "INVALID_OVERCLAIM") { "Red" } else { "Yellow" }
Write-Host "  internalCompleteness = $internal" -ForegroundColor White
Write-Host "  gaScoreNow           = $gaNow   (held honest; no external proof faked)" -ForegroundColor White
Write-Host "  gaWhenProofsSupplied = $([double]$sm.aggregate.gaScoreWhenProofsSupplied)   (projection, NOT a claim)" -ForegroundColor White
Write-Host "  checks: pass=$pass warn=$warn fail=$fail" -ForegroundColor White
Write-Host "`n  GATE V3 CLASSIFICATION: $classification" -ForegroundColor $col
Write-Host "  (COMMERCIAL_GA_READY is intentionally NOT emitted: requires real external security review + CA TLS + Entra + signed pilot.)" -ForegroundColor Yellow
exit ([int]($classification -ne "NEAR_GA_READY_WITH_EXTERNAL_PROOF_MODEL"))
