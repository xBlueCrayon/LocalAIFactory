# LLM Knowledge-Enhancement Architecture

> **Status:** Architecture / governance reference.
> **Authoritative source:** `MASTER_VISION.md`. If anything here conflicts with it, the vision wins.

## 1. Purpose and the one rule that governs everything

LocalAIFactory's memory is a **curated, human-approved** knowledge base stored in **MSSQL — the
single source of truth**. A local LLM (Ollama) is an **optional accelerator**. This document
describes how an optional LLM may *propose* improvements to knowledge **without ever becoming
authoritative**.

The single governing rule: **an LLM proposes; a human approves; MSSQL records.** No AI output is
ever written directly to a curated knowledge item. Every automated change is routed through the
existing **propose-never-overwrite** chokepoint (`IPermanenceGuard`) and lands as a
`ProposedRevision` in the review queue.

This is not aspirational — the enforcement primitives already exist in code:

- `IPermanenceGuard.ProposeRevisionAsync(...)` (`src/LocalAIFactory.Core/Abstractions/IPermanenceGuard.cs`)
- `ProposedRevision` entity (`src/LocalAIFactory.Core/Entities/ProposedRevision.cs`)
- `ProvenanceEvent` entity with `ProvenanceMethod` (`src/LocalAIFactory.Core/Entities/ProvenanceEvent.cs`)
- `PermanenceTier { Derived, Curated, Raw }` (`src/LocalAIFactory.Core/Enums/Enums.cs`)

## 2. What "enhancement" means here (and what it does not)

Permitted enhancement *proposals* the LLM may produce:

- **Source-linked summarization** — condense an imported artifact or a verbose curated item into a
  tighter summary, *with a reference back to the source* (`ProvenanceEvent.SourceArtifactId`).
- **Duplicate / near-duplicate detection** — flag two knowledge items that appear to say the same
  thing, and propose a consolidation (`RevisionSource.Consolidation`).
- **Weak-item detection** — flag items that are vague, untitled, contradictory, or unsupported by a
  source, and propose a clearer rewrite (`RevisionSource.Extraction`).
- **Gap suggestions** — surface that a referenced symbol/rule has no curated note, as a *suggestion*,
  never an auto-authored entry.

Explicitly **not** permitted:

- Writing directly to any `Curated` item. `IPermanenceGuard.IsCurated(tier)` gates this.
- Auto-promoting a `ProposedRevision` to `Approved`. Promotion is a human action only.
- Inventing facts, rules, citations, or source references. See §6.
- Treating LLM output as ground truth for retrieval ranking or for downstream prompts.

## 3. The proposal flow (concrete)

```
imported artifact / curated item
        │
        ▼
  optional LLM (Ollama, if reachable)         ──►  if not reachable: skip silently, no error
        │  produces candidate text + confidence
        ▼
  IPermanenceGuard.ProposeRevisionAsync(...)        (never overwrites)
        │  creates:
        ▼
  ProposedRevision { Status = NeedsReview, Source = Extraction|Consolidation }
        │  and a paired:
        ▼
  ProvenanceEvent { Method = Semantic, ExtractorOrModelId = "<model:tag@digest>",
                    Reason = "<prompt id + summary>" }
        │
        ▼
  Human review queue (KE-014)  ──►  Approve → KnowledgeVersion appended, item becomes Curated
                                └─►  Reject  → revision marked Deprecated; provenance retained
```

Key invariants:

- The `ProposedRevision.Status` starts at `KnowledgeStatus.NeedsReview` and **only a human** moves it
  to `Approved`.
- A matching `ProvenanceEvent` with `Method = ProvenanceMethod.Semantic` is written for every
  LLM-originated proposal, so the lineage is explainable and append-only (provenance is never
  mutated or deleted).
- On approval, history is preserved via `KnowledgeVersion`; nothing is silently overwritten.

## 4. AI output provenance and confidence

Every LLM proposal must carry, at minimum:

| Field | Stored in | Meaning |
|-------|-----------|---------|
| Model id + tag + digest | `ProvenanceEvent.ExtractorOrModelId` | exactly which model produced it |
| Method = Semantic | `ProvenanceEvent.Method` | distinguishes AI from `Human` / `Import` / `Deterministic` |
| Source link | `ProvenanceEvent.SourceArtifactId` | the artifact the proposal summarizes (if any) |
| Reason / prompt id | `ProvenanceEvent.Reason` | which governed prompt template was used |
| Confidence | carried in `ChangeReason` / proposal metadata | self-reported; advisory only, never a gate |

Confidence is **advisory**. A high self-reported confidence does **not** shorten or bypass review.
Local models are small and confidence is unreliable; it is used only to *order* the review queue,
never to auto-approve.

## 5. Offline / fallback behaviour (MSSQL-only)

This is a hard requirement, consistent with CLAUDE.md rule #1–#4:

- If Ollama is **not reachable** (the default deployment), enhancement features are simply absent.
  The app loads, curated knowledge serves, retrieval works — all from MSSQL.
- The reachability probe is non-blocking and cached (`IServiceHealthCache` / `HealthMonitorService`).
  No controller or view calls Ollama synchronously on the request path.
- `scripts/ai/check-ollama.ps1` reports reachability and installed models read-only; it **never pulls
  a model** and exits 0 even when Ollama is absent (AI is optional, not a dependency).
- No background job may block waiting on a model. If the model is unavailable mid-batch, the batch
  records "skipped — model unavailable" and proceeds.

## 6. Refusal and limitation policy

The LLM layer must refuse rather than guess:

- **No fabricated citations.** If the model cannot tie a claim to an actual source artifact, it must
  omit the citation and the proposal is flagged "unsourced" for the reviewer — it does not invent a
  `SourceArtifactId`.
- **No new authoritative claims.** The model summarizes and reorganizes existing material; it does
  not introduce banking facts, business rules, or regulatory statements that are absent from the
  source. Such output is rejected before it reaches the queue.
- **Bounded generation.** Proposals are length-capped to keep the local model fast and reviewable.
- **Honest about capability.** There is **no fine-tuned domain model**. The installed general models
  (e.g. `qwen2.5-coder:14b`) have no special knowledge of BDM/MCIB/ChequeXpert/ETAMS. They are text
  assistants, not domain experts. Treat all output as a draft for a human who *does* know the domain.

## 7. Honest limits summary

- Local models are small (14B class) and **optional**; quality is modest and varies by prompt.
- AI is **never** the source of truth and **never** auto-promoted. MSSQL + human approval is.
- Reasoning models (e.g. `deepseek-r1:14b`) spend part of their token budget on hidden `<think>`
  output, reducing usable answer length — see `Prompt-Governance.md`.
- This layer adds *velocity to curation*, not *authority*. Removing it must leave a fully functional
  MSSQL-only platform, and it does.
