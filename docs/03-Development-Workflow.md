# 03 — Development Workflow (with Claude Code)

## Golden path

1. Pull latest; create a feature branch.
2. Read `CLAUDE.md` and the relevant `docs/`.
3. Make the smallest change that satisfies the task.
4. **Build:** `dotnet build LocalAIFactory.sln -c Release` (or `./scripts/build.ps1`).
5. **Runtime-validate:** `./scripts/verify.ps1` — the four core pages must return 200 in under a
   second. Never ship a change that can hang a page.
6. Commit with a descriptive message; open a PR.

## Non-negotiable engineering rules

- **MSSQL-only must keep working.** No page may depend on an external service to render.
- **No blocking external calls on the request path.** Use the cached `IServiceHealthCache`.
- **Prefer separate `CountAsync()` calls. Never `GroupBy(_ => 1)`** or group-by-constant aggregates —
  they have caused indefinite hangs.
- **Project list views to lightweight rows.** Never load `Content`/`RawText`-style columns for lists.
- **Qdrant/Ollama are optional and gated** behind config flags; features degrade gracefully.
- **Always build before packaging or claiming done.**
- **No new features / no redesigns** during stabilization or packaging tasks.

## EF Core query guidance

- Counts: `await _db.X.CountAsync(predicate, ct)` → `SELECT COUNT(*) … WHERE …`.
- Lists: `…Select(x => new XRow(x.A, x.B)).ToListAsync(ct)` — select only what the view needs.
- Use `AsNoTracking()` for read-only queries. Bound results with `Take(n)`.
- For parallel reads, use a scope-per-task (`IServiceScopeFactory`) so each query gets its own
  `AppDbContext`. Do not run concurrent queries on one context.

## Migrations

See `docs/02-Setup.md` and `CLAUDE.md §6`. Schema changes must be **additive** and **approved**;
stabilization work is schema-frozen. Regenerate the model snapshot via EF, never by hand.

## PR checklist

- [ ] Builds in Release.
- [ ] `verify.ps1` passes (or core pages confirmed manually).
- [ ] No `GroupBy(_ => 1)`, no full-entity list loads of large columns.
- [ ] No synchronous Qdrant/Ollama calls added to actions/views.
- [ ] Works with Ollama/Qdrant disabled.
- [ ] No secrets, keys, DB files, or artifacts committed.
