# CLAUDE.md — Operating Guide for Claude Code

This file is the contract for any AI or human contributor working in this repository.
Read it fully before changing code. It encodes hard-won runtime lessons; ignoring it has
repeatedly reintroduced page hangs.

---

## 1. Project vision

**LocalAIFactory** is a private, **local-first AI software-engineering platform** for a banking
middleware estate (C#/.NET, MSSQL, EF Core, Python, IIS; domain systems BDM, MCIB,
ChequeXpert/Parascript, ETAMS). It is **not** a general chatbot.

Its defining feature is a **persistent, curated project memory with an approval lifecycle**:
approved knowledge, business rules, and code snippets are injected **first** into every prompt
context. The long-term goal is to import legacy banking projects and use local models to enhance,
debug, and evolve them, while accumulating approved knowledge over time.

Everything runs locally and must keep working with **only SQL Server present** — no GPU, no
internet, no Ollama, no Qdrant.

---

## 2. Current architecture

.NET 10 / ASP.NET Core MVC. Eight projects, **no dependency cycles**:

```
Core         (no project deps)        domain entities, enums, options, view models, abstractions
Data         -> Core                  EF Core AppDbContext (34 tables), migrations, seeding, security
Rag          -> Core, Data            embeddings, Qdrant vector store, retrieval, health cache
Agent        -> Core, Data, Rag       model execution, chat orchestration, task profiles
Ingestion    -> Core, Data, Rag       ZIP import pipeline, profiling, code extraction
Workspaces   -> Core, Data            Phase-1 scaffold for code-modification sandboxes (guarded off)
Terminal     -> Core                  sandboxed command policy/execution
Web          -> all                   ASP.NET Core MVC UI, controllers, hosted services, DI root
```

- **Database:** MSSQL + EF Core, migrations in `src/LocalAIFactory.Data/Migrations`.
- **Vector store:** Qdrant, **REST only**, **optional**. `projectId=0` denotes global knowledge.
- **Inference:** Ollama, **optional**. Default models: `qwen2.5-coder:14b`, `nomic-embed-text` (768-dim).
- **Frontend:** Bootstrap 5 + bootstrap-icons, `marked.js` (CDN, client-side markdown).
- **Hosted services (Web):** `IngestionBackgroundService` (drains the import queue) and
  `HealthMonitorService` (probes Qdrant/Ollama on a background timer and caches the result).

See `docs/01-Architecture.md` for detail.

---

## 3. Non-negotiable rules

1. **MSSQL is the primary memory store.** It must work standalone. Never make a page depend on an
   external service to render.
2. **Qdrant is optional. Ollama is optional.** Both are gated behind config flags and degrade
   gracefully when absent.
3. **The solution must work in MSSQL-only mode** (no GPU, no internet, no Ollama, no Qdrant).
4. **Home, Projects, Knowledge, and Models pages must always load**, on an empty database, a seeded
   database, and an MSSQL-only deployment.
5. **Do not reintroduce blocking external-service calls on the request path.** Health is read from a
   cached snapshot (`IServiceHealthCache`); never call Qdrant/Ollama synchronously inside a controller
   action or a Razor view.
6. **Prefer simple, reliable EF queries over clever aggregation.** Use separate `CountAsync()` calls.
   **Never use `GroupBy(_ => 1)`** or other group-by-constant aggregate projections — they have
   produced indefinite page hangs on SQL Server.
7. **Never materialize large text columns in list views.** `KnowledgeItem.Content`,
   `KnowledgeChunk.Content`, `ImportedFile.RawText`, `ChatMessage.Content`,
   `ProjectProfileSection.Content` are large. **Project list queries to lightweight rows** (a
   `record`) that select only the columns the view needs. Full-entity list loads with these columns
   cause unbounded client-side materialization that the SQL command timeout does **not** catch.
8. **No destructive database changes without explicit approval** (see §6).
9. **Always build before packaging or claiming done** (see §5).
10. **No secrets in the repo.** API keys are encrypted at rest via Data Protection; keys live in
    `keys/` which is git-ignored. Connection strings with credentials go in environment variables or
    a git-ignored local override, never in committed config.

---

## 4. Build & run commands

From the repository root:

```powershell
dotnet restore
dotnet build LocalAIFactory.sln -c Release
dotnet run --project src/LocalAIFactory.Web
```

PowerShell helpers (in `scripts/`): `build.ps1`, `run.ps1`, `migrate.ps1`, `clean.ps1`, `verify.ps1`.

The app **migrates and seeds the database automatically on startup**, so `dotnet run` is enough for a
first run once the connection string points at a reachable SQL Server.

---

## 5. Runtime validation commands

A successful build is necessary but not sufficient — this project's failures are runtime hangs.
After any change that touches a controller, a query, the dashboard, or DI:

```powershell
# 1. Build must succeed
dotnet build LocalAIFactory.sln -c Release

# 2. Start the app
dotnet run --project src/LocalAIFactory.Web

# 3. Each core page must return quickly (separate shell). None may hang.
curl -s -o NUL -w "%{http_code} %{time_total}s`n" http://localhost:5000/
curl -s -o NUL -w "%{http_code} %{time_total}s`n" http://localhost:5000/Projects
curl -s -o NUL -w "%{http_code} %{time_total}s`n" http://localhost:5000/Knowledge
curl -s -o NUL -w "%{http_code} %{time_total}s`n" http://localhost:5000/Models
```

`RequestTimingMiddleware` logs `-> {path} started` and `<- {path} {status} in {ms} ms` (Warning > 1 s)
for every request. **A hung endpoint logs "started" with no matching "completed" line** — use this to
locate a stall. The dashboard logs `Dashboard build started/completed in {ms} ms`.

Target: every core page completes in well under one second on SQL Express.

---

## 6. Migration & database rules

- Migrations live in `src/LocalAIFactory.Data/Migrations`. The DbContext is `AppDbContext` in
  `LocalAIFactory.Data`.
- Canonical commands (run from repo root):

  ```powershell
  # Apply latest migrations
  dotnet ef database update --project src/LocalAIFactory.Data --startup-project src/LocalAIFactory.Web

  # Add a new migration (only when entities change and after approval)
  dotnet ef migrations add <Name> --project src/LocalAIFactory.Data --startup-project src/LocalAIFactory.Web
  ```

- **No destructive DB changes without explicit human approval.** Dropping/renaming columns or tables,
  changing column types in a lossy way, deleting data, or any migration that is not cleanly additive
  must be proposed and approved first. Default to **additive, backward-compatible** schema changes.
- The 34-table schema and its `ModelSnapshot` are kept consistent. If you add a migration, regenerate
  the snapshot through EF — never hand-edit it into an inconsistent state.
- Stabilization work (Phase 1.2.x) is **schema-frozen**: it must not require a migration.

---

## 7. What this project is NOT (scope guards)

- Do **not** add new application features during stabilization/packaging tasks.
- Do **not** redesign the system or the UI. The modern card/table UI and the bulk
  approve/multi-select/select-all toolbar already exist — confirm, don't rebuild.
- Do **not** introduce `IDbContextFactory` churn or risky DI changes to "optimize" the dashboard;
  parallel reads use a scope-per-task pattern (`IServiceScopeFactory`) that is already in place.

---

## 8. Pointers

- Architecture: `docs/01-Architecture.md`
- Setup & configuration: `docs/02-Setup.md`
- Development workflow: `docs/03-Development-Workflow.md`
- Deployment (IIS): `docs/04-Deployment.md`
- Roadmaps: `docs/05-Knowledge-Engine-Roadmap.md`, `docs/06-AI-Runtime-Roadmap.md`
- Troubleshooting (incl. the page-hang history): `docs/07-Troubleshooting.md`
- Phase changelogs: `Phase-1.1-Release-Notes.md`, `Phase-1.2-Release-Notes.md`, `Phase-1.2-Runtime-Audit.md`
- Phase briefs / prompts: `prompts/`
