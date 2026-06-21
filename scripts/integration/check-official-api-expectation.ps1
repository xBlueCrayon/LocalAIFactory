<#
.SYNOPSIS  Check an integration-expectation file against the official-doc-cross-check contract. READ-ONLY.
.DESCRIPTION Validates that a benchmarks/integration-expectations/<system>.json entry carries an honest expectation:
             a status (EMULATED_EXPECTATION_ONLY | SUPPORTED), an official-doc source reference, and the required
             fields - WITHOUT claiming a live/certified integration. Pass -System to target one; default checks all.
.PARAMETER System  Optional system name (file stem), e.g. powerbi, servicenow, salesforce, tableau.
#>
param([string]$System)
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$dir = Join-Path $repo "benchmarks/integration-expectations"
$fail = 0
function Ok($m){ Write-Host "  [ OK ] $m" -ForegroundColor Green }
function Bad($m){ Write-Host "  [FAIL] $m" -ForegroundColor Red; $script:fail++ }
Write-Host "== Official-API expectation check ==" -ForegroundColor Cyan
$files = if ($System){ Get-ChildItem $dir -Filter "$System.json" } else { Get-ChildItem $dir -Filter *.json | Where-Object Name -ne 'manifest.json' }
if (-not $files){ Bad "no expectation file found$(if($System){" for '$System'"})"; exit 1 }
$validStatus = @("EMULATED_EXPECTATION_ONLY","SUPPORTED")
foreach ($f in $files){
  $raw = Get-Content $f.FullName -Raw
  $j = $raw | ConvertFrom-Json
  $issues=@()
  if (-not $j.status -or $j.status -notin $validStatus){ $issues += "status must be one of $($validStatus -join '/')" }
  if (-not ($j.officialDocsUrl -or $j.apiDocsUrl)){ $issues += "no official-doc reference (officialDocsUrl/apiDocsUrl)" }
  if (-not ($j.expectedSuccessResponseShape -or $j.expectedRequestShape -or $j.keyEndpoints)){ $issues += "no expected behavior (request/response shape or keyEndpoints)" }
  if (-not $j.commonIntegrationFailure -and -not $j.expectedFailureResponseShape){ $issues += "no failure-mode model" }
  if ($raw -match '(?i)\bcertified\b|guaranteed compatible'){ $issues += "overclaims certification" }
  if ($issues.Count -eq 0){ Ok "$($f.BaseName) -> $($j.status) (honest expectation, doc-referenced)" }
  else { Bad "$($f.BaseName): $($issues -join '; ')" }
}
Write-Host "`n  Expectations are documented contracts, NOT live/certified integrations." -ForegroundColor Yellow
Write-Host "OFFICIAL-API-EXPECTATION: $(if($fail -eq 0){"PASS ($($files.Count) checked)"}else{"FAIL ($fail)"})" -ForegroundColor ($fail -eq 0 ? "Green":"Red")
exit ([int]($fail -ne 0))
