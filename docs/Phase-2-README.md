# Phase 2 — Knowledge Engine (authoritative index)

This is the entry point for **Phase 2 — Understanding: the memory becomes structured** (MASTER_VISION §16).
Read the three documents below **in order**; they are the source of truth for Phase 2 work.

## Read order

1. **`Phase-2-Knowledge-Engine-Design.md`** — the complete design: entity model, relationships, knowledge
   types, scopes, fix/learning records, graph, vectors, approval, retrieval, consolidation.
2. **`Phase-2-Refinements-and-Alignment.md`** — the depth pass: the three-tier permanence model and
   propose-never-overwrite rule, identity/dedup, the quality trust band, deterministic retrieval, cold-start
   behavior, and the surgical MASTER_VISION clarifications (now applied via KE-001).
3. **`Phase-2-Execution-Backlog.md`** — the execution source of truth: epics E1–E8, milestones M0–M7,
   issues KE-001…KE-032 in exact execution order, with acceptance criteria and dependencies.

## Document precedence (highest wins)

1. `MASTER_VISION.md` — canonical; governs everything.
2. `CLAUDE.md` and the current-state references `docs/01-Architecture.md`–`docs/04`, `docs/07` — how the
   system works **today**.
3. The Phase-2 set above (Design → Refinements → Backlog) — what Phase 2 builds.
4. **Superseded / reclassified material** — historical only, never load-bearing:
   - `docs/05-Knowledge-Engine-Roadmap.md` (superseded by this set).
   - `docs/06-AI-Runtime-Roadmap.md` and `prompts/Phase-2-AI-Runtime.md` (autonomous code edits =
     **Phase 4 — Autonomy**, not Phase 2).
   - `prompts/Phase-2-Knowledge-Engine.md` (old punch-list; superseded by the backlog).

## Status

- **M0 / KE-001 — applied.** The four MASTER_VISION clarifications (three permanence tiers;
  re-derivation proposes, never overwrites; progressive "constrains"; interpretable, degradation-safe
  quality band) are in `MASTER_VISION.md`. Execution may proceed to M1 (KE-002 onward).
- Already shipped in Phase 1 (do not re-plan): code-candidate promotion UI, ProjectProfile curation UI,
  vector hygiene for knowledge items (graph hygiene still open → KE-020).

> No physical folder reorganization has been performed; the Phase-2 documents remain flat in `docs/`.
> Authority is established by this index and the banners on superseded files, not by directory layout.
