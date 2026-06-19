# Phase 1.2 — Stabilization & runtime hardening (brief)

**Goal:** the app must be fully usable on a bare **MSSQL-only** VM — no GPU, no internet, no Ollama,
no Qdrant — and must never block page rendering on an external service. Enabling Ollama/Qdrant later
must require **no code change**. **No schema changes.**

**Scope delivered:**
- **Background-cached health** (`IServiceHealthCache` + `HealthMonitorService`); environment modes
  (Minimal/Standard/FullAi); graceful degradation throughout.
- **Fail-safe defaults:** `Qdrant.Enabled=false`, `Rag.UseVectorSearch=false`; vectors only when both
  are true; the Qdrant probe honors both flags.
- **Runtime hang fixes (1.2.1–1.2.3):**
  - Removed a blocking Qdrant health call from the render path.
  - Removed `GroupBy(_ => 1)` dashboard aggregates → plain `CountAsync`.
  - **Root cause of the persistent `/Home` + `/Knowledge` hangs:** list views materialized full
    `KnowledgeItem` entities including the large `Content` column. Fixed by **projecting list queries
    to lightweight rows**; rebuilt the dashboard with parallel `Task.WhenAll` counts (scope-per-task);
    added `RequestTimingMiddleware`.

**Acceptance:** Home, Projects, Knowledge, Models load on empty / seeded / MSSQL-only deployments,
each in well under one second. See `Phase-1.2-Release-Notes.md` and `Phase-1.2-Runtime-Audit.md`.
