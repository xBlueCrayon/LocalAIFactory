# AI / Agent / RAG Community & Official Notes

Operational notes for LocalAIFactory's AI layer — local model inference (Ollama), vector store
(Qdrant), RAG retrieval, and agent-as-proposal governance — drawn from official docs (Ollama FAQ,
Qdrant installation, NIST AI RMF 1.0) and community failure patterns, mapped to how the repo already
handles each concern.

> **Governing principle (product + NIST AI RMF):** AI output is a **proposal, not truth**. A human
> approves before anything enters curated project memory. Approved knowledge is injected **first** into
> prompt context. The platform must remain fully usable in **MSSQL-only mode with no AI at all**.

---

## 1. Ollama (optional local inference)

**Official behavior (ollama.readthedocs.io FAQ):**
- Serves an HTTP API on `127.0.0.1:11434` by default.
- Runs **fully offline** once models are pulled (`C:\Users\<user>\.ollama\models` on Windows);
  conversation data does not leave the machine.
- `OLLAMA_HOST` changes the bind address; `OLLAMA_ORIGINS` controls CORS.
- "Connection refused" almost always means the server isn't running.

**How LocalAIFactory handles it:**
- Ollama is **optional** and gated behind config (CLAUDE.md rule 2). Default models per CLAUDE.md:
  `qwen2.5-coder:14b` and `nomic-embed-text` (768-dim).
- Health is probed on a background timer (`HealthMonitorService`) and read from a cached snapshot
  (`IServiceHealthCache`) — **never** called synchronously inside a controller action or Razor view
  (CLAUDE.md rule 5). This is the single most important rule for keeping pages from hanging.
- Local-first / offline design aligns with Ollama's offline guarantee — no internet needed once models
  are present.

**Operational notes / recommendations:**
- Verify availability with `scripts/diagnostics/ollama-health-check.ps1` / `scripts/ai/check-ollama.ps1`.
- Keep embedding dimension consistent (768 for `nomic-embed-text`) with the Qdrant collection; a
  dimension mismatch is a classic silent RAG failure.
- Treat model unavailability as a degraded (not failed) state — the app must still render and serve
  MSSQL-backed knowledge.

## 2. Qdrant (optional vector store)

**Official behavior (qdrant.tech installation):**
- Run via Docker: `docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant`; REST on 6333, gRPC on 6334.
- Readiness `/readyz`, health `/healthz`, dashboard at `:6333/dashboard`.
- Requires Docker or a binary to run.

**How LocalAIFactory handles it:**
- Qdrant is **optional and REST-only** (CLAUDE.md). `projectId=0` denotes global knowledge.
- Health cached, never on the request path. When Qdrant is absent the app falls back to MSSQL as the
  primary memory store (CLAUDE.md rule 1) — vector retrieval is an enhancement, not a dependency.

**Operational notes / recommendations:**
- Probe `http://localhost:6333/readyz` (not `/`); start via `scripts/start-qdrant-docker.ps1`.
- Because Qdrant needs Docker, and Docker needs WSL2/virtualization on Windows
  (see `COMMUNITY_FAILURE_PATTERNS.md` #9), keep the "Qdrant unavailable" path first-class so a missing
  Docker install never blocks the product.

## 3. RAG retrieval

**Pattern (community + design):**
- Retrieval quality hinges on consistent embedding model/dimension, sane chunking, and
  curated-first ranking. Pulling large text columns into list views or doing clever group-by
  aggregation is a known hang source in this repo specifically.

**How LocalAIFactory handles it:**
- Approved/curated knowledge is injected **first** into prompt context — the defining product feature.
- CLAUDE.md rule 7 forbids materializing large text columns (`KnowledgeChunk.Content`, etc.) in list
  views; project to lightweight `record` rows. Rule 6 forbids `GroupBy(_ => 1)` aggregation. Both
  directly protect the retrieval/admin pages from the project's historical indefinite hangs.
- MSSQL is the source of truth; the vector store is an accelerator that degrades gracefully.

**Operational notes / recommendations:**
- Keep the embedding model and Qdrant collection dimension in lockstep (768).
- When the vector store is unavailable, fall back to MSSQL keyword/structured retrieval rather than
  failing the request.

## 4. Agent-as-proposal governance (NIST AI RMF mapping)

**Official framing (NIST AI RMF 1.0):** Govern / Map / Measure / Manage, with trustworthiness
characteristics including validity & reliability, accountability & transparency, and **human oversight**.

**How LocalAIFactory maps to it:**
- **Govern / human oversight:** approval lifecycle — AI proposes; a human approves before knowledge,
  rules, or snippets enter curated memory. Nothing AI-generated is treated as authoritative.
- **Manage / propose-never-overwrite:** generated changes are proposals against a sandbox/workspace
  scaffold (Workspaces project, guarded off in Phase 1), not in-place mutations of source of truth.
- **Accountability / traceability:** append-only audit (commit R2-P0B) gives a tamper-evident record of
  who approved what; `RequestTimingMiddleware` provides request-level observability.
- **Validity / reliability:** the benchmark/verification harness (`scripts/poc/verify-poc.ps1`,
  validation scenarios) measures capability rather than assuming it.

**Operational notes / recommendations:**
- Continue to present AI output explicitly as *proposals pending approval* in the UI.
- Keep the approval boundary server-side (consistent with the deny-by-default RBAC + server-side
  project access from R2-P0B) so a client cannot bypass it.

## 5. Offline / MSSQL-only mode

**Design guarantee (CLAUDE.md rules 1–4):** the solution must work with **only SQL Server present** —
no GPU, no internet, no Ollama, no Qdrant. Home, Projects, Knowledge, and Models pages must always
load on an empty DB, a seeded DB, and an MSSQL-only deployment.

**How this aligns with the AI layer:**
- Both AI dependencies (Ollama, Qdrant) are optional, health-cached, and off the request path, so
  their absence degrades features without breaking pages.
- This matches Ollama's and Qdrant's own local/offline operation models and is the correct posture for
  a private banking-estate tool with no outbound internet.

**Verification:**
- `scripts/poc/verify-poc.ps1 -AppUrl http://localhost:<port>` checks core pages return 200.
- `scripts/diagnostics/ollama-health-check.ps1` and a `/readyz` probe to Qdrant confirm the optional
  services independently; the app must pass the page checks **with both stopped**.

---

### Honesty caveats

- Community/social signals describe operational edge cases and are **not** authoritative; the
  governance mapping above is an engineering interpretation of NIST AI RMF, not a compliance attestation.
- No claim of regulatory, financial, or legal certainty is made. The mapping shows that the existing
  controls are *consistent with* recognized AI-risk and secure-development principles, not that they
  certify against them.
