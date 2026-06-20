<# R2-ACC-INDUSTRIAL: lightweight validation for the financial-institution-operations scenario. This scenario
   composes graph capabilities proven elsewhere (COREBANK/KYCAML/ERPCRM); it ships no fixture of its own. The
   check confirms the scenario documents are present and that the backing fixtures it relies on are declared
   in the benchmark manifest. #>
param([string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../..").Path)
$ErrorActionPreference = "Stop"
$ok = $true

$docs = @("README.md","workflow.md","controls-matrix.md","approval-matrix.md","operating-manager-dashboard.md","expected-questions.md","expected-answers.md")
foreach ($d in $docs) {
    if (Test-Path (Join-Path $PSScriptRoot $d)) { Write-Host "OK   $d" -ForegroundColor Green }
    else { Write-Host "MISS $d" -ForegroundColor Red; $ok = $false }
}

$manifest = Join-Path $RepoRoot "benchmarks/benchmarks.json"
if (Test-Path $manifest) {
    $codes = (Get-Content $manifest -Raw | ConvertFrom-Json).repos.code
    foreach ($c in @("COREBANK","KYCAML","ERPCRM")) {
        if ($codes -contains $c) { Write-Host "OK   backing fixture present: $c" -ForegroundColor Green }
        else { Write-Host "MISS backing fixture: $c" -ForegroundColor Red; $ok = $false }
    }
} else { Write-Host "MISS benchmarks.json" -ForegroundColor Red; $ok = $false }

if ($ok) { Write-Host "financial-institution-operations: scenario assets present." -ForegroundColor Green; exit 0 }
else { Write-Host "financial-institution-operations: missing assets." -ForegroundColor Red; exit 1 }
