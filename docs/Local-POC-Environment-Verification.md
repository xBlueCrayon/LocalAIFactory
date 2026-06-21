# Local POC Environment Verification

**Phase:** R2-ACC-POC-COMPLETE · **Date:** 2026-06-21 · **Host:** `DESKTOP-M1HANKN` (Windows 11 Pro)

Every value below was captured live on this machine. No secrets are exposed.

| # | Check | Evidence |
|---|---|---|
| 1 | Current directory | `D:\AI\Repositories\LocalAIFactory` |
| 2 | Current branch | `ke-008-code-symbols` |
| 3 | Latest commit | `1ccd494` — *FINAL-ENTERPRISE-REASONING: add giant-pattern benchmark and readiness validation* |
| 4 | Working tree status | **clean** (verified before this pass) |
| 5 | .NET SDK | **10.0.301** (`dotnet --version`) |
| 6 | LocalDB availability | **`MSSQLLocalDB`** present; engine version **17.0.4025.3** (`sqllocaldb info MSSQLLocalDB`) |
| 7 | Connection string in use | `Server=(localdb)\MSSQLLocalDB;Database=LocalAIFactory;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True` — from `src/LocalAIFactory.Web/appsettings.json` (`ConnectionStrings:DefaultConnection`). Trusted (Windows) auth; **no credentials in config**. |
| 8 | `LocalAIFactory` DB exists | **YES** (`SELECT DB_ID('LocalAIFactory')` → present) |
| 9 | App connects to LocalDB | **YES** — proven two ways: direct `sqlcmd` queries (see `LocalDB-POC-Evidence.md`) and the running web app serving DB-backed pages (see `HTTP-POC-Evidence.md`). |
| 10 | Ollama available | **YES** — `ollama version 0.30.10`; HTTP API `http://localhost:11434/api/tags` returned **200**. |
| 11 | Ollama models | `qwen2.5-coder:14b` (9.0 GB), `deepseek-r1:14b` (9.0 GB). Configured chat model `qwen2.5-coder:14b` is present. Configured embedding model `nomic-embed-text` is **not pulled** (see `Ollama-Local-AI-POC-Evidence.md`). |
| 12 | Qdrant config | Present and **disabled** (`Qdrant.Enabled=false`, `BaseUrl=http://localhost:6333`, `VectorSize=768`). Vectors are optional; the app runs MSSQL-only. |
| 13 | GPU services optional | **Yes.** The core app requires no GPU. Ollama can use the local GPU (RTX 5070 Ti) when present, but inference is optional and off the request path. |

## What this proves

The configured local environment is real and reachable: .NET 10 SDK, a live LocalDB instance holding
the `LocalAIFactory` database, an optional-but-running Ollama with a code model, and an optional
(disabled) Qdrant. The platform's non-negotiable rule — **works MSSQL-only, external services
optional** — holds on this host: Qdrant is off and the app still serves every page.

## Honesty notes

- `Ollama.Enabled=true` in config, but the **embedding** model (`nomic-embed-text`) is not pulled and
  Qdrant is disabled, so semantic/vector retrieval is **not** active in this POC. Deterministic
  structural retrieval and SQL-backed knowledge search are what is proven. This is by design and is
  not a defect — the platform degrades gracefully.
- No secrets, tokens, or credentials are present in committed configuration (Trusted_Connection only).
