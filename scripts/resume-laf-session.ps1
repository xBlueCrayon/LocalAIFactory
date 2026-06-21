<#
.SYNOPSIS  Orientation for resuming LocalAIFactory work on a new machine (e.g. over SSH).
.DESCRIPTION  Prints repo status, latest commit, branch, the key current reports, the next recommended task,
              and the validation commands to run. Read-only — it changes nothing.
#>
$ErrorActionPreference = "SilentlyContinue"
$repo = Split-Path -Parent $PSScriptRoot
Set-Location $repo

function H($t) { Write-Host ""; Write-Host "== $t ==" -ForegroundColor Cyan }

H "Repository"
Write-Host "  path   : $repo"
Write-Host "  branch : $(git branch --show-current)"
Write-Host "  commit : $(git log --oneline -1)"
$dirty = git status --porcelain
Write-Host "  status : $([string]::IsNullOrWhiteSpace($dirty) ? 'clean' : "$((($dirty -split "`n").Count)) change(s) — review before working")"
git fetch origin 2>$null | Out-Null
$local = git rev-parse HEAD; $remote = git rev-parse "origin/$(git branch --show-current)"
Write-Host "  remote : $([string]::Equals($local,$remote) ? 'in sync with origin' : 'DIVERGED from origin — reconcile first')"

H "Recent commits"
git log --oneline -5 | ForEach-Object { Write-Host "  $_" }

H "Key current reports"
foreach ($r in @(
  "docs/reports/SSH_SESSION_HANDOFF_LOCALAIFACTORY.md",
  "docs/reports/LAF_REASONING_V2_FINAL_VALIDATION.md",
  "docs/architecture/LAF_SOFTWARE_REASONING_ENGINE_V2.md",
  "benchmarks/results/laf-product-maturity-score.json",
  "benchmarks/results/laf-building-block-composition-benchmark.json",
  "CLAUDE.md", "MASTER_VISION.md")) {
  Write-Host "  $((Test-Path $r) ? '[ok] ' : '[!!] ')$r"
}

H "Last sprint"
Write-Host "  LAF Software Reasoning Engine V2 -> LAF_SOFTWARE_REASONING_ENGINE_V2_LOCAL_CORE_READY"
Write-Host "  reasoning-family 176 tests | factory 257 | composition 20/20 | safe-patch 10/10 | 45 packs / 1195 items"

H "Next recommended task"
Write-Host "  Wire a LIVE local model into the model-driven Plan->Patch->Verify loop (replace the fake IPatchPlanner"
Write-Host "  with one backed by LocalModelRouter/GpuAwareOrchestrator) BEHIND the existing safety gates."
Write-Host "  Then: standalone LearningLoop + full Python workers (local venv). Do NOT let a model edit the main repo."
Write-Host "  Do NOT merge / tag v1.0 / publish a release."

H "Validation commands (run before new work)"
@(
  "dotnet build LocalAIFactory.sln -c Release",
  "dotnet test tests/LocalAIFactory.Tests/LocalAIFactory.Tests.csproj -c Release",
  "dotnet test tests/LocalAIFactory.Reasoning.Tests/LocalAIFactory.Reasoning.Tests.csproj -c Release",
  "dotnet test tests/LocalAIFactory.CodeBlocks.Tests/LocalAIFactory.CodeBlocks.Tests.csproj -c Release",
  "dotnet test tests/LocalAIFactory.PythonBridge.Tests/LocalAIFactory.PythonBridge.Tests.csproj -c Release",
  "dotnet test tests/LocalAIFactory.KnowledgeGrowth.Tests/LocalAIFactory.KnowledgeGrowth.Tests.csproj -c Release",
  "pwsh -File scripts/knowledge/verify-all-knowledge-packs.ps1",
  "pwsh -File scripts/production/verify-production-readiness-v3.ps1",
  "pwsh -File scripts/security/security-audit.ps1"
) | ForEach-Object { Write-Host "  $_" }
Write-Host ""
Write-Host "Full handoff: docs/reports/SSH_SESSION_HANDOFF_LOCALAIFACTORY.md" -ForegroundColor Green
