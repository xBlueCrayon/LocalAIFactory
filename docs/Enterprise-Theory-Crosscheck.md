# Enterprise Theory Cross-Check

A narrative companion to `benchmarks/theory-crosscheck-matrix.json`. It asks, for each established
enterprise software-engineering and governance concept: how do real systems represent it, what do
benchmark systems show, how does **LocalAIFactory** model it today, where are the honest gaps, and
what concrete proof would let us claim full support?

This is a self-assessment. Confidence values are author estimates, not certifications. No external
audit or certification is asserted.

---

## Why this exists

LocalAIFactory's purpose is to import, understand, and evolve real banking/middleware projects while
accumulating approved knowledge. To do that credibly it has to *recognise* the theory those systems
embody. This cross-check keeps the project honest about which enterprise concepts it genuinely
implements, which it only has awareness-level knowledge of, and which it merely integrates with.

---

## Architecture and modeling

**DDD, Clean Architecture, Hexagonal.** LocalAIFactory's strongest alignment is structural. The
eight-project graph with no dependency cycles, a dependency-free `Core`, and a `Web` composition
root is a faithful Clean Architecture skeleton (confidence ~0.8). DDD and Ports & Adapters are
present in spirit — context-like boundaries (knowledge, retrieval, inference) and optional adapters
(Qdrant/Ollama) — but the bounded contexts are technical layers rather than a documented ubiquitous
language, and the port surface is informal. The proof to advance is explicit: name the bounded
contexts and aggregate roots, publish a port/adapter catalog, and add a build-time dependency test.

**CQRS / Event Sourcing.** Partial. The documented "project list queries to lightweight read-model
records" rule is a read/write separation for performance, not a command/query bus or an event store.
This is honestly a convention, not CQRS (confidence ~0.6).

## Process and workflow

**BPMN / workflow.** LocalAIFactory implements a real approval lifecycle with explicit gates and an
audit trail for knowledge and agent proposals, which is a domain-specific workflow engine in code —
but not a general, modelable BPMN engine (confidence ~0.68). Expressing the lifecycle as an explicit,
diagrammed state machine reused by a second workflow would advance it.

## Enterprise domain (ERP / CRM / ITSM)

These are the weakest by design: LocalAIFactory is **not** an ERP or CRM. It carries
awareness-level accounting/finance/banking knowledge and integration patterns (Odoo, ERPNext,
WooCommerce, Magento, etc.), so the honest relationship is *advisory and integration*, not
implementation (ERP ~0.55, CRM ~0.5). ITSM is stronger because the support-issue learning registry
plus the backup/restore and rollback runbooks capture incident patterns and change-with-approval —
though without SLA timers or a CMDB (~0.62).

## Operations and governance

**SRE, DevOps, Observability, Resilience.** This is a relative strength. Graceful degradation is a
hard rule (MSSQL-only mode; optional Qdrant/Ollama; cached health snapshots), and
`RequestTimingMiddleware`'s per-request start/complete timing is a genuine, documented
hang-detection technique. The gaps are formalisation: no tracked SLOs/error budgets, no distributed
tracing or metrics backend, single-node by design, and no high-concurrency load evidence. Proofs
to advance are concrete — define SLOs/SLIs with breach detection, run a load benchmark, and add
explicit timeout/retry/circuit-breaker policies.

## Security and AI governance

**OWASP ASVS, NIST SSDF, NIST AI RMF, Secure SDLC, RBAC/ABAC.** LocalAIFactory has real, evidenced
controls: Windows/Negotiate auth proven at IIS, least-privilege SQL (`is_sysadmin=0`), append-only
audit, no secrets in the repo (Data Protection with git-ignored `keys/`), and a 0-HIGH security
audit. These map to ASVS and SSDF practices (see `docs/security/`). The honest gaps are equally
clear: no external penetration test, self-signed TLS in the pilot, app-level authorization is
dev-grade behind IIS, no formal ABAC engine, and no quantitative AI-evaluation/model-risk metrics.
Confidence sits in the 0.63–0.66 band — credible controls, not a certified posture.

## Data and integration

**Data Governance, Audit/Provenance, DB Migration, API Design.** The approval lifecycle, source
attribution on knowledge items, provenance retention, and EF Core additive-migration discipline are
strong. Audit is append-only by design but not yet hash-chained/tamper-evident. Migration is solid
for additive changes; reversibility for destructive changes relies on backups. API design is
internal-first with no versioned public contract. The advancing proofs are specific: a classification
taxonomy with demonstrated lineage, hash-chained audit entries, a reversible-migration drill, and an
OpenAPI contract for any external surface.

---

## Summary posture

LocalAIFactory is **architecturally faithful** (Clean Architecture, resilience, audit), **procedurally
governed** (approval lifecycle, provenance, secure-SDLC contract), and **honest about scope**: it is a
local-first knowledge/engineering platform, not an ERP/CRM, and its security posture is a credible
self-assessment rather than a certified one. Each row in the matrix carries a single, concrete
`proofToAdvance` so the gaps are actionable rather than rhetorical.
