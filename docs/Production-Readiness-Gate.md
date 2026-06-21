# Production-Readiness Gate

A strict, evidence-based gate that classifies every production requirement and computes a single final
classification. It **fails closed**: docs may not say "production ready" unless this gate returns
`FULL_PRODUCTION_READY`.

Run: `pwsh scripts\production\verify-production-readiness.ps1` → `benchmarks/results/production-readiness-gate.json`.

## Classifications (per area)

| Class | Meaning |
|---|---|
| `PASS` | proven by a live check or committed evidence |
| `PARTIAL` | partially met; a real gap remains (e.g. self-signed TLS, dev-auth behind IIS, simulation-only load) |
| `BLOCKED_OPERATOR_REQUIRED` | needs the operator/host (Windows Server, CA cert, license decision) |
| `BLOCKED_EXTERNAL_REQUIRED` | needs a third party (pen-test, Entra tenant) |
| `BLOCKED_CUSTOMER_REQUIRED` | needs the customer (signed pilot on sanitized data) |
| `FAIL` | a technical gate is broken |
| `NOT_APPLICABLE` | — |

## The 30 areas

Build · Tests · Release package · Knowledge packs · LocalDB · SQL Express · IIS · HTTPS · Windows-auth ·
Production env config · SQL least privilege · Backup/restore · Rollback · Support bundle · Security audit ·
OWASP/ASVS · NIST/SSDF · Load test · 100-system benchmark · Docs/API cross-check · Knowledge governance ·
Local-LLM governance · Issue/fix pack · License enforcement · External security review · Entra/OIDC tenant ·
Customer pilot · Production host · Monitoring/alerting · Incident response.

## Final classification rules

```
FULL_PRODUCTION_READY  only if every hard technical gate PASSes AND no BLOCKED_* / PARTIAL remains.
PILOT_READY            if local/IIS/SQL/HTTPS/auth/backup/rollback/security basics pass and only
                       external/operator/customer gates (and known PARTIALs) remain.
NOT_READY              if any critical technical gate FAILs.
```

The live checks (IIS site state, HTTPS 200, Windows-auth 401 challenge, SQL `is_sysadmin=0`, security-audit
HIGH count, knowledge-pack validation) are run **each time** — the gate reflects the real current state, not a
stale assertion.
