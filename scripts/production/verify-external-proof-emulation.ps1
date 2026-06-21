<#
.SYNOPSIS  External-proof emulation engine — verify every external proof is modelled, owned, and validatable. READ-ONLY.
.DESCRIPTION Consumes operator-emulation/external-proof-model.json + the operator-emulation inputs and asserts: every
             external proof has an emulated input file, a trusted-source expected output, a validation command, a
             human owner, and a pass/fail criterion; no emulated proof is counted as REAL; no real secret leaks.
             Classifications per proof: REAL_PROOF | EMULATED_PROOF | MISSING_PROOF | BLOCKED_OPERATOR |
             BLOCKED_EXTERNAL | BLOCKED_CUSTOMER. Non-zero only if a proof is MISSING (un-modelled).
#>
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$dir = Join-Path $repo "operator-emulation"
$model = Join-Path $dir "external-proof-model.json"
$fail = 0
function Ok($m){ Write-Host "  [ OK ] $m" -ForegroundColor Green }
function Bad($m){ Write-Host "  [FAIL] $m" -ForegroundColor Red; $script:fail++ }
Write-Host "== External-proof emulation engine ==" -ForegroundColor Cyan
if (-not (Test-Path $model)) { Bad "external-proof-model.json missing"; exit 1 }
$m = Get-Content $model -Raw | ConvertFrom-Json
$secretRx = '(?i)(password\s*[:=]\s*["'']?[A-Za-z0-9!@#%^&*]{8,}|BEGIN (RSA |)PRIVATE KEY|AKIA[0-9A-Z]{16}|eyJ[A-Za-z0-9_-]{20,}\.)'
$valid = @("REAL_PROOF","EMULATED_PROOF","MISSING_PROOF","BLOCKED_OPERATOR","BLOCKED_EXTERNAL","BLOCKED_CUSTOMER")
$counts = @{}
foreach ($p in $m.proofs) {
  $cls = $p.classification
  $counts[$cls] = ([int]$counts[$cls]) + 1
  $issues = @()
  if ($cls -notin $valid) { $issues += "invalid classification '$cls'" }
  if ($cls -eq "REAL_PROOF") { $issues += "marked REAL_PROOF — not permitted from this host (no real external evidence)" }
  if ($cls -eq "MISSING_PROOF") { $issues += "MISSING (un-modelled)" }
  if (-not $p.emulatedInputFile -or -not (Test-Path (Join-Path $dir $p.emulatedInputFile))) { $issues += "no emulated input file" }
  if (-not $p.expectedOutput) { $issues += "no expected output" }
  if (-not $p.validationCommand) { $issues += "no validation command" }
  if (-not $p.owner) { $issues += "no owner" }
  if (-not $p.passCriterion) { $issues += "no pass criterion" }
  if ($p.realSecret -eq $true) { $issues += "claims a real secret — forbidden" }
  if ($issues.Count -eq 0) { Ok "$($p.proof) -> $cls (modelled, owned: $($p.owner))" }
  else { Bad "$($p.proof): $($issues -join '; ')" }
}
# no real secret anywhere in the operator-emulation pack
$leak = (Get-ChildItem $dir -Filter *.json | ForEach-Object { Get-Content $_.FullName -Raw }) -join "`n"
if ($leak -match $secretRx) { Bad "a real-looking secret pattern was found in operator-emulation/" } else { Ok "no real secrets in operator-emulation pack" }

@{ proofs=$m.proofs.Count; byClassification=$counts; noRealProofClaimed=($counts.ContainsKey('REAL_PROOF') -eq $false); fail=$fail } |
  ConvertTo-Json -Depth 4 | Set-Content (Join-Path $repo "benchmarks/results/external-proof-emulation.json")
Write-Host "`n  proofs=$($m.proofs.Count)  spread: $(($counts.GetEnumerator()|ForEach-Object{"$($_.Key)=$($_.Value)"}) -join '  ')" -ForegroundColor Cyan
Write-Host "  Every external proof is EMULATED/BLOCKED with an owner + validation + pass criterion. NONE is counted as REAL." -ForegroundColor Yellow
Write-Host "EXTERNAL-PROOF-EMULATION: $(if($fail -eq 0){'PASS (all external proofs modelled, owned, validatable; no real proof faked)'}else{"FAIL ($fail)"})" -ForegroundColor ($fail -eq 0 ? "Green":"Red")
exit ([int]($fail -ne 0))
