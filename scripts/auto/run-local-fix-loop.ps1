<#
.SYNOPSIS
  R2-ACC-20X: operator entry point for the safe local fix loop.
.DESCRIPTION
  The fix loop (LocalAIFactory.Workspaces.Autonomy.LocalFixLoop) applies a patch to an ISOLATED workspace, runs
  allowlisted checks, and ROLLS BACK on any failure. It NEVER commits or pushes; default is dry-run. This script
  exercises the loop's behavioural proof via its test suite (real temp workspace + injected check runner) and
  prints the safety guarantees. It performs NO changes to your repository.
.PARAMETER Execute
  Reserved. Real execute-mode patching is operator-gated and intended to run against a throwaway worktree, not
  your working tree. This script does not patch your repo; it proves the loop's safety contract.
#>
param([switch]$Execute)

$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$proj = Join-Path $repo "tests/LocalAIFactory.Tests/LocalAIFactory.Tests.csproj"

Write-Host "== Local fix-loop safety contract ==" -ForegroundColor Cyan
@(
  "1. Default DRY-RUN: applies nothing, runs nothing.",
  "2. EXECUTE mode patches only an ISOLATED workspace directory (path-escape is rejected).",
  "3. Checks are allowlisted (build/test/read); denied/approval-gated commands never run.",
  "4. On ANY check failure, all patches are ROLLED BACK (created files deleted, edits restored).",
  "5. The loop NEVER commits, pushes or merges — promotion is a separate human-approved step."
) | ForEach-Object { Write-Host "  $_" }

Write-Host "`n== Behavioural proof (LocalFixLoopTests) ==" -ForegroundColor Cyan
& dotnet test $proj -c Release --nologo --filter "FullyQualifiedName~LocalFixLoopTests" 2>&1 |
  Select-String -Pattern "Passed!|Failed!|error" | Select-Object -Last 3 | Out-Host

if ($LASTEXITCODE -eq 0) { Write-Host "RUN-LOCAL-FIX-LOOP: PASS (safety contract verified)" -ForegroundColor Green; exit 0 }
Write-Host "RUN-LOCAL-FIX-LOOP: FAIL" -ForegroundColor Red; exit 1
