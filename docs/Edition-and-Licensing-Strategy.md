# Edition & Licensing Strategy (Proposed)

This document proposes how LocalAIFactory **could** be packaged and licensed commercially. It is a strategy,
not a shipped capability. **No license enforcement exists in the product today** — every feature is available
in every build, and access is governed by contract during pilots, not by code. This document is deliberately
honest about that gap and about what must be built before a commercial sale.

---

## 1. Honest starting point

- **Update (R2-ACC-20X):** a **demo-safe edition/license seam is now implemented and tested** —
  `Core/Licensing/` (`Edition`, `LicenseFeature`, `LicenseVerifier`: 14-day grace, degrade-to-Community,
  core-features unlicenseable; 8 tests), surfaced read-only on `/Support`. It is still a **placeholder
  enforcement** seam: there is **no DRM, no phone-home, no signed-license loading, no seat counting**. See
  [`License-Enforcement-Design.md`](License-Enforcement-Design.md) and [`Edition-Matrix.md`](Edition-Matrix.md).
  The gating model below remains the commercial concept, not yet a hard entitlement check.
- The platform is a **local-first, MSSQL-authoritative** engineering + knowledge tool. It works fully offline
  in MSSQL-only mode. Any licensing approach must respect that air-gapped reality (no phone-home).
- Readiness for "Commercial Product" maturity is **low by design** on the scorecard (`/Readiness`). This
  document describes the path, not the destination.

---

## 2. Proposed editions

These editions describe *intended* capability tiers. Today they all map to the same single build.

| Edition | Intended audience | Intended scope |
|---|---|---|
| **Pilot** | A single buyer running a controlled, supervised evaluation | Full feature set, time-boxed by contract, single instance, business-hours support. This is what exists today (see `Commercial-Pilot-Package.md`). |
| **Standard** | A single team / department | Deterministic C#/T-SQL/Python understanding, C#↔SQL & Python↔SQL bridges, coverage/gap, Base Knowledge Pack, Windows auth + RBAC + audit, backup/restore, per-server install. |
| **Enterprise** | Multiple teams / estate-wide | Standard plus (planned) cross-repository estate model, SSO/IdP, scale testing, supportability dashboard, defined SLAs, external audit support. |

> Enterprise-only items (estate model, SSO, scale testing, SLAs) are **not built yet**. An Enterprise edition
> cannot honestly be sold until they exist (see §6).

---

## 3. Feature-gating concept (not enforced)

If/when enforcement is built, candidate gating boundaries would be:

| Capability | Pilot | Standard | Enterprise |
|---|---|---|---|
| C#/T-SQL/Python understanding + bridges | ✅ | ✅ | ✅ |
| Coverage / gap reporting | ✅ | ✅ | ✅ |
| Professional Base Knowledge Pack | ✅ | ✅ | ✅ |
| Windows auth + RBAC + append-only audit | ✅ | ✅ | ✅ |
| Backup / restore tooling | ✅ | ✅ | ✅ |
| Number of imported projects | contract-limited | per-server | unlimited |
| Cross-repository estate model | — | — | planned |
| SSO / external IdP | — | — | planned |
| Optional local AI (Ollama/Qdrant) | ✅ optional | ✅ optional | ✅ optional |

Optional AI is **never** a paywall lever in a way that breaks the local-first guarantee — the product must
remain fully functional in MSSQL-only mode at every tier.

---

## 4. Licensing-approach options

No model is committed. Candidate models, with trade-offs for a local-first, air-gapped product:

- **Per-server (per-instance) perpetual + annual maintenance.** Fits on-prem banking estates; simplest to
  reason about; no seat tracking. Enforcement would be an offline, signed license file scoped to a server.
- **Per-seat subscription.** Aligns price to usage but requires counting authenticated `DOMAIN\user`
  principals (the audit trail already knows them) and an offline reconciliation story — there is no phone-home.
- **Annual subscription per environment** (dev/test/prod). Predictable; pairs naturally with the per-server
  deploy model and the existing `database/*` create/seed scripts.

Whatever the model, enforcement must be **offline-capable** (signed file, no internet check) to preserve the
air-gapped guarantee, and must **degrade safely** (a lapsed license should warn, never silently corrupt data
or block read access to a customer's own knowledge).

---

## 5. Third-party & open-source posture

- **Runtime dependencies:** .NET 10 / ASP.NET Core, EF Core, MSSQL (customer-licensed), Bootstrap 5 +
  bootstrap-icons, `marked.js`. Optional: Ollama and Qdrant — both **optional** and gated behind config.
- **Optional models** verified with `qwen2.5-coder` and `deepseek-r1` are **not bundled or relicensed**; the
  customer obtains and runs them under their own terms. The product does not redistribute model weights
  (none are tracked in the repo).
- **Knowledge pack governance:** the Professional Base Knowledge Pack contains **original summaries only** —
  no verbatim ISO/IEC, IFRS, PMBOK, FATF, Basel, vendor, or regulatory text. Every protected source has
  `verbatimCopyAllowed=false`; research families are topic-level with **no fabricated citations**. See
  `docs/Professional-Base-Knowledge-Pack-v1.2-Sources.md`. This keeps the pack commercially distributable.
- **No vendor compatibility, certification, or equivalence claims** are made about any third party.

---

## 6. What must exist before commercial sale

A truthful checklist. Until these are real, the only honest commercial motion is a **paid pilot**:

1. **License enforcement** — offline signed entitlements, edition gating, graceful lapse behaviour, tests.
2. **Hardened packaging / installer** — today there are release scripts (`scripts/release/*`) and a documented
   IIS dry-run, but **no proven production installer**. Needs a real, idempotent, verifiable install.
3. **Support tiers** — defined response targets, escalation paths, and on-call beyond the current
   business-hours pilot support (`docs/Support-Runbook.md`).
4. **SSO / IdP** — for Enterprise; currently Windows auth only.
5. **Cross-repository estate model + scale testing** — for the Enterprise value proposition.
6. **Proven production deployment** — none demonstrated yet.

Track progress honestly at `/Readiness` and in `docs/Gap-Closure-Roadmap-To-100.md`. **Do not market an
edition or licensing term that the code cannot enforce or the deployment cannot prove.**
