# ERP Gold — Hardened Production Review

**Sprint:** ERP-GOLD HARDENING · **Stamp:** 2026-06-21

Honest self-assessment of the **local** product. Not externally audited; not ERPNext parity.

## Scores (`benchmarks/results/`)

| Score file | Value | Meaning |
|------------|-------|---------|
| `erp-gold-production-grade-score.json` | mean **78** | Core `ERP_LOCAL_PRODUCTION_READY` criteria met + residual depth / external gates |
| `erp-gold-erpnext-parity-score.json` | **~39%** | NOT ERPNext-grade (kept honest and low, not inflated) |
| `erp-gold-hardening-score.json` | 67 → **78** | 138 tests / mean 67 → 210 tests / mean 78; 3 local blockers closed |

## Category breakdown (production-grade score)

| Category | Score |
|----------|-------|
| Auth & security | 85 |
| Migrations & database | 84 |
| Accounting | 85 |
| Stock | 80 |
| Selling / buying | 80 |
| Manufacturing skeleton | 55 |
| HR / POS / e-commerce skeleton | 52 |
| Workflow / RBAC / audit | 85 |
| UI completeness | 78 |
| Reports / import-export | 70 |
| Tests | 88 |
| Playwright | 85 |
| Scenarios | 72 |
| Deployment | 80 |
| Generator reproduction | 92 |
| **Mean** | **78** |

## Classification

- **BEFORE:** high `ERP_PILOT_READY` (most local-production criteria).
- **AFTER:** `ERP_LOCAL_PRODUCTION_READY` (core local criteria met), with documented residual depth and external gates.

**Residual depth (local):** manufacturing / HR / POS skeletons; posted-document immutability by design; EF migration not applied on a live SQL Express here.

**External gates remaining:** SSO/OIDC, CA-signed TLS, independent external security review, signed customer acceptance.

## Honest limitations

- **No 100% claim. No ERPNext parity claim. No external certification claim.**
- ERPNext parity is **~39%** and intentionally not the product goal.
- Scores are an honest internal self-assessment, not an external audit.
