<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: SMTP relay reachability check (read-only; sends no mail).
.DESCRIPTION TCP-connects to the SMTP host:port to confirm reachability before configuring send. No credentials,
             no email. Use smtp-test-send.ps1 (to a dev sink) to validate an actual send.
#>
param([string]$SmtpHost = "localhost", [int]$Port = 25, [int]$TimeoutSec = 5)
$ErrorActionPreference = "Stop"
Write-Host "Checking SMTP $SmtpHost`:$Port ..." -ForegroundColor Cyan
$r = Test-NetConnection -ComputerName $SmtpHost -Port $Port -WarningAction SilentlyContinue
if ($r.TcpTestSucceeded) { Write-Host "SMTP: REACHABLE ($SmtpHost`:$Port)" -ForegroundColor Green; exit 0 }
else { Write-Host "SMTP: NOT reachable ($SmtpHost`:$Port). Check host/port/firewall." -ForegroundColor Red; exit 1 }
