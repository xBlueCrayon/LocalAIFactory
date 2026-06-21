# ERP Gold — Hardening Ollama Review (Phase 9)

**Sprint:** ERP-GOLD HARDENING · **Stamp:** 2026-06-21
**Evidence:** `benchmarks/results/erp-gold-hardening-ollama-review.json`

Two local models reviewed the hardened product. **Authority: local models REVIEW only** — deterministic templates write all committed code; every code change was human-authored and tested.

- `qwen2.5-coder:14b` — code/security reviewer
- `deepseek-r1:14b` — architect/planner

## Findings and dispositions

| By | Finding | Disposition | Action |
|----|---------|-------------|--------|
| qwen2.5-coder | Stringent password complexity to resist brute force | ALREADY DONE | `PasswordPolicy` (min length + 4 char classes) + failed-login lockout, tested |
| qwen2.5-coder | Session expiration/renewal to mitigate hijacking | ALREADY DONE | 30-min sliding cookie + HttpOnly + SameSite + SecurePolicy configured |
| qwen2.5-coder | Data encryption at rest | ACCEPTED → BACKLOG | Documented as a deployment requirement (SQL Server TDE / column encryption); out of local app scope |
| deepseek-r1 | Comprehensive CRM | PARTIAL | `CrmService` (Lead/Opportunity) skeleton exists; full CRM depth is a documented parity gap |
| deepseek-r1 | Mobile applications for field access | ACCEPTED → BACKLOG | Out of scope for a local server ERP this sprint; future backlog |
| deepseek-r1 | Advanced reporting & analytics / dashboards | ACCEPTED → BACKLOG | Query-based reports exist; BI/dashboard/print-designer is a documented parity gap |

## Outcome

No accepted finding contradicts the honest classification. **Two new backlog items** recorded: encryption-at-rest deployment guidance, and BI/analytics dashboards. The remaining findings were already implemented or already documented as parity gaps. Accepted backlog items this sprint: **encryption-at-rest (deployment/TDE), BI/analytics dashboards, mobile apps.**

## Honest limitations

- Local-model review is **advisory**, not an independent external security audit.
- Models were run locally; their findings inform the backlog but did not author code.
- Accepted items are recorded as backlog/parity gaps, not delivered this sprint.
