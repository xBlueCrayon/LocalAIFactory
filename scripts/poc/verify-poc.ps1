<#
.SYNOPSIS
  R2-ACC-POC-ENTERPRISE — safe, read-only proof-of-capability verification.
.DESCRIPTION
  Verifies git status, latest commit, build, tests, benchmark, required POC artifacts, and that no forbidden
  runtime artifacts are tracked. Optionally HTTP-checks a running app. Performs NO destructive actions
  (no resets, deletes, DB drops, IIS or production changes).
.PARAMETER AppUrl
  Optional base URL of a running app (e.g. http://localhost:60398) to add live HTTP checks.
.PARAMETER Fast
  Skip build/test/benchmark; only check artifacts + tracking hygiene.
#>
param([string]$AppUrl = "", [switch]$Fast)

$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$fail = 0
function Ok($m)   { Write-Host "  [PASS] $m" -ForegroundColor Green }
function Bad($m)  { Write-Host "  [FAIL] $m" -ForegroundColor Red; $script:fail++ }
function Head($m) { Write-Host "`n== $m ==" -ForegroundColor Cyan }

Head "Repository"
Push-Location $repo
try {
  $branch = (git rev-parse --abbrev-ref HEAD 2>$null)
  $commit = (git log -1 --oneline 2>$null)
  Write-Host "  branch: $branch"
  Write-Host "  commit: $commit"
  $dirty = (git status --porcelain 2>$null)
  if ($dirty) { Write-Host "  working tree: DIRTY (uncommitted changes present)" -ForegroundColor Yellow }
  else { Ok "working tree clean" }

  Head "Required POC artifacts"
  $required = @(
    "knowledge-packs/professional-base-v1/manifest.json",
    "knowledge-packs/professional-base-v1/source-registry.json",
    "docs/readiness-scorecard.json",
    "docs/Readiness-Maturity-Model.md",
    "docs/Enterprise-Readiness-Scorecard.md",
    "docs/POC-Evidence-Pack.md",
    "docs/High-End-Enterprise-Solution-Comparison.md",
    "docs/Enterprise-Solution-Evaluation-Rubric.md",
    "docs/POC-Demo-Script.md",
    "docs/Public-Material-Learning-Governance.md",
    "docs/Repository-Cleanliness-Audit.md",
    "deploy/docs/hardware-sizing-guide.md",
    "benchmarks/repo-candidates.json",
    "scripts/poc/ui-smoke-test.ps1",
    "enterprise-scenarios/README.md"
  )
  foreach ($r in $required) { if (Test-Path (Join-Path $repo $r)) { Ok $r } else { Bad "missing $r" } }

  $scenarioDirs = Get-ChildItem (Join-Path $repo "enterprise-scenarios") -Directory -ErrorAction SilentlyContinue
  if ($scenarioDirs.Count -ge 14) { Ok "enterprise scenarios: $($scenarioDirs.Count) folders" } else { Bad "expected >=14 scenario folders, found $($scenarioDirs.Count)" }
  foreach ($d in $scenarioDirs) {
    # Two valid shapes: canonical advisory scenarios (4 markdown files), or industrial capability fixtures
    # (README + a validation script that runs the benchmark proof).
    $canonical = @('scenario.md','expected-capabilities.md','acceptance-criteria.md','test-questions.md') |
      ForEach-Object { Test-Path (Join-Path $d.FullName $_) } | Where-Object { -not $_ } | Measure-Object
    $hasCanonical = ($canonical.Count -eq 0)
    $hasIndustrial = (Test-Path (Join-Path $d.FullName 'README.md')) -and (Test-Path (Join-Path $d.FullName 'validation-script.ps1'))
    if (-not ($hasCanonical -or $hasIndustrial)) { Bad "scenario $($d.Name): neither canonical 4 files nor (README.md + validation-script.ps1)" }
  }

  Head "Tracking hygiene (no forbidden runtime artifacts tracked)"
  $tracked = git ls-files
  $forbidden = $tracked | Where-Object { $_ -match '/(bin|obj)/' -or $_ -match '^benchmarks/cache/' -or $_ -match '\.(mdf|ldf|bak|gguf|onnx|safetensors)$' -or $_ -match '^keys/' }
  if ($forbidden) { $forbidden | ForEach-Object { Bad "forbidden tracked: $_" } } else { Ok "no bin/obj/cache/db/model/keys tracked" }
  # no tracked file > 5 MB
  $big = $tracked | Where-Object { (Test-Path $_) -and ((Get-Item $_).Length -gt 5MB) }
  if ($big) { $big | ForEach-Object { Bad "large tracked file (>5MB): $_" } } else { Ok "no tracked file exceeds 5 MB" }

  if (-not $Fast) {
    Head "Build"
    & dotnet build "$repo/LocalAIFactory.sln" -c Release --nologo 2>&1 | Select-Object -Last 3 | Out-Host
    if ($LASTEXITCODE -eq 0) { Ok "dotnet build" } else { Bad "dotnet build (exit $LASTEXITCODE)" }

    Head "Tests"
    & dotnet test "$repo/tests/LocalAIFactory.Tests/LocalAIFactory.Tests.csproj" -c Release --nologo 2>&1 | Select-String -Pattern "Passed!|Failed!|error" | Select-Object -Last 3 | Out-Host
    if ($LASTEXITCODE -eq 0) { Ok "dotnet test" } else { Bad "dotnet test (exit $LASTEXITCODE)" }

    Head "Benchmark"
    Push-Location "$repo/tools/LocalAIFactory.Benchmark"
    $bench = & dotnet run -c Release -- --inmemory 2>&1
    Pop-Location
    if (($bench | Select-String "Result: PASS")) { Ok "benchmark PASS" } else { Bad "benchmark did not report PASS" }
  } else { Write-Host "`n(skipping build/test/benchmark: -Fast)" -ForegroundColor Yellow }

  if ($AppUrl) {
    Head "Live HTTP checks ($AppUrl)"
    foreach ($p in @("/","/BaseKnowledge","/Readiness")) {
      try {
        $code = (Invoke-WebRequest -UseBasicParsing -Uri "$AppUrl$p" -TimeoutSec 15).StatusCode
        if ($code -eq 200) { Ok "GET $p -> 200" } else { Bad "GET $p -> $code" }
      } catch { Bad "GET $p -> error" }
    }
  }
}
finally { Pop-Location }

Head "Result"
if ($fail -eq 0) { Write-Host "VERIFY-POC: PASS" -ForegroundColor Green; exit 0 }
else { Write-Host "VERIFY-POC: FAIL ($fail issue(s))" -ForegroundColor Red; exit 1 }
