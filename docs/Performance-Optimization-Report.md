# Performance Optimization Report

An honest account of the platform's current performance posture and a **prioritised list of
opportunities**. The opportunities are labelled as such — they are candidate improvements, not
claims of measured speedups. Where a "before/after" is promised, it is a *to-be-measured* commitment,
not a result.

The defining failure mode of this project is the **page hang**, not raw throughput. Most of the
posture below exists to prevent hangs, per the hard-won rules in `CLAUDE.md` §3 and §5–§7.

---

## 1. Current posture (what is already in place)

These are implemented and reflected in the measured timings (`docs/Resource-and-Performance-Evidence.md`):
core pages return well under a second on the validation host, and the full test suite runs in ~1 s.

| Practice | What it does | Source of rule |
|---|---|---|
| **Bounded queries** | List views read a fixed, lightweight shape — never an unbounded full-entity load | CLAUDE.md §7 |
| **Lightweight list projections** | List queries project to a `record` with only the columns the view needs; large text columns (`KnowledgeItem.Content`, `ImportedFile.RawText`, `ChatMessage.Content`, `ProjectProfileSection.Content`, `KnowledgeChunk.Content`) are never materialised in lists | CLAUDE.md §7 |
| **Separate `CountAsync()` calls** | Counts are computed with individual `CountAsync()` rather than aggregate group-by projections | CLAUDE.md §6 |
| **No `GroupBy(_ => 1)`** | Group-by-constant aggregates are banned — they produced indefinite SQL Server page hangs | CLAUDE.md §6 |
| **Cached health snapshot** | Qdrant/Ollama health is read from `IServiceHealthCache`, populated by a background `HealthMonitorService`; no controller or view calls an external service synchronously on the request path | CLAUDE.md §5 |
| **Scope-per-task parallel reads** | Dashboard parallel reads use an `IServiceScopeFactory` scope-per-task pattern already in place — not to be churned | CLAUDE.md §7 |
| **Request timing visibility** | `RequestTimingMiddleware` logs start/complete per request and warns over 1 s, so any stall is immediately locatable | CLAUDE.md §5 |

**Net effect:** the request path does not block on external services, does not materialise large
text in lists, and does not use the aggregation patterns that historically hung. This is a
*hang-avoidance* posture first and a throughput posture second.

---

## 2. Prioritised optimisation opportunities

Each item below is an **opportunity**, not a delivered result. None has a measured before/after yet;
the "Proof to close" column states exactly what evidence would justify the change.

### Priority 1 — Index review

- **What:** Audit indexes on the hot query paths (project listing, knowledge listing/approval,
  audit queries, coverage/gap reporting) against the actual `WHERE`/`ORDER BY`/join columns.
- **Why:** As imported data grows, missing covering indexes are the most likely cause of slow lists.
- **Risk:** Additive (indexes only); must remain a non-destructive, additive migration (CLAUDE.md §6).
- **Proof to close:** query plans before/after on a representative dataset, plus page timings via
  `performance-profile.ps1` and `RequestTimingMiddleware` logs.

### Priority 2 — N+1 review on detail/list pages

- **What:** Walk the project, knowledge, and audit views for per-row follow-up queries (N+1) and
  fold them into the projection or a single batched query.
- **Why:** N+1 patterns scale poorly with row count and are invisible at small data sizes.
- **Risk:** Low; query-shape changes only, must keep the lightweight-projection rule (CLAUDE.md §7).
- **Proof to close:** query count per page before/after (SQL profiler or EF logging) + page timings.

### Priority 3 — Benchmark clone caching

- **What:** Cache or reuse cloned repositories in the benchmark harness instead of re-cloning per run.
- **Why:** Repeated clones dominate wall-clock in repeated benchmark/load runs (`load-smoke.ps1`).
- **Risk:** Low; affects tooling, not the product request path.
- **Proof to close:** `load-smoke.ps1` average run time before/after across N iterations.

### Priority 4 — Graph/profile rebuild scope

- **What:** Scope profile/graph rebuilds to the changed project (incremental) rather than rebuilding
  broadly on import.
- **Why:** Full rebuilds are wasted work when only one project changed.
- **Risk:** Medium; correctness of incremental invalidation must be proven so stale state never ships.
- **Proof to close:** rebuild time before/after on a multi-project dataset, plus a correctness check
  that incremental output matches a full rebuild.

### Priority 5 — UI loading indicators

- **What:** Add explicit loading/spinner states on any action that can legitimately take time
  (large import, model call), so a slow-but-working action is not mistaken for a hang.
- **Why:** Perceived performance and supportability — distinguishes "working" from "stuck".
- **Risk:** Low; UI only. Do **not** redesign the existing UI (CLAUDE.md §7) — add indicators only.
- **Proof to close:** manual verification that long actions show progress and the page never appears
  frozen; core-page smoke timings unchanged.

---

## 3. Measurement method (how a before/after would be captured)

Any claimed speedup must come with reproducible evidence using the existing harness:

```powershell
# Build / test / benchmark wall-clock baseline
pwsh scripts/diagnostics/performance-profile.ps1

# Repeated bounded load
pwsh scripts/tests/load-smoke.ps1 -Iterations 5

# Per-request timings while exercising core pages (read RequestTimingMiddleware logs)
curl -s -o NUL -w "%{http_code} %{time_total}s`n" http://localhost:5000/
curl -s -o NUL -w "%{http_code} %{time_total}s`n" http://localhost:5000/Projects
curl -s -o NUL -w "%{http_code} %{time_total}s`n" http://localhost:5000/Knowledge
curl -s -o NUL -w "%{http_code} %{time_total}s`n" http://localhost:5000/Models
```

Capture the same readings before and after a change on the **same host** and dataset. A change
without a measured before/after is an opportunity, not an improvement.

---

## 4. Explicit non-goals during stabilisation

- No risky DI changes or `IDbContextFactory` churn to "optimise" the dashboard (CLAUDE.md §7).
- No reintroduction of `GroupBy(_ => 1)` or blocking external calls for the sake of a micro-optimisation.
- No UI redesign — the existing card/table UI and toolbars stay (CLAUDE.md §7).
