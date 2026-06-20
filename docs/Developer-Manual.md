# Developer Manual

For engineers building on LocalAIFactory. This manual covers the solution layout, build/test/
benchmark commands, how to extend the platform (knowledge packs, the chat extractor, the benchmark
harness, the C#↔SQL bridge), and the **non-negotiable coding rules** that keep core pages from
hanging.

Read `CLAUDE.md` and `MASTER_VISION.md` before making changes. `MASTER_VISION.md` is authoritative on
direction; `CLAUDE.md` encodes hard-won runtime rules; this manual is the practical "how".

---

## 1. Solution layout

.NET 10 / ASP.NET Core MVC, eight projects, **strict acyclic dependency graph**:

```
Core         (no project deps)        entities, enums, options, view models, abstractions
Data         -> Core                  EF Core AppDbContext (34 tables), migrations, seeding, security
Rag          -> Core, Data            embeddings, Qdrant vector store, retrieval, health cache
Agent        -> Core, Data, Rag       model execution, chat orchestration, task profiles
Ingestion    -> Core, Data, Rag       ZIP import pipeline, profiling, code extraction
Workspaces   -> Core, Data            Phase-1 scaffold for code-modification sandboxes (guarded off)
Terminal     -> Core                  sandboxed command policy/execution
Web          -> all                   ASP.NET Core MVC UI, controllers, hosted services, DI root
```

**Rules:** never introduce a cycle. `Core` stays dependency-free. Hosting abstractions (background
services) live in `Web`, not in the libraries. See `docs/01-Architecture.md`.

- **Database:** MSSQL + EF Core; migrations in `src/LocalAIFactory.Data/Migrations`; the DbContext is
  `AppDbContext`.
- **Vector store:** Qdrant, REST only, optional. `projectId=0` denotes global knowledge.
- **Inference:** Ollama, optional. `qwen2.5-coder:14b`, `nomic-embed-text` (768-dim).
- **Frontend:** Bootstrap 5 + bootstrap-icons, `marked.js` (client-side markdown).
- **Hosted services (Web):** `IngestionBackgroundService` (drains the import queue) and
  `HealthMonitorService` (probes Qdrant/Ollama on a timer and caches the result).

---

## 2. Build, test, benchmark

```powershell
# Build (Release)
dotnet build LocalAIFactory.sln -c Release        # current status: 0 errors

# Tests
dotnet test LocalAIFactory.sln -c Release         # current status: 235/235 pass

# Validation harness (benchmarks + fixtures)
scripts/poc/verify-poc.ps1                         # current status: PASS
```

The benchmark standard suite passes against its fixtures, including ERP/CRM (Gold 6/6), core-banking
(Gold 6/6), KYC/AML (Gold 7/7), and eShopOnWeb (Gold). These numbers reflect the fixtures in this
repository at this point in time; treat them as the validation baseline, not a permanent claim.

A successful build is necessary but **not sufficient** — this project's failures are runtime hangs.
After touching a controller, a query, the dashboard, or DI, run the app and the core-page smoke (see
the Operator Manual §4.1 / `CLAUDE.md` §5).

---

## 3. Coding rules (from CLAUDE.md)

These have each caused real page hangs. They are mandatory.

1. **Never use `GroupBy(_ => 1)`** or group-by-constant aggregate projections — they have produced
   indefinite SQL Server hangs. Prefer separate `CountAsync()` calls.
2. **Never materialize large text columns in list views.** `KnowledgeItem.Content`,
   `KnowledgeChunk.Content`, `ImportedFile.RawText`, `ChatMessage.Content`,
   `ProjectProfileSection.Content` are large. Project list queries to lightweight `record` rows that
   select only the columns the view needs.
3. **No blocking external-service calls on the request path.** Read health from
   `IServiceHealthCache`; never call Qdrant/Ollama synchronously in a controller action or a Razor
   view.
4. **Prefer simple, reliable EF queries over clever aggregation.** Parallel dashboard reads use a
   scope-per-task pattern (`IServiceScopeFactory`) — a single `AppDbContext` is not thread-safe.
5. **Core pages must always load** — empty DB, seeded DB, MSSQL-only.
6. **No destructive DB changes without explicit approval**; additive, backward-compatible migrations
   by default.
7. **No secrets in the repo.**

The `SupportController` is a good reference implementation of these rules: cached-snapshot health,
guarded `CountAsync` tiles that degrade to "unavailable", no large text columns, always renders.

---

## 4. Migrations

```powershell
# Apply latest migrations
dotnet ef database update --project src/LocalAIFactory.Data --startup-project src/LocalAIFactory.Web

# Add a migration (only after entities change AND approval)
dotnet ef migrations add <Name> --project src/LocalAIFactory.Data --startup-project src/LocalAIFactory.Web
```

Keep the `ModelSnapshot` consistent via EF — never hand-edit it. Stabilization work is schema-frozen.

---

## 5. Adding a knowledge pack

A pack is a folder under `knowledge-packs/<name>-v1/`:

- `manifest.json` — `packUid`, `name`, `version`, `description`, `license`, `itemCount`, `files`,
  `sourcePolicy`, `legalLimitations`, `reviewStatus`.
- One or more item files (e.g. `operations.json`, `controls-and-risk.json`) referenced by `files`.

Rules:

- **Original summaries only** — no verbatim ISO/IEC, IFRS, PMBOK, FATF, Basel, vendor, or regulatory
  text. Protected sources carry `verbatimCopyAllowed=false`; research families are topic-level with
  **no fabricated citations**.
- Every item carries a **limitation note** and a **confidence**.
- Installation is Admin-only and **idempotent**; the installer validates the source policy and
  rejects unregistered sources.

Verify with `database/verify-knowledge-base.ps1`. Authoring details:
`docs/Knowledge-Pack-Authoring-Guide.md`.

---

## 6. The chat extractor

The chat-to-knowledge pipeline turns exported AI conversations into candidate knowledge. It extracts
candidate items (rules, snippets, facts) which enter the lifecycle as **candidates** and are kept out
of authoritative retrieval until reviewed/approved. Curated knowledge is never silently overwritten —
re-derivation **proposes** a change for review. See `docs/Chat-Learning-Pipeline.md`,
`docs/Conversation-To-Knowledge-Extraction-Rules.md`, and
`docs/Chat-Import-Knowledge-Learning.md`.

---

## 7. The benchmark harness and fixtures

The validation harness (`scripts/poc/verify-poc.ps1`) runs the platform against curated fixtures and
checks expected proofs (the "Gold" fixtures: ERP/CRM, core-banking, KYC/AML, eShopOnWeb). Use it as
the regression gate when changing extraction, the bridges, or impact analysis. The **Benchmarks**
page surfaces the same status in the UI. `scripts/release/verify-installation.ps1` wraps the
fast-mode harness plus knowledge-base verification.

---

## 8. The C#↔SQL bridge

The bridge connects code structure to data structure:

- SQL embedded in C# is parsed and resolved to schema symbols (tables, stored procedures, views),
  recorded as `AccessesSql` edges in the `CodeEdge` graph.
- The Python↔SQL bridge applies the same idea to Python data access.
- Impact analysis traverses these edges in **both directions** (code→SQL and SQL→code), deterministic
  and MSSQL-only — no model or vectors required. Results carry confidence and evidence.

When extending the bridge, add coverage to the benchmark fixtures so the proof set grows with the
capability.

---

## 9. Where things live

- Architecture: `docs/01-Architecture.md`
- Setup / config: `docs/02-Setup.md`
- Dev workflow: `docs/03-Development-Workflow.md`
- Security: `docs/Security-Model.md`, `docs/RBAC-Matrix.md`, `docs/Secrets-Handling.md`
- Phase 2 (authoritative roadmap): `docs/Phase-2-README.md` and linked docs
- Troubleshooting (incl. page-hang history): `docs/07-Troubleshooting.md`
