# Final Release Overview — LocalAIFactory

What this release **is**, what is **proven**, what is **roadmap**, and the honest score. This is a
release-facing summary for a buyer, operator, or reviewer deciding whether to run a pilot. It
deliberately under-claims: every gap is named with the proof that would close it.

> Authoritative source of truth: `MASTER_VISION.md`. Conservative readiness scores:
> [`readiness-scorecard.json`](readiness-scorecard.json), rendered live at `/Readiness`.

---

## 1. What this release is

LocalAIFactory is a private, local-first, **MSSQL-authoritative** AI software-engineering platform
for a banking middleware estate (.NET 10 / ASP.NET Core MVC, EF Core, MSSQL). It imports legacy
projects (C#/.NET, T-SQL, Python), understands them **structurally**, and accumulates a **governed,
approval-gated knowledge base** that is injected first into every prompt context.

Its defining property is the **persistent curated memory with an approval lifecycle**. The durable
asset is the curated, governed knowledge plus the platform that grows it — not any particular model.

- **MSSQL is the source of truth.** The system of record works with only SQL Server present.
- **Ollama, Qdrant, and a GPU are optional** and degrade gracefully when absent.
- This is **not** a general chatbot and makes **no** vendor-certification, regulatory, financial,
  fraud-proof, or commercial-GA claim.

---

## 2. What is proven (this release)

These are the implemented, tested, demonstrated behaviors. Verified facts cited from the build host:

- **Deterministic structural understanding** — C# (Roslyn), T-SQL (ScriptDom), Python (pure-C#
  parser) symbol extraction into a `CodeSymbol`/`CodeEdge` graph with reference resolution and
  bidirectional impact analysis.
- **C#↔SQL bridge** — links code methods to the SQL objects they access (`AccessesSql` edges, with
  confidence + evidence); answers "what code touches `dbo.X`" and "what is the blast radius".
- **Governed knowledge base** — permanence tiers (Curated), provenance, versioning, a source
  registry, and a **propose-never-overwrite** guard (`IPermanenceGuard`). MSSQL is the runtime source
  of truth; JSON packs are the seed/import format.
- **Knowledge packs (4)** — `professional-base-v1` (390 items) plus three domain packs
  (financial-institution-operations, kyc-aml-transaction-approval, market-intelligence-forecasting;
  16 items each = 48) for a total of **438 items**. The app **seeds all packs at startup**,
  idempotently (`KnowledgePacks:InstallAllAtStartup`, default `true`).
- **General / project / chat-imported knowledge** — general packs (`ProjectId=null`, Curated),
  project-scoped knowledge, and a deterministic chat-import extractor that produces **proposals only**.
- **Project import** — ZIP pipeline with profiling, code extraction, and per-import coverage / gap
  reporting (no silent blind spots).
- **Security & audit** — Windows-auth RBAC enforced server-side, deny-by-default, append-only audit,
  IDOR guard, secrets encrypted at rest via Data Protection.
- **Supportability** — read-only `/Support` dashboard; never blocks on an external service.
- **Safe local fix loop** — applies a patch to an isolated workspace, runs allowlisted checks,
  rolls back on failure, never commits/pushes (default dry-run).

### Validation table (verified on the build host)

| Gate | Result |
|---|---|
| `dotnet build -c Release` | **0 errors** |
| `dotnet test` | **235 / 235 pass** |
| Benchmark — ERP/CRM fixture | **Gold 6/6** |
| Benchmark — core-banking fixture | **Gold 6/6** |
| Benchmark — KYC/AML → transaction-approval fixture | **Gold 7/7** |
| Benchmark standard suite | **PASS** |
| UI smoke (`scripts/poc/ui-smoke-test.ps1`) | **PASS** (11 pages, incl. `/Support`) |
| `scripts/poc/verify-poc.ps1` | **PASS** |
| `dotnet publish` | **151 files / ~45 MB** |
| Knowledge-base verify (`database/verify-knowledge-base.ps1`) | **VERIFIED**, all curated, no duplicate Uids |

### Tooling present on the build host

.NET 10; `sqlcmd`; gh CLI 2.95.0; Node v24.17.0; Ollama with `qwen2.5-coder:14b` and
`deepseek-r1:14b`; NVIDIA RTX 5070 Ti (16 GB VRAM). **Docker is NOT installed** on this host.

---

## 3. What is roadmap (not yet proven)

Each item has a concrete proof-to-close in [`Known-Limitations.md`](Known-Limitations.md) and a
per-area `proofRequiredFor100` in the scorecard:

- **Executed production deployment** — IIS scripts are dry-run runbooks; no representative production
  host was deployed in this work. SQL Express and Docker were not exercised on the build host.
- **Docker** — `deploy/Dockerfile` + compose files exist, but Docker is not installed here; the
  container path is documented, not verified. See [`Docker-Deployment-Guide.md`](Docker-Deployment-Guide.md).
- **OCR / CV document intelligence** — deterministic PDF/cheque-triage prototypes only; **no trained
  CV model**; no fraud/signature certainty. See [`OCR-CNN-Document-Intelligence-Status.md`](OCR-CNN-Document-Intelligence-Status.md).
- **Autonomous engineering at scale** — the controlled fix loop is safe by construction but proven on
  a synthetic workspace only, not a real defect in a real repo. See [`Autonomous-Engineering-Status.md`](Autonomous-Engineering-Status.md).
- **Enterprise SSO / IdP** — Windows/Negotiate only; no SAML/OIDC/SCIM/MFA layer.
- **Estate-level model** — reasoning is per-project; no cross-project dependency/impact graph.
- **Market intelligence module** — design only; disclaimers pre-committed
  ([`Market-Module-Disclaimers.md`](Market-Module-Disclaimers.md)).
- **Multi-agent knowledge factory** — design + entity skeleton only
  ([`Multi-Agent-Knowledge-Factory.md`](Multi-Agent-Knowledge-Factory.md)).
- **Tamper-evident audit / external pen-test** — audit is append-only by convention, not hash-chained;
  no third-party security assessment performed.

---

## 4. The honest score

- **Position:** sellable as a **controlled, operator-assisted paid pilot** scoped to the proven core,
  with OCR/PDF, banking compliance, SSO, and autonomy-at-scale presented explicitly as roadmap.
- **Not** ready for unattended production or **commercial general availability**.
- Scoring is conservative: 100 means implemented **and** tested **and** demonstrated **and**
  documented **and** reviewable. **No area is at 100.** Domain, deployment, and commercial areas are
  deliberately modest until shipped and proven.

This conservative posture is a feature: over-claiming is the failure mode the project guards against.
For the certificate-style framing see
[`Industrial-Ship-Readiness-Certificate.md`](Industrial-Ship-Readiness-Certificate.md); for the
42-area audit see [`Final-20X-Gap-Audit.md`](Final-20X-Gap-Audit.md).

---

## 5. Where to go next

- Receiving the package → [`FINAL_CUSTOMER_HANDOVER_INDEX.md`](FINAL_CUSTOMER_HANDOVER_INDEX.md)
- Fastest local run → [`FINAL_LOCAL_DEPLOYMENT_GUIDE.md`](FINAL_LOCAL_DEPLOYMENT_GUIDE.md)
- The included knowledge → [`FINAL_KNOWLEDGE_BASE_GUIDE.md`](FINAL_KNOWLEDGE_BASE_GUIDE.md)
- Choosing a deployment mode → [`Deployment-Guide.md`](Deployment-Guide.md)
- What is not claimed → [`Known-Limitations.md`](Known-Limitations.md)
