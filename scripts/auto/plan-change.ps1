<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: print a DRY-RUN autonomous change plan (executes nothing).
.DESCRIPTION Renders the fixed, human-reviewable plan for a change request and the command-policy decision for
             each planned command. Commit/push are always approval-gated. Nothing is executed by this script.
#>
param([string]$Title = "Untitled change", [string]$Description = "", [string]$TargetRepoPath = ".")
$steps = @(
  @{ k = "CreateIsolatedWorkspace"; d = "Create an isolated worktree/branch (never a shared branch)."; c = $null },
  @{ k = "ProposePatch";            d = "Propose a patch as a diff (not applied without approval)."; c = $null },
  @{ k = "Build";                   d = "Build."; c = "dotnet build LocalAIFactory.sln -c Release" },
  @{ k = "Test";                    d = "Test."; c = "dotnet test tests/LocalAIFactory.Tests/LocalAIFactory.Tests.csproj -c Release" },
  @{ k = "Benchmark";               d = "Benchmark."; c = "dotnet run --project tools/LocalAIFactory.Benchmark -c Release -- --inmemory" },
  @{ k = "UiSmoke";                 d = "UI smoke."; c = "pwsh scripts/poc/ui-smoke-test.ps1" },
  @{ k = "GenerateReport";          d = "Generate an evidence report."; c = $null },
  @{ k = "RequestHumanApproval";    d = "STOP — require explicit human approval before any state change."; c = $null },
  @{ k = "Commit";                  d = "Commit (after approval only)."; c = "git commit -m <message>" },
  @{ k = "Push";                    d = "Push the branch (after approval only; never merge)."; c = "git push origin <branch>" }
)
function Decide($cmd) {
  if (-not $cmd) { return "n/a" }
  $deny = @("reset --hard","clean -fd","push --force","rebase","merge","rm -rf","drop database","truncate","iisreset","netsh","firewall")
  foreach ($d in $deny) { if ($cmd -match [regex]::Escape($d)) { return "DENIED" } }
  if ($cmd -match "^git (commit|push|tag)" -or $cmd -match "dotnet ef") { return "RequiresApproval" }
  if ($cmd -match "^(dotnet build|dotnet test|dotnet run --project tools|git status|git diff|git log)") { return "Allowed" }
  return "RequiresApproval"
}
Write-Host "== DRY-RUN PLAN: $Title ==" -ForegroundColor Cyan
Write-Host "Target: $TargetRepoPath`n"
$i = 1
foreach ($s in $steps) {
  $gate = ($s.k -in @("Commit","Push","RequestHumanApproval")) ? "  [APPROVAL REQUIRED]" : ""
  Write-Host ("  {0}. {1}: {2}{3}" -f $i++, $s.k, $s.d, $gate)
  if ($s.c) { Write-Host ("       $ {0}   -> {1}" -f $s.c, (Decide $s.c)) }
}
Write-Host "`nDRY-RUN: nothing executed. Operator reviews this plan, then runs run-approved-local-checks.ps1 -Execute." -ForegroundColor Green
exit 0
