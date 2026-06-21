# Official Integration Expectation Library

**Date:** 2026-06-21 · `benchmarks/integration-expectations/` (**19 systems**) · validated by
`scripts/integration/validate-integration-expectations.ps1` (**PASS**)

Official-source **expectation models** for integrating with real systems — auth method, base URL pattern, key
endpoints, expected request/success/failure shapes, common failure, diagnostic, and prevention rule per
system. **No live integration was executed against any endpoint.**

## Support-status spread (honest)

| Status | Count | Systems |
|---|---:|---|
| **SUPPORTED** (the project actually uses these) | 4 | Ollama API, SQL Server backup/restore, ASP.NET Core IIS/ANCM, GitHub Releases API |
| **PARTIAL** (design + validators) | 2 | Entra OIDC, Keycloak Admin REST |
| **METADATA_ONLY / EMULATED** | 2 | OpenTelemetry, Prometheus/Grafana |
| **EMULATED_EXPECTATION_ONLY** (no live endpoint) | 11 | Odoo, WordPress REST, WooCommerce REST, ERPNext/Frappe REST, Magento REST, Drupal JSON:API, Joomla, PrestaShop, OpenCart, SMTP, SFTP |

Each file carries the note: *"Official expectation model only — live integration NOT executed."*

## What this is — and is NOT

- **Is:** a reusable, source-referenced contract library so an operator can integrate each system correctly
  (correct auth, endpoints, payload shapes, and the common failure + fix). The 4 **SUPPORTED** entries reflect
  integrations the project genuinely performs.
- **Is NOT:** proof that any CMS/ERP/eCommerce integration *works*. No live endpoint was called; the 11
  `EMULATED_EXPECTATION_ONLY` entries are **expectation models**, not tested integrations. We do **not** claim
  integration success without a live endpoint.

## Path to real integration

Supply a live endpoint + credentials (via the operator-emulation pack) and execute the documented diagnostic/
request against it; only then does an entry move from `EMULATED_EXPECTATION_ONLY` to a tested status.
