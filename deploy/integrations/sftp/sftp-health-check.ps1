<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: SFTP reachability + host-key reminder (read-only; no transfer, no credentials).
.DESCRIPTION TCP-connects to the SFTP host:port. Does NOT authenticate or transfer. Reminds the operator to pin
             the host-key fingerprint. Actual transfer tests (sftp-upload-test/sftp-download-test) require an
             SFTP client/module (e.g. Posh-SSH or WinSCP) and a configured endpoint.
#>
param([string]$SftpHost = "localhost", [int]$Port = 22, [int]$TimeoutSec = 5)
$ErrorActionPreference = "Stop"
Write-Host "Checking SFTP $SftpHost`:$Port ..." -ForegroundColor Cyan
$r = Test-NetConnection -ComputerName $SftpHost -Port $Port -WarningAction SilentlyContinue
if ($r.TcpTestSucceeded) {
  Write-Host "SFTP port: REACHABLE ($SftpHost`:$Port)" -ForegroundColor Green
  Write-Host "Reminder: pin and verify the host-key fingerprint before trusting this endpoint." -ForegroundColor Yellow
  exit 0
} else { Write-Host "SFTP port: NOT reachable ($SftpHost`:$Port)." -ForegroundColor Red; exit 1 }
