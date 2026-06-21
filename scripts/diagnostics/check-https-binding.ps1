<#
.SYNOPSIS  Diagnose an IIS HTTPS binding (cert + sslcert + reachability). READ-ONLY.
#>
param([string]$Site="LocalAIFactoryPilot",[int]$Port=8443)
$appcmd="$env:windir\system32\inetsrv\appcmd.exe"
Write-Host "== HTTPS binding check ($Site :$Port) ==" -ForegroundColor Cyan
$b=(& $appcmd list site $Site /text:bindings 2>$null|Out-String)
Write-Host "  bindings: $($b.Trim())"
if($b -match "https/\*:${Port}:"){ Write-Host "  [ OK ] https binding on :$Port present" -ForegroundColor Green } else { Write-Host "  [WARN] no https binding on :$Port" -ForegroundColor Yellow }
$ssl=(netsh http show sslcert ipport=0.0.0.0:$Port 2>$null|Out-String)
if($ssl -match "Certificate Hash"){ Write-Host "  [ OK ] netsh sslcert bound on 0.0.0.0:$Port" -ForegroundColor Green } else { Write-Host "  [WARN] no sslcert on 0.0.0.0:$Port (fix: netsh http add sslcert ipport=0.0.0.0:$Port certhash=<thumb> appid=<guid>)" -ForegroundColor Yellow }
try{ $c=[int](Invoke-WebRequest -UseBasicParsing "https://localhost:$Port/" -TimeoutSec 8 -SkipCertificateCheck -UseDefaultCredentials).StatusCode; Write-Host "  [ OK ] https reachable -> $c" -ForegroundColor Green }catch{ Write-Host "  [WARN] https not reachable: $(($_.Exception.Message -split '\.')[0])" -ForegroundColor Yellow }
