<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: seed the database and install the Professional Base Knowledge Pack.
.DESCRIPTION The app migrates + seeds + installs the knowledge pack on startup (idempotently). This script
             applies migrations, starts the app briefly against the target database to trigger the install,
             waits until the pack is present (or times out), stops the app, and verifies. Non-destructive and
             idempotent — re-running does not duplicate items.
#>
param(
  [string]$Instance = "(localdb)\MSSQLLocalDB",
  [string]$Database = "LocalAIFactory",
  [int]$Port = 5099,
  [int]$TimeoutSec = 180
)
$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot/..").Path
$cs = "Server=$Instance;Database=$Database;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"

& "$PSScriptRoot/apply-migrations.ps1" -ConnectionString $cs -RepoRoot $repo
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Host "Building web app..." -ForegroundColor Cyan
& dotnet build "$repo/src/LocalAIFactory.Web/LocalAIFactory.Web.csproj" -c Release --nologo | Select-Object -Last 1 | Out-Host
if ($LASTEXITCODE -ne 0) { Write-Host "Build failed." -ForegroundColor Red; exit 1 }

Write-Host "Starting app to seed + install knowledge pack..." -ForegroundColor Cyan
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:$Port"
$env:ConnectionStrings__DefaultConnection = $cs
Start-Process -FilePath "dotnet" -ArgumentList @("run","--project","$repo/src/LocalAIFactory.Web/LocalAIFactory.Web.csproj","-c","Release","--no-build") -WindowStyle Hidden | Out-Null

$seeded = $false
for ($i = 0; $i -lt [math]::Ceiling($TimeoutSec / 3); $i++) {
  Start-Sleep -Seconds 3
  $n = (sqlcmd -S "$Instance" -d $Database -E -C -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM KnowledgeItems WHERE KnowledgePackId IS NOT NULL;" 2>$null | Select-Object -First 1)
  if ($n -and ([int]$n.Trim()) -ge 100) { $seeded = $true; break }
}
Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" -ErrorAction SilentlyContinue |
  Where-Object { $_.CommandLine -match 'LocalAIFactory.Web' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }

if (-not $seeded) { Write-Host "Seed did not complete within ${TimeoutSec}s." -ForegroundColor Red; exit 1 }
Write-Host "Seed complete. Verifying..." -ForegroundColor Green
& "$PSScriptRoot/verify-knowledge-base.ps1" -ServerInstance $Instance -Database $Database
exit $LASTEXITCODE
