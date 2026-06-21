<#
.SYNOPSIS  Publish this generated ERP (Release) to a local folder. Runs on SQLite by default; set
           ConnectionStrings:Default for SQL Server / SQL Express. EXE stays local (git-ignored).
#>
param([string]$OutputRoot = "C:\LAFEnterpriseERP-Gold")
$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot/..").Path
$web = Join-Path $repo "src/LafErp.Web/LafErp.Web.csproj"
try { New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null } catch { $OutputRoot = Join-Path $repo "dist-local"; New-Item -ItemType Directory -Force -Path $OutputRoot | Out-Null }
dotnet publish $web -c Release -o $OutputRoot --nologo | Out-Null
@"
LAF Enterprise ERP Gold - local production
Run: dotnet LafErp.Web.dll  (or LafErp.Web.exe) then open http://localhost:5000
SQLite by default. For SQL Server/Express set:
  set ConnectionStrings__Default=Server=.\SQLEXPRESS;Database=LafErpGold;Trusted_Connection=True;TrustServerCertificate=True
Real login: admin / Admin#12345 (PBKDF2; change in production). Schema: EnsureCreated (SQLite) /
Database.Migrate (SQL Server, when migrations are present). RBAC + maker/checker + audit enforced.
"@ | Set-Content (Join-Path $OutputRoot "README-LOCAL-PRODUCTION.txt")
Write-Host "Published: $(Join-Path $OutputRoot 'LafErp.Web.dll')" -ForegroundColor Green
