# Phase 2 — AI Runtime / Autonomous code edits (forward brief / Claude Code prompt)

> **Status: RECLASSIFIED to Phase 4 — Autonomy** (MASTER_VISION §16). This is not Phase 2 work.
> Phase 2 is the Knowledge Engine (`docs/Phase-2-Execution-Backlog.md`). Retained as Phase-4 forward context.

**Objective:** implement the guarded autonomous loop:

> Ask a question → identify relevant files → snapshot → propose modifications → user approves →
> edit files → show diff → run build → store the successful fix as approved knowledge.

**Build on the existing scaffold:** `Workspace`, `WorkspaceSnapshot`, `WorkspaceChange`,
`WorkspaceFile`; `IWorkspaceManager`, `IWorkspaceSnapshotService`, `IWorkspaceModificationService`,
`IDiffService`; disabled task profiles `CodeModification`, `BuildAnalysis`, `WorkspacePlanning`.

**Steps:**
1. **File relevance selection** over an imported workspace.
2. **Snapshot + diff** (wire up the snapshot/diff services).
3. **Approval-gated apply** — replace the `NotSupportedException` guard in `ApplyChangeAsync` with an
   apply that edits **only inside a workspace sandbox**.
4. **Build verification** — compile after edits; surface results.
5. **Knowledge capture** — on a successful, approved build, store the fix as approved knowledge.

**Hard safety rules:**
- Edits happen only inside a workspace sandbox — never against the running solution.
- Every apply is approval-gated and audit-logged.
- AI stays optional: the platform remains fully usable with Ollama/Qdrant off.
- Mandatory build/verify before any change becomes approved knowledge.
- Honor all `CLAUDE.md` rules (MSSQL-only, no blocking calls, simple EF queries, projected lists).
