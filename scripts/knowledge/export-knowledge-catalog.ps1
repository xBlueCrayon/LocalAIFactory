<#
.SYNOPSIS
  R2-ACC-FINAL: export a human-readable catalog of the included knowledge base from the source packs.
.DESCRIPTION
  Read-only. Reads every knowledge-packs/<pack>/manifest.json + category files and writes
  docs/Included-Knowledge-Base-Catalog.md (and a machine-readable JSON next to it). No DB needed.
#>
param([string]$OutMd = "")

$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$packsRoot = Join-Path $repo "knowledge-packs"
if (-not $OutMd) { $OutMd = Join-Path $repo "docs/Included-Knowledge-Base-Catalog.md" }
$OutJson = [System.IO.Path]::ChangeExtension($OutMd, ".json")

$packs = @(); $grandItems = 0
foreach ($dir in Get-ChildItem $packsRoot -Directory | Sort-Object Name) {
  $mp = Join-Path $dir.FullName "manifest.json"
  if (-not (Test-Path $mp)) { continue }
  $m = Get-Content $mp -Raw | ConvertFrom-Json
  $cats = @{}
  foreach ($f in $m.files) {
    $fp = Join-Path $dir.FullName $f
    if (-not (Test-Path $fp)) { continue }
    $c = Get-Content $fp -Raw | ConvertFrom-Json
    $catName = if ($c.category) { $c.category } else { [IO.Path]::GetFileNameWithoutExtension($f) }
    $cats[$catName] = ($c.items | Measure-Object).Count
  }
  $count = ($cats.Values | Measure-Object -Sum).Sum
  $grandItems += $count
  $packs += [ordered]@{ folder=$dir.Name; name=$m.name; version=$m.version; itemCount=$count
    manifestItemCount=$m.itemCount; categories=$cats; reviewStatus=$m.reviewStatus }
}

# Markdown
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("# Included Knowledge Base — Catalog`n")
[void]$sb.AppendLine("Generated from the source-controlled knowledge packs under `knowledge-packs/`. MSSQL is the")
[void]$sb.AppendLine("runtime source of truth; these JSON packs are the seed/import format. The application installs all")
[void]$sb.AppendLine("packs on startup (idempotent). All content is original professional summaries with explicit")
[void]$sb.AppendLine("limitation notes — no proprietary/regulatory text is reproduced, and no compliance/financial/fraud")
[void]$sb.AppendLine("certainty is claimed.`n")
[void]$sb.AppendLine("**Packs:** $($packs.Count) · **Total items:** $grandItems`n")
[void]$sb.AppendLine("| Pack | Version | Items | Review |")
[void]$sb.AppendLine("|---|---|---:|---|")
$bt = [char]96  # literal backtick for the markdown code span (avoids the `$ escape collision)
foreach ($p in $packs) { [void]$sb.AppendLine("| $($p.name) ($bt$($p.folder)$bt) | $($p.version) | $($p.itemCount) | $($p.reviewStatus) |") }
foreach ($p in $packs) {
  [void]$sb.AppendLine("`n## $($p.name) — v$($p.version)  ($($p.itemCount) items)")
  [void]$sb.AppendLine("`n| Category | Items |`n|---|---:|")
  foreach ($k in ($p.categories.Keys | Sort-Object)) { [void]$sb.AppendLine("| $k | $($p.categories[$k]) |") }
}
$sb.ToString() | Set-Content -Path $OutMd -Encoding UTF8

# JSON
[ordered]@{ generated="source-pack-export"; packCount=$packs.Count; totalItems=$grandItems; packs=$packs } |
  ConvertTo-Json -Depth 6 | Set-Content -Path $OutJson -Encoding UTF8

Write-Host "Catalog written:" -ForegroundColor Green
Write-Host "  $OutMd"
Write-Host "  $OutJson"
Write-Host ("Packs: {0} | Total items: {1}" -f $packs.Count, $grandItems)
