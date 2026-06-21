# Near-100% GA Score Model

**Generated:** 2026-06-21
**Gate:** V3 — `NEAR_GA_READY_WITH_EXTERNAL_PROOF_MODEL`
**Machine-readable companion:** `benchmarks/results/near-ga-score-model.json`
**Gate result:** `benchmarks/results/production-readiness-v3.json`

## Why a separate model

The 22-area readiness scorecard (`docs/readiness-scorecard.json`, mean **63.4**) measures
breadth. This model measures the **honest distance to GA** by splitting every dimension into:

- **Internal completeness** — how complete/proven the dimension is using only evidence we can
  produce locally (build, tests, IIS pilot, benchmarks, knowledge packs, scripts).
- **GA-score-now** — its honest contribution to *commercial* GA **today**, with every
  externally-gated dimension held down because **no external proof has been faked**.
- **GA-when-proofs-supplied** — the projected contribution once the named external proof is
  *genuinely* delivered. This is a projection, **not** a claim that it exists.

## Aggregate result

| Measure | Score | Meaning |
|---|---|---|
| **Internal completeness** | **84.8** | What we have actually proven on this host |
| **GA score now** | **65.4** | Honest commercial-GA position today (no faked external proof) |
| **GA when proofs supplied** | **94.3** | Projection after real external proofs are delivered |

This aligns with the independent human-interaction GA impact model
(`docs/reports/HUMAN_INTERACTION_GA_IMPACT_MODEL.md`: ~62% now → ~97% after commercial signoff)
and is intentionally **more conservative** on the projected ceiling.

## Per-dimension scores

| Dimension | Wt | Internal | GA now | GA w/proof | External proof required |
|---|---:|---:|---:|---:|---|
| Build & compile | 8 | 100 | 100 | 100 | — |
| Automated tests | 8 | 100 | 95 | 100 | — |
| Security hardening | 10 | 85 | 55 | 95 | independent pen-test / security review (EXTERNAL) |
| Auth & identity | 8 | 80 | 50 | 95 | real Entra/OIDC tenant (EXTERNAL) |
| TLS / transport | 6 | 75 | 45 | 95 | CA-issued cert + real domain (OPERATOR) |
| Deployment / IIS | 9 | 90 | 70 | 95 | Windows Server production host (OPERATOR) |
| Performance / load | 7 | 80 | 60 | 90 | load test on prod-spec hardware (OPERATOR) |
| Reliability / backup / rollback | 8 | 80 | 60 | 92 | offsite backup + verified prod restore (OPERATOR) |
| Observability / monitoring | 7 | 80 | 55 | 90 | external SIEM/alerting + live alert (OPERATOR) |
| Knowledge & intelligence | 10 | 90 | 85 | 95 | — |
| Integration expectations | 6 | 80 | 50 | 88 | live integration vs real endpoints (OPERATOR/EXTERNAL) |
| Customer pilot / acceptance | 6 | 70 | 30 | 95 | signed pilot on sanitized data (CUSTOMER) |
| Docs & operability | 7 | 90 | 85 | 95 | — |
| Licensing / commercial | 4 | 70 | 45 | 90 | business decision to enforce (OPERATOR) |

10 of 14 dimensions are **external-gated**; 4 are internal-only and already near-complete.

## Classification logic (gate V3)

`NEAR_GA_READY_WITH_EXTERNAL_PROOF_MODEL` is emitted only when **all** hold:

1. Internal gates green — 240/240 tests, build, known-issue diagnostics clean.
2. Internal completeness ≥ 80 (actual: **84.8**).
3. **Every** external proof is MODELLED + OWNED + VALIDATABLE via
   `scripts/production/verify-external-proof-emulation.ps1` — and **none is faked as real**.
4. The model makes **no** `COMMERCIAL_GA_READY` and **no** 100% claim.

The gate **refuses** to emit `COMMERCIAL_GA_READY`. That requires real external evidence —
independent security review, CA TLS, a real Entra tenant, and a signed customer pilot — none of
which can be produced from this workstation. They are owned by the operator, an external firm,
and the customer, and each has a recorded owner + validation command in
`operator-emulation/external-proof-model.json`.

## How to reproduce

```powershell
pwsh -File scripts/production/verify-external-proof-emulation.ps1   # external proofs modelled, none faked
pwsh -File scripts/diagnostics/run-known-issue-diagnostics.ps1      # no live anti-pattern
pwsh -File scripts/production/verify-production-readiness-v3.ps1     # emits the classification
```
