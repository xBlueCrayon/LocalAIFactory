# Ollama / Local AI POC Evidence

**Phase:** R2-ACC-POC-COMPLETE · **Date:** 2026-06-21 · **Host:** `DESKTOP-M1HANKN`

Captured live and safely. No models were pulled; no large downloads; no prompts or secrets exposed.

## 1–3. Availability and models

| Check | Evidence |
|---|---|
| Ollama service reachable | **YES** — `ollama version 0.30.10`; HTTP `GET http://localhost:11434/api/tags` → **200** |
| Model list works | **YES** — `ollama list` succeeded |
| Available models | `qwen2.5-coder:14b` (9.0 GB), `deepseek-r1:14b` (9.0 GB) |

## 4–5. Configuration and whether the POC requires Ollama

From `src/LocalAIFactory.Web/appsettings.json`:

```
Ollama: { Enabled: true, BaseUrl: http://localhost:11434,
          DefaultModel: qwen2.5-coder:14b, EmbeddingModel: nomic-embed-text }
Qdrant: { Enabled: false, VectorSize: 768 }
```

- The configured **chat/code model `qwen2.5-coder:14b` is present**.
- The configured **embedding model `nomic-embed-text` is NOT pulled**, and **Qdrant is disabled**, so
  vector/semantic retrieval is **not active** in this POC.
- **The POC does not require Ollama.** Every proof in this phase — build, tests, benchmark, LocalDB
  knowledge base, and all HTTP pages/searches — passed with Ollama off the critical path. Local AI is
  **optional and additive**, consistent with the architecture rule "Ollama is optional; degrade
  gracefully when absent."

## 6. How Ollama fits (additive, never authoritative)

| Use | Role | Status |
|---|---|---|
| Local summarization / drafting | Propose knowledge or summaries for **human approval** | Available (model present); not on the request path |
| Local embeddings | Populate Qdrant vectors for semantic search | **Not active** — `nomic-embed-text` not pulled, Qdrant disabled |
| OCR / computer vision | Future document-intelligence work | Not implemented (prototypes only; no trained CV model) |
| AI-assisted analysis | Assist code/impact reasoning | Optional; deterministic graph is the authority |

## 7. Limitations (honest)

- **Ollama does not replace deterministic evidence.** The benchmark harness (graph/impact/POV) and the
  SQL-backed knowledge base are the authoritative proofs; AI output is a **proposal** that must be
  validated and human-approved (propose-never-overwrite).
- **Embeddings/semantic retrieval are off** in this POC (embedding model not pulled, Qdrant disabled).
  The knowledge search proven over HTTP is **structural/SQL**, not vector similarity.
- **GPU is optional** for the core app; only Ollama inference would use it.
- No new models were downloaded for this evidence (per the safety constraint).

## Reproduce

```powershell
ollama --version
ollama list
(Invoke-WebRequest -UseBasicParsing http://localhost:11434/api/tags).StatusCode   # expect 200
```
