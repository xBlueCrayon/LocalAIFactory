# Local AI Model Registry

> **Status:** Architecture / governance reference.
> **Authoritative source:** `MASTER_VISION.md`.

## 1. Purpose

The platform already persists model definitions in the **`ModelConfiguration`** entity
(`src/LocalAIFactory.Core/Entities/ModelConfiguration.cs`) and reads optional Ollama/Qdrant settings
from `appsettings`. This document describes a thin **model registry** concept layered over that
entity: a curated, health-checked inventory of the local models a deployment is *allowed* to use,
with capability flags so the runtime can route a task to a model that can actually do it.

The registry is **descriptive and optional**. With no Ollama present the platform runs MSSQL-only;
the registry is simply empty of reachable models and the AI features are absent.

## 2. What `ModelConfiguration` already gives us

```csharp
public class ModelConfiguration
{
    public int Id;
    public string Name;                         // human label, e.g. "Local coder 14B"
    public ModelProvider Provider;              // Ollama | OpenAiCompatible | OpenAi | Claude
    public string ModelName;                    // e.g. "qwen2.5-coder:14b"
    public string BaseUrl;                      // e.g. http://localhost:11434
    public string? ApiKeyEncrypted;             // Data-Protection encrypted; null for local Ollama
    public double Temperature = 0.2;
    public int MaxTokens = 2048;
    public int ContextWindowHint = 8192;
    public string? EmbeddingModel;              // e.g. "nomic-embed-text"
    public bool IsEnabled;
    public bool IsDefault;
    public DateTime CreatedUtc; UpdatedUtc;
}
```

This is the storage backbone. The registry concept adds three things on top, none of which require a
schema migration to *describe* (any new persisted fields are out of scope here and would need
approval per CLAUDE.md §6): **capability flags**, **health/version metadata**, and a **pull policy**.

## 3. Capability flags

Each registered model is tagged with the capabilities it actually supports, so routing never asks a
model to do something it cannot:

| Flag | Meaning | Example model |
|------|---------|---------------|
| `chat` | general conversational completion | qwen2.5-coder:14b |
| `code` | code understanding / generation | qwen2.5-coder:14b |
| `embeddings` | produces vectors (768-dim for `nomic-embed-text`) | nomic-embed-text |
| `reasoning` | emits hidden `<think>` chain-of-thought before the answer | deepseek-r1:14b |

Notes:

- A model can hold several flags. A reasoning model is still a chat model — but see the
  reasoning caveat in §6 and in `Prompt-Governance.md`.
- Embedding models and chat models are distinct roles; `ModelConfiguration.EmbeddingModel` already
  separates the embedding model from the chat `ModelName`.

## 4. Health check

The registry's "is this model usable right now?" signal comes from the existing read-only probe:

- `scripts/ai/check-ollama.ps1` — reports whether Ollama is reachable and lists installed models with
  their sizes. Exit 0 even when Ollama is absent (AI is optional). **Never pulls a model.**
- `scripts/ai/test-installed-model.ps1` — runs one tiny bounded generation against an *installed*
  model to prove the end-to-end path. Defaults to a non-reasoning model so the token budget is not
  spent on `<think>`.

At runtime, reachability is surfaced through the cached health snapshot
(`HealthMonitorService` / `IServiceHealthCache`) — never probed synchronously on the request path.

## 5. Worked example — the two installed models (live-verified)

Verified live on an **RTX 5070 Ti (16 GB VRAM)**. Both 14B models are installed and ~8.4 GB each, so
they fit comfortably in 16 GB. A tiny "reply OK" inference returned successfully in ~10 s.

| Name | ModelName | Size | Capabilities | Notes |
|------|-----------|------|--------------|-------|
| Local coder 14B | `qwen2.5-coder:14b` | ~8.4 GB | chat, code | preferred for code/summarization proposals |
| Local reasoner 14B | `deepseek-r1:14b` | ~8.4 GB | chat, reasoning | **reasoning model** — emits hidden `<think>` tokens |
| Embeddings | `nomic-embed-text` | small | embeddings | 768-dim vectors (used when Qdrant is enabled) |

Version / digest: record each model's Ollama **tag and digest** (from `/api/tags`) so a proposal's
`ProvenanceEvent.ExtractorOrModelId` names the *exact* build that produced it. Two pulls of the same
tag can differ; the digest disambiguates.

## 6. GPU / CPU notes

- 14B-class models at Q4-ish quantization (~8.4 GB) fit on a 16 GB GPU with room for context.
- These models also run CPU-only, but much slower; the optional-accelerator framing still holds —
  if it is too slow on a given box, disable it and run MSSQL-only.
- **Reasoning caveat:** `deepseek-r1:14b` spends part of its output budget on hidden `<think>`
  reasoning. Set a higher `num_predict` / `MaxTokens` for it, or prefer the coder model for short
  structured proposals. The test script deliberately defaults away from reasoning models.

## 7. "Do not pull large models" policy

- The registry **does not** auto-download models. Both helper scripts are explicitly no-pull.
- Adding a model to the registry is a deliberate human act: install it via Ollama yourself, confirm
  it fits in available VRAM, then register a `ModelConfiguration` row pointing at it.
- Do not pull models larger than what the target GPU can hold; a swapping/oversized model defeats the
  "fast optional accelerator" purpose and risks request-path stalls.
- No remote/cloud model is enabled by default. `Provider` values `OpenAi` / `Claude` exist in the
  enum but require explicit configuration and an encrypted key; local-first is the default posture.

## 8. Honest limits

- The registry tells you what is *installed and reachable*, not what is *good*. Capability flags are
  declared, not benchmarked.
- There is **no fine-tuned domain model**; all listed models are general-purpose.
- Everything here is optional. An empty registry is a valid, fully-supported state.
