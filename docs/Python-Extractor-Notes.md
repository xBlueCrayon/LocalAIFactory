# Python Structural Extractor — Notes & Limitations (R2-ACC-CAP3)

LocalAIFactory now has a **deterministic, dependency-free Python structural extractor** (`PythonSymbolExtractor`).
It is **pure C#** — no Python runtime, no `ast`, no network — implemented as an indentation-aware line parser.
Python is now a **supported** language in coverage/gap reporting (moved out of the "unsupported" set).

## What it extracts (deterministic)

- **Classes** (`class X:`) and their **methods**, with containment (method → class).
- **Functions** and **async functions** (`def` / `async def`) — async is recorded in the signature.
- **FastAPI-style routes** — a `@app.get("/path")` / `@router.post(...)` decorator above a function is captured
  into the function's signature (e.g. `[GET /health]`).
- **Python↔SQL bridge** — SQL objects named in string literals (`"... FROM dbo.Invoices ..."`, `EXEC ...`)
  become `AccessesSql` references, resolved to SQL symbols exactly like the C# bridge, so impact flows both
  directions (Python→SQL and SQL→Python). Proven by the `python-bridge` benchmark fixture (Gold, 4/4).

## Honest limitations

- **Syntactic, not semantic.** No type inference, no call-graph resolution, no name binding. It recovers
  declared structure and string-evidenced SQL access — not Python call edges between functions.
- **Indentation-based.** Assumes conventional indentation; deeply unconventional or heavily mixed tabs/spaces
  may under-parse. Malformed Python never throws — it yields the structure it can (best-effort).
- **No module/import graph.** `import` / `from … import` lines are recognized and skipped; they are not yet
  modelled as symbols or edges. Cross-module Python references are not resolved.
- **Functions map to the `Method` kind.** The language-neutral `CodeSymbolKind` has no distinct "function"
  kind; top-level functions are stored as `Method` (with no parent) — a labelling simplification, not a bug.
- **SQL detection is string-evidence only.** Dynamic/parameterised SQL, ORM query builders, and external
  objects produce no edge (never fabricated). Confidence is below 1.0 by design.
- **Decorators other than routes** are recognized but not modelled.

## Where it runs

Registered in DI alongside the C# extractor; the ingestion pipeline, single-file import, project consolidation,
and the benchmark harness all route Python artifacts through the shared code-symbol store. Coverage/gap reports
count Python files as **extracted** (with symbols) or **no-symbols**, never silently "unsupported".

## Next improvements

Import/module graph, function-call edges (where statically resolvable), distinct function kind, and a pinned
public Python repo promoted into the Extended benchmark suite (currently gap-only candidates in
`benchmarks/repo-candidates.json`).
