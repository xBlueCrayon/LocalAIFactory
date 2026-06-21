# Offline / MSSQL-Only Mode Guide — LocalAIFactory

How LocalAIFactory runs **air-gapped** — with **only SQL Server present**, no internet, no Ollama, no
Qdrant, no GPU. This is the baseline the product guarantees: the system of record works standalone,
and every external service is optional and degrades gracefully.

> Non-negotiable rule (CLAUDE.md §3): MSSQL is the primary memory store and must work standalone;
> Qdrant and Ollama are optional and gated behind config flags; no page may depend on an external
> service to render.

---

## 1. The guarantee

- **MSSQL is the source of truth.** The full system of record runs on SQL Server alone.
- **No internet is required.** Air-gapped deployment is supported
  ([`Hardware-Profiles.md`](Hardware-Profiles.md) §1).
- **Ollama, Qdrant, and a GPU are optional.** When absent, the paths that use them detect the missing
  service from a **cached health snapshot** (`IServiceHealthCache`) and stay disabled — they never
  block a request or a page render.
- **The four core pages always load** — Home, Projects, Knowledge, Models — on an empty DB, a seeded
  DB, or in MSSQL-only mode.

---

## 2. What works without internet / AI

Everything in the proven core:

- **Repository import** and deterministic **C# / T-SQL / Python** structural understanding
  (`CodeSymbol`/`CodeEdge`), reference resolution, **bidirectional impact analysis**.
- **C#↔SQL bridge** — "what code touches `dbo.X`", blast-radius queries.
- **Coverage / gap reporting** per import (no silent blind spots).
- **Curated knowledge base** with the approval lifecycle: all **4 packs (438 items)** auto-seed at
  startup, idempotently, with no network access ([`FINAL_KNOWLEDGE_BASE_GUIDE.md`](FINAL_KNOWLEDGE_BASE_GUIDE.md)).
- **Windows-auth RBAC + append-only audit**, IDOR guard, secrets encrypted at rest via Data
  Protection.
- **Supportability** — the read-only `/Support` dashboard (it reads the cached health snapshot, so it
  loads even when Ollama/Qdrant are down or absent).
- **Backup / restore**, **deterministic PDF/cheque-triage prototypes** (CPU-only), and the
  **benchmark harness** (`--inmemory`).

---

## 3. What degrades (and how)

| Capability | Without the optional service | Behaviour |
|---|---|---|
| **Local AI chat** | No Ollama | No AI outputs; the chat paths are disabled. No proposals to approve. |
| **Embeddings** | No Ollama | Semantic embedding generation is unavailable. |
| **Vector / semantic retrieval** | No Qdrant | Vector search is off; retrieval falls back to the MSSQL-backed paths. |
| **OCR / CV field reading & signature analysis** | No Python CV service / GPU | DTO fields are `null` / "not assessed"; the deterministic cheque-risk engine raises flags and forces human review ([`OCR-CNN-Document-Intelligence-Status.md`](OCR-CNN-Document-Intelligence-Status.md)). |
| **Inference throughput** | No GPU | Any optional local model runs CPU-only and is slow ([`Hardware-Profiles.md`](Hardware-Profiles.md) §2). |

Crucially, AI is an **optional accelerator for curation velocity, never an authority**. Removing the
AI layer entirely leaves a fully functional MSSQL-only platform; AI quality is never on the critical
path for the system of record ([`AI-Governance.md`](AI-Governance.md),
[`AI-Output-Provenance-and-Approval.md`](AI-Output-Provenance-and-Approval.md) §6).

---

## 4. Configuration for offline mode

Use any SQL engine example; set the optional services off (the examples already default to this):

```jsonc
// appsettings (Production/Development)
{
  "Qdrant":  { "Enabled": false },
  "Ollama":  { "Enabled": false },
  "Security": { "UseDevAuth": false }   // real Windows auth on a pilot/production host
}
```

- `Qdrant.Enabled=false` and `Ollama.Enabled=false` keep the optional services off.
- The app reads service health from the cached snapshot; it never calls Qdrant/Ollama synchronously
  on the request path (CLAUDE.md §5).

---

## 5. Air-gapped install considerations

- **Have the binaries and packs locally.** The deployable package already contains `app/`,
  `database/`, `knowledge-packs/`, and the appsettings examples — no download is needed at install
  time ([`Customer-Handover-Package.md`](Customer-Handover-Package.md)).
- **No model weights are bundled.** Optional Ollama models are obtained by the customer under their
  own terms; in a true air-gapped deployment you simply leave AI off, or pre-stage model files on the
  host before enabling Ollama.
- **Knowledge packs seed locally.** Pack install reads the JSON packs from disk and writes to MSSQL —
  no network call. All 438 items seed offline.
- **Data Protection keys** live in `./keys` on the host; preserve them across restarts. Nothing is
  phoned home — the edition/license skeleton is demo-safe with no DRM or phone-home.

---

## 6. Verify offline

All verification gates are local and read-only:

```powershell
pwsh database/verify-knowledge-base.ps1 -ServerInstance "<your-instance>" -Database "LocalAIFactory"
pwsh scripts/release/post-install-healthcheck.ps1 -Url "http://localhost:5000"
pwsh scripts/diagnostics/system-snapshot.ps1   # CPU/RAM/disk/GPU snapshot (GPU optional)
```

The health check GETs the core pages and asserts they render without any external service. If a page
ever hangs waiting on Qdrant/Ollama, that is a defect against CLAUDE.md §5 — see
[`07-Troubleshooting.md`](07-Troubleshooting.md).

---

## 7. Honest limits

- **MSSQL-only mode is the guaranteed baseline**, but multi-user concurrency and large-repo import
  memory behaviour are **unmeasured** ([`Known-Limitations.md`](Known-Limitations.md),
  [`Hardware-Profiles.md`](Hardware-Profiles.md) §5).
- Turning AI off does not reduce security or correctness of the system of record — it only removes the
  optional acceleration.
