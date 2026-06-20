# Phase 2 — Knowledge Engine (forward brief / Claude Code prompt)

> **Status: SUPERSEDED.** This punch-list is replaced by `docs/Phase-2-Execution-Backlog.md`
> (KE-001…KE-032). Items 1 (code-candidate promotion UI) and 2 (ProjectProfile UI) are already shipped;
> item 3 (vector cleanup) = KE-020; item 4 (import ceiling) = KE-007. Follow the backlog, not this file.

**Objective:** evolve the curated memory into a richer knowledge engine **without** breaking
MSSQL-only operation or the approval lifecycle, and **without** destructive schema changes.

**Work items (prioritized):**
1. **Code-candidate promotion UI** — list extracted code blocks (NeedsReview) and promote to approved
   snippets through the existing lifecycle + audit log.
2. **ProjectProfile UI** — surface generated project profiles/sections for review and curation.
3. **Vector cleanup on deprecate/delete** — remove Qdrant vectors for deprecated/deleted knowledge so
   retrieval stays consistent (no-op when vectors are disabled).
4. **Larger import ceiling** — raise the multipart limit for very large BDM ZIPs; keep imports
   resumable.
5. **In-flight job recovery** — improve beyond simple requeue.
6. *(Optional)* full-text indexing for reliable large-scale content search.

**Rules for the agent:**
- Build before claiming done; run `./scripts/verify.ps1` — the four core pages must stay fast.
- No `GroupBy(_ => 1)`; project all list views; no synchronous external calls on render.
- Schema changes must be additive and approved; regenerate the model snapshot via EF.
- Every promotion writes through the approval lifecycle and audit log.
