# LAF-Generated ERP V2 — Fix Loop

**Date:** 2026-06-21 · **Harness:** `scripts/generator/run-laf-generated-erp-fix-loop.ps1`
**Status artifact:** `benchmarks/results/laf-erp-v2-fix-loop-status.json`

## Principle

When the generated product fails, the fix is applied to the **generator/templates** and the product is
**re-emitted** — never hand-edited into the generated product. This keeps generation autonomy at 100%.

## Failures found and fixed (all fixed in the generator, then re-emitted)

| # | Failure | Root cause (in the generator) | Fix | Attribution |
|---|---|---|---|---|
| 1 | Web build error: catalog endpoints spliced into the `/health` MapGet | the `__CATALOG_ENDPOINTS__` marker was injected mid-statement in the template | moved the marker to its own line before the health statement | `LAF_GENERATED_THEN_FIXED_BY_LAF` |
| 2 | App startup crash: "Failure to infer one or more parameters" on catalog POST | minimal-API couldn't bind the open-generic `CatalogCrudService<T>` vs the body | generator now emits `[FromServices]` + `[FromBody]` on catalog POST params | `LAF_GENERATED_THEN_FIXED_BY_LAF` |
| 3 | `/Catalog` page returned HTTP 500 in Playwright | catalog controller action/view route mismatch (view under `Views/Home`, nav to `/Home/Catalog`) | generator now emits `CatalogController.Index` → `Views/Catalog/Index.cshtml`, nav `/Catalog` | `LAF_GENERATED_THEN_FIXED_BY_LAF` |

## Manual operator action

- **Generator/template edits:** 3 (each re-runs generation).
- **Hand edits to generated product source:** **0.** Every product file is emitted by the generator.

## Final loop result

```json
{ "finalStatus": "GREEN", "iterations": [ { "iteration": 1, "generate": "ok", "build": "ok", "test": "ok" } ] }
```

After the three generator fixes, a clean re-generation builds, passes 82 .NET tests + 13 Playwright tests,
and runs with 0 HTTP 500s — **GREEN on the first iteration of the final loop**.

## Before/after

| | Before fixes | After fixes |
|---|---|---|
| Web build | fail (5 errors) | 0 errors |
| App startup | crash | runs |
| .NET tests | (blocked) | 82 pass |
| Playwright | 12/13 (Catalog 500) | 13/13 pass |
