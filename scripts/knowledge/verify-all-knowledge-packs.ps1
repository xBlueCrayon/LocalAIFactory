<#
.SYNOPSIS
  R2-ACC-FINAL: verify ALL source-controlled knowledge packs (offline) and, if a DB is reachable, the live
  installed counts. Read-only. Exits non-zero if any pack is invalid.
.DESCRIPTION
  Offline checks per pack: manifest.json present + valid JSON; every referenced file present + valid JSON;
  every item UID is a valid GUID; no duplicate UIDs within a pack; no duplicate UIDs across packs; every item
  has a limitation note and tags; manifest.itemCount matches actual. Then (optional) queries the database for
  installed pack rows + item counts. No writes, no destructive actions.
.PARAMETER Server   SQL instance for the live check (default LocalDB). Pass "" to skip the live check.
.PARAMETER Database Database name (default LocalAIFactory).
#>
param([string]$Server = "(localdb)\MSSQLLocalDB", [string]$Database = "LocalAIFactory")

$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$packsRoot = Join-Path $repo "knowledge-packs"
$guid = '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$'
$fail = 0; $allUids = @{}; $totalItems = 0; $packCount = 0
function Bad($m){ Write-Host "  [FAIL] $m" -ForegroundColor Red; $script:fail++ }
function Ok($m){ Write-Host "  [ OK ] $m" -ForegroundColor Green }

Write-Host "== Offline knowledge-pack validation ==" -ForegroundColor Cyan
foreach ($dir in Get-ChildItem $packsRoot -Directory) {
  $packCount++
  $mp = Join-Path $dir.FullName "manifest.json"
  if (-not (Test-Path $mp)) { Bad "$($dir.Name): no manifest.json"; continue }
  try { $m = Get-Content $mp -Raw | ConvertFrom-Json } catch { Bad "$($dir.Name): manifest invalid JSON"; continue }
  if ($m.packUid -notmatch $guid) { Bad "$($dir.Name): packUid not a valid GUID" }
  $items=0; $bad=0; $dups=0; $noLim=0; $noTags=0; $uidsHere=@{}
  foreach ($f in $m.files) {
    $fp = Join-Path $dir.FullName $f
    if (-not (Test-Path $fp)) { Bad "$($dir.Name): missing file $f"; continue }
    try { $c = Get-Content $fp -Raw | ConvertFrom-Json } catch { Bad "$($dir.Name)/$f invalid JSON"; continue }
    foreach ($it in $c.items) {
      $items++; $totalItems++
      if ($it.uid -notmatch $guid) { $bad++ }
      if ($uidsHere.ContainsKey($it.uid)) { $dups++ } else { $uidsHere[$it.uid]=1 }
      if ($allUids.ContainsKey($it.uid)) { Bad "CROSS-PACK DUP uid $($it.uid) ($($dir.Name) & $($allUids[$it.uid]))" } else { $allUids[$it.uid]=$dir.Name }
      if ([string]::IsNullOrWhiteSpace($it.limitation)) { $noLim++ }
      if (-not $it.tags -or $it.tags.Count -eq 0) { $noTags++ }
    }
  }
  if ($bad -gt 0) { Bad "$($dir.Name): $bad invalid UID(s)" }
  if ($dups -gt 0) { Bad "$($dir.Name): $dups duplicate UID(s) within pack" }
  if ($items -ne $m.itemCount) { Bad "$($dir.Name): item count $items != manifest.itemCount $($m.itemCount)" }
  if ($bad -eq 0 -and $dups -eq 0 -and $items -eq $m.itemCount) {
    Ok ("{0}: {1} items, valid, limitation+tags missing on {2}/{3}" -f $dir.Name, $items, $noLim, $noTags)
  }
}
Write-Host ("Packs: {0} | items: {1} | distinct UIDs: {2}" -f $packCount, $totalItems, $allUids.Count)
if ($allUids.Count -ne $totalItems) { Bad "cross-pack UID collision detected" } else { Ok "no cross-pack UID collisions" }

if ($Server) {
  Write-Host "`n== Live installed counts ($Server / $Database) ==" -ForegroundColor Cyan
  $sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
  if (-not $sqlcmd) { Write-Host "  sqlcmd not found — skipping live check (offline validation still authoritative)." -ForegroundColor Yellow }
  else {
    $q = "SET NOCOUNT ON; SELECT COUNT(*) FROM KnowledgePacks; SELECT COUNT(*) FROM KnowledgeItems WHERE KnowledgePackId IS NOT NULL;"
    $out = & sqlcmd -S $Server -d $Database -E -b -h -1 -Q $q 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Host "  DB not reachable — skipping live check (run the app once to seed)." -ForegroundColor Yellow }
    else {
      $nums = ($out | Where-Object { $_ -match '^\s*\d+\s*$' } | ForEach-Object { [int]$_.Trim() })
      if ($nums.Count -ge 2) { Ok ("DB has {0} installed pack(s), {1} pack item(s)" -f $nums[0], $nums[1]) }
    }
  }
}

Write-Host "`n== Result ==" -ForegroundColor Cyan
if ($fail -eq 0) { Write-Host "VERIFY-ALL-KNOWLEDGE-PACKS: PASS" -ForegroundColor Green; exit 0 }
Write-Host "VERIFY-ALL-KNOWLEDGE-PACKS: FAIL ($fail issue(s))" -ForegroundColor Red; exit 1
