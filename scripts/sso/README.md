# SSO / Entra ID readiness scripts

**Read-only** validators for the additive OIDC/Entra ID front-door described in
[`../../docs/SSO-IdP-Readiness.md`](../../docs/SSO-IdP-Readiness.md) and
[`../../docs/SSO_ENTRA_ID_PROOF_PACK.md`](../../docs/SSO_ENTRA_ID_PROOF_PACK.md).

> **Honest status:** Enterprise SSO (Entra ID / OIDC) is **design + minimal-hooks roadmap**, not
> implemented in this release. Production auth is **Windows / Negotiate** with a guarded dev handler;
> RBAC + per-project ACLs are enforced server-side and audited. These scripts validate the *shape* of an
> OIDC configuration when an operator adds one on a target host — they neither enable SSO nor print secrets.

## Scripts

| Script | Default | What it does |
|--------|---------|--------------|
| `check-oidc-config.ps1` | read-only | Reports whether `Security:AuthScheme` + an `Oidc` section are configured and whether the required keys are present. Never prints secret values. Reports "not configured" (exit 0) on this release. |
| `validate-claims-mapping.ps1` | read-only | Validates the shape of `Oidc:ClaimsMapping` (subject claim, IdP-group→`UserRole` map, default role, optional project-access claim) against the platform's role model. |

## Usage

```powershell
.\scripts\sso\check-oidc-config.ps1
.\scripts\sso\validate-claims-mapping.ps1
# Point at a specific environment's settings:
.\scripts\sso\check-oidc-config.ps1 -SettingsFile C:\inetpub\LocalAIFactory\appsettings.json
```

## Safety contract

- **Read-only.** Neither script writes config, enables a scheme, or contacts an IdP.
- **No secret exposure.** Client secrets / certificate thumbprints are reported as present/absent only;
  their values are never printed. An inline-literal `ClientSecret` triggers a warning to move it to an
  env var / Key Vault / certificate.
- **Safe default.** With no OIDC section (this release's default), both scripts report the expected
  "not configured" state and exit 0 — they do not fail a build or gate.

The end-to-end proof these scripts support — an actual Entra sign-in mapping to `UserRole`/project ACLs,
captured, with Windows/dev auth still working — is **not yet executed**. See the proof pack for the
procedure and the success criteria.
