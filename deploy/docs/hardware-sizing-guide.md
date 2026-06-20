# LocalAIFactory — Hardware Sizing & Local-AI Capacity Guide

LocalAIFactory is **local-first and MSSQL-authoritative**. The core platform runs on modest hardware; GPU
and local-AI components are **optional accelerators**, not requirements. This guide states honestly what runs
where, and where capacity limits begin.

## What needs no GPU (always works on CPU + SQL Server)

These never require a GPU and must keep working in MSSQL-only mode:

- ASP.NET Core MVC web app, auth (Windows auth / RBAC), audit
- MSSQL / SQL Express / LocalDB / external SQL Server
- Coverage & gap reports, structural graph browsing, symbol/dependency/impact exploration
- Base Knowledge (the Professional Base Knowledge Pack) — install, search, browse
- Import pipeline (deterministic C#/T-SQL extraction), benchmark harness

## What benefits from a GPU (optional)

A GPU helps **only** for AI/inference-heavy work:

- Local LLM inference (Ollama) for chat / summarization / AI-assisted code analysis
- Embedding generation for vector retrieval (Qdrant)
- OCR / computer-vision model inference (cheque OCR, document classification)
- Large summarization or batch document-intelligence jobs

If these components are disabled or absent, the platform degrades gracefully (keyword search, deterministic
extraction, MSSQL-only). **GPU is never required to render a page or to use the deterministic engine.**

## Deployment profiles

| Profile | Components | GPU |
|---|---|---|
| **Minimal** | MVC + MSSQL/LocalDB only | none |
| **Standard** | + Qdrant (vectors), optional small local model | optional |
| **Full-AI** | + local LLM (Ollama), OCR/CV inference, batch summarization | recommended |

Supporting infrastructure to plan regardless of profile: **backup/restore** (and restore testing),
**health checks**, **support diagnostics**, Data Protection key-ring persistence, and a benchmark-cache
**cleanup policy** (clones accumulate).

### Container notes

- **Docker (CPU)** — fine for MVC + MSSQL + Qdrant.
- **Docker (GPU)** — needed for containerized LLM/OCR inference (NVIDIA Container Toolkit). Verify driver +
  CUDA/runtime compatibility for the targeted model/runtime; this changes between versions.

## Reference workstation profile (developer / pilot)

Hardware: **Ryzen 7 9800X3D · RTX 5070 Ti · 32 GB RAM · 2 TB SSD.**

**Good for (honest assessment):**
- Development and running the LocalAIFactory web app
- MSSQL / LocalDB / SQL Express
- Qdrant (vectors)
- The controlled benchmark repositories
- The Professional Base Knowledge Pack
- Small/medium local models (e.g. ~7B–14B class, quantized)
- OCR / computer-vision experiments (single-image and small batches)

**Honest limitations:**
- **32 GB RAM** is the main constraint for *large* local LLMs and for running several heavy services at once
  (SQL Server + Qdrant + a large model + OCR). Expect to choose, not run everything large simultaneously.
- Heavy OCR/computer-vision **datasets** need deliberate storage planning; 2 TB fills quickly with image
  corpora plus benchmark clones plus model weights — define a cleanup/retention policy.
- Large or numerous **benchmark repo caches** need periodic cleanup.
- For heavier enterprise local AI, **64 GB+ RAM** is recommended; **96–128 GB** is better for many large
  repositories and larger local models. VRAM (here 16 GB class) bounds the largest model/precision that fits
  on-GPU; bigger models require quantization, CPU offload, or more VRAM.

## Sizing summary

- **Pilot / dev:** the reference profile above is sufficient for everything except large-model and
  large-dataset workloads.
- **Heavier local AI / many large repos:** plan 64–128 GB RAM and storage/VRAM headroom.
- **Production web tier (no local AI):** CPU + SQL Server is enough; size SQL Server for the estate, not the
  app server.

> All figures are planning guidance, not guarantees. Validate against your actual models, datasets, and
> concurrency before committing to a deployment.
