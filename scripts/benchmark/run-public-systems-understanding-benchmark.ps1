<#
.SYNOPSIS  Score the 100+ system understanding questions by EVIDENCE AVAILABILITY (honest; no hallucinated answers).
.DESCRIPTION This does NOT ask a model to answer 588 questions and grade itself. It scores each question by whether
             the REQUIRED EVIDENCE actually exists and at what fidelity:
               - code evidence: real extracted symbols (from the 51-repo benchmark results) on a SUPPORTED repo;
               - doc/API evidence: an OFFICIAL docs URL that was actually FETCHED (sample) vs merely registered;
               - unsupported language / no docs → honest low score.
             This avoids "fake API understanding": a question is only credited as doc-grounded if the official
             doc was fetched and the expected topics were found; otherwise it is capped at "registered/metadata".
.PARAMETER Manifest / Registry / Questions
#>
param(
  [string]$Manifest = "benchmarks/public-systems-100.json",
  [string]$Registry = "benchmarks/public-systems-docs-registry.json",
  [string]$Questions = "benchmarks/public-systems-understanding-questions.json"
)
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$res = Join-Path $repo "benchmarks/results"; New-Item -ItemType Directory -Force $res | Out-Null
$systems = (Get-Content (Join-Path $repo $Manifest) -Raw | ConvertFrom-Json).systems
$reg = (Get-Content (Join-Path $repo $Registry) -Raw | ConvertFrom-Json).systems
$qs = (Get-Content (Join-Path $repo $Questions) -Raw | ConvertFrom-Json).questions

# real code evidence: systems with extracted symbols from the 51-repo run (match by repo owner/name)
$codeProven = @{}
$p50 = Join-Path $repo "benchmarks/results/public50-results.json"
if (Test-Path $p50) { foreach ($r in (Get-Content $p50 -Raw|ConvertFrom-Json).results) { if ($r.status -in 'Passed','PassedPartial') { $codeProven[$r.id] = $r.symbolsCSharp + $r.symbolsPython } } }
# fetched docs (sample)
$docsFetched = @{}
$df = Join-Path $repo "benchmarks/results/public-systems-docs-fetch-sample.json"
if (Test-Path $df) { foreach ($r in (Get-Content $df -Raw|ConvertFrom-Json)) { if ($r.status -eq 200 -and $r.topicsFound -gt 0) { $docsFetched[$r.id] = $r.topicsFound } } }

$sysById = @{}; foreach ($s in $systems) { $sysById[$s.id] = $s }
$regById = @{}; foreach ($r in $reg) { $regById[$r.systemId] = $r }
function Supported($s){ $s.benchmarkMode -eq "full" -and ($s.primaryLanguage -in @("C#","Python") -or ($s.includePatterns -join '') -match '\.cs|\.py|\.sql') }

$rows = New-Object System.Collections.ArrayList
foreach ($q in $qs) {
  $s = $sysById[$q.systemId]; $r = $regById[$q.systemId]
  $score = 25; $basis = "generic"
  $codeOk = $codeProven.ContainsKey($q.systemId)
  $docsFetchedOk = $docsFetched.ContainsKey($q.systemId)
  $docsRegOk = $r -and $r.officialDocsUrl -and $r.docsType -eq "official"
  $supp = $s -and (Supported $s)

  if ($q.requiredCodeEvidence -and $q.requiredApiDocEvidence) {
    if ($codeOk -and $docsFetchedOk) { $score=90; $basis="code symbols + fetched official docs" }
    elseif ($codeOk -and $docsRegOk) { $score=75; $basis="code symbols + registered official docs (content not deep-verified)" }
    elseif ($codeOk -or $docsRegOk) { $score=50; $basis="partial (one evidence side)" }
    else { $score=25; $basis="evidence not available" }
  }
  elseif ($q.requiredCodeEvidence) {
    if ($codeOk) { $score=75; $basis="grounded in extracted code symbols" }
    elseif ($supp) { $score=50; $basis="supported language; extractable (not yet extracted)" }
    else { $score=25; $basis="unsupported language — honest gap" }
  }
  elseif ($q.requiredApiDocEvidence -or $q.requiredDocEvidence) {
    if ($docsFetchedOk) { $score=75; $basis="grounded in fetched official docs (sample)" }
    elseif ($docsRegOk) { $score=50; $basis="official docs registered (metadata-level; content not deep-verified)" }
    else { $score=25; $basis="docs not registered/available" }
  }
  else {
    if ($docsRegOk -and $codeOk) { $score=60; $basis="repo + official docs registered" }
    elseif ($docsRegOk -or $codeOk) { $score=50; $basis="repo or docs available" }
    else { $score=25; $basis="generic" }
  }
  [void]$rows.Add([pscustomobject]@{ id=$q.id; systemId=$q.systemId; type=$q.type; score=$score; basis=$basis })
}

$mean = [math]::Round((($rows | Measure-Object score -Average).Average),1)
$summary = [ordered]@{
  totalQuestions = $rows.Count
  meanScore = $mean
  codeGrounded = ($rows | Where-Object { $_.basis -match 'code symbols' }).Count
  docsFetchedGrounded = ($rows | Where-Object { $_.basis -match 'fetched official docs' }).Count
  docsMetadataOnly = ($rows | Where-Object { $_.basis -match 'metadata-level|registered official docs' }).Count
  unsupportedGap = ($rows | Where-Object { $_.basis -match 'unsupported language' }).Count
  byType = @{}
  systemsWithRealCode = $codeProven.Count
  docsFetchedSample = $docsFetched.Count
}
foreach ($g in ($rows | Group-Object type)) { $summary.byType[$g.Name] = [math]::Round((($g.Group | Measure-Object score -Average).Average),1) }
@{ results=$rows } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $res "public-systems-understanding-results.json")
$summary | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $res "public-systems-understanding-summary.json")

Write-Host "== Public-systems understanding benchmark ==" -ForegroundColor Cyan
Write-Host "  questions=$($rows.Count)  meanScore=$mean (honest, evidence-availability)" -ForegroundColor Cyan
Write-Host "  code-grounded=$($summary.codeGrounded)  docs-fetched-grounded=$($summary.docsFetchedGrounded)  docs-metadata-only=$($summary.docsMetadataOnly)  unsupported-gap=$($summary.unsupportedGap)"
Write-Host "  NOTE: doc/API questions are credited as 'grounded' ONLY where the official doc was actually fetched; otherwise metadata-level. No question is answered by a model here — this scores evidence availability." -ForegroundColor Yellow
