<#
.SYNOPSIS  Diagnose reverse-proxy / forwarded-headers readiness (X-Forwarded-For/Proto). READ-ONLY advisory.
.DESCRIPTION Behind a reverse proxy / load balancer, ASP.NET Core must enable ForwardedHeaders middleware so the
             app sees the real client scheme/IP (else auth cookies, HTTPS redirects, and audit IPs break).
#>
param([string]$WebProject="src/LocalAIFactory.Web")
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
Write-Host "== Forwarded-headers readiness (advisory) ==" -ForegroundColor Cyan
$prog = Get-ChildItem (Join-Path $repo $WebProject) -Filter "Program.cs" -Recurse -EA SilentlyContinue | Select-Object -First 1
$has = $false
if ($prog) { $has = (Get-Content $prog.FullName -Raw) -match 'ForwardedHeaders|UseForwardedHeaders' }
if ($has) { Write-Host "  [ OK ] ForwardedHeaders configured in the app" -ForegroundColor Green }
else { Write-Host "  [INFO] ForwardedHeaders NOT configured — fine for direct IIS/Kestrel; REQUIRED behind a reverse proxy/LB so the app sees real scheme+client IP (auth/redirects/audit). Add UseForwardedHeaders + KnownProxies/Networks." -ForegroundColor Yellow }
