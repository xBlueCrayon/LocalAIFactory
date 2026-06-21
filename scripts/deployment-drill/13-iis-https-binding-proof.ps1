<#
.SYNOPSIS  Deployment drill 13 — MODE A: add an HTTPS binding to the IIS pilot site (localhost self-signed). DRY-RUN default.
.DESCRIPTION In dry-run it prints the plan. With -Execute it creates a self-signed **localhost** certificate (if
             absent), adds an https binding to the site, and associates the certificate via `netsh http add sslcert`.
             Self-signed is for a LOCAL pilot only — this is NOT production TLS and makes no CA-trust claim. The
             HTTP binding is left intact. Outputs JSON evidence under .tmp-*; prints rollback steps.
.PARAMETER SiteName / HttpsPort / Dns / Execute
#>
param(
  [string]$SiteName = "LocalAIFactoryPilot",
  [int]$HttpsPort = 8443,
  [string]$Dns = "localhost",
  [switch]$Execute
)
$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$appcmd = Join-Path $env:windir "system32\inetsrv\appcmd.exe"

Write-Host "== IIS HTTPS binding plan ($SiteName :$HttpsPort, self-signed $Dns) ==" -ForegroundColor Cyan
Write-Host "  NOTE: self-signed localhost cert = LOCAL PILOT TLS only. NOT production TLS, NOT CA-trusted." -ForegroundColor Yellow
if (-not $Execute) {
  Write-Host "  DRY-RUN. Would: New-SelfSignedCertificate -DnsName $Dns; appcmd add https binding :$HttpsPort; netsh http add sslcert." -ForegroundColor Yellow
  Write-Host "  Re-run elevated with -Execute to apply. Rollback: remove binding + netsh delete sslcert + remove cert." -ForegroundColor Yellow
  return
}

# 1. Self-signed localhost cert (reuse if one already exists for this DNS).
$cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -eq "CN=$Dns" -and $_.NotAfter -gt (Get-Date) } | Select-Object -First 1
if (-not $cert) {
  $cert = New-SelfSignedCertificate -DnsName $Dns -CertStoreLocation Cert:\LocalMachine\My -FriendlyName "LocalAIFactory pilot ($Dns)" -NotAfter (Get-Date).AddMonths(6)
  Write-Host "  created self-signed cert (thumbprint $($cert.Thumbprint))" -ForegroundColor Green
} else { Write-Host "  reusing self-signed cert (thumbprint $($cert.Thumbprint))" -ForegroundColor Green }

# 2. HTTPS binding on the site (idempotent).
$bindings = & $appcmd list site $SiteName /text:bindings
if ($bindings -notmatch "https/\*:${HttpsPort}:") {
  & $appcmd set site $SiteName "/+bindings.[protocol='https',bindingInformation='*:${HttpsPort}:']" | Out-Null
  Write-Host "  added https binding *:$HttpsPort" -ForegroundColor Green
} else { Write-Host "  https binding *:$HttpsPort already present" -ForegroundColor Yellow }

# 3. Associate the cert with the port (netsh). appid is an arbitrary stable GUID for this app.
$appid = "{9f0e0da5-0001-4a00-9000-0000000000aa}"
& netsh http delete sslcert ipport=0.0.0.0:$HttpsPort 2>$null | Out-Null
$null = & netsh http add sslcert ipport=0.0.0.0:$HttpsPort certhash=$($cert.Thumbprint) appid="$appid" certstorename=MY
Write-Host "  bound cert to 0.0.0.0:$HttpsPort" -ForegroundColor Green

& $appcmd start site $SiteName 2>$null | Out-Null
@{ site=$SiteName; httpsPort=$HttpsPort; dns=$Dns; thumbprint=$cert.Thumbprint; selfSigned=$true } | ConvertTo-Json |
  Set-Content (Join-Path $repo ".tmp-iis-https-evidence.json")
Write-Host "`n  HTTPS pilot binding ready: https://${Dns}:$HttpsPort (self-signed — use -SkipCertificateCheck to probe)." -ForegroundColor Green
Write-Host "  Rollback: appcmd set site $SiteName /-bindings.[protocol='https',bindingInformation='*:${HttpsPort}:'];" -ForegroundColor Yellow
Write-Host "            netsh http delete sslcert ipport=0.0.0.0:$HttpsPort; remove cert $($cert.Thumbprint) from Cert:\LocalMachine\My." -ForegroundColor Yellow
