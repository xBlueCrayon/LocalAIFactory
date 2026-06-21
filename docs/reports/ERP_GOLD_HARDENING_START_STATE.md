# ERP Gold Hardening — Start State (Phase 0)

**Date:** 2026-06-21 · **Branch:** ke-008-code-symbols · **Starting commit:** `0896c10`

Captured before the hardening sprint began, to make the delta honest and measurable.

## Gates at start

| Gate | Result |
|------|--------|
| `dotnet build LocalAIFactory.sln -c Release` | 0 errors |
| `LocalAIFactory.Tests` | 240 / 240 pass |
| `verify-all-knowledge-packs.ps1` | PASS — 22 packs / 876 items, no UID collisions |
| `verify-production-readiness-v3.ps1` | `NEAR_GA_READY_WITH_EXTERNAL_PROOF_MODEL` (internalCompleteness 84.8, gaNow 65.4) |
| `security-audit.ps1` | PASS — 0 HIGH findings |

## ERP Gold at start (from the prior sprint)

- 138 xUnit tests, 16 Playwright tests.
- Real PBKDF2 auth + cookie login + role claims + login audit.
- Create / list / read UI; no edit/delete.
- Schema via `EnsureCreated`; **no committed EF migration history**.
- Reproduction (GoldGenerated): 82% module / 93% test.

## Open LOCAL blockers this sprint must close

1. No committed EF migration history.
2. Create/list/read only; no edit/delete UI.
3. Auth lacks lockout / password policy / anti-forgery / secure-cookie hardening.

(Blocker #4 module breadth and #5 external gates — SSO/OIDC, CA TLS, external security review, customer
acceptance — are out of local scope and tracked honestly, not "closed".)

## Process cleanup

Stale `dotnet`/`node`/Playwright processes from the prior sprint were stopped before starting (0 left running).
