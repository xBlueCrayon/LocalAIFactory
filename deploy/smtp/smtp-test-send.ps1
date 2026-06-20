<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: send ONE test email — intended for a DEV SINK (Mailpit/MailHog), not production.
.DESCRIPTION Sends a test message via SMTP. Defaults target a local Mailpit/MailHog dev sink (localhost:1025)
             so no real mail leaves the machine. Refuses to send to a non-dev host unless -AllowExternal is set
             explicitly (operator intent). Credentials, if any, come from -Credential — never embedded.
#>
param(
  [string]$SmtpHost = "localhost",
  [int]$Port = 1025,
  [string]$From = "noreply@dev.local",
  [string]$To = "sink@dev.local",
  [switch]$AllowExternal,
  [System.Management.Automation.PSCredential]$Credential,
  [switch]$UseStartTls
)
$ErrorActionPreference = "Stop"
$isDevSink = ($SmtpHost -in @("localhost","127.0.0.1")) -and ($Port -in @(1025,1026,2525))
if (-not $isDevSink -and -not $AllowExternal) {
  Write-Host "Refusing to send to a non-dev host without -AllowExternal (safety). Use a Mailpit/MailHog dev sink." -ForegroundColor Yellow
  exit 1
}
try {
  $msg = [System.Net.Mail.MailMessage]::new($From, $To, "LocalAIFactory SMTP test", "This is a test send from smtp-test-send.ps1.")
  $client = [System.Net.Mail.SmtpClient]::new($SmtpHost, $Port)
  if ($UseStartTls) { $client.EnableSsl = $true }
  if ($Credential) { $client.Credentials = $Credential.GetNetworkCredential() }
  $client.Send($msg)
  Write-Host "SMTP test send OK -> $SmtpHost`:$Port ($To)" -ForegroundColor Green
  exit 0
} catch { Write-Host "SMTP test send FAILED: $($_.Exception.Message)" -ForegroundColor Red; exit 1 }
