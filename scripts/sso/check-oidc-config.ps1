<#
.SYNOPSIS  SSO readiness — check whether an OIDC/Entra ID front-door is configured. READ-ONLY.
.DESCRIPTION Inspects the app settings for the additive SSO hooks described in docs/SSO-IdP-Readiness.md
             (Security:AuthScheme + an Oidc section). It reports presence/shape only; it NEVER prints secret
             values (client secret / certificate) and NEVER writes anything. On this release OIDC is by design
             NOT configured (Windows/Negotiate + guarded dev auth), so "not configured" is the expected result.
.PARAMETER SettingsFile  appsettings json to inspect (default: the Web project's appsettings.json).
#>
param([string]$SettingsFile = "")
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
if (-not $SettingsFile) { $SettingsFile = Join-Path $repo "src/LocalAIFactory.Web/appsettings.json" }
Write-Host "== OIDC / Entra ID config check (read-only) ==" -ForegroundColor Cyan
Write-Host "  Settings: $SettingsFile"
if (-not (Test-Path $SettingsFile)) { Write-Host "  [FAIL] settings file not found." -ForegroundColor Red; exit 1 }

$cfg = Get-Content $SettingsFile -Raw | ConvertFrom-Json
$scheme = $cfg.Security?.AuthScheme
$oidc   = $cfg.Oidc

Write-Host "`n  Security:AuthScheme = $(if ($scheme) { $scheme } else { '(unset -> defaults to Windows)' })"
if (-not $oidc) {
  Write-Host "  Oidc section        = (absent)" -ForegroundColor Yellow
  Write-Host "`n  RESULT: OIDC NOT configured. This is the expected, supported default for this release" -ForegroundColor Yellow
  Write-Host "          (Windows/Negotiate + guarded dev auth). To enable Entra ID later, add the Oidc section per" -ForegroundColor Yellow
  Write-Host "          docs/SSO_ENTRA_ID_PROOF_PACK.md, then re-run this check on the target host." -ForegroundColor Yellow
  exit 0
}

# OIDC section present — validate the SHAPE of required keys, never their secret values.
$required = @("Authority","ClientId","CallbackPath")
$secretish = @("ClientSecret","ClientCertificateThumbprint")
$missing = @()
foreach ($k in $required) { if (-not $oidc.$k) { $missing += $k } }
Write-Host "`n  Oidc section present. Required keys:"
foreach ($k in $required) {
  $present = [bool]$oidc.$k
  Write-Host ("    {0,-22} {1}" -f $k, $(if ($present) { "[ OK ] set ($($oidc.$k))" } else { "[MISS]" })) -ForegroundColor ($present ? "Green":"Red")
}
Write-Host "  Secret material (presence only — values never shown):"
foreach ($k in $secretish) {
  Write-Host ("    {0,-30} {1}" -f $k, $(if ($oidc.$k) { "[ set ]" } else { "[ not set ]" }))
}
if ($oidc.ClientSecret -and $oidc.ClientSecret -notmatch '^\$\{?env:' ) {
  Write-Host "  [WARN] ClientSecret appears to be an inline literal. Use an env var / Key Vault / certificate, not committed config." -ForegroundColor Yellow
}
if ($missing.Count) { Write-Host "`n  RESULT: OIDC partially configured — missing: $($missing -join ', ')" -ForegroundColor Red; exit 1 }
Write-Host "`n  RESULT: OIDC shape looks complete. Validate claims mapping next: scripts/sso/validate-claims-mapping.ps1" -ForegroundColor Green
exit 0
