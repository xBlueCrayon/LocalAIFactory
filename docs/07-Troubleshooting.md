# 07 — Troubleshooting

## Diagnosing a slow or hung page

`RequestTimingMiddleware` logs every request:

```
-> GET /Knowledge started.
<- GET /Knowledge 200 in 42 ms.
```

A **"started" line with no matching "completed" line** means that endpoint stalled — that is where to
look. The dashboard additionally logs `Dashboard build started/completed in {ms} ms`.

## Page-hang history (and the lessons baked into the rules)

This project hit three *distinct* runtime hangs. Each fix is now a rule in `CLAUDE.md`.

1. **Blocking external health call.** A controller/dashboard called Qdrant health synchronously on
   render. **Fix:** read health from a cached snapshot (`IServiceHealthCache`); a background monitor
   probes with a 1s bound. **Rule:** no synchronous external calls on the request path.
2. **`GroupBy(_ => 1)` aggregate.** The dashboard projected several `Count(predicate)` values through a
   group-by-constant; on SQL Server this produced an indefinite hang with no exception. **Fix:** plain
   `CountAsync()` calls. **Rule:** never use `GroupBy(_ => 1)`.
3. **Full-entity list materialization (the real `/Home` + `/Knowledge` hang).** List views loaded full
   `KnowledgeItem` entities including the `nvarchar(max)` `Content` column. Materializing hundreds of
   large rows is a client-side memory operation **not bounded by the SQL command timeout**, so the
   symptom was an indefinite stall with no exception. `/Projects` and `/Models` were fine because they
   load small entities. **Fix:** project list queries to lightweight rows (e.g. `KnowledgeListRow`).
   **Rule:** never materialize large text columns in list views.

If a page hangs again, check in this order: (a) a new synchronous external call, (b) a `GroupBy`
aggregate, (c) a list query loading a large-column entity without a projection.

## "Works only when SQL Server is up"

That is by design — MSSQL is the primary memory store. Confirm the connection string
(`ConnectionStrings:DefaultConnection`) points at a reachable instance and that
`MultipleActiveResultSets=true` is present.

## Database / migration issues

- **Login/permission errors on startup:** the app migrates and seeds on start; the SQL identity needs
  rights to create/alter the database, or pre-apply with
  `dotnet ef database update --project src/LocalAIFactory.Data --startup-project src/LocalAIFactory.Web`.
- **`dotnet ef` not found:** `dotnet tool install --global dotnet-ef` (or run `./scripts/migrate.ps1`,
  which installs it).
- **Migrations seem missing:** they live in `src/LocalAIFactory.Data/Migrations`; the EF command must
  use `--project src/LocalAIFactory.Data` and `--startup-project src/LocalAIFactory.Web`.

## Turning AI features off

- **Disable Qdrant:** `Qdrant.Enabled=false` (default). Retrieval falls back to MSSQL keyword search.
- **Disable Ollama:** `Ollama.Enabled=false`. Chat/embeddings unavailable; all pages still load.
- **Minimal mode:** disable both plus `Rag.UseVectorSearch=false`.
- Re-enabling either service requires **no code change** — flip the flags and restart.

## Stored API keys unreadable after a move/recycle

API keys are encrypted with Data Protection keyed to the `keys/` folder under the content root. If
that folder is lost or differs across instances, previously stored keys can't be decrypted — re-enter
them, or persist Data Protection keys to a shared/secured location.
