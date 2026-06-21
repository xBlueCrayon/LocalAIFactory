<#
.SYNOPSIS  Deployment drill 11 — MODE A: health check the IIS-hosted app. READ-ONLY.
.DESCRIPTION Probes the IIS endpoint (HTTP), the SQL Express deployment DB (packs/items/migrations), and the IIS
             site/app-pool state via appcmd. Writes a JSON evidence file under .tmp-* (git-ignored). Returns
             non-zero on any failed gate. Changes nothing.
.PARAMETER AppUrl / SqlServer / Database / SiteName / AppPoolName / ExpectPacks / ExpectItems
#>
param(
  [string]$AppUrl = "http://localhost:8095",
  [string]$SqlServer = ".\SQLEXPRESS",
  [string]$Database = "LocalAIFactory_IISProof",
  [string]$SiteName = "LocalAIFactoryPilot",
  [string]$AppPoolName = "LocalAIFactoryPilotPool",
  [int]$ExpectPacks = 4,
  [int]$ExpectItems = 438
)
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$appcmd = Join-Path $env:windir "system32\inetsrv\appcmd.exe"
$fail = 0
function Ok($m){ Write-Host "  [ OK ] $m" -ForegroundColor Green }
function Bad($m){ Write-Host "  [FAIL] $m" -ForegroundColor Red; $script:fail++ }

Write-Host "== Mode A IIS health check — $AppUrl / $SqlServer / $Database ==" -ForegroundColor Cyan
$rows = New-Object System.Collections.ArrayList

# HTTP pages
foreach ($p in @("/","/Support","/Readiness","/BaseKnowledge","/Coverage","/Graph","/Benchmarks")) {
  $sw=[Diagnostics.Stopwatch]::StartNew()
  try { $r=Invoke-WebRequest -UseBasicParsing "$AppUrl$p" -TimeoutSec 25; $c=[int]$r.StatusCode; $body=$r.Content } catch { $resp=$_.Exception.Response; $c=if($resp){[int]$resp.StatusCode}else{0}; $body="" }
  $sw.Stop()
  [void]$rows.Add([pscustomobject]@{ path=$p; code=$c; ms=[int]$sw.ElapsedMilliseconds })
  if ($c -eq 200) { Ok "GET $p -> 200 ($([int]$sw.ElapsedMilliseconds) ms)" } elseif ($c -ge 500) { Bad "GET $p -> $c (server error)" } else { Bad "GET $p -> $c" }
}
# Base Knowledge search
foreach ($t in @("OCR","Mauritius","market")) {
  try { $b=(Invoke-WebRequest -UseBasicParsing "$AppUrl/BaseKnowledge?q=$t" -TimeoutSec 20).Content } catch { $b="" }
  $n=([regex]::Matches($b,"/BaseKnowledge/Details/")).Count
  [void]$rows.Add([pscustomobject]@{ path="/BaseKnowledge?q=$t"; code=200; matches=$n })
  if ($n -ge 1) { Ok "search '$t' -> $n matches" } else { Bad "search '$t' -> 0 matches" }
}

# DB counts
$packs=(sqlcmd -S $SqlServer -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.KnowledgePacks" -h -1 2>$null|Out-String).Trim()
$items=(sqlcmd -S $SqlServer -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.KnowledgeItems WHERE KnowledgePackId IS NOT NULL" -h -1 2>$null|Out-String).Trim()
$migs=(sqlcmd -S $SqlServer -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.__EFMigrationsHistory" -h -1 2>$null|Out-String).Trim()
if ($packs -match '^\d+$' -and [int]$packs -ge $ExpectPacks) { Ok "DB packs = $packs" } else { Bad "DB packs = '$packs'" }
if ($items -match '^\d+$' -and [int]$items -ge $ExpectItems) { Ok "DB pack items = $items" } else { Bad "DB items = '$items'" }
if ($migs -match '^\d+$' -and [int]$migs -ge 1) { Ok "migrations = $migs" } else { Bad "migrations = '$migs'" }

# IIS site / app pool state
if (Test-Path $appcmd) {
  $siteState = (& $appcmd list site $SiteName /text:state 2>$null | Out-String).Trim()
  $poolState = (& $appcmd list apppool $AppPoolName /text:state 2>$null | Out-String).Trim()
  if ($siteState -eq "Started") { Ok "IIS site '$SiteName' state = Started" } else { Bad "IIS site state = '$siteState'" }
  Write-Host "  IIS app pool '$AppPoolName' state = $poolState"
} else { Write-Host "  [INFO] appcmd not found — IIS state not checked" -ForegroundColor Yellow }

$err500 = ($rows | Where-Object { $_.code -ge 500 }).Count
$rows | ConvertTo-Json | Set-Content (Join-Path $repo ".tmp-iis-mode-a-health.json")
Write-Host "`n  HTTP 500 count: $err500"
Write-Host "MODE-A-IIS-HEALTHCHECK: $(if ($fail -eq 0) { 'PASS' } else { "FAIL ($fail)" })" -ForegroundColor ($fail -eq 0 ? "Green":"Red")
exit ([int]($fail -ne 0))
