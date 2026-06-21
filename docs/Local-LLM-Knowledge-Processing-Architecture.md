# Local LLM Knowledge-Processing Architecture

> **The LLM is not the product. Memory is the product. The model is replaceable.** Local models are an
> **optional** processing layer; MSSQL is the authoritative source of truth and the platform runs fully
> without any model present.

## Pipeline

```
Source files / docs / APIs
        ↓
Deterministic extractor          (authoritative; C#/T-SQL/Python symbols, schema, evidence)
        ↓
Raw evidence in MSSQL            (the source of truth)
        ↓
Local LLM proposal generation    (OPTIONAL; qwen2.5-coder:14b / deepseek-r1:14b via Ollama)
        ↓
Proposal record                 (reviewStatus=PENDING_REVIEW, authoritative=false, NOT installed)
        ↓
Human / system review           (approve / reject / edit)
        ↓
Approved knowledge item         (propose-never-overwrite; versioned)
        ↓
Versioned MSSQL memory          (the durable, curated memory)
        ↓
Exportable JSON / Markdown pack
```

## Where each piece lives

| Stage | Implementation |
|---|---|
| Deterministic extractor | the product's C#/Roslyn + T-SQL/ScriptDom + Python extractors (authoritative) |
| Raw evidence | MSSQL (`CodeSymbol`, schema, `KnowledgeItem` raw) |
| LLM proposal | `scripts/ai/extract-workflow-rules-with-local-llm.ps1`, `propose-knowledge-items-with-local-llm.ps1` (Ollama; output → `.tmp-llm-proposals/`, git-ignored) |
| Proposal record | JSON with `reviewStatus=PENDING_REVIEW`, `authoritative=false`, `installedToMssql=false` |
| Review → approve | the existing knowledge approval lifecycle (propose-never-overwrite, `IPermanenceGuard`) |
| Versioned memory | `KnowledgeItem` + `KnowledgeVersion` in MSSQL |
| Export | `scripts/knowledge/export-knowledge-catalog.ps1` |

## What the local LLM is used for (optional, additive)

Summarizing imported docs · extracting workflow roles/states/transitions · proposing SQL schema · proposing
service-layer validations · identifying missing audit controls · comparing code against a rule · producing
test plans · flagging likely production issues · generating human-reviewable fix plans.

## What the local LLM is NEVER used for

- It never **directly overwrites** source knowledge.
- It is never **authoritative** — its output is always a proposal pending review.
- It is never on the **request path** of the core pages (they are MSSQL-only).
- It is never required — the platform degrades gracefully when Ollama/models are absent.

See `Local-LLM-Reasoning-Governance.md` for the governance rules and `reports/LOCAL_LLM_REASONING_PROOF.md`
for the executed proof (mean 90/90-cap, hallucination-refusal verified).
