# Tech Support / Issue Learning Report

**Date:** 2026-06-21 · `benchmarks/support-issue-learning-registry.json` (42 issues) ·
`knowledge-packs/production-issue-fixes-v1` (42 items) · `docs/Production-Issue-Fix-Knowledge-Base.md`

Real production problems and fixes were learned and encoded as reusable rules + governed knowledge — **not**
copied support text.

## Coverage (42 issue patterns)

IIS deploy / ANCM / Hosting-Bundle / Windows-auth / 500.19 / 500.30 / 502.5 · SQL Express connection /
app-pool login / EF migration · HTTPS cert binding / `netsh sslcert` · appsettings/env · stdout logging /
locked files · LocalDB vs Express · backup/restore · file permissions · process locks · Playwright · GitHub
release upload · PowerShell quoting/path · long-path/clone-timeout · large-repo benchmark · Ollama / Qdrant ·
OCR/PDF · SFTP/SMTP · WordPress/Odoo/ERPNext/WooCommerce/Magento/Drupal API · Keycloak/OIDC · Entra claims ·
reverse-proxy/forwarded-headers · CORS/cookies · upload limits · DB timeout/pool · high-memory · rollback.

## Firsthand findings from THIS project (highest confidence)

Several registry entries are **firsthand**, discovered and verified here:

- **Clone-retry "destination exists"** → delete the partial clone dir before retrying (fixed in the runner).
- **ANCM absent right after enabling IIS** → install the Hosting Bundle (winget), which registers
  `AspNetCoreModuleV2` under `C:\Program Files\IIS\Asp.Net Core Module\V2\`.
- **`Invoke-WebRequest -UseDefaultCredentials` won't send creds over plain HTTP** → use HTTPS for the
  Windows/Negotiate round-trip.
- **`appcmd list config /text:<section>.enabled` is the wrong attribute path** → parse the section XML.

## Reusable diagnostics (executable)

`scripts/diagnostics/` — `check-iis-50030`, `check-iis-50019`, `check-sql-apppool-login`, `check-https-binding`,
`check-forwarded-headers`, `check-large-repo-clone-failure`, `check-ollama-models`, `check-qdrant-health`. Each
encodes the detect/fix rule for its issue class. Example rule: *"If IIS returns 500.30 after deployment, check
Event Viewer + stdout logs + appsettings + runtime bundle before changing code."*

## Honest scope

No web browsing was performed for this pass; items are original, concise, and framed with confidence levels.
Official-source confirmation is flagged per item; community patterns are discovery aids, not authority.
