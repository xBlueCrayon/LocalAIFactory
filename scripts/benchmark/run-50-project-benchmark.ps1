<#
.SYNOPSIS  Public-project breadth benchmark — clone N real public repos (shallow/pinned), classify + count, status each.
.DESCRIPTION For each repo in the manifest: shallow-clone (with a per-repo timeout + one retry), resolve the HEAD
             sha, classify files by language, count SUPPORTED files (C#/T-SQL/Python) vs unsupported, estimate LOC
             and a LIGHTWEIGHT symbol count (regex declaration count — NOT the full Roslyn/graph extraction), and
             assign an honest status. Each clone is DELETED after analysis to bound disk. Clones live under a
             git-ignored cache. This is the BREADTH layer (many repos/languages); the DEPTH/graph layer remains the
             .NET harness (tools/LocalAIFactory.Benchmark) on the supported C# fixtures/repos.
.PARAMETER Manifest / MaxRepos / Suite / TimeoutPerRepoSeconds / CacheDir / KeepClones
#>
param(
  [string]$Manifest = "benchmarks/public-projects-50.json",
  [int]$MaxRepos = 50,
  [string]$Suite = "public50",
  [int]$TimeoutPerRepoSeconds = 240,
  [string]$CacheDir = ".tmp-benchmark-repos",
  [switch]$KeepClones
)
$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$manifestPath = Join-Path $repo $Manifest
if (-not (Test-Path $manifestPath)) { Write-Host "manifest not found: $manifestPath" -ForegroundColor Red; exit 1 }
$cache = Join-Path $repo $CacheDir
New-Item -ItemType Directory -Force $cache | Out-Null
$resultsDir = Join-Path $repo "benchmarks/results"
New-Item -ItemType Directory -Force $resultsDir | Out-Null

$specs = (Get-Content $manifestPath -Raw | ConvertFrom-Json).repos | Select-Object -First $MaxRepos
Write-Host "== Public-project benchmark: $Suite ($($specs.Count) repos, timeout ${TimeoutPerRepoSeconds}s/repo) ==" -ForegroundColor Cyan

$supportedExt = @{ ".cs"="csharp"; ".sql"="sql"; ".py"="python" }
function Count-Symbols($files) {
  $cs=0;$py=0;$sqlo=0
  foreach ($f in $files) {
    $ext=[IO.Path]::GetExtension($f).ToLower()
    try { $txt = Get-Content $f -Raw -EA Stop } catch { continue }
    if ([string]::IsNullOrEmpty($txt)) { continue }
    switch ($ext) {
      ".cs"  { $cs  += ([regex]::Matches($txt,'(?m)^\s*(public|internal|private|protected|sealed|static|abstract|partial)[^\r\n;{]*\b(class|interface|record|struct|enum)\s+\w+')).Count
               $cs  += ([regex]::Matches($txt,'(?m)^\s*(public|internal|private|protected|static|async)[^=\r\n;]*\b\w+\s*\([^)]*\)\s*(\{|=>)')).Count }
      ".py"  { $py  += ([regex]::Matches($txt,'(?m)^\s*(def|class)\s+\w+')).Count }
      ".sql" { $sqlo += ([regex]::Matches($txt,'(?i)CREATE\s+(TABLE|PROCEDURE|PROC|VIEW|FUNCTION)\s')).Count }
    }
  }
  return @{ csharp=$cs; python=$py; sql=$sqlo }
}

$results = New-Object System.Collections.ArrayList
$i = 0
foreach ($s in $specs) {
  $i++
  $dir = Join-Path $cache $s.id
  $sw = [Diagnostics.Stopwatch]::StartNew()
  $status="";$sha="";$files=0;$supported=0;$byLang=@{};$loc=0;$syms=@{csharp=0;python=0;sql=0};$note=""
  Write-Host ("[{0,2}/{1}] {2} ({3}, {4})" -f $i,$specs.Count,$s.id,$s.primaryLanguage,$s.sizeTier) -ForegroundColor Cyan
  if (Test-Path $dir) { Remove-Item $dir -Recurse -Force -EA SilentlyContinue }

  # shallow clone with timeout + one retry
  $cloned=$false
  foreach ($attempt in 1..2) {
    if (Test-Path $dir) { Remove-Item $dir -Recurse -Force -EA SilentlyContinue }   # clean partial dir before (re)try — fixes "destination already exists" after a timeout-kill
    $p = Start-Process git -ArgumentList @("clone","--depth","1","--single-branch","--branch",$s.defaultBranch,"--quiet",$s.url,$dir) -PassThru -WindowStyle Hidden -RedirectStandardError (Join-Path $cache "$($s.id).clone.err")
    if ($p.WaitForExit($TimeoutPerRepoSeconds*1000)) { if ($p.ExitCode -eq 0) { $cloned=$true; break } }
    else { try { $p.Kill() } catch {}; $status="TimedOut"; $note="clone exceeded ${TimeoutPerRepoSeconds}s"; break }
    Start-Sleep 1
  }
  if (-not $cloned -and $status -ne "TimedOut") { $status="CloneFailed"; $note=(Get-Content (Join-Path $cache "$($s.id).clone.err") -Raw -EA SilentlyContinue) -replace '\s+',' ' | ForEach-Object { $_.Substring(0,[Math]::Min(120,$_.Length)) } }

  if ($cloned) {
    $sha = (& git -C $dir rev-parse HEAD 2>$null); $sha = ($sha | Out-String).Trim()
    if (-not $sha) { $status="CheckoutFailed" }
    else {
      # enumerate files honouring excludes
      $all = Get-ChildItem $dir -Recurse -File -EA SilentlyContinue | Where-Object {
        $rel = $_.FullName.Substring($dir.Length)
        ($rel -notmatch '[\\/](bin|obj|node_modules|\.git|dist|build|vendor|packages)[\\/]')
      }
      $files = $all.Count
      $byLang = $all | Group-Object { [IO.Path]::GetExtension($_.Name).ToLower() } | ForEach-Object { @{ ext=$_.Name; n=$_.Count } }
      $supportedFiles = $all | Where-Object { $supportedExt.ContainsKey([IO.Path]::GetExtension($_.Name).ToLower()) }
      $supported = $supportedFiles.Count
      $cap = if ($s.maxFilesToAnalyze) { [int]$s.maxFilesToAnalyze } else { 2000 }
      $analyze = $supportedFiles | Select-Object -First $cap
      if ($supported -eq 0) {
        $status = if ($s.benchmarkMode -eq "validation-only") { "UnsupportedLanguage" } else { "NoSupportedFiles" }
      }
      elseif ($s.benchmarkMode -eq "validation-only") { $status="ValidationOnly"; $note="cloned + classified; deep extraction skipped by design" }
      else {
        $loc = ($analyze | ForEach-Object { try { (Get-Content $_.FullName -EA Stop | Measure-Object -Line).Lines } catch { 0 } } | Measure-Object -Sum).Sum
        $syms = Count-Symbols ($analyze.FullName)
        if ($supported -gt $cap) { $status="PassedPartial"; $note="analyzed first $cap of $supported supported files" }
        else { $status="Passed" }
      }
    }
  }
  $sw.Stop()
  if (-not $KeepClones -and (Test-Path $dir)) { Remove-Item $dir -Recurse -Force -EA SilentlyContinue }
  Remove-Item (Join-Path $cache "$($s.id).clone.err") -Force -EA SilentlyContinue

  $row = [ordered]@{ id=$s.id; displayName=$s.displayName; primaryLanguage=$s.primaryLanguage; sizeTier=$s.sizeTier;
    status=$status; sha=$sha; totalFiles=$files; supportedFiles=$supported; loc=$loc;
    symbolsCSharp=$syms.csharp; symbolsPython=$syms.python; sqlObjects=$syms.sql; durationSec=[math]::Round($sw.Elapsed.TotalSeconds,1); note=$note }
  [void]$results.Add([pscustomobject]$row)
  Write-Host ("        -> {0}  files={1} supported={2} loc={3} symC#={4} symPy={5} sql={6} ({7}s)" -f $status,$files,$supported,$loc,$syms.csharp,$syms.python,$syms.sql,([math]::Round($sw.Elapsed.TotalSeconds,1))) -ForegroundColor Gray
}

$outJson = Join-Path $resultsDir "$Suite-results.json"
@{ suite=$Suite; manifest=$Manifest; attempted=$results.Count; results=$results } | ConvertTo-Json -Depth 6 | Set-Content $outJson -Encoding UTF8
Write-Host "`nResults: $outJson  (attempted=$($results.Count))" -ForegroundColor Green
$byStatus = $results | Group-Object status | ForEach-Object { "$($_.Name)=$($_.Count)" }
Write-Host "Status: $($byStatus -join '  ')" -ForegroundColor Cyan
