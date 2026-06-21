<#
.SYNOPSIS  Validate the 100+ public-systems manifest + docs registry + question set. READ-ONLY.
#>
param(
  [string]$Manifest = "benchmarks/public-systems-100.json",
  [string]$Registry = "benchmarks/public-systems-docs-registry.json",
  [string]$Questions = "benchmarks/public-systems-understanding-questions.json"
)
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$fail = 0
function Ok($m){ Write-Host "  [ OK ] $m" -ForegroundColor Green }
function Bad($m){ Write-Host "  [FAIL] $m" -ForegroundColor Red; $script:fail++ }
Write-Host "== Validate public-systems manifest ==" -ForegroundColor Cyan
$m = Get-Content (Join-Path $repo $Manifest) -Raw | ConvertFrom-Json
$d = Get-Content (Join-Path $repo $Registry) -Raw | ConvertFrom-Json
$q = Get-Content (Join-Path $repo $Questions) -Raw | ConvertFrom-Json
if ($m.systems.Count -ge 100) { Ok "systems: $($m.systems.Count) (>=100)" } else { Bad "only $($m.systems.Count) systems (<100)" }
$ids = $m.systems.id; $dupes = ($ids | Group-Object | Where-Object Count -gt 1).Count
if ($dupes -eq 0) { Ok "system ids unique" } else { Bad "$dupes duplicate system ids" }
$badUrl = @($m.systems | Where-Object { $_.benchmarkMode -ne "DocsOnlyReference" -and $_.sourceRepoUrl -notmatch '^https://github\.com/' }).Count
if ($badUrl -eq 0) { Ok "all source repos are github URLs" } else { Bad "$badUrl systems with non-github source url" }
if ($d.systems.Count -ge $m.systems.Count) { Ok "docs registry covers all systems ($($d.systems.Count))" } else { Bad "docs registry only $($d.systems.Count) of $($m.systems.Count)" }
if ($q.questions.Count -ge 500) { Ok "questions: $($q.questions.Count) (>=500)" } else { Bad "only $($q.questions.Count) questions (<500)" }
$qIds = $q.questions.systemId | Sort-Object -Unique
$orphan = @($qIds | Where-Object { $_ -notin $ids }).Count
if ($orphan -eq 0) { Ok "all question systemIds exist in the manifest" } else { Bad "$orphan question systemIds not in manifest" }
Write-Host "`nVALIDATE-PUBLIC-SYSTEMS-MANIFEST: $(if($fail -eq 0){'PASS'}else{"FAIL ($fail)"})" -ForegroundColor ($fail -eq 0 ? "Green":"Red")
exit ([int]($fail -ne 0))
