<#
.SYNOPSIS  Deployment drill 02 — install runtime prerequisites. DRY-RUN by default; -Execute is operator-gated.
.DESCRIPTION In dry-run (default) it PRINTS what a real run would install — it downloads/installs NOTHING.
             -Execute is intentionally a no-op placeholder: prerequisite installation (Hosting Bundle, SQL
             Express) must be performed by the operator from the official Microsoft sources, deliberately, on an
             approved host. This script never silently changes the system.
#>
param([switch]$Execute)
Write-Host "== Prerequisite install plan (DRY-RUN) ==" -ForegroundColor Cyan
@(
  "1. ASP.NET Core 10 Runtime + Hosting Bundle (required for IIS): download from the official Microsoft .NET site",
  "   (community failure pattern: a missing Hosting Bundle causes IIS 500.31/502.5 — see docs/research/COMMUNITY_FAILURE_PATTERNS.md).",
  "2. SQL Server (Express for a pilot, or a full instance for production): install + enable TCP/named-pipes as required.",
  "3. SQL command-line tools (sqlcmd) for verification.",
  "4. (Optional) Ollama for local AI; (optional) Node + Playwright for screenshot regeneration."
) | ForEach-Object { Write-Host "  $_" }
if ($Execute) {
  Write-Host "`n-Execute requested: this script does NOT auto-install prerequisites by design." -ForegroundColor Yellow
  Write-Host "Install them from the official Microsoft sources on the approved host, then re-run 00-prerequisites.ps1." -ForegroundColor Yellow
} else {
  Write-Host "`nDRY-RUN only. Re-run with -Execute to see the operator instructions." -ForegroundColor Yellow
}
