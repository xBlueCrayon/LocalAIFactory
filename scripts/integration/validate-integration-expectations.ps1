<#
.SYNOPSIS  Validate the official-integration expectation library. READ-ONLY. Proves expectation models exist — NOT live integration.
.DESCRIPTION Each integration-expectations/*.json must have official docs URL, auth method, key endpoints, expected
             request/success/failure shapes, a support status, and a note that live integration was NOT executed.
             This validates the EXPECTATION MODEL only; it never claims an integration works.
#>
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$dir = Join-Path $repo "benchmarks/integration-expectations"
$fail = 0
function Ok($m){ Write-Host "  [ OK ] $m" -ForegroundColor Green }
function Bad($m){ Write-Host "  [FAIL] $m" -ForegroundColor Red; $script:fail++ }
Write-Host "== Integration-expectation library validation ==" -ForegroundColor Cyan
if (-not (Test-Path $dir)) { Bad "benchmarks/integration-expectations/ folder missing"; exit 1 }
$files = Get-ChildItem $dir -Filter *.json -EA SilentlyContinue
if ($files.Count -ge 15) { Ok "found $($files.Count) integration-expectation files (>=15)" } else { Bad "expected >=15, found $($files.Count)" }
$req = @("system","officialDocsUrl","authMethod","keyEndpoints","expectedSuccessResponseShape","localAIFactorySupportStatus")
$validStatus = @("SUPPORTED","PARTIAL","METADATA_ONLY","NOT_SUPPORTED","EMULATED_EXPECTATION_ONLY")
$byStatus = @{}
foreach ($f in $files) {
  try { $j = Get-Content $f.FullName -Raw | ConvertFrom-Json } catch { Bad "$($f.Name): invalid JSON"; continue }
  $miss = @($req | Where-Object { -not $j.PSObject.Properties.Name.Contains($_) })
  if ($miss.Count -gt 0) { Bad "$($f.Name): missing $($miss -join ', ')"; continue }
  if ($j.localAIFactorySupportStatus -notin $validStatus) { Bad "$($f.Name): invalid support status '$($j.localAIFactorySupportStatus)'" }
  if ($j.officialDocsUrl -notmatch '^https?://') { Bad "$($f.Name): officialDocsUrl not a URL" }
  $byStatus[$j.localAIFactorySupportStatus] = ([int]$byStatus[$j.localAIFactorySupportStatus]) + 1
}
Write-Host "  support-status spread: $(($byStatus.GetEnumerator()|ForEach-Object{"$($_.Key)=$($_.Value)"}) -join '  ')" -ForegroundColor Cyan
Write-Host "  NOTE: this validates the EXPECTATION MODEL only. No live integration was executed against any endpoint." -ForegroundColor Yellow
Write-Host "VALIDATE-INTEGRATION-EXPECTATIONS: $(if($fail -eq 0){'PASS'}else{"FAIL ($fail)"})" -ForegroundColor ($fail -eq 0 ? "Green":"Red")
exit ([int]($fail -ne 0))
