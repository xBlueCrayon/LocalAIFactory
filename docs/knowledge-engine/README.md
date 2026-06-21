# Knowledge Engine

**Authoritative current status:** [`docs/reports/CURRENT_STATUS.md`](../reports/CURRENT_STATUS.md)

The knowledge engine is LocalAIFactory's defining feature: a **persistent, curated project memory
with an approval lifecycle**. Approved knowledge, business rules, and code patterns are injected
**first** into the prompt context, so local models reason against vetted, attributed material rather
than from scratch.

## What it is

- A set of **default knowledge packs** shipped in the repo under `knowledge-packs/`, installed by
  default into the application.
- Each pack is **source-only JSON**: a manifest, one or more category files of knowledge items, and
  a source registry for attribution.
- Every item carries an explicit **limitation note**, a **confidence score**, **tags**, and a
  globally unique **UID**.
- Packs flow through an **approval lifecycle** (`reviewStatus`) before their content is treated as
  approved memory.

## Current state (verified this sprint)

- **20 packs / 852 items**, **852 distinct UIDs**, **no collisions**.
- `verify-all-knowledge-packs` → **PASS**; `security-audit` → **PASS**.
- Catalogued and used by the generator (see the knowledge-usage report).

See [`KNOWLEDGE_PACKS.md`](KNOWLEDGE_PACKS.md) for the pack format and authoring rules,
[`DEFAULT_KNOWLEDGE_CATALOG.md`](DEFAULT_KNOWLEDGE_CATALOG.md) for the full 20-pack catalog, and
[`GENERATOR_KNOWLEDGE_USAGE.md`](GENERATOR_KNOWLEDGE_USAGE.md) for how the generator maps packs to
modules.

## How it works (high level)

1. **Author / extend a pack** under `knowledge-packs/<pack-id>/` (manifest + category JSON +
   source registry). Each item gets a UID, limitation note, confidence, and tags.
2. **Validate** the pack: schema, UID uniqueness (no collisions across all packs), limitation-note
   presence, and source attribution. The `verify-all-knowledge-packs` check enforces this across all
   20 packs / 852 items.
3. **Approve** the pack (`reviewStatus: "Approved"`) so its items are eligible as injected memory.
4. **Inject first:** at prompt-build time, approved knowledge is placed at the front of the model
   context, ahead of the user request and retrieved code, so the model is grounded in vetted
   material.
5. **Generate / assist:** the product generator catalogues the relevant ERP packs and maps each
   generated module to the knowledge category that governs it; modules without supporting knowledge
   are not emitted.

## Design rules

- **Original summaries only.** No proprietary/official standard text is reproduced verbatim;
  research-derived items are attributed to **source families** in the source registry and flagged
  "verification required."
- **Honest limitations.** Every item states what it does **not** assert (no compliance claim, no
  financial advice, no guaranteed OCR accuracy, etc.).
- **Local-first.** Packs are plain JSON in the repo and work in MSSQL-only mode — no internet,
  Qdrant, or Ollama required to read or validate them.
