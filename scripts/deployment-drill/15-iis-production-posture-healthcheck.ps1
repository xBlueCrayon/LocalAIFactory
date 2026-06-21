<#
.SYNOPSIS  Deployment drill 15 — MODE A: production-posture health check (HTTPS + Windows auth). READ-ONLY.
.DESCRIPTION Probes the IIS pilot over HTTPS using the current Windows credentials (the production-posture path),
             checks the SQL Express DB counts, and reports the site's auth configuration. Read-only. Non-zero on
             failure. Uses -SkipCertificateCheck for the self-signed pilot cert (LOCAL pilot TLS only).
.PARAMETER AppUrl (https) / SqlServer / Database / SiteName / ExpectPacks / ExpectItems
#>
param(
  [string]$AppUrl = "https://localhost:8443",
  [string]$SqlServer = ".\SQLEXPRESS",
  [string]$Database = "LocalAIFactory_IISProof",
  [string]$SiteName = "LocalAIFactoryPilot",
  [int]$ExpectPacks = 4,
  [int]$ExpectItems = 438
)
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$appcmd = Join-Path $env:windir "system32\inetsrv\appcmd.exe"
$fail = 0
function Ok($m){ Write-Host "  [ OK ] $m" -ForegroundColor Green }
function Bad($m){ Write-Host "  [FAIL] $m" -ForegroundColor Red; $script:fail++ }

Write-Host "== Mode A production-posture healthcheck ($AppUrl) ==" -ForegroundColor Cyan

# Detect the auth posture by behaviour: a no-credential probe returning 401 means Windows auth is enforced.
$probe = 0
try { $probe = [int](Invoke-WebRequest -UseBasicParsing "$AppUrl/" -TimeoutSec 15 -SkipCertificateCheck).StatusCode }
catch { $resp = $_.Exception.Response; $probe = if ($resp) { [int]$resp.StatusCode } else { 0 } }
$useCreds = ($probe -eq 401)
$winCfg = (& $appcmd list config $SiteName /section:windowsAuthentication 2>$null | Out-String)
$winAuthOn = ($winCfg -match 'enabled="true"')
Write-Host "  no-credential probe -> HTTP $probe $(if($useCreds){'(Windows/Negotiate enforced -> will send Windows credentials)'}else{'(anonymous)'}); windowsAuthentication config enabled=$winAuthOn"

# HTTPS pages with credentials when Windows auth is on
$rows = New-Object System.Collections.ArrayList
foreach ($p in @("/","/Support","/Readiness","/BaseKnowledge","/Coverage","/Graph")) {
  $sw=[Diagnostics.Stopwatch]::StartNew()
  try {
    $iwr = @{ UseBasicParsing=$true; Uri="$AppUrl$p"; TimeoutSec=25; SkipCertificateCheck=$true }
    if ($useCreds) { $iwr.UseDefaultCredentials = $true }
    $r = Invoke-WebRequest @iwr; $c=[int]$r.StatusCode
  } catch { $resp=$_.Exception.Response; $c=if($resp){[int]$resp.StatusCode}else{0} }
  $sw.Stop()
  [void]$rows.Add([pscustomobject]@{ path=$p; code=$c; ms=[int]$sw.ElapsedMilliseconds })
  if ($c -eq 200) { Ok "HTTPS GET $p -> 200 ($([int]$sw.ElapsedMilliseconds) ms)" } elseif ($c -ge 500) { Bad "GET $p -> $c (server error)" } else { Bad "GET $p -> $c" }
}
# search
foreach ($t in @("OCR","Mauritius")) {
  $iwr = @{ UseBasicParsing=$true; Uri="$AppUrl/BaseKnowledge?q=$t"; TimeoutSec=20; SkipCertificateCheck=$true }
  if ($useCreds) { $iwr.UseDefaultCredentials = $true }
  try { $b=(Invoke-WebRequest @iwr).Content } catch { $b="" }
  $n=([regex]::Matches($b,"/BaseKnowledge/Details/")).Count
  if ($n -ge 1) { Ok "search '$t' -> $n matches" } else { Bad "search '$t' -> 0 matches" }
}
# DB
$packs=(sqlcmd -S $SqlServer -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.KnowledgePacks" -h -1 2>$null|Out-String).Trim()
$items=(sqlcmd -S $SqlServer -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.KnowledgeItems WHERE KnowledgePackId IS NOT NULL" -h -1 2>$null|Out-String).Trim()
if ($packs -match '^\d+$' -and [int]$packs -ge $ExpectPacks) { Ok "DB packs = $packs" } else { Bad "DB packs = '$packs'" }
if ($items -match '^\d+$' -and [int]$items -ge $ExpectItems) { Ok "DB pack items = $items" } else { Bad "DB items = '$items'" }

$err500 = ($rows | Where-Object { $_.code -ge 500 }).Count
$rows | ConvertTo-Json | Set-Content (Join-Path $repo ".tmp-iis-prod-posture.json")
Write-Host "`n  TLS: self-signed localhost (pilot only, NOT production CA). Auth posture: $(if($useCreds){'Windows/Negotiate (authenticated requests)'}else{'anonymous + app dev-auth'}). HTTP 500s: $err500"
Write-Host "MODE-A-PRODUCTION-POSTURE-HEALTHCHECK: $(if($fail -eq 0){'PASS'}else{"FAIL ($fail)"})" -ForegroundColor ($fail -eq 0 ? "Green":"Red")
exit ([int]($fail -ne 0))
