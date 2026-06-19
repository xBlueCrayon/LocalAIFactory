# LocalAIFactory — Runtime Audit & Architecture Review (Phase 1.2.3)

This is a **runtime-behaviour** review (not a build review). It explains the `/Home` and `/Knowledge` hangs, documents every page/controller against the audit checklist, and records the corrections.

---

## A. Root cause of the hangs

**`/Knowledge` and (pre-fix) `/Home` materialized full `KnowledgeItem` entities, including the `nvarchar(max)` `Content` column.**

- `KnowledgeController.Index` ran `…Take(300).ToListAsync()` over **full entities**. `KnowledgeItem.Content` holds entire imported file contents. Loading hundreds of those rows pulls hundreds of MB (or more) of text into memory and constructs the strings client-side.
- This is **not bounded by the SQL command timeout** (default 30 s applies to the database operation, not to client-side materialization/GC), so the symptom is an *indefinite* stall with *no exception* — exactly what was observed.
- `/Projects` and `/Models` never touch a large-column entity, which is why they always responded.
- The earlier `/Home` stall had the same shape: the dashboard loaded full `KnowledgeItem` rows for "recent activity". (It had already been moved off the blocking Qdrant health call and off `GroupBy(_ => 1)` in prior fixes; the remaining stall was the full-entity load.)

**Why previous fixes did not resolve it:** they addressed real but *different* issues (the blocking Qdrant health call; the `GroupBy(_ => 1)` aggregate). Neither removed the large-column materialization, so `/Knowledge` kept hanging.

### Fix
- **Project every list query to a lightweight row.** `KnowledgeController.Index` now selects `KnowledgeListRow(Id, Title, SourceType, Status, UpdatedUtc, IsApproved)` — `Content` is filtered on server-side but **never selected**. The dashboard's "recent" lists project to `RecentKnowledgeRow` / `RecentImportRow` / `RecentApprovalRow`.
- Page size reduced from 300 to 200 for the list.

---

## B. Audit checklist results

| # | Checklist item | Finding | Action |
|---|----------------|---------|--------|
| 1 | Every page/controller | 20 controllers reviewed (table C) | Projected the offending list query |
| 2 | Blocking operations | Only the full-entity materialization in `KnowledgeController.Index` | Fixed via projection |
| 3 | Synchronous waits | None. No `.Result`/`.Wait()`/`.GetAwaiter().GetResult()` on the request path | — |
| 4 | EF queries that generate problematic SQL | `GroupBy(_ => 1)` (already removed); full-entity loads of large-column entities | Counts use plain `CountAsync`; lists projected |
| 5 | External-service dependency on render | Dashboard read a cached snapshot; no controller calls Ollama/Qdrant on render | Confirmed; `/Knowledge` does keyword `LIKE`, no AI |
| 6 | N+1 patterns | No lazy-loading proxies are enabled, and no view accesses navigation properties in a loop | — |
| 7 | Dashboard aggregation that can stall | `GroupBy(_ => 1)` removed earlier; now plain counts | Counts run in parallel (section D) |
| 8 | Startup services that delay requests | `Migrate()` + `DbSeeder` (DB only) run before serving; both fast and local | — |
| 9 | Hosted services | `IngestionBackgroundService` (DB-only recovery, then async queue) and `HealthMonitorService` (1 s bounded probes) both yield immediately | — |
| 10 | Health-monitor logic | Probes are bounded to 1 s; Qdrant probe gated on `Qdrant.Enabled && Rag.UseVectorSearch`; dashboard reads cache only | — |

**Data-layer review:** `AppDbContext` has **no global query filters, no interceptors, no lazy-loading proxies, no `HasData`**, so a `CountAsync` is a plain `SELECT COUNT(*)`. The seeder inserts only Projects, ModelConfigurations, PromptTemplates, TaskProfiles, SystemSettings — never `KnowledgeItems` — so a fresh database has an empty table and the dashboard is instant.

---

## C. Per-controller list-query audit

Entities with large text columns: `KnowledgeItem.Content`, `KnowledgeChunk.Content`, `ImportedFile.RawText`, `ChatMessage.Content`, `ProjectProfileSection.Content`.

| Controller / action | Loads large-column entity as a list? | Status |
|---|---|---|
| `KnowledgeController.Index` | **Yes — KnowledgeItem.Content** | **Fixed → projected `KnowledgeListRow`** |
| `Home` (DashboardService) | recent lists | **Fixed → projected rows** |
| `ImportWizard.Status` / `ProjectProfiles` / `PromptRuns` (lists) | already projected | OK |
| `Knowledge.Details`, `Chat` (one session's messages) | single-item / per-conversation, bounded | OK (not a list scan) |
| `Projects`, `Models`, `BusinessRules`, `ApprovedCode`, `TaskProfiles`, `KnowledgeGraph`, `Workspaces`, `AgentTasks` | small entities only | OK |

Lower-priority follow-ups (not on the reported paths, bounded today): `Chat` messages for a very long conversation, and Workspace change/snapshot lists, could also be projected if those datasets grow.

---

## D. Dashboard rebuild

- **No `GroupBy(_ => 1)`, no complex aggregation** — every metric is a plain `CountAsync()`.
- **Parallel independent counts via `Task.WhenAll`.** Because a single `AppDbContext` is not thread-safe (overlapping queries throw "A second operation was started on this context instance…"), each parallel branch runs on **its own DI scope and context** (`RunAsync`), the same scope-per-operation pattern the hosted services use. Eight branches run concurrently.
- **Structured logging:** `Dashboard build started` → `Dashboard build completed in {ms} ms (knowledge=…, jobs=…, rules=…)`.
- Health comes from the cached snapshot — no external call on the render path.
- Result: the dashboard completes in well under 1 s on SQL Express and on an empty database.

## E. Request-level observability

Added `RequestTimingMiddleware`: every controller request logs `→ {method} {path} started` and `← {method} {path} {status} in {ms} ms` (Warning if > 1 s). A hung request logs the "started" line with **no** matching "completed" line, which pinpoints the stalling endpoint immediately. Static assets are skipped.

## F. MSSQL-only / no-AI guarantees

- `/`, `/Home`, `/Knowledge` perform **only** database work on render. None call Ollama, Qdrant, or embeddings.
- `/Knowledge` search is a SQL `LIKE` over title/content — no embeddings, no vector store.
- Qdrant is contacted only when `Qdrant.Enabled && Rag.UseVectorSearch` (both default to `false`); the health monitor and vector store share that gate.
- With Ollama present, dashboards and knowledge browsing are unaffected — AI is only used for chat and (optional) enrichment, all of which degrade gracefully.

## G. Discovered runtime issues (summary list)

1. **`/Knowledge` hang** — full `KnowledgeItem` (with `Content`) materialized for the list. **Fixed (projection).**
2. **`/Home` hang (residual)** — dashboard loaded full `KnowledgeItem` rows for recent activity. **Fixed (projection).**
3. **Dashboard `GroupBy(_ => 1)`** — replaced earlier with plain counts; now also parallelized. **Fixed.**
4. **Blocking Qdrant health on render** — removed earlier (cached health). **Fixed.**
5. **Qdrant probe ignored `Rag.UseVectorSearch`** — gate now requires both flags. **Fixed.**
6. No synchronous waits, no N+1, no startup blocking found.

## H. Migration

**None required.** Only queries, projections, and view models changed. No entity or table was modified.
