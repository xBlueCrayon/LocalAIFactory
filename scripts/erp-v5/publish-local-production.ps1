<#
.SYNOPSIS  Publish LAF Enterprise ERP V5 to a local folder for local-production testing (framework-dependent).
.DESCRIPTION Publishes the generated ERP V5 web app in Release. Runs on SQLite by default (zero external
             services); set ConnectionStrings:Default to an MSSQL/SQL Express instance for production use.
             The EXE is kept LOCAL (git-ignored), never committed.
.PARAMETER OutputRoot  Default C:\LAFEnterpriseERP-V5 (falls back to the product dist-local if C:\ unwritable).
#>
param([string]$OutputRoot = "C:\LAFEnterpriseERP-V5")
$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$web = Join-Path $repo "generated-products/LAF-EnterpriseERP-V5/src/LafErp.Web/LafErp.Web.csproj"
try { New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null; [IO.File]::WriteAllText((Join-Path $OutputRoot ".w"),"x"); Remove-Item (Join-Path $OutputRoot ".w") -Force }
catch { $OutputRoot = Join-Path $repo "generated-products/LAF-EnterpriseERP-V5/dist-local"; New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null }

Write-Host "Publishing ERP V5 (Release) -> $OutputRoot ..." -ForegroundColor Cyan
dotnet publish $web -c Release -o $OutputRoot --nologo | Out-Null

@"
LAF Enterprise ERP V5 - LOCAL PRODUCTION (read me)
==================================================
Run:   dotnet LafErp.Web.dll   (or LafErp.Web.exe)   then open http://localhost:5000
Database: SQLite by default (laferp.db, zero external services).
For MSSQL/SQL Express set an environment variable before running:
   set ConnectionStrings__Default=Server=.\SQLEXPRESS;Database=LafErp;Trusted_Connection=True;TrustServerCertificate=True
The app creates the schema (EnsureCreated) and seeds demo data on first run.

Production constraints (honest): schema via EnsureCreated (not EF migrations yet); dev auth (bind to
Windows/SSO for real auth); no TLS in-app (terminate TLS at IIS/reverse proxy). RBAC, maker/checker, and
audit are enforced by design.
"@ | Set-Content (Join-Path $OutputRoot "README-LOCAL-PRODUCTION.txt")

Write-Host "DONE. App: $(Join-Path $OutputRoot 'LafErp.Web.dll')" -ForegroundColor Green
