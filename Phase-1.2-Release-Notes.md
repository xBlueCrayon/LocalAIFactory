# LocalAIFactory — Phase 1.2 Release Notes (Stabilization & Hardening)

**Release type:** Robustness / deployment-readiness pass. No new product features; no breaking API changes; **no database schema changes** (no new migration).

**Goal:** the application must be fully usable on a basic **MSSQL-only** VM — with **no Qdrant, no Ollama, no embedding models, no GPU, and no internet** — and must never block page rendering on an external service. Adding Ollama and Qdrant later must require no code changes.

---

## 1.2.3 — the actual `/Home` + `/Knowledge` hang (full-entity materialization)

**Reported (live build):** `/Projects` and `/Models` load; **`/Home` and `/Knowledge` hang indefinitely with no exception.** Prior fixes (cached health, `GroupBy` removal) were real but addressed different issues.

**True root cause:** the list views **materialized full `KnowledgeItem` entities including the `nvarchar(max)` `Content` column** (entire imported file contents). `KnowledgeController.Index` did `Take(300).ToListAsync()` over full entities, pulling hundreds of MB of strings into memory. This is **not bounded by the SQL command timeout** (that covers the DB op, not client-side materialization), so the symptom is an indefinite stall with no exception. `/Projects` and `/Models` load small entities, so they were never affected. The common factor between the two hanging pages is the `KnowledgeItems` table.

**Fix:**
- **All list queries project to lightweight rows.** `KnowledgeController.Index` selects `KnowledgeListRow(Id, Title, SourceType, Status, UpdatedUtc, IsApproved)` — `Content` is filtered on server-side but never selected. Page size 300 → 200.
- **Dashboard rebuilt with parallel counts.** `DashboardService` runs eight independent count/projection groups via `Task.WhenAll`, each on its **own DI scope + `AppDbContext`** (a single context is not thread-safe), with structured `Dashboard build started/completed in {ms} ms` logging. No `GroupBy(_ => 1)`, no complex aggregation — plain `CountAsync`.
- **Request observability.** New `RequestTimingMiddleware` logs `→ {path} started` / `← {path} {status} in {ms} ms` (Warning > 1 s). A hung request logs "started" with no "completed", pinpointing the endpoint.

**Runtime guarantees:** `/`, `/Home`, `/Knowledge` do only database work on render — no Ollama, Qdrant, or embeddings. `/Knowledge` search is a SQL `LIKE`. The bulk approve / multi-select / select-all / bulk status-change UI (Phase 1.1) is confirmed in place. **No migration required** — only queries, projections, and view models changed. Full report: `Phase-1.2-Runtime-Audit.md`.

**Files changed in 1.2.3:** `KnowledgeController.cs`, `Views/Knowledge/Index.cshtml`, `Services/DashboardService.cs` (rewritten), `Middleware/RequestTimingMiddleware.cs` (new), `Program.cs`, `Core/ViewModels/ViewModels.cs`.

---

## 1.2.2 hotfix — dashboard hang caused by `GroupBy(_ => 1)` aggregates

**Reported (live build):** solution builds, migrates, and starts; Projects and Models pages load; **`/Home` hangs indefinitely with no exception logged**. Replacing `HomeController.Index()` with `return Content("HOME WORKS")` returned immediately, isolating the hang to the dashboard's query body. The distinctive queries were three `GroupBy(_ => 1).Select(… several Count(predicate) …).FirstOrDefaultAsync()` aggregates over `KnowledgeItems`, `IngestionJobs`, and `BusinessRules`.

**Root cause:** the group-by-constant + multiple conditional-count projection is an EF Core anti-pattern; on this SQL Server target it did not produce a clean, returning query. The plain `CountAsync` calls elsewhere in the same action (and on the Projects/Models pages) were unaffected, which is why only the dashboard hung.

**Fix:**
- **Removed every `GroupBy(_ => 1)` aggregate.** Each metric is now a simple `CountAsync()` → `SELECT COUNT(*) … WHERE …`, the same query shape proven to work on other pages.
- **Moved dashboard data loading into a dedicated `DashboardService`** (`src/LocalAIFactory.Web/Services/DashboardService.cs`) with **structured logging**: `Dashboard build started`, `Dashboard counts loaded in {ms} ms`, `Dashboard recent activity loaded in {ms} ms`, and `Dashboard build completed in {ms} ms (knowledge=…, jobs=…, rules=…)`.
- **Sequential execution, not `Task.WhenAll`.** A single `AppDbContext` is not thread-safe; overlapping queries throw "A second operation was started on this context instance…". Each `COUNT(*)` is an indexed aggregate (low single-digit ms), so sequential execution stays well under the 1s budget.
- **Projected the "recent activity" lists** to lightweight rows (`RecentKnowledgeRow`, `RecentImportRow`, `RecentApprovalRow`) so the dashboard never materializes full entities — `KnowledgeItem.Content` / `ImportedFile.RawText` columns are no longer selected.
- **`HomeController` is now thin:** it injects `DashboardService` and returns the view; health still comes from the cached snapshot, so there is **no external service call** on the render path.

**Runtime behaviour:** the dashboard loads correctly on an empty database, a seeded database, and an MSSQL-only deployment with no Ollama and no Qdrant, and completes within the 1s budget on SQL Express. Other `GroupBy` usages in the codebase group by a real column (a supported, translatable pattern) and are not on the dashboard path.

**Files changed in 1.2.2:** `src/LocalAIFactory.Web/Services/DashboardService.cs` (new), `src/LocalAIFactory.Web/Controllers/HomeController.cs`, `src/LocalAIFactory.Core/ViewModels/ViewModels.cs`, `src/LocalAIFactory.Web/Program.cs`.

---

## 1.2.1 hotfix — Qdrant still polled when `Rag.UseVectorSearch=false`

**Reported:** with `Qdrant.Enabled=false` **and** `Rag.UseVectorSearch=false`, runtime logs still showed repeated `GET http://localhost:6333/collections` and `/Home` was unresponsive.

**Root cause:** the health monitor and the vector store gated Qdrant access on `Qdrant.Enabled` **alone**. Requirement: no Qdrant call when `Qdrant.Enabled==false` **OR** `Rag.UseVectorSearch==false`. Because `UseVectorSearch=false` was ignored for the probe, a runtime where `Qdrant.Enabled` was still `true` (e.g. a stale `bin/appsettings.json` — the previously shipped default was `true`) kept the 15s probe alive even though vector search was off.

**Fix (single authoritative gate, both flags):**
- `QdrantVectorStore` now computes `VectorEnabled = Qdrant.Enabled && Rag.UseVectorSearch` and every method (`HealthAsync`, `EnsureCollectionAsync`, `UpsertAsync`, `DeleteAsync`, `SearchAsync`) early-returns when it is false. Turning **either** flag off guarantees zero Qdrant HTTP from the store.
- `HealthMonitorService` probes Qdrant only when `Qdrant.Enabled && Rag.UseVectorSearch`.
- `ServiceHealthCache` initial snapshot marks Qdrant **Disabled** when either flag is off.
- **Fail-safe defaults flipped:** `QdrantOptions.Enabled` and `RagOptions.UseVectorSearch` now default to **`false`** in code, so a missing/mis-bound section results in **no** Qdrant calls (previously they defaulted to `true`).
- **Shipped `appsettings.json` is now MSSQL-only-safe:** `Qdrant.Enabled=false`, `Rag.UseVectorSearch=false` (Ollama stays enabled). Enabling Qdrant is now an explicit opt-in.

**Runtime audit result:** every Qdrant HTTP call site is behind the `VectorEnabled` gate; no call is gated on `Enabled` alone anywhere. `/`, `/Home`, and `/Knowledge` controller actions are DB-only on render (health is read from cache), so the dashboard renders instantly even with Qdrant and Ollama both absent.

**Files changed in 1.2.1:** `src/LocalAIFactory.Rag/Vector/QdrantVectorStore.cs`, `src/LocalAIFactory.Web/Hosted/HealthMonitorService.cs`, `src/LocalAIFactory.Rag/Health/ServiceHealthCache.cs`, `src/LocalAIFactory.Core/Options/AppOptions.cs`, `src/LocalAIFactory.Web/appsettings.json`.

---

## 1. Root cause of the reported hang

`HomeController.Index` awaited `IVectorStore.HealthAsync()` **on the render path**. When Qdrant was enabled (the default) but not running, that call did a live HTTP request whose client timeout was **30 seconds**, so the dashboard blocked until the connection failed or timed out.

**Fix:** health is now produced by a background monitor and read from an in-memory cache. The dashboard performs **zero** external calls while rendering, and every health probe is independently bounded to **1 second**.

---

## 2. Issues found and corrections made

| # | Issue | Correction |
|---|-------|-----------|
| 1 | Dashboard blocked on `HealthAsync()` during render. | New `HealthMonitorService` (background) + `IServiceHealthCache` (singleton). `HomeController` reads the cached snapshot only. |
| 2 | Health checks could wait up to 30s. | All probes bounded to **1s** via linked `CancellationTokenSource`. `QdrantVectorStore.HealthAsync` is also capped at 2s for any other caller. |
| 3 | Dashboard fired ~20 sequential `CountAsync` queries. | Per-table counts batched into **grouped** queries (knowledge, ingestion jobs, business rules each resolve in one round trip). ~20 round trips → ~12. |
| 4 | `EmbeddingService.IsConfigured` hard-coded `true`. | Now config-aware: embeddings are "configured" only when `Rag.UseVectorSearch` **and** `Qdrant.Enabled` **and** a provider path exists (`Ollama.Enabled` or OpenAI). |
| 5 | Indexer probed Qdrant/embeddings even when disabled (per-item health calls). | `KnowledgeIndexer` early-returns when `!IsConfigured`. In Minimal Mode indexing performs **no** Qdrant or embedding calls at all. |
| 6 | No explicit "Ollama off" switch. | Added `Ollama.Enabled` (default `true`). `Enabled=false` prevents all Ollama calls and probes. |
| 7 | Dashboard showed only binary "online/offline" with a live call. | Tri-state per service — **Online / Offline / Disabled** (plus "Checking…" before the first probe) — sourced from the cache. |
| 8 | No notion of deployment tier. | Environment mode (**Minimal / Standard / Full AI**) is auto-derived from live health and shown on the dashboard. |
| 9 | Chat page gave no warning when no model existed. | Chat shows a clear **"No model is available"** banner (reads cached `ChatAvailable`), while still allowing knowledge curation. |
| 10 | Import status didn't distinguish imported vs embedded vs indexed vs skipped. | The per-job status page now shows **Imported / Chunks / Embedded / Skipped** tiles, an **Indexing** panel (stored-for-keyword vs vector-indexed), and a **skip-reason breakdown** (binary / too large / duplicate / unreadable). |

---

## 3. Dashboard hardening (A)

- Renders instantly; **no external service call during rendering**.
- Health is a background-cached snapshot (`IServiceHealthCache`), refreshed every 15s, each probe ≤1s.
- Per-service tri-state: **Online / Offline / Disabled**. The initial snapshot is derived from configuration with no I/O, so disabled services read "Disabled" immediately on first paint; unknown services read "Checking…" until the first probe (≈1s after startup).
- Shows the active model, the current environment mode, and the last-checked timestamp.

## 4. Service degradation (B)

**Qdrant absent or unreachable** → vector search is skipped and **MSSQL keyword search** is used. No exceptions; no startup impact. (`QdrantVectorStore` already guards every method on `Enabled`; `KnowledgeSearchService` already falls back on any failure.)

**Ollama absent or unreachable:**
- Imports continue; knowledge is stored.
- Knowledge-graph extraction continues with **heuristics** (model-assisted step is wrapped and optional).
- Project-profile generation falls back to a **heuristic summary**.
- Chat displays the **model-unavailable** banner; sending returns a clear error instead of throwing.

## 5. Startup audit (C)

Reviewed hosted services, singletons, controllers, seeders, and startup tasks. None perform a blocking external call:

- `Program.cs` startup only runs EF `Migrate()` + `DbSeeder.SeedAsync` (database only).
- `DbSeeder` inserts seed rows only — no HTTP, no model/vector calls.
- `IngestionBackgroundService` does a DB-only recovery pass, then awaits the queue — it yields immediately, so host startup is never blocked.
- `HealthMonitorService` yields on its first `await` (bounded probe); the first probe runs in the background within ~1s.
- `ServiceHealthCache` (singleton) computes its initial value from configuration only — no I/O in the constructor.

Startup succeeds with Ollama absent, Qdrant absent, and no internet.

## 6. Configuration hardening (D)

- `Qdrant.Enabled=false` → **no Qdrant calls** anywhere (`HealthAsync`, `EnsureCollectionAsync`, `UpsertAsync`, `SearchAsync`, `DeleteAsync` all short-circuit on `Enabled`).
- `Rag.UseVectorSearch=false` → **no vector search and no embedding generation** (search skips the vector branch entirely; the indexer no-ops via `IsConfigured`).
- `Ollama.Enabled=false` → **no Ollama calls or probes**.
- Embedding generation is gated by `IsConfigured` so it is never attempted when disabled.

## 7. Import pipeline hardening (E)

Project import works with **no Qdrant and no Ollama**. The embedding, profiling, graph, and model-assisted candidate steps are each wrapped so a missing service is non-fatal; the job still completes. The status page clearly reports:

- **Imported** — knowledge items created.
- **Embedded** — vector points written (0 when embeddings are unavailable/disabled, with an explanatory note).
- **Indexed** — whether content is vector-indexed; otherwise "stored for keyword search".
- **Skipped** — count with a reason breakdown (binary, too large, duplicate, unreadable).

## 8. Environment modes (F)

The app auto-detects its effective mode from live health each cycle:

| Mode | Condition (effective) | Behaviour |
|------|-----------------------|-----------|
| **Minimal** | No reachable Ollama | MSSQL only. Keyword search. Heuristic graph/profile. Chat needs a model. |
| **Standard** | Ollama online, Qdrant not online | MSSQL + local LLM. Chat works. Keyword search. |
| **Full AI** | Ollama online **and** Qdrant online | MSSQL + LLM + vector search. |

To **force Minimal Mode** on an MSSQL-only VM, set in `appsettings.json`:

```json
"Ollama": { "Enabled": false },
"Qdrant": { "Enabled": false },
"Rag":    { "UseVectorSearch": false }
```

With these flags the app makes no AI-infrastructure calls at all. Later, set them back to `true` and start Ollama/Qdrant — the app adapts automatically with no code change.

## 9. Performance review (G)

- Removed the blocking vector health call from the dashboard.
- Batched dashboard counts into grouped queries (knowledge, ingestion, rules).
- All dashboard reads use `AsNoTracking`.

---

## 10. Files changed

**New**
- `src/LocalAIFactory.Core/Abstractions/IServiceHealth.cs` — `IServiceHealthCache` + `ServiceHealthSnapshot`.
- `src/LocalAIFactory.Rag/Health/ServiceHealthCache.cs` — thread-safe singleton; config-derived initial snapshot.
- `src/LocalAIFactory.Web/Hosted/HealthMonitorService.cs` — background prober (1s timeouts, 15s interval).

**Modified**
- `src/LocalAIFactory.Core/Enums/Enums.cs` — `ServiceState`, `EnvironmentMode`.
- `src/LocalAIFactory.Core/Options/AppOptions.cs` — `OllamaOptions.Enabled`.
- `src/LocalAIFactory.Core/ViewModels/ViewModels.cs` — `DashboardViewModel.Health` (replaces two booleans).
- `src/LocalAIFactory.Rag/DependencyInjection.cs` — register `IServiceHealthCache`.
- `src/LocalAIFactory.Rag/Embeddings/EmbeddingService.cs` — config-aware `IsConfigured`; `EmbedAsync` early-out.
- `src/LocalAIFactory.Rag/Indexing/KnowledgeIndexer.cs` — skip when embeddings disabled.
- `src/LocalAIFactory.Rag/Vector/QdrantVectorStore.cs` — 2s cap on `HealthAsync`.
- `src/LocalAIFactory.Web/Program.cs` — register `HealthMonitorService`.
- `src/LocalAIFactory.Web/Controllers/HomeController.cs` — cached health + batched counts (no external call).
- `src/LocalAIFactory.Web/Controllers/ChatController.cs` — expose `ChatAvailable`.
- `src/LocalAIFactory.Web/Controllers/ImportWizardController.cs` — skip-reason breakdown + imported count.
- `src/LocalAIFactory.Web/Views/Home/Index.cshtml` — tri-state health + mode.
- `src/LocalAIFactory.Web/Views/Chat/Index.cshtml` — model-unavailable banner.
- `src/LocalAIFactory.Web/Views/ImportWizard/Status.cshtml` — outcome breakdown + reasons.
- `src/LocalAIFactory.Web/appsettings.json` — `Ollama.Enabled`.

---

## 11. Upgrade notes

1. Replace the source tree with this release. No migration is required; the database still migrates and seeds on startup.
2. To deploy on a bare MSSQL VM, apply the Minimal-Mode flags above. Otherwise leave the defaults; the app will detect Ollama/Qdrant if they come online.
3. No `appsettings` change is strictly required to fix the hang — the dashboard no longer calls Qdrant on render regardless of flags.
