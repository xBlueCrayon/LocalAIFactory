<# R2-ACC-INDUSTRIAL: validate KYC -> transaction-approval capability via the KYCAML benchmark fixture (live
   proof, no network). Builds a KYCAML-only manifest so the proof does not depend on the git/sqlfiles repos,
   runs the harness in-memory, and asserts Gold pov=7/7. #>
param([string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../..").Path)
$ErrorActionPreference = "Stop"

$srcManifest = Join-Path $RepoRoot "benchmarks/benchmarks.json"
$tmpManifest = Join-Path $RepoRoot "benchmarks/_kycaml-validation.json"
try {
    $m = Get-Content $srcManifest -Raw | ConvertFrom-Json
    $only = @($m.repos | Where-Object { $_.code -eq "KYCAML" })
    if ($only.Count -lt 1) { Write-Host "KYCAML entry not found in benchmarks.json." -ForegroundColor Red; exit 1 }
    [pscustomobject]@{ repos = $only } | ConvertTo-Json -Depth 12 | Set-Content $tmpManifest -Encoding UTF8

    Push-Location "$RepoRoot/tools/LocalAIFactory.Benchmark"
    try { $out = & dotnet run -c Release -- --inmemory --manifest $tmpManifest 2>&1 } finally { Pop-Location }
}
finally {
    if (Test-Path $tmpManifest) { Remove-Item $tmpManifest -Force }
}

$out | Select-String "KycAmlApproval|\[PASS\]|\[FAIL\]|Result:" | ForEach-Object { Write-Host $_ }
if ($out | Select-String "KycAmlApproval\s+Gold\s+.*pov=7/7") {
    Write-Host "KYC -> transaction approval: Gold 7/7 — capability proven." -ForegroundColor Green; exit 0
}
else { Write-Host "KYCAML fixture did not reach Gold 7/7." -ForegroundColor Red; exit 1 }
