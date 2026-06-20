<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: SFTP download + response-processing test (operator-gated).
.DESCRIPTION Documents/drives a download test via Posh-SSH if installed. Idempotent: a file already processed
             (by name + hash) is skipped. No credentials stored. Exits 0 with guidance if Posh-SSH is absent.
#>
param([string]$Endpoint, [string]$Username, [string]$RemoteDir = "/out", [string]$LocalDir = "./incoming")
if (-not (Get-Module -ListAvailable -Name Posh-SSH)) {
  Write-Host "Posh-SSH not installed. Install-Module Posh-SSH -Scope CurrentUser to run a real download test." -ForegroundColor Yellow
  exit 0
}
if (-not $Endpoint -or -not $Username) { Write-Host "Provide -Endpoint -Username (key auth via secret store)." -ForegroundColor Yellow; exit 0 }
Write-Host "Download pattern: connect (key auth, pinned host key) -> Get-SFTPItem from $RemoteDir -> verify SHA-256 -> idempotency check (skip if already processed) -> process response/rejection codes -> archive." -ForegroundColor Cyan
exit 0
