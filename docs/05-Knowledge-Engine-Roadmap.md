# 05 — Knowledge Engine Roadmap

The knowledge engine is the heart of LocalAIFactory: a curated, persistent memory that improves over
time. This roadmap covers what exists and what is planned. **No work here should break MSSQL-only
operation or the approval lifecycle.**

## In place today

- **Approval lifecycle:** Draft → Approved → Deprecated → NeedsReview for knowledge items, business
  rules, and code snippets.
- **Priority injection:** approved items first; project-specific overrides generic.
- **Retrieval:** MSSQL keyword search by default; optional Qdrant vectors when both vector flags are on.
- **Ingestion:** ZIP import → extract code/SQL/docs → profile → create knowledge/candidates.
- **Bulk curation UI:** select-all, multi-select, bulk approve/deprecate/delete on the Knowledge page.

## Planned (Phase 2 — Knowledge Engine)

Prioritized from the solution audit:

**High**
- **Code-candidate promotion UI** — review extracted code blocks (NeedsReview) and promote to approved
  snippets.
- **ProjectProfile UI** — surface and curate generated project profiles/sections.
- **Bind `WorkspacesOptions` to configuration** consistently.

**Medium**
- **Vector cleanup on deprecate/delete** — when a knowledge item is deprecated or deleted, remove its
  vectors from Qdrant so retrieval stays consistent.
- **Raise the multipart/import ceiling** for very large BDM ZIPs (currently bounded).

**Lower**
- **In-flight job recovery** beyond simple requeue on restart.
- **Optional full-text indexing** for reliable large-scale content search (today the list search is a
  bounded `LIKE`).

## Guardrails

- Every new retrieval/curation feature must degrade gracefully without Qdrant/Ollama.
- Promotion actions must write through the existing approval lifecycle and audit log.
- No destructive schema or data changes without approval; prefer additive migrations.
