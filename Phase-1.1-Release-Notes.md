# LocalAIFactory — Phase 1.1 Release Notes

**Release type:** Incremental UI & workflow release on top of the verified Phase 1 baseline.
**Compatibility:** Drop-in. **No architecture changes. No database schema changes. No new migration.**

Phase 1.1 extends the existing implementation — it does not replace any module. The layered architecture
(Core → Data → Rag → Agent → Ingestion, with Workspaces/Terminal and Web on top), the Task-Profile model
strategy, approval memory, project-scoped RAG, the workspace scaffold, and the import pipeline are all preserved.

---

## 1. Verification findings addressed

All eight findings raised during Phase 1 verification are resolved.

| # | Finding | Resolution |
|---|---------|-----------|
| 1 | **WorkspacesOptions not bound** | `AddLocalAIFactoryWorkspaces(IConfiguration)` now calls `services.Configure<WorkspacesOptions>(config.GetSection("Workspaces"))`. Configuration drives the options; no silent hardcoded overrides. |
| 2 | **No Project Profile UI** | New `ProjectProfilesController` + views: list profiles (with per-profile section counts and generation status), drill into sections, approve sections (single or bulk), and trigger (re)generation. |
| 3 | **No Code-Candidate promotion UI** | New `CodeCandidatesController` + view: filter `ExtractedCodeBlocks` by project/status, **Promote** a candidate to Approved Code, and **bulk** promote/deprecate/delete. |
| 4 | **No PromptRun / ModelOutput review UI** | New `PromptRunsController` + views: execution history, per-run **retrieved-context** panel, **side-by-side output comparison** (tokens, latency, rating), and *mark output approved*. |
| 5 | **No vector cleanup** | `IVectorStore.DeleteAsync(ids)` (Qdrant REST `points/delete`) and `IKnowledgeIndexer.RemoveKnowledgeItemAsync(id)`. Deprecating **or** deleting a knowledge item now removes its vectors (best-effort; never fails the operation). |
| 6 | **Large uploads blocked** | Upload ceiling raised to **1 GB** across Kestrel (`Limits.MaxRequestBodySize`), IIS (`IISServerOptions.MaxRequestBodySize` + `web.config` `requestLimits`), and form parsing (`FormOptions.MultipartBodyLengthLimit`), plus `[RequestSizeLimit]` / `[RequestFormLimits]` on the upload actions. |
| 7 | **No ingestion recovery** | The hosted ingestion worker now runs a recovery pass on startup: jobs left `Running`/`Pending` are reset to `Pending` and safely re-enqueued. |
| 8 | **Snapshot vs. model** | Verified in sync. All 34 entities have `DbSet`s and appear in `AppDbContextModelSnapshot`. Phase 1.1 adds no entities/columns, so `dotnet ef migrations add` produces an **empty diff** — intentionally, no migration is added (see *Migration changes*). |

---

## 2. New features

### Bulk operations
A single, reusable multi-select component now powers bulk approval across every review grid:

- Checkbox column with **select page** / **select all** (indeterminate state when partial)
- Live **selected count** and a floating bulk toolbar
- **Bulk Approve**, **Bulk Deprecate**, **Bulk Delete** (and **Bulk Promote** for code candidates)
- **Confirmation dialog** for destructive actions (deprecate/delete)

Applied consistently to: **Knowledge Items, Business Rules, Knowledge-Graph Entities, Knowledge-Graph Relationships, Project-Profile Sections, and Code Candidates.**

Server side, the bulk endpoints funnel through `IApprovalService`:
`BulkApproveAsync(kind, ids)`, `BulkDeprecateAsync(kind, ids)`, `BulkDeleteAsync(kind, ids)`, and
`BulkPromoteCodeBlocksAsync(ids)`, where `kind ∈ {knowledge, rule, entity, relationship, section, code}`.
Deletes respect referential integrity (e.g. relationships referencing a deleted entity are removed first).

### Dashboard
- Clickable **stat tiles**: Projects, Knowledge, Business Rules, Approved Code, Imports, Workspaces, Model Configs, Chat Sessions
- **Review backlog** (knowledge / rules / code candidates / profile sections awaiting approval)
- **Recent activity**: recent knowledge, recent imports (with progress), and recent approval audit entries
- **Health**: active model, embeddings configured vs. keyword-only, vector store online vs. fallback
- **Ingestion counters**: running / pending / completed / failed

### Import monitoring
A dedicated import dashboard shows running / pending / completed / failed jobs with **progress bars,
timestamps, computed duration, and inline error rows**, plus the existing auto-refreshing per-job status page.

### Chat improvements
- Cleaner message bubbles and a session sidebar
- Per-message **retrieved-context panel** (approved-first, with source and score)
- **Model + token metadata** and a link to the originating **Prompt Run**
- One-click **save to memory**: Knowledge, Approved Code, or Business Rule
- Thumbs up/down rating wired through to the stored `ModelOutput`

---

## 3. UI modernization

A refreshed, minimalist enterprise UI (inspired by Linear, GitHub, and Azure Portal):

- Fixed **sidebar navigation** grouped by area (Workspace / Knowledge / Pipeline / Configuration), with active-state highlighting and a responsive collapse on narrow screens
- **Rounded cards** with soft shadows, **modern tables**, consistent **status badges**, and **progress indicators**
- A shared `_StatusBadge` partial maps every lifecycle state to a consistent colour
- Clean spacing and typography; no heavy gradients, neon colours, or excessive animation
- Markdown rendering (marked.js) and the `Scripts` section are preserved

---

## 4. Migration changes

**None.** Phase 1.1 is schema-stable: it adds no entities and no columns. Every entity used by the new
features (`ProjectProfile`, `ProjectProfileSection`, `ExtractedCodeBlock`, `PromptRun`, `ModelOutput`,
`AuditLog`, `IngestionJob`, `KnowledgeEntity`, `KnowledgeRelationship`) already existed in the 34-table
schema and in `AppDbContextModelSnapshot`. The existing migration `20260101000000_InitialCreate` and the
snapshot remain authoritative; the database still **migrates and seeds automatically on startup**.

> Running `dotnet ef migrations add <Name>` against this release will generate an empty migration. That is
> expected and confirms the snapshot is reconciled with the model. Do not commit an empty migration.

---

## 5. Files changed (summary)

**Build / packaging**
- `Directory.Packages.props` — added `<PackageVersion Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="10.0.0" />`
- `src/LocalAIFactory.Rag/LocalAIFactory.Rag.csproj` — added `<PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" />` (provides the `Configure<TOptions>(IConfiguration)` overload used to bind `Ollama`/`Qdrant`/`Rag` options)
- `src/LocalAIFactory.Workspaces/LocalAIFactory.Workspaces.csproj` — added the same `<PackageReference>` (binds the `Workspaces` options section). Fixes **CS1503** (`IConfigurationSection` → `Action<WorkspacesOptions>`) caused by the missing overload.

**Backend**
- `Core/Abstractions/IRag.cs` — `IVectorStore.DeleteAsync`, `IKnowledgeIndexer.RemoveKnowledgeItemAsync`
- `Core/Abstractions/IApproval.cs` — delete + bulk + promote signatures
- `Core/ViewModels/ViewModels.cs` — extended `DashboardViewModel`
- `Rag/Vector/QdrantVectorStore.cs` — `DeleteAsync` (REST `points/delete`)
- `Rag/Indexing/KnowledgeIndexer.cs` — `RemoveKnowledgeItemAsync`
- `Rag/Approval/ApprovalService.cs` — delete, bulk, and promote implementations
- `Workspaces/DependencyInjection.cs` — config binding for `WorkspacesOptions`
- `Web/Program.cs` + `Web/web.config` — 1 GB upload limits
- `Web/Hosted/IngestionBackgroundService.cs` — startup job recovery
- `Web/Controllers/*` — new `ProjectProfiles`, `CodeCandidates`, `PromptRuns`; bulk endpoints + search on `Knowledge`, `BusinessRules`, `KnowledgeGraph`; dashboard population in `Home`

**Frontend**
- `Web/wwwroot/css/site.css` — new enterprise design system
- `Web/wwwroot/js/bulk-actions.js` — reusable multi-select
- `Web/Views/Shared/_Layout.cshtml`, `_StatusBadge.cshtml` — new shell + badge partial
- New views: `ProjectProfiles/{Index,Details}`, `CodeCandidates/Index`, `PromptRuns/{Index,Details}`
- Modernized views: `Home/Index`, `Knowledge/{Index,Details}`, `BusinessRules/Index`, `KnowledgeGraph/Index`, `ImportWizard/Index`, `Chat/Index`

---

## 6. Upgrade notes

1. Replace the source tree with this release (or pull the changes).
2. Build and run `LocalAIFactory.Web` as before — the database migrates and seeds on startup; **no manual migration step is required**.
3. No `appsettings.json` changes are required. The `Workspaces` section is now honoured if present; defaults apply when it is absent.
4. For IIS hosting, the included `web.config` raises `maxAllowedContentLength` to 1 GB to match the application limits.
