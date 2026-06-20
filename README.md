# LocalAIFactory

**A private, local-first, MSSQL-authoritative AI software-engineering platform for a banking middleware estate.**
It imports legacy projects (C#/.NET, T-SQL, Python), understands them *structurally*, and accumulates a
**governed, approval-gated knowledge base** that is injected first into every prompt context. Everything runs
locally and keeps working with **only SQL Server present** — no GPU, no internet, no Ollama, no Qdrant required.

> `MASTER_VISION.md` is the authoritative source of truth. `CLAUDE.md` is the contributor contract.
> This is **not** a general chatbot, and it makes **no** vendor-certification, regulatory, financial, or
> fraud-proof claims.

---

## What it is

.NET 10 / ASP.NET Core MVC. Eight projects, no dependency cycles: **Core, Data, Rag, Agent, Ingestion,
Workspaces, Terminal, Web**. MSSQL + EF Core is the primary memory store; Qdrant (vectors) and Ollama (local
inference) are **optional** and degrade gracefully when absent.

## What it can do today (proven)

- **Deterministic structural understanding** — C# (Roslyn), T-SQL (ScriptDom) and Python (pure-C# parser) symbol
  extraction into a structural graph (`CodeSymbol`/`CodeEdge`), with reference resolution and **bidirectional
  impact analysis**.
- **C#↔SQL bridge** — links C#/Python methods to the SQL objects they access (`AccessesSql` edges, with
  confidence + evidence); answers "what code touches `dbo.X`" and "what is the blast radius of this change".
- **Governed knowledge base** — `KnowledgeItem`/`KnowledgePack` with permanence tiers (Curated), provenance,
  versioning, a source registry, and a **propose-never-overwrite** guard. MSSQL is the runtime source of truth;
  JSON packs are the source-controlled seed/import format.
- **General vs project vs chat-imported knowledge** — general packs (`ProjectId=null`, Curated), project-scoped
  knowledge (`ProjectId` set), and a deterministic **chat-import extractor** that turns ChatGPT/Claude/markdown
  conversations into *proposals only* (never auto-approved).
- **Project import** — ZIP import pipeline with profiling, code extraction, and per-import **coverage / gap
  reporting** (no silent blind spots).
- **Knowledge packs (4)** — `professional-base-v1` (390 items) plus `financial-institution-operations-v1`,
  `kyc-aml-transaction-approval-v1`, `market-intelligence-forecasting-v1` (48 items), all installable through the
  validated, idempotent installer.
- **Benchmark harness** — pinned-repo + local-fixture fixtures with Smoke/Standard/Extended tiers and golden
  snapshots, including **ERP/CRM (Gold 6/6)**, **core-banking (Gold 6/6)** and **KYC/AML→transaction-approval
  (Gold 7/7)** fixtures where the bridge answers industrial impact questions.
- **Security & audit** — Windows-auth RBAC enforced server-side, deny-by-default, append-only audit, IDOR guard.
- **Supportability** — read-only `/Support` dashboard (build/version, edition+license, cached service health, DB
  counts, last import/audit, disk, warnings); never blocks on an external service.
- **Edition / license skeleton** — demo-safe (no DRM, no phone-home; a missing/expired paid license degrades to
  the Community core).
- **Safe local fix loop** — applies a patch to an isolated workspace, runs allowlisted checks, **rolls back on
  failure**, and **never commits/pushes** (default dry-run).

### Current validation status

| Gate | Status |
|---|---|
| `dotnet build -c Release` | **0 errors** |
| `dotnet test` | **235 / 235 pass** |
| Benchmark (smoke / standard) | **PASS** (KYC/AML fixture Gold 7/7) |
| UI smoke (`scripts/poc/ui-smoke-test.ps1`) | **PASS** (11 pages 200, incl. `/Support`) |
| `scripts/poc/verify-poc.ps1` | **PASS** |
| `dotnet publish` | **151 files / 45 MB** |

**Readiness:** overall mean ≈ 57.6% (see `/Readiness` and [`docs/readiness-scorecard.json`](docs/readiness-scorecard.json)).
**No area is at 100** — scoring is deliberately conservative.

## What it is NOT (yet)

No enterprise SSO/IdP; no executed production (IIS/Docker/Express/full-SQL) deployment; no real OCR/CV engine
(deterministic PDF/cheque-triage prototypes only); no cross-repository estate model; no autonomous fix loop on a
real repo; no commercial licensing enforcement; **not commercial-GA**. See
[`docs/Known-Limitations.md`](docs/Known-Limitations.md) and the per-area `proofRequiredFor100` in the scorecard.

## Pilot readiness

Sellable as a **controlled, operator-assisted paid pilot** scoped to the proven core, with OCR/PDF, banking
compliance, SSO and autonomy-at-scale presented as roadmap. **Not** ready for unattended production or commercial
general availability. See [`docs/Industrial-Ship-Readiness-Certificate.md`](docs/Industrial-Ship-Readiness-Certificate.md).

---

## Setup

```powershell
dotnet restore
dotnet build LocalAIFactory.sln -c Release
# Create the local database (LocalDB; create-if-absent, never drops):
database/create-localdb.ps1
# Run (auto-migrates + seeds on first run once the connection string points at a reachable SQL Server):
dotnet run --project src/LocalAIFactory.Web
```

The four core pages (Home, Projects, Knowledge, Models) must always load — on an empty DB, a seeded DB, or in
MSSQL-only mode. See [`docs/02-Setup.md`](docs/02-Setup.md).

## Validation

```powershell
dotnet build LocalAIFactory.sln -c Release          # must be 0 errors
dotnet test tests/LocalAIFactory.Tests              # 235/235
cd tools/LocalAIFactory.Benchmark; dotnet run -c Release -- --inmemory --suite standard
scripts/poc/verify-poc.ps1                          # artifacts + build + test + benchmark + hygiene
scripts/poc/ui-smoke-test.ps1                       # starts app, asserts no 500s
database/verify-knowledge-base.ps1                  # KB integrity
scripts/security/security-audit.ps1                 # secrets / dangerous-command / large-artifact scan
scripts/diagnostics/system-snapshot.ps1             # CPU/RAM/disk/GPU snapshot
```

## Documentation

Start at the **[documentation hub: `docs/README.md`](docs/README.md)** — indexed by role (executive, user, admin,
operator, developer, deployment, database, security, support, AI governance, knowledge base, ERP/CRM, core
banking, KYC/AML, market intelligence, autonomous engineering, troubleshooting).

Key entry points: [Architecture](docs/01-Architecture.md) · [Setup](docs/02-Setup.md) ·
[Development Workflow](docs/03-Development-Workflow.md) · [Deployment (IIS)](docs/04-Deployment.md) ·
[Troubleshooting](docs/07-Troubleshooting.md) · [Final 20X Completion Report](docs/Final-20X-Completion-Report.md) ·
[Gap-Closure Roadmap to 100](docs/Gap-Closure-Roadmap-To-100.md).

## License & contributing

See `CLAUDE.md` for the operating contract and non-negotiable runtime rules (MSSQL-only must work; Qdrant/Ollama
optional; no blocking external calls on the request path; no destructive DB changes without approval; no secrets
in the repo). Knowledge-pack content is original professional summaries authored for LocalAIFactory — no
third-party proprietary text is vendored.
