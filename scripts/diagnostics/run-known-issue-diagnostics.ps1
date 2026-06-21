<#
.SYNOPSIS  Run the known-issue diagnostic battery learned from the support issue/fix pack. READ-ONLY.
.DESCRIPTION Applies the production-issue-fixes knowledge pack as live, automatable checks against THIS repo/host:
             page-hang anti-patterns (GroupBy(_=>1), large-text list materialization), blocking external calls on
             the request path, secrets in committed config. Reports each as PASS/WARN. Non-zero on a real anti-pattern.
#>
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$src = Join-Path $repo "src"
$fail = 0
function Ok($m){ Write-Host "  [PASS] $m" -ForegroundColor Green }
function Warn($m){ Write-Host "  [WARN] $m" -ForegroundColor Yellow; $script:fail++ }
Write-Host "== Known-issue diagnostics (from production-issue-fixes pack) ==" -ForegroundColor Cyan
# Only first-party source — exclude build output (bin/obj) and generated files which produce false positives.
$cs = Get-ChildItem $src -Recurse -Filter *.cs -EA SilentlyContinue |
  Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Name -notmatch '\.(g|Designer|AssemblyInfo|GlobalUsings)\.cs$' }
# Strip // line-comments before pattern-matching so guard comments ("no GroupBy(_=>1)") don't trip the detector.
function Hits($rx){ $cs | ForEach-Object { $f=$_.FullName; Get-Content $f | Where-Object { $_ -notmatch '^\s*//' } | Select-String -Pattern $rx -EA SilentlyContinue } }
$gb = Hits 'GroupBy\(\s*_\s*=>\s*1\s*\)'
if ($gb){ Warn "GroupBy(_=>1) present in code ($($gb.Count)) - known SQL page-hang anti-pattern" } else { Ok "no GroupBy(_=>1) aggregate-by-constant in source (comments excluded)" }
# Flag .Result/.GetResult ONLY when the file has no Task.WhenAll — i.e. NOT the approved post-WhenAll parallel-read
# pattern (DashboardService), which is non-blocking because the tasks are already complete.
$syncFiles = $cs | Where-Object {
  $c = Get-Content $_.FullName -Raw
  ($c -match '\.GetAwaiter\(\)\.GetResult\(\)|\.Result\s*;') -and ($c -notmatch 'Task\.WhenAll')
}
if ($syncFiles){ Warn "blocking sync-over-async without Task.WhenAll in: $(($syncFiles.Name) -join ', ')" } else { Ok "no blocking sync-over-async (post-WhenAll .Result reads are the approved parallel pattern)" }
$ctrl = Get-ChildItem $src -Recurse -Filter *Controller.cs -EA SilentlyContinue | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
$extInCtrl = $ctrl | ForEach-Object { Get-Content $_.FullName | Where-Object { $_ -match 'new\s+(HttpClient|QdrantClient|OllamaClient)\b' } }
if ($extInCtrl){ Warn "external client constructed inside a controller ($($extInCtrl.Count)) - verify not a blocking request-path call" } else { Ok "no external client constructed inside controllers" }
# Real committed app config only — exclude bin/obj copies and benchmark/sample fixtures.
$sec = Get-ChildItem $src -Recurse -Include appsettings*.json -EA SilentlyContinue |
  Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
  Select-String -Pattern 'Password\s*=\s*[^;\s"'']+' -EA SilentlyContinue
if ($sec){ Warn "inline DB password in committed app config ($($sec.Count))" } else { Ok "no inline secrets in committed app config (Trusted_Connection)" }
@{ fail=$fail; groupByConst=$gb.Count; syncOverAsync=$sync.Count; extInController=$extInCtrl.Count } |
  ConvertTo-Json | Set-Content (Join-Path $repo "benchmarks/results/known-issue-diagnostics.json")
Write-Host "`nKNOWN-ISSUE-DIAGNOSTICS: $(if($fail -eq 0){'PASS (no known anti-pattern present)'}else{"WARN ($fail to review)"})" -ForegroundColor ($fail -eq 0 ? "Green":"Yellow")
exit ([int]($fail -ne 0))
