# Full-Production Blockers (by owner)

**Date:** 2026-06-21 · Derived from the production-readiness gate (**PILOT_READY**, 0 technical FAIL).

## Code-completable now — DONE

Build · tests · release package · knowledge packs · LocalDB · SQL Express (Mode C) · IIS (Mode A) ·
HTTPS binding · Windows/Negotiate round-trip · least-privilege SQL · backup/restore · rollback · support
bundle · security audit (0 HIGH) · OWASP-ASVS + NIST-SSDF mappings · local-LLM reasoning + governance ·
100-system benchmark · issue/fix knowledge pack · load **simulation** · production-readiness gate.

## Host-completable now (this workstation) — DONE to pilot level

IIS + ANCM + SQL Express + HTTPS + Windows-auth pilot, all on the local Win 11 box. Further host work
(production env config, app-RBAC under Windows identity) needs a **Server** host (below).

## Human / operator required

- Windows **Server** production host + production DNS + firewall.
- **CA-issued TLS certificate** (replaces the self-signed pilot cert).
- Domain **service account** for the app pool (replaces ApplicationPoolIdentity).
- Production `ASPNETCORE_ENVIRONMENT=Production` with **app RBAC bound to the Windows identity** + a seeded
  bootstrap admin.
- Backup **retention policy** + scheduled job; monitoring/alerting destination; incident escalation contacts;
  change window + rollback approval.
- **Commercial license enforcement** decision.

## External (third-party) required

- **External penetration test / security review** with remediation.
- **Entra ID / OIDC** real tenant integration (app registration, claims mapping against a live tenant).

## Customer / pilot required

- **Signed customer pilot** on sanitized estate data, against written acceptance criteria.

## Reaching FULL_PRODUCTION_READY

The production-readiness gate returns `FULL_PRODUCTION_READY` only when the operator/external/customer items
above are satisfied **and** the gate's PARTIALs (self-signed TLS, dev-auth-behind-IIS, simulation-load,
sampled-docs, runbook-only monitoring) are closed on a real production host. Until then the honest, gate-
enforced status is **PILOT_READY**.
