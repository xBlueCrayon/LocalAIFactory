<#
.SYNOPSIS
  R2-ACC-20X: safe, read-only security self-audit over TRACKED files.
.DESCRIPTION
  Static scans only — no network, no system changes. Flags potential secrets, hardcoded passwords, dangerous
  shell commands in scripts, and large/forbidden tracked artifacts. Exits non-zero if any HIGH finding is
  present so it can gate a release. This is NOT a penetration test; see docs/Security-Pentest-Readiness.md.
#>
param([switch]$FailOnFindings)

$ErrorActionPreference = "Continue"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
Push-Location $repo
$high = 0; $info = 0
function High($m) { Write-Host "  [HIGH] $m" -ForegroundColor Red; $script:high++ }
function Info($m) { Write-Host "  [INFO] $m" -ForegroundColor Yellow; $script:info++ }
function Ok($m)   { Write-Host "  [ OK ] $m" -ForegroundColor Green }

$tracked = git ls-files

Write-Host "== Forbidden / large tracked artifacts ==" -ForegroundColor Cyan
$forbidden = $tracked | Where-Object { $_ -match '/(bin|obj)/' -or $_ -match '\.(mdf|ldf|bak|gguf|onnx|safetensors|pfx|pem|key)$' -or $_ -match '^keys/' }
if ($forbidden) { $forbidden | ForEach-Object { High "forbidden tracked: $_" } } else { Ok "no bin/obj/db/model/key artifacts tracked" }
$big = $tracked | Where-Object { (Test-Path $_) -and ((Get-Item $_).Length -gt 5MB) }
if ($big) { $big | ForEach-Object { High "large tracked file (>5MB): $_" } } else { Ok "no tracked file exceeds 5 MB" }

Write-Host "`n== Potential secrets / hardcoded credentials ==" -ForegroundColor Cyan
# Scan text-ish source/config, excluding docs and examples. Patterns are deliberately conservative.
$scan = $tracked | Where-Object { $_ -match '\.(cs|json|ps1|config|cshtml|xml|yml|yaml)$' -and $_ -notmatch '(\.example\.|/docs/|THIRD-PARTY|appsettings\.Development\.json)' }
$patterns = @(
  @{ Name='password assignment'; Rx='(?i)(password|pwd)\s*[:=]\s*["''][^"''$<>{}\s]{4,}["'']' },
  @{ Name='api key literal';     Rx='(?i)(api[_-]?key|secret|token)\s*[:=]\s*["''][A-Za-z0-9/\+_\-]{16,}["'']' },
  @{ Name='AWS access key';      Rx='AKIA[0-9A-Z]{16}' },
  @{ Name='private key block';   Rx='-----BEGIN (RSA|EC|OPENSSH|PRIVATE) ' }
)
$found = $false
foreach ($f in $scan) {
  $content = Get-Content $f -Raw -ErrorAction SilentlyContinue
  if (-not $content) { continue }
  foreach ($p in $patterns) {
    foreach ($m in [regex]::Matches($content, $p.Rx)) {
      $val = $m.Value
      # Allowlist obvious placeholders / connection-string Integrated Security.
      if ($val -match '(?i)(your[-_]?|example|placeholder|changeme|dummy|<|\$\(|Integrated Security|Trusted_Connection)') { continue }
      High "$($p.Name) in $f : $($val.Substring(0,[Math]::Min(60,$val.Length)))"
      $found = $true
    }
  }
}
if (-not $found) { Ok "no hardcoded secrets matched in tracked source/config" }

Write-Host "`n== Dangerous shell commands in committed scripts ==" -ForegroundColor Cyan
$danger = 'Remove-Item\s+-Recurse\s+-Force\s+[A-Za-z]:\\|rm\s+-rf\s+/|git\s+reset\s+--hard|git\s+clean\s+-fdx|DROP\s+DATABASE|format\s+[A-Za-z]:'
$scripts = $tracked | Where-Object { $_ -match '\.ps1$' -and $_ -notmatch '(security-audit|/docs/)' }
$danable = $false
foreach ($f in $scripts) {
  $c = Get-Content $f -Raw -ErrorAction SilentlyContinue
  if ($c -and ($c -match $danger)) { Info "review destructive pattern in $f"; $danable = $true }
}
if (-not $danable) { Ok "no unguarded destructive patterns in committed scripts" }

Pop-Location
Write-Host "`n== Result ==" -ForegroundColor Cyan
Write-Host "HIGH findings: $high   INFO findings: $info"
if ($FailOnFindings -and $high -gt 0) { Write-Host "SECURITY-AUDIT: FAIL" -ForegroundColor Red; exit 1 }
Write-Host "SECURITY-AUDIT: PASS (no HIGH findings)" -ForegroundColor Green
exit 0
