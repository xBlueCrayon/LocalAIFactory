<#
.SYNOPSIS  SSO readiness — validate the claims→role/project mapping config. READ-ONLY.
.DESCRIPTION Checks the shape of the Oidc:ClaimsMapping block that translates IdP roles/groups into the
             platform's UserRole (Viewer/Analyst/Admin) and ProjectAccess grants (see docs/Claims-Roles-Mapping.md).
             It validates structure only and changes nothing. If OIDC is not configured (the default on this
             release) it reports that and exits 0.
.PARAMETER SettingsFile  appsettings json to inspect (default: the Web project's appsettings.json).
#>
param([string]$SettingsFile = "")
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
if (-not $SettingsFile) { $SettingsFile = Join-Path $repo "src/LocalAIFactory.Web/appsettings.json" }
Write-Host "== Claims -> role/project mapping validation (read-only) ==" -ForegroundColor Cyan
if (-not (Test-Path $SettingsFile)) { Write-Host "  [FAIL] settings file not found: $SettingsFile" -ForegroundColor Red; exit 1 }

$cfg = Get-Content $SettingsFile -Raw | ConvertFrom-Json
$map = $cfg.Oidc?.ClaimsMapping
if (-not $cfg.Oidc) {
  Write-Host "  Oidc not configured -> no claims mapping to validate (expected default for this release)." -ForegroundColor Yellow
  exit 0
}
if (-not $map) {
  Write-Host "  [FAIL] Oidc is configured but Oidc:ClaimsMapping is absent. Add role + project mapping per docs/Claims-Roles-Mapping.md." -ForegroundColor Red
  exit 1
}

$validRoles = @("Viewer","Analyst","Admin")
$fail = 0
function Bad($m){ Write-Host "  [FAIL] $m" -ForegroundColor Red; $script:fail++ }
function Ok($m){ Write-Host "  [ OK ] $m" -ForegroundColor Green }

# Subject claim: which claim is the stable external id
if ($map.SubjectClaim) { Ok "SubjectClaim = $($map.SubjectClaim)" } else { Bad "SubjectClaim missing (the stable external id used to resolve UserAccount)" }

# Role mapping: each entry maps an IdP group/role -> a valid UserRole
if ($map.RoleMap) {
  foreach ($prop in $map.RoleMap.PSObject.Properties) {
    if ($validRoles -contains $prop.Value) { Ok "role: '$($prop.Name)' -> $($prop.Value)" }
    else { Bad "role: '$($prop.Name)' -> '$($prop.Value)' is not one of $($validRoles -join '/')" }
  }
} else { Bad "RoleMap missing (IdP group/role -> UserRole)" }

# Default role: must be present and the least-privilege Viewer is recommended
if ($map.DefaultRole) {
  if ($validRoles -contains $map.DefaultRole) {
    Ok "DefaultRole = $($map.DefaultRole)"
    if ($map.DefaultRole -ne "Viewer") { Write-Host "  [WARN] DefaultRole is not the least-privilege 'Viewer' — confirm this is intended." -ForegroundColor Yellow }
  } else { Bad "DefaultRole '$($map.DefaultRole)' is not a valid UserRole" }
} else { Bad "DefaultRole missing (fallback role when no group matches — should default to Viewer)" }

# Project access mapping is optional but, if present, must reference a claim
if ($map.ProjectAccessClaim) { Ok "ProjectAccessClaim = $($map.ProjectAccessClaim)" }
else { Write-Host "  [INFO] No ProjectAccessClaim — project grants will be managed in-app (deny-by-default still applies)." -ForegroundColor Cyan }

Write-Host "`nVALIDATE-CLAIMS-MAPPING: $(if ($fail -eq 0) { 'PASS' } else { 'FAIL' })" -ForegroundColor ($fail -eq 0 ? "Green":"Red")
exit ([int]($fail -ne 0))
