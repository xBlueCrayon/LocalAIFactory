# LocalAIFactory — Proof-of-Capability Evidence Pack

**Audience:** enterprise technology reviewers (CTO, enterprise architect, head of IT, auditor).
**Purpose:** hard, reproducible evidence that LocalAIFactory is a real, clean, tested, demonstrable
enterprise AI proof-of-capability — and an honest statement of what it is **not** yet.

> Reproduce everything here with `scripts/poc/verify-poc.ps1` (build + test + benchmark + artifact checks)
> and `scripts/poc/ui-smoke-test.ps1` (live HTTP UI checks). Nothing in this pack is asserted without a
> reproducible command, a test, a benchmark, a database query, or an HTTP response behind it.

## 0. Live POC run — R2-ACC-POC-COMPLETE (2026-06-21, commit `1ccd494`)

Captured live on this machine against the configured `(localdb)\MSSQLLocalDB / LocalAIFactory` database.
Full evidence in the linked docs.

| Proof | Result | Evidence doc |
|---|---|---|
| Build (Release) | **0 errors** | — |
| Tests | **235 / 235 pass** | — |
| Benchmark (all 10 repos) | **PASS** — 47/47 POV, 9 Gold + 1 Bronze; `EnterpriseGiantPatterns` Gold 14/14 | — |
| Environment | .NET 10.0.301, LocalDB v17, Ollama 0.30.10 (API 200), Qdrant optional/off | [`Local-POC-Environment-Verification.md`](Local-POC-Environment-Verification.md) |
| LocalDB knowledge base | **4 packs / 438 baseline items**, 0 duplicate Uids, 1,488 total (1,050 imported), pack installs audited | [`LocalDB-POC-Evidence.md`](LocalDB-POC-Evidence.md) |
| HTTP (app on LocalDB) | **22 routes → 200, 0 HTTP 500s**, 16–89 ms; searches OCR 57 / Mauritius 52 / insurance 20 / VB6 1 … | [`HTTP-POC-Evidence.md`](HTTP-POC-Evidence.md) |
| Local AI (Ollama) | Reachable, 2 models; **optional** — every proof passed with it off the critical path | [`Ollama-Local-AI-POC-Evidence.md`](Ollama-Local-AI-POC-Evidence.md) |
| Enterprise reasoning benchmark | **PASS** — mean 94.5/100 (14 structural @100 + 17 advisory @90) | [`reports/ENTERPRISE_REASONING_BENCHMARK_RESULTS.md`](reports/ENTERPRISE_REASONING_BENCHMARK_RESULTS.md) |
| Repo cleanliness | 6.3 MB tracked / 788 files, .git 3.3 MB, **0** forbidden, **0** > 5 MB | [`Repository-Cleanliness-Audit.md`](Repository-Cleanliness-Audit.md) |
| **Deployment (Mode C)** | **EXECUTED** — published app → **SQL Server Express 2022** (fresh DB, 14 migrations + 4 packs/438 items), 13 routes 200, 0 HTTP 500s, healthcheck + rollback PASS. Not IIS, not production. | [`reports/DEPLOYMENT_PUBLISHED_APP_PROOF.md`](reports/DEPLOYMENT_PUBLISHED_APP_PROOF.md) |
| **Deployment (Mode A)** | **EXECUTED** — **real IIS** + ANCM (Hosting Bundle 10.0.9) → SQL Express `LocalAIFactory_IISProof` with **least-privilege** app-pool login; served **through IIS** (`Server: Microsoft-IIS/10.0`), 7 routes 200, 0 HTTP 500s, Windows-auth 401 challenge, rollback proven. **Pilot, not production.** | [`reports/MODE_A_IIS_HTTP_AUTH_HEALTHCHECK.md`](reports/MODE_A_IIS_HTTP_AUTH_HEALTHCHECK.md) |
| **HTTPS + Windows auth** | **EXECUTED** — HTTPS `:8443` (self-signed); all pages 200 over TLS; **Windows/Negotiate round-trip** (401 → **200 with Windows creds over HTTPS**); posture healthcheck PASS, 0 HTTP 500s. Self-signed pilot TLS; app dev-auth behind IIS. | [`reports/IIS_PRODUCTION_POSTURE_HEALTHCHECK.md`](reports/IIS_PRODUCTION_POSTURE_HEALTHCHECK.md) |
| **50-project benchmark** | **51 real public repos attempted** — 22 Passed/7 Partial/5 ValidationOnly/5 Unsupported/12 CloneFailed-or-TimedOut; **123,849 C# + 241,730 Python symbols + 1,733 SQL objects** over 7.56M LOC; avg 60.8. | [`reports/PUBLIC_50_PROJECT_BENCHMARK_RESULTS.md`](reports/PUBLIC_50_PROJECT_BENCHMARK_RESULTS.md) |

> Note: the knowledge base now holds **4 packs / 438 items** (the 390-item professional base + 3 domain
> packs of 16), not the 390 quoted in the older sections below — those sections describe the original
> single-pack baseline and remain accurate for that pack.

## 1. Executive summary

LocalAIFactory is a **local-first, MSSQL-authoritative** platform that deterministically understands
C#/.NET and T-SQL repositories (symbols, references, dependency graph, impact analysis, honest coverage/gap
reporting), enforces a **server-side security boundary** (Windows auth, RBAC, project access, append-only
audit), **ingests imperfect repositories without crashing**, and ships a **governed professional knowledge
base** (390 curated items, 22 categories, a validated source registry). It is proven by **a passing test
suite, a deterministic benchmark with pinned public repositories and golden snapshots, and live LocalDB +
HTTP verification.** It is a strong **technical POC / early pilot** — not a finished enterprise product, and
it does not claim parity, compliance, or certification with any vendor or standard.

## 2. Current implemented capabilities (real, in the codebase)

- **Deterministic repository understanding** (C#/Roslyn, T-SQL/ScriptDom): symbols, references, `CodeEdge`
  graph, `vCodeGraph` view, impact analysis, structural retrieval — MSSQL-only, no model/vectors required.
- **Honest coverage & gap reporting** (R2-P0A): every file's extraction outcome is recorded; no silent zeros.
- **Security boundary** (R2-P0B): Negotiate (Windows) auth, `UserRole` RBAC, per-project access, deny-by-default,
  append-only `AuditEvent`, server-side enforcement (`SecuredController`), IDOR guard.
- **Ingestion robustness** (R2-P0C): content-binary detection, honest encoding decode, bulletproof per-file
  loop and enumeration — a bad repository never crashes the platform.
- **Professional Base Knowledge Pack** (R2-ACC-B1/B3): 390 curated, versioned, searchable items across 22
  categories; portable pack with a **source registry** (21 sources) validated at install; propose-never-overwrite
  permanence; provenance with pack origin.
- **Validation harness**: 4 pinned public repos, golden snapshots, Bronze/Silver/Gold scoring, CI exit codes.
- **Web UI**: dashboard, projects, graph explorer, coverage, Base Knowledge, **Readiness**, admin (users/audit),
  import wizard — fast (<1s), MSSQL-only, never blocks on external services.
- **Enterprise reasoning assets**: 14 synthetic enterprise scenarios, 21 solution playbooks, an evaluation
  rubric, a readiness scorecard, and an honest high-end comparison matrix.

## 3. Evidence table

| Claim | Evidence type | Where to verify |
|---|---|---|
| Builds clean | build | `dotnet build LocalAIFactory.sln -c Release` → 0 errors |
| Broad automated tests pass | test | `dotnet test` → all green (see §4) |
| Deterministic capability, no regressions | benchmark | `tools/LocalAIFactory.Benchmark --inmemory` → PASS (see §5) |
| Security enforced server-side | test + live | `SecurityTests`, AccessDenied 403, IDOR regression |
| Bad repos don't crash ingestion | test + live | `RobustnessTests`; R2-P0C live bad-repo import Completed |
| Knowledge installed & governed | test + DB + HTTP | `KnowledgePackTests`; 390 items; `/BaseKnowledge` search |
| Source attribution validated | test + DB | unregistered-source rejection test; `src:`/`jur:` tags |
| UI works (not just compiles) | HTTP smoke | `scripts/poc/ui-smoke-test.ps1` |
| Repo is clean | audit | `docs/Repository-Cleanliness-Audit.md` |

## 4. Tests passed

The xUnit suite (`tests/LocalAIFactory.Tests`) covers identity/convergence, code symbols & references, the
structural graph, retrieval, consolidation, permanence guard, quality, import coverage, **security**
(auth/RBAC/project-access/IDOR/audit/dev-auth-guard), **ingestion robustness**, **knowledge pack**
(validation, idempotency, no-silent-overwrite, source governance), and the **POC readiness artifacts**.
Run `dotnet test -c Release` to see the live count; every test protects a real behaviour (no placeholder tests).

## 5. Benchmark results

`tools/LocalAIFactory.Benchmark` over 4 pinned repos:

| Repo | Tier | Proof-of-vision |
|---|---|---|
| WideWorldImporters (T-SQL) | **Gold** | 3/3 |
| CleanArchitecture (DDD) | **Bronze** | 3/3 |
| eShopOnWeb (clean monolith) | **Gold** | 2/2 |
| eShopOnAbp (SaaS microservices) | **Gold** | 4/4 |

`Result: PASS (povFailures=0, regressions=0, coverageFailures=0)`. Golden snapshots in `benchmarks/golden/`
detect regressions; a proof-of-vision regression or a missing coverage report fails the run.

## 6. Security / auth / audit evidence

Windows (Negotiate) auth in production; dev-only auth guarded out of non-Development (test). RBAC
(Viewer/Analyst/Admin), per-project `ProjectAccess`, deny-by-default for new users. Server-side enforcement in
`SecuredController` returns **403 AccessDenied** and writes an `AuditEvent`. IDOR fixed and pinned by a
regression test (`Symbol_detail_does_not_leak_across_projects`). Pack install is Admin-only and audited
(`AuditEventType.KnowledgePackInstalled`). Append-only audit answers who/what/when/which-project/denied.

## 7. Ingestion robustness evidence

`RobustText` content-binary detection (NUL sniff), BOM/UTF-8/Latin-1 honest decode (records non-UTF-8
fallback), bulletproof per-file loop (one bad file → recorded skip, never an aborted job), tolerant
enumeration (`EnumerationOptions` + safe enumerator). Proven live: a hostile 9-entry repo (binary-as-`.cs`,
oversized, non-UTF-8, malformed SQL, deep nesting) imported to **Completed** with correct skip buckets and no
crash; `RobustnessTests` pin the behaviour.

## 8. Knowledge base evidence

390 baseline `KnowledgeItem`s with `KnowledgePackId` (distinct from imported project knowledge), all
`Tier=Curated`, versioned, with pack-origin provenance. Categories include software engineering, CRUD/MSSQL,
databases, security, governance, finance/accounting, Mauritius banking, payments, leasing, insurance, cheque
OCR/forgery, Python OCR/CV, PDF intelligence, financial-market model risk, source-attributed research, and
enterprise playbooks. Live `/BaseKnowledge` search returns matches for OCR, Mauritius banking, IFRS, direct
debit, VB6, signature forgery, Qdrant, PDF summarizer, and more.

## 9. Source registry evidence

`knowledge-packs/professional-base-v1/source-registry.json` registers 21 sources (BoM, FSC, FRC-MU, DPO-MU,
FIU-MU, Companies Act, IFRS, FATF, Basel, ISO, PMI, NIST, OWASP, .NET, Python, Qdrant + 5 research families).
Every source carries governance metadata; `verbatimCopyAllowed=false` everywhere; research families are
explicitly *"verification required"* with **no fabricated DOIs/titles**. The installer **rejects any item that
references an unregistered source** (test-pinned), and stores attributions as `src:`/`jur:` tags.

## 10. Deployment readiness evidence

MSSQL-only mode works (no GPU/internet/Ollama/Qdrant required); app auto-migrates + seeds + installs the pack
on startup; Data Protection keys persist; `deploy/docs/hardware-sizing-guide.md` states honest CPU/GPU
profiles and the reference-workstation limits. **Gaps:** no automated installer/package, no backup/restore
tooling, no supportability dashboard (see the readiness scorecard).

## 11. UI readiness evidence

Core pages return 200 in well under a second and never block on external services (health is read from a
cached snapshot). `scripts/poc/ui-smoke-test.ps1` exercises home, Base Knowledge (+ searches), Readiness,
graph/coverage where data exists, and asserts no 500s. Playwright is not yet added (path documented in
`docs/Readiness-Maturity-Model.md`); HTTP smoke is the current deterministic check.

## 12. Repository cleanliness evidence

347 tracked files; `.git` ≈ 2.9 MB. No `bin/`, `obj/`, benchmark cache, database files, model weights, or
keys are tracked. Largest tracked files are EF migration designer files (~100 KB). See
`docs/Repository-Cleanliness-Audit.md`.

## 13. Current limitations

C#/T-SQL only (Python/VB/Razor/Oracle PL/SQL are reported gaps, not failures); syntactic (no semantic model);
no cross-repository estate model yet; OCR/PDF/forecasting are **knowledge + design**, not shipped engines; no
SSO/IdP, backup/restore, supportability dashboard, load testing, or packaging/licensing. Domain content is
advisory/awareness — **not legal, regulatory, tax, audit, or financial advice**, and not a compliance claim.

## 14. What is real

The deterministic engine, security boundary, ingestion robustness, coverage/gap honesty, governed knowledge
base + source registry, benchmark harness, and the web UI — all tested and demonstrable today.

## 15. What is planned

C#↔SQL bridge; Python and legacy-language extractors; cross-repository estate model; OCR/cheque and PDF
implementation; deployment hardening (installer, backup/restore, supportability); benchmark tiering; an
optional autonomous fix/test loop. See `docs/Readiness-Maturity-Model.md` for the path-to-100 per area.

## 16. What is not supported yet

Automatic VB6/Oracle migration; cheque-fraud *proof*; market *prediction*/financial advice; medical-grade
imaging; SAP/Sage/Oracle/Cisco equivalence or device management; production deployment; multi-tenant scale.

## 17. CEO/CTO decision summary

LocalAIFactory is a **credible, defensible enterprise POC**: it demonstrably understands real C#/SQL codebases,
enforces enterprise-grade access control and audit, survives messy inputs, and reasons about enterprise
solutions with a governed, source-attributed knowledge base — all locally, on MSSQL, with reproducible
evidence. It is **honest about its limits** and does not pretend to be a finished product or a vendor
replacement. The right decision is to fund a **controlled pilot** focused on the next capability proof.

## 18. Recommended next investment

**C# ↔ SQL bridge** — the single highest-leverage, lowest-risk capability slice: it reuses the proven C# and
T-SQL extractors and the existing graph to deliver the most compelling banking-middleware demonstration
("change this stored procedure → exact C# blast radius, and vice-versa"), directly activating the
payments/leasing/Mauritius knowledge already shipped. The Python extractor should follow (it unlocks the
OCR/PDF and multi-language benchmark work).

## Addendum — R2-ACC-CAPABILITY-MAX (capability sprint, delivered)

The recommended bridge and several follow-ons were then **built and proven** (all tests + benchmark green):

- **C#↔SQL bridge** (`e96515d`) — deterministic SQL-in-C# detection resolved to schema symbols as `AccessesSql`
  edges; both-direction blast radius; confidence + evidence; 5 tests; **benchmark fixture Gold 4/4**.
- **Benchmark tiering** (`d1ccd25`) — Smoke/Standard/Extended + `--suite`; governance metadata; the C#↔SQL
  fixture proves both directions.
- **Python extractor** (`4cc919d`) — pure-C#, no runtime; classes/async/FastAPI routes + **Python↔SQL bridge**;
  Python now a *supported* coverage language; 6 tests; **Python fixture Gold 4/4**.
- **PDF + cheque-OCR risk prototypes, deployment hardening, autonomous-workspace skeleton** (`2ab6966`) —
  PDF analyzer (hash/classify/provenance) + extractive summarizer; cheque risk-triage (detection≠verification,
  human-review-required, never a fraud verdict); docker-compose/scripts (validated, no secrets); command
  allow/deny policy + dry-run planner (executes nothing); 31 tests.

Test count rose from 157 → **199**; benchmark remains **PASS** over 6 pinned items. Readiness rose ≈45% → ≈49%
(see `Enterprise-Readiness-Scorecard.md` and `Gap-Closure-Roadmap-To-100.md`) — only where shipped evidence
improved; nothing reached 100. Honest boundary: OCR/PDF/forecasting remain prototypes/design (no real CV/parser
engine), legacy VB6/Oracle parsing and a cross-repository estate model are not yet built, and no production
deployment or autonomous execution loop exists.
