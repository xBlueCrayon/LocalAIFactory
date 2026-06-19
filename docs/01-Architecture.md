# 01 — Architecture

LocalAIFactory is a .NET 10 / ASP.NET Core MVC application built as eight projects with a strict
acyclic dependency graph.

## Projects & dependencies

| Project | Depends on | Responsibility |
|---|---|---|
| `Core` | — | Entities, enums, options, view models, abstractions. No external deps. |
| `Data` | Core | `AppDbContext` (34 tables), migrations, seeding, Data Protection key handling. |
| `Rag` | Core, Data | Embeddings, Qdrant REST vector store, retrieval, background health cache. |
| `Agent` | Core, Data, Rag | Model execution, chat orchestration, Task Profiles. |
| `Ingestion` | Core, Data, Rag | ZIP import pipeline, project profiling, code/SQL/doc extraction. |
| `Workspaces` | Core, Data | Phase-1 scaffold for code-modification sandboxes (guarded off). |
| `Terminal` | Core | Sandboxed command policy + execution. |
| `Web` | all | MVC UI, controllers, hosted services, DI composition root. |

**Rule:** never introduce a cycle. `Core` stays dependency-free. Hosting abstractions live in `Web`
(e.g. the ingestion background service is in `Web`, not the `Ingestion` library).

## Data & memory

- **MSSQL is the primary, always-on memory store.** EF Core, 34-table schema, migrations in
  `src/LocalAIFactory.Data/Migrations`.
- **Qdrant** (REST only, optional) holds vectors; `projectId=0` marks global knowledge.
- **Ollama** (optional) serves `qwen2.5-coder:14b` and `nomic-embed-text` (768-dim).

## Knowledge approval lifecycle

Knowledge, business rules, and code snippets move through **Draft → Approved → Deprecated →
NeedsReview**. **Approved** items are injected **first** into prompt context; **project-specific**
knowledge overrides **generic** knowledge. This curated memory is the core differentiator.

## Request & runtime model

- Controllers do **only database work** on the render path. Health/service status is read from
  `IServiceHealthCache`, a snapshot updated by a background monitor — **no synchronous Qdrant/Ollama
  calls in actions or views**.
- The dashboard (`DashboardService`) computes metrics with plain `CountAsync` calls run in parallel
  via `Task.WhenAll`, each on its own DI scope/`AppDbContext` (a single context is not thread-safe).
- List views project to lightweight `record` rows so large text columns
  (`KnowledgeItem.Content`, etc.) are never materialized.
- `RequestTimingMiddleware` logs start/finish/duration for every request.

## Hosted services (Web)

- **`IngestionBackgroundService`** — recovers interrupted jobs on startup (DB-only requeue), then
  drains the import queue, processing each job in its own scope.
- **`HealthMonitorService`** — probes Qdrant/Ollama on a timer with a 1s bound and writes the result
  into `IServiceHealthCache`. The Qdrant probe is gated on `Qdrant.Enabled && Rag.UseVectorSearch`.

## Environment modes

The app derives a mode from configuration/health: **Minimal** (MSSQL only), **Standard** (MSSQL +
one of Ollama/Qdrant), **FullAi** (MSSQL + Ollama + Qdrant). The mode is shown on the dashboard.
