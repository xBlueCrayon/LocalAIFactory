<#
.SYNOPSIS  Publish LAF ScreenStream Assist to a simple local folder a non-developer can double-click.
.DESCRIPTION Publishes the Server and Client (framework-dependent .NET 10 EXEs), lays out the simple folder
             structure, generates a ready TestClient, and writes plain-language READMEs + Start-Server.bat.
.PARAMETER OutputRoot  Where to place the folder. Defaults to C:\LAFScreenStreamAssist; falls back to the
             repo dist-local folder if C:\ is not writable.
#>
param([string]$OutputRoot = "C:\LAFScreenStreamAssist")

$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot/..").Path           # the product root
$src = Join-Path $repo "src"

# Fall back to a repo-local dist folder if C:\ cannot be written.
try { New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null; [IO.File]::WriteAllText((Join-Path $OutputRoot ".w"), "x"); Remove-Item (Join-Path $OutputRoot ".w") -Force }
catch { $OutputRoot = Join-Path $repo "dist-local"; Write-Host "C:\ not writable; using $OutputRoot" -ForegroundColor Yellow; New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null }

$serverDir = Join-Path $OutputRoot "Server"
$clientTpl = Join-Path $serverDir "ClientTemplate"
$genRoot   = Join-Path $OutputRoot "GeneratedClients"
$testClient = Join-Path $genRoot "TestClient"
$gen       = Join-Path $OutputRoot "ClientGenerator"
$evidence  = Join-Path $OutputRoot "Evidence"
foreach ($d in @($serverDir, $clientTpl, $genRoot, $testClient, $gen, (Join-Path $evidence "screenshots"), (Join-Path $evidence "test-results"), (Join-Path $evidence "logs"))) { New-Item -ItemType Directory -Force -Path $d | Out-Null }

Write-Host "Publishing Server..." -ForegroundColor Cyan
dotnet publish (Join-Path $src "LafScreenStream.Server/LafScreenStream.Server.csproj") -c Release -o $serverDir --nologo | Out-Null
Write-Host "Publishing Client (template + TestClient)..." -ForegroundColor Cyan
dotnet publish (Join-Path $src "LafScreenStream.Client/LafScreenStream.Client.csproj") -c Release -o $clientTpl --nologo | Out-Null

# A fixed session token so the pre-made TestClient works with the server immediately.
$token = -join ((1..32) | ForEach-Object { '{0:x}' -f (Get-Random -Max 16) })
[IO.File]::WriteAllText((Join-Path $serverDir "screenstream-token.txt"), $token)

# Server appsettings: client template + generated clients location + dashboard port.
@"
{
  "ScreenStream": {
    "Port": 5090,
    "PublicServerUrl": "ws://localhost:5090/stream",
    "ClientPublishDir": "ClientTemplate",
    "GeneratedClientsDir": "..\\GeneratedClients"
  },
  "Logging": { "LogLevel": { "Default": "Warning" } }
}
"@ | Set-Content (Join-Path $serverDir "appsettings.json")

# Pre-generate the TestClient package (copy client + config + readme + checksum).
Copy-Item (Join-Path $clientTpl "*") $testClient -Recurse -Force
@"
{
  "serverWsUrl": "ws://localhost:5090/stream",
  "displayName": "TestClient",
  "token": "$token"
}
"@ | Set-Content (Join-Path $testClient "client-config.json")
$exe = Join-Path $testClient "LAFScreenStream.Client.exe"
$sum = if (Test-Path $exe) { (Get-FileHash $exe -Algorithm SHA256).Hash.ToLower() } else { "" }
"LAFScreenStream.Client.exe SHA256: $sum" | Set-Content (Join-Path $testClient "checksum.txt")

# Simple files for a non-developer.
"@echo off`r`ncd /d ""%~dp0""`r`nstart """" ""%~dp0LAFScreenStream.Server.exe""`r`n" | Set-Content (Join-Path $serverDir "Start-Server.bat")
"@echo off`r`necho Open the dashboard, type a name, and click 'Generate Client'.`r`nstart http://localhost:5090`r`npause`r`n" | Set-Content (Join-Path $gen "Generate-TestClient.bat")

@"
LAF ScreenStream Assist - SERVER (read me first)
================================================
1) Double-click  Start-Server.bat
2) Your browser opens the dashboard (or open http://localhost:5090).
3) Type a name and click 'Generate Client'. A client folder is created under ..\GeneratedClients.
4) Send that whole folder to the other person. They double-click LAFScreenStream.Client.exe.
5) Their PRIMARY screen appears on your dashboard. They can click Disconnect any time.

A ready-made TestClient is already in ..\GeneratedClients\TestClient for same-PC testing.

Network: works on the SAME PC and SAME Wi-Fi/LAN (allow it through Windows Firewall when asked).
For the internet you need a reachable public address (port-forward or a relay) + TLS. See
README-START-HERE.txt. This tool shares ONLY the primary screen and never captures keyboard, files,
clipboard, webcam, or microphone, and never runs hidden.
"@ | Set-Content (Join-Path $serverDir "README-FIRST.txt")

@"
LAF ScreenStream Assist - START HERE (for everyone)
===================================================
ON YOUR PC (the helper):
  1. Open the 'Server' folder. Double-click 'Start-Server.bat'.
  2. A dashboard opens in your browser.
  3. Click 'Generate Client'. Send the new folder in 'GeneratedClients' to your friend.

ON YOUR FRIEND'S PC (the one sharing):
  4. They open the folder you sent and double-click 'LAFScreenStream.Client.exe'.
  5. A window says their primary screen is being shared, with a Disconnect button.
  6. To stop, they click Disconnect.

ONLY use this with the other person's permission. It shares the primary screen only.
If they are not on your Wi-Fi/LAN, you need a public server address (router port-forward or a relay)
and TLS for the internet - see SCREENSTREAM_NETWORK_FEASIBILITY_REPORT.md.
"@ | Set-Content (Join-Path $OutputRoot "README-START-HERE.txt")

Write-Host "`nDONE." -ForegroundColor Green
Write-Host "Server EXE : $(Join-Path $serverDir 'LAFScreenStream.Server.exe')"
Write-Host "Start file  : $(Join-Path $serverDir 'Start-Server.bat')"
Write-Host "Test client : $exe"
