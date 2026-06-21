# IIS Windows / Negotiate Authentication Proof (Phase 1)

**Date:** 2026-06-21 · **Site:** `LocalAIFactoryPilot` · **Over:** `https://localhost:8443`

`scripts/deployment-drill/14-iis-windows-auth-proof.ps1 -Execute` enabled **Windows Authentication** and
disabled **Anonymous** on the pilot site, then exercised the full Negotiate round-trip over HTTPS.

| Probe | Result |
|---|---|
| `GET /` **without** credentials | **HTTP 401** — IIS enforces the Negotiate challenge ✅ |
| `GET /` **with** current Windows credentials (`-UseDefaultCredentials`, over HTTPS) | **HTTP 200** — authenticated round-trip ✅ |

**`WINDOWS-AUTH-PROOF: PASS`** — the full Windows/Negotiate authenticated round-trip is proven.

## Why this needed HTTPS

In the earlier Mode A pass (plain HTTP), `Invoke-WebRequest -UseDefaultCredentials` **refused to send
default Windows credentials over an unencrypted connection** ("cannot protect plain text secrets"). Over
**HTTPS** the client sends the credentials, so the **401 → authenticated 200** handshake completes. This
closes the prior "no full Windows/Negotiate authenticated round-trip under IIS" gap **at the IIS layer**.

## Honest scope

- This proves IIS **Windows/Negotiate authentication is enforcing and accepting** the current Windows
  identity over HTTPS.
- The application currently runs `ASPNETCORE_ENVIRONMENT=Development` (dev-auth) **behind** IIS for full
  page reachability. Wiring the application's own RBAC/deny-by-default to consume the IIS-provided Windows
  identity (production `Negotiate` scheme + a seeded Windows bootstrap admin) is the remaining
  **application** auth-config step — the IIS transport/auth layer itself is proven.
- The site is left in the Windows-auth posture; restore anonymous with
  `14-iis-windows-auth-proof.ps1 -Revert`.
