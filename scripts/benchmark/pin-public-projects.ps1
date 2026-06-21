<#
.SYNOPSIS  Resolve each public repo's current HEAD commit SHA (no clone) and write it into the manifest. Reproducible.
.DESCRIPTION Uses `git ls-remote` (network metadata only — no clone) to resolve the tip of each repo's default
             branch, and records it as pinnedCommitSha so a benchmark run is pinned/reproducible. Read-only except
             for updating the manifest's pinnedCommitSha fields.
.PARAMETER Manifest
#>
param([string]$Manifest = "benchmarks/public-projects-50.json")
$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$path = Join-Path $repo $Manifest
$m = Get-Content $path -Raw | ConvertFrom-Json
Write-Host "== Pinning $($m.repos.Count) repos via git ls-remote ==" -ForegroundColor Cyan
$ok=0;$fail=0
foreach ($r in $m.repos) {
  $ref = & git ls-remote $r.url "refs/heads/$($r.defaultBranch)" 2>$null
  $sha = ($ref | Out-String).Trim().Split("`t")[0]
  if ($sha -match '^[0-9a-f]{40}$') { $r.pinnedCommitSha = $sha; $ok++ }
  else { $fail++; Write-Host "  [warn] could not resolve $($r.id) ($($r.url) @ $($r.defaultBranch))" -ForegroundColor Yellow }
}
$m | ConvertTo-Json -Depth 8 | Set-Content $path -Encoding UTF8
Write-Host "Pinned $ok/$($m.repos.Count) repos (failed $fail). Manifest updated: $Manifest" -ForegroundColor Green
