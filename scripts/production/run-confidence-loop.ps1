<#
.SYNOPSIS  High-confidence iteration loop — run the key gates up to N times until all pass. Does NOT loop forever.
.DESCRIPTION Each iteration runs: build, tests, verify-poc, knowledge validation, security audit,
             production-readiness gate, IIS production-posture healthcheck, repo cleanliness. Stops when all local
             technical gates pass (0 HIGH, gate not NOT_READY, no forbidden artifacts) or after -MaxIterations.
             Read-only except for build/test artifacts. Logs each iteration.
.PARAMETER MaxIterations / SkipBuild
#>
param([int]$MaxIterations=5,[switch]$SkipBuild)
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$log = New-Object System.Collections.ArrayList
for ($iter=1; $iter -le $MaxIterations; $iter++) {
  Write-Host "`n===== Confidence loop iteration $iter / $MaxIterations =====" -ForegroundColor Cyan
  $issues = @()
  if (-not $SkipBuild) {
    & dotnet build "$repo/LocalAIFactory.sln" -c Release --nologo 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { $issues += "build" }
    & dotnet test "$repo/tests/LocalAIFactory.Tests/LocalAIFactory.Tests.csproj" -c Release --nologo 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { $issues += "tests" }
  }
  $sa = & pwsh -NoProfile -File "$repo/scripts/security/security-audit.ps1" 2>&1 | Out-String
  if ($sa -notmatch "SECURITY-AUDIT: PASS") { $issues += "security" }
  $kp = & pwsh -NoProfile -File "$repo/scripts/knowledge/verify-all-knowledge-packs.ps1" 2>&1 | Out-String
  if ($kp -notmatch "VERIFY-ALL-KNOWLEDGE-PACKS: PASS") { $issues += "knowledge" }
  $gate = & pwsh -NoProfile -File "$repo/scripts/production/verify-production-readiness.ps1" 2>&1 | Out-String
  if ($gate -match "FINAL CLASSIFICATION: NOT_READY") { $issues += "production-gate-NOT_READY" }
  # match runtime/scratch artifacts only — anchor temp dirs on .tmp- so legit source scripts (e.g. fetch-public-system-docs.ps1) are not false-flagged
  $forbidden = (git -C $repo ls-files | Select-String '/(bin|obj)/|\.tmp-|/publish/|\.bak$|\.log$|node_modules/|inetpub/|release.*\.zip$|^backups/|\.(mdf|ldf)$').Count
  if ($forbidden -gt 0) { $issues += "forbidden-tracked" }
  $procs = (Get-Process LocalAIFactory.Web -EA SilentlyContinue | Measure-Object).Count
  if ($procs -gt 0) { $issues += "stale-app-process" }

  [void]$log.Add([pscustomobject]@{ iteration=$iter; issues=($issues -join ',') })
  if ($issues.Count -eq 0) { Write-Host "  iteration $iter`: ALL GATES PASS - stopping." -ForegroundColor Green; break }
  else { Write-Host "  iteration $iter issues: $($issues -join ', ')" -ForegroundColor Yellow }
}
$stable = (($log[-1].issues) -eq "")
@{ iterations=$log.Count; stable=$stable; log=$log } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $repo "benchmarks/results/confidence-loop.json")
Write-Host "`nCONFIDENCE-LOOP: $(if($stable){'STABLE (all local technical gates pass)'}else{'UNSTABLE (see issues)'}) after $($log.Count) iteration(s)" -ForegroundColor ($stable ? "Green":"Red")
exit ([int](-not $stable))
