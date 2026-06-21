<#
.SYNOPSIS  Honest reachability check for LAF ScreenStream Assist. READ-ONLY.
.DESCRIPTION Reports what a client can actually reach: localhost (always), the server's LAN IPv4s (clients on
             the same network can use these if Windows Firewall allows the port), and the honest truth about
             the internet (a home PC behind NAT is NOT reachable without a public address + port-forward/relay).
.PARAMETER Port  Dashboard/stream port (default 5090).
#>
param([int]$Port = 5090)
Write-Host "== LAF ScreenStream Assist - network reachability ==" -ForegroundColor Cyan

Write-Host "`n[1] Loopback (same PC): ws://localhost:$Port/stream  -> ALWAYS works." -ForegroundColor Green

Write-Host "`n[2] LAN (same Wi-Fi/router): clients on your network can use one of these, if Windows Firewall allows the port:" -ForegroundColor Yellow
Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
  Where-Object { $_.IPAddress -notlike '169.*' -and $_.IPAddress -ne '127.0.0.1' } |
  ForEach-Object { "    ws://$($_.IPAddress):$Port/stream   (interface: $($_.InterfaceAlias))" }

$fw = Get-NetFirewallProfile -ErrorAction SilentlyContinue | Where-Object Enabled -eq $true
Write-Host "`n[3] Firewall: $((($fw).Name) -join ', ') profile(s) enabled. On first run Windows asks to Allow the app - click Allow for Private networks." -ForegroundColor Yellow

Write-Host "`n[4] Internet (different network / NAT): a home PC is NOT directly reachable from the internet." -ForegroundColor Red
Write-Host "    To use over the internet you need ONE of:" -ForegroundColor Red
Write-Host "      - a fixed public IP + router PORT FORWARDING of port $Port to this PC, or"
Write-Host "      - a tunnel/relay you run, then set ScreenStream:PublicServerUrl to that address."
Write-Host "    Production internet use also needs TLS/WSS (wss://) - not configured in this sample."
Write-Host "    This tool does NOT and will NOT bypass NAT or your firewall. No fake internet reachability." -ForegroundColor Red
