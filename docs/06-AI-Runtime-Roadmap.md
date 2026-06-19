# 06 — AI Runtime Roadmap

This covers the local AI execution layer and the Phase-2 autonomous code-modification vision.

## In place today

- **Task Profiles** — one profile per task type, pointing at a primary model with optional
  validation/comparison models. Replaces fixed model roles.
- **Single-model baseline** — `qwen2.5-coder:14b` for inference, `nomic-embed-text` (768-dim) for
  embeddings, served by Ollama.
- **Chat orchestration** with approved-knowledge-first context injection.
- **Graceful degradation** — when Ollama is absent, model execution returns a failure result rather
  than throwing, and the rest of the app is unaffected.
- **Workspace scaffold** — entities (`Workspace`, `WorkspaceSnapshot`, `WorkspaceChange`,
  `WorkspaceFile`), interfaces (`IWorkspaceManager`, `IWorkspaceSnapshotService`,
  `IWorkspaceModificationService`, `IDiffService`), and disabled task profiles
  (`CodeModification`, `BuildAnalysis`, `WorkspacePlanning`). `ApplyChangeAsync` deliberately throws
  `NotSupportedException` as a Phase-1 guard.

## Planned (Phase 2 — AI Runtime)

The autonomous loop:

> Ask a question → identify relevant files → create a snapshot → propose modifications → user approves
> → system edits files → show a diff → run the build → store the successful fix as approved knowledge.

Building blocks to implement:
- **File relevance selection** over the imported workspace.
- **Snapshot + diff** services (wire up `IWorkspaceSnapshotService` / `IDiffService`).
- **Guarded apply** — replace the `NotSupportedException` guard with an approval-gated
  `ApplyChangeAsync` that only edits within a workspace sandbox.
- **Build verification** — compile after edits; only on success offer to store the change.
- **Knowledge capture** — persist successful fixes as approved knowledge for future reuse.

## Safety guards (must hold)

- Autonomous edits happen **only** inside a workspace sandbox, **never** against the running solution.
- Every apply is **approval-gated** and audit-logged.
- AI features remain **optional**: the platform stays fully usable with Ollama/Qdrant off.
- The build/verify step is mandatory before a change can be promoted to approved knowledge.
