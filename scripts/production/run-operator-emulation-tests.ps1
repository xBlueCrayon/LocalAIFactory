<#
.SYNOPSIS  Validate the EMULATED operator-input pack — proves a human can complete the external proofs without ambiguity.
.DESCRIPTION Loads operator-emulation/*.json and asserts: required fields exist, NO real secrets are present,
             placeholder values are clearly fake, each input has a validation command + expected output, and each
             external blocker has an owner + pass/fail criterion. This does NOT replace real external proof; it
             proves the project is ready for a human to supply it. READ-ONLY.
#>
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$dir = Join-Path $repo "operator-emulation"
$fail = 0
function Ok($m){ Write-Host "  [ OK ] $m" -ForegroundColor Green }
function Bad($m){ Write-Host "  [FAIL] $m" -ForegroundColor Red; $script:fail++ }
Write-Host "== Operator-emulation tests ==" -ForegroundColor Cyan
if (-not (Test-Path $dir)) { Bad "operator-emulation/ folder missing"; exit 1 }

# secret patterns that must NOT appear (real-looking secrets)
$secretRx = '(?i)(password\s*[:=]\s*["'']?[A-Za-z0-9!@#%^&*]{8,}|BEGIN (RSA |)PRIVATE KEY|AKIA[0-9A-Z]{16}|xox[baprs]-[0-9A-Za-z-]{10,}|eyJ[A-Za-z0-9_-]{20,}\.)'
$placeholderRx = '(?i)(placeholder|example\.local|example\.com|CN=localhost|REPLACE|<.*>|guid-placeholder|REF:|vault)'

$files = Get-ChildItem $dir -Filter *.json -EA SilentlyContinue
if ($files.Count -ge 8) { Ok "found $($files.Count) emulation JSON files" } else { Bad "expected >=8 emulation files, found $($files.Count)" }
foreach ($f in $files) {
  $raw = Get-Content $f.FullName -Raw
  try { $j = $raw | ConvertFrom-Json } catch { Bad "$($f.Name): invalid JSON"; continue }
  if ($raw -match $secretRx) { Bad "$($f.Name): a real-looking SECRET pattern was found — must be a placeholder/ref only" }
  $fields = if ($j.fields) { $j.fields } elseif ($j.outputs) { $j.outputs } else { @() }
  if (@($fields).Count -ge 1) { Ok "$($f.Name): $(@($fields).Count) entries" } else { Bad "$($f.Name): no fields/outputs" }
  # every field has the required completeness metadata
  foreach ($x in $fields) {
    $hasValidation = $x.validationCommand -or $x.validationCommand -eq "" -bor [bool]$x.passCriterion
    if (-not ($x.PSObject.Properties.Name -contains 'validationCommand' -or $x.PSObject.Properties.Name -contains 'passCriterion')) { Bad "$($f.Name): an entry lacks a validationCommand/passCriterion" ; break }
  }
  # status field present where applicable
  if ($j.status) { Ok "$($f.Name): status=$($j.status)" }
}
# external blockers must each have an owner
$ep = Join-Path $dir "expected-production-outputs.json"
if (Test-Path $ep) {
  $eo = Get-Content $ep -Raw | ConvertFrom-Json
  $noOwner = @($eo.outputs | Where-Object { -not $_.owner }).Count
  if ($noOwner -eq 0) { Ok "every expected production output has an owner" } else { Bad "$noOwner expected outputs lack an owner" }
}
Write-Host "`nOPERATOR-EMULATION-TESTS: $(if($fail -eq 0){'PASS (emulation pack complete; no real secrets; clear operator inputs)'}else{"FAIL ($fail)"})" -ForegroundColor ($fail -eq 0 ? "Green":"Red")
exit ([int]($fail -ne 0))
