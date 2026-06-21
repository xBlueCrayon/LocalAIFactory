<#
.SYNOPSIS  Fetch a SAMPLE of official docs pages for a few systems (HEAD/GET, polite) into a git-ignored cache.
.DESCRIPTION Demonstrates real doc-content availability for a sampled subset (NOT all 113 — that would be
             aggressive scraping). Records HTTP status + whether expected topics appear. Caches under
             .tmp-public-system-docs/ (git-ignored). Respectful: small sample, single GET per system, short timeout.
.PARAMETER Registry / SampleIds / Cache
#>
param(
  [string]$Registry = "benchmarks/public-systems-docs-registry.json",
  [string[]]$SampleIds = @("odoo","wordpress","erpnext","keycloak","airflow","superset","grafana","drupal"),
  [string]$Cache = ".tmp-public-system-docs"
)
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$cacheDir = Join-Path $repo $Cache; New-Item -ItemType Directory -Force $cacheDir | Out-Null
$reg = (Get-Content (Join-Path $repo $Registry) -Raw | ConvertFrom-Json).systems
$results = New-Object System.Collections.ArrayList
Write-Host "== Fetch official-docs sample ($($SampleIds.Count) systems) ==" -ForegroundColor Cyan
foreach ($id in $SampleIds) {
  $e = $reg | Where-Object { $_.systemId -eq $id } | Select-Object -First 1
  if (-not $e -or -not $e.officialDocsUrl) { [void]$results.Add([pscustomobject]@{ id=$id; status="no-docs-url"; topicsFound=0 }); continue }
  $code=0; $len=0; $topics=0
  try {
    $r = Invoke-WebRequest -UseBasicParsing $e.officialDocsUrl -TimeoutSec 20
    $code = [int]$r.StatusCode; $body = $r.Content; $len = $body.Length
    if ($e.expectedDocTopics) { $topics = @($e.expectedDocTopics | Where-Object { $body -match [regex]::Escape($_) }).Count }
    $body | Set-Content (Join-Path $cacheDir "$id.html") -EA SilentlyContinue
  } catch { $resp=$_.Exception.Response; $code = if($resp){[int]$resp.StatusCode}else{0} }
  [void]$results.Add([pscustomobject]@{ id=$id; url=$e.officialDocsUrl; status=$code; bytes=$len; topicsFound=$topics; topicsExpected=@($e.expectedDocTopics).Count })
  Write-Host ("  {0,-12} HTTP {1}  {2} bytes  topics {3}/{4}" -f $id,$code,$len,$topics,@($e.expectedDocTopics).Count)
  Start-Sleep -Milliseconds 600   # polite
}
$results | ConvertTo-Json | Set-Content (Join-Path $repo "benchmarks/results/public-systems-docs-fetch-sample.json")
$ok = ($results | Where-Object { $_.status -eq 200 }).Count
Write-Host "`nFetched $ok/$($SampleIds.Count) official-docs pages (cache git-ignored). Result: benchmarks/results/public-systems-docs-fetch-sample.json" -ForegroundColor Green
