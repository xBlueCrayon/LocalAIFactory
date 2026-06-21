<#
.SYNOPSIS  Deployment drill 01 — capture host facts for the deployment record. READ-ONLY.
#>
$ErrorActionPreference = "SilentlyContinue"
$os = Get-CimInstance Win32_OperatingSystem
$cs = Get-CimInstance Win32_ComputerSystem
Write-Host "== Host facts (read-only) ==" -ForegroundColor Cyan
"Machine        : $env:COMPUTERNAME"
"OS             : $($os.Caption) $($os.Version)"
"RAM (GB)       : $([math]::Round($cs.TotalPhysicalMemory/1GB,1))"
"Logical CPUs   : $($cs.NumberOfLogicalProcessors)"
".NET           : $((& dotnet --version) 2>$null)"
"IIS installed  : $([bool](Get-Service W3SVC -EA SilentlyContinue))"
"SQL services   : $((Get-Service 'MSSQL*' -EA SilentlyContinue | Select-Object -Expand Name) -join ', ')"
$ipv4 = (Get-NetIPAddress -AddressFamily IPv4 -EA SilentlyContinue | Where-Object { $_.IPAddress -notlike '127.*' } | Select-Object -First 1).IPAddress
"Primary IPv4   : $ipv4"
Write-Host "`nCapture this output into the deployment evidence (08-capture-evidence.ps1)." -ForegroundColor Yellow
