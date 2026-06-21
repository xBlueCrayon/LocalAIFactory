# IIS Production-Posture Healthcheck (Phase 1)

**Date:** 2026-06-21 · `scripts/deployment-drill/15-iis-production-posture-healthcheck.ps1` (read-only)

Probes the pilot in the **HTTPS + Windows-auth** posture (the closest production-like posture reachable on
this host), sending the current Windows credentials over TLS.

| Check | Result |
|---|---|
| No-credential probe | **HTTP 401** (Windows/Negotiate enforced; `windowsAuthentication enabled=true`) |
| `GET /` (HTTPS + Windows creds) | **200** (41 ms) |
| `GET /Support` | **200** (56 ms) |
| `GET /Readiness` | **200** (38 ms) |
| `GET /BaseKnowledge` | **200** (97 ms) |
| `GET /Coverage` | **200** (41 ms) |
| `GET /Graph` | **200** (56 ms) |
| search `OCR` / `Mauritius` | **57 / 90** matches |
| SQL Express DB | 4 packs / 438 items |
| **HTTP 500 count** | **0** |

**`MODE-A-PRODUCTION-POSTURE-HEALTHCHECK: PASS`**

## What this proves

**HTTPS + Windows/Negotiate authenticated requests through IIS, against SQL Express with a
least-privilege app-pool login, 0 HTTP 500s.** Updated proof ladder:

**Local POC ✅ → Published-app + SQL Express ✅ → IIS pilot ✅ → IIS HTTPS/Windows-auth pilot ✅ →
Production ⬜ → Commercial GA ⬜**

## What it does NOT prove (honest)

- **Self-signed** localhost TLS — not a CA-trusted production certificate.
- The application runs **dev-auth behind IIS**; the app's own RBAC under the IIS Windows identity (with a
  seeded bootstrap admin) is the remaining application-config step.
- Single workstation (not a Windows **Server** edition); no staged/blue-green rollout; no operations over
  time. These remain the **Production** rung of the ladder.
