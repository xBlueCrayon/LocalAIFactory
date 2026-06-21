# Production Issue/Fix — Finalization

**Date:** 2026-06-21

## Status

- `production-issue-fixes-v1` knowledge pack: **42 items** (preserved + validated; 42 distinct GUID uids).
- `benchmarks/support-issue-learning-registry.json`: **42 issues** (several **firsthand** from this project).
- `docs/Production-Issue-Fix-Knowledge-Base.md`: readable index + reusable IF/THEN rules.

The 42 patterns span the required industries (banking, ERP, CRM, eCommerce, CMS, ITSM, BI, identity/auth,
DevOps, SFTP/SMTP/API, document/OCR, high-volume web) and the deployment stack (IIS 500.19/500.30/502.5,
ANCM/Hosting-Bundle, SQL Express, EF migration, HTTPS/sslcert, app-pool login, forwarded-headers, clone
timeouts, Ollama/Qdrant, OCR/PDF). Each carries: symptom, affected systems, diagnostic command, fix pattern,
prevention rule, detection rule, and confidence.

## 100-pattern target — honest

The prompt's stretch target is **≥100** patterns. The current **42** are original, concise, source-governed,
and validated (0 UID collisions). Expanding to 100 with the **same quality bar** (original, non-copied,
source-attributed, deduplicated) is a follow-up content task; padding to 100 with low-quality or copied items
would violate the no-copy / governance rules, so it was **not** done. The 42 are the honest, validated floor;
the integration-expectation library (`benchmarks/integration-expectations/`, 20 systems) extends issue/fix
coverage into per-system integration contracts.

## Companion

- 8 executable diagnostics in `scripts/diagnostics/` encode the detect/fix rule for the highest-value patterns.
- The integration-expectation library adds per-system **common integration failure + diagnostic + prevention**
  for 20 named systems (expectation-only; no live integration).
