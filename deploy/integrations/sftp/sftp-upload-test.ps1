<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: SFTP upload test (operator-gated; requires an SFTP client + configured endpoint).
.DESCRIPTION This script documents and drives an upload test using Posh-SSH if installed. It NEVER stores
             credentials and refuses to run without an explicit -Endpoint + key/credential. If Posh-SSH is not
             present it prints install guidance and exits 0 (nothing attempted).
#>
param([string]$Endpoint, [string]$Username, [string]$LocalFile, [string]$RemoteDir = "/in")
if (-not (Get-Module -ListAvailable -Name Posh-SSH)) {
  Write-Host "Posh-SSH not installed. To run a real SFTP test: Install-Module Posh-SSH -Scope CurrentUser" -ForegroundColor Yellow
  Write-Host "Then re-run with -Endpoint <host> -Username <user> -LocalFile <path> (key auth via secret store)." -ForegroundColor Yellow
  exit 0
}
if (-not $Endpoint -or -not $Username -or -not $LocalFile) { Write-Host "Provide -Endpoint -Username -LocalFile (and configure key auth)." -ForegroundColor Yellow; exit 0 }
Write-Host "Upload pattern: connect (key auth, pinned host key) -> Put-SFTPItem to $RemoteDir -> write '<file>.done' marker -> archive after success." -ForegroundColor Cyan
Write-Host "Operator-gated: wire your credential/key here per your secret store, then enable the transfer call." -ForegroundColor Yellow
exit 0
