# Knowledge Architecture — General, Project, and Chat

> **Status:** Design + implemented-core reference. Describes how LocalAIFactory separates the *kinds* of
> knowledge it holds, how they are scoped and retrieved, and how MSSQL stays the runtime source of truth
> while JSON packs serve only as an import/export seed format.
> **Authority:** subordinate to `MASTER_VISION.md`. Where this conflicts, the vision prevails.

## 1. The single store, the many kinds

Every piece of durable knowledge in LocalAIFactory is one `KnowledgeItem` row in MSSQL. There is **no
parallel store** per knowledge kind. The kinds below are distinguished by *columns and provenance on the
same table*, not by separate databases or services. This is a deliberate consequence of MASTER_VISION
Principle 2 ("MSSQL is the source of truth") and of the Professional Base Knowledge Pack design, which
already proves the pattern: a pack item becomes an ordinary `KnowledgeItem` and flows through the
existing search, approval, permanence, versioning and provenance with zero new mechanisms.

The discriminators on `KnowledgeItem` (and adjacent tables) are:

| Concern | Field / mechanism |
|---|---|
| Portable identity (survives re-import/export) | `KnowledgeItem.Uid` (GUID) |
| General vs project scope | `KnowledgeItem.ProjectId` (`null` = global) |
| Baseline pack origin | `KnowledgeItem.KnowledgePackId` (non-null ⇒ from a pack) |
| Permanence / overwrite protection | `KnowledgeItem.Tier` (`PermanenceTier.Derived` / `Curated` / `Raw`) + `IPermanenceGuard` |
| Lifecycle state | `KnowledgeStatus` (`Draft` / `NeedsReview` / `Approved` / `Deprecated`) |
| Where it came from | `SourceType` (e.g. `SourceCode`, `SqlScript`, `ChatGptExport`, `ClaudeExport`, `Documentation`) |
| Typed meaning | `KnowledgeType` (`CodeSymbol`, `BusinessRule`, `FixPattern`, `ConversationInsight`, …) |
| Scope/precedence vocabulary | `KnowledgeScope` (`Global`, `Project`, `Standards`, `Regulatory`, `Team`) |
| Authority order | `AuthorityLevel` (`Normal` / `Low` / `High` / `Binding`) |
| Lineage chain | `ProvenanceEvent` (append-only; `Method`, `Actor`, `OriginPackUid`) |
| Version history | `KnowledgeVersion` (hash-guarded) |

The seven (plus two optional) knowledge categories described below are **views over this one table**,
selected by these discriminators.

## 2. The categories

### (a) General professional knowledge — `KnowledgePack`, `ProjectId = null`, `Tier = Curated`

Baseline, estate-agnostic professional knowledge: C#/ASP.NET Core patterns, EF Core/MSSQL idioms,
security/RBAC standards, governance summaries, finance/accounting/banking domain concepts. Shipped as a
**Knowledge Pack** and installed into MSSQL.

- `KnowledgePackId` is non-null; `ProjectId` is null (global).
- `Tier = Curated` so automated processes can never silently overwrite it; changes go through
  `IPermanenceGuard` as a proposed revision.
- Shown on the Base Knowledge surface with a *Baseline* badge.
- **Implemented.** This is the Professional Base Knowledge Pack v1 / v1.2 path.

### (b) Project-specific knowledge — `ProjectId` set

Knowledge extracted from, or authored about, a single imported project: code symbols, SQL objects,
business rules, architecture notes, fixes. `ProjectId` is set; `KnowledgePackId` is null.

- Machine-extracted items default to `Tier = Derived` (regenerable on re-import).
- Human-authored/approved items become `Tier = Curated`.
- Lineage traces back to the `SourceArtifact` (the imported file) via provenance.
- **Implemented** for C#, T-SQL and Python extraction.

### (c) Chat-imported knowledge — `SourceType = ChatGptExport` / `ClaudeExport`

Knowledge distilled from exported AI conversations (ChatGPT, Claude) or pasted chat transcripts.
Identified by `SourceType.ChatGptExport`, `SourceType.ClaudeExport`, or `SourceType.ConversationTranscript`.

- Always enters as **proposals**, never auto-approved (see `docs/Chat-Import-Knowledge-Learning.md`).
- May be **general** (`ProjectId = null`) or **project-scoped** (`ProjectId` set) depending on project
  detection during import.
- `KnowledgeType.ConversationInsight` is the natural type for extracted decisions/insights.
- **In progress:** a minimal markdown/text chat extractor is being implemented; the full pipeline is
  designed.

### (d) Code-graph evidence — `CodeSymbol` / `CodeEdge`

Deterministic structural facts about code: namespaces, types, members, and the typed edges between them
(`Inherits`, `Implements`, `Calls`, `References`, `DefinedIn`, …). These live in their **own lean tables**
(`CodeSymbol`, `CodeSymbolReference`, and resolved `CodeEdge`s), not in `KnowledgeItem`, because they are
regenerable structural projections with no approval chain of their own.

- They are **evidence**, not curated assertions: a `CodeSymbol` carries identity, location and syntactic
  attributes but no version/provenance/approval chain — it converges on a stable `SourceLocusKey` and
  keeps its `Uid` across re-extraction.
- Knowledge items (e.g. a `CodeSymbol`-typed `KnowledgeItem`) and pack references join to the graph by
  `NormalizedKey`.
- **Implemented** (C#/T-SQL/Python symbol + edge extraction; benchmark Gold on pinned repos).

### (e) SQL-graph evidence — `AccessesSql` edges (the C#↔SQL bridge)

The bridge that links application code to the database it touches. A C# (or Python) member that names a
SQL object in a string literal — raw SQL, `FromSqlRaw`/`ExecuteSqlRaw`, Dapper, ADO.NET `CommandText`,
`EXEC` — produces an `AccessesSql` edge (`RelationType.AccessesSql`) to the corresponding SQL
`CodeSymbol`, joined by canonical `schema.object` → `NormalizedKey`.

- Confidence is **below 1.0** because the evidence is syntactic string matching, not a semantic model —
  and the system says so. This honours "honesty over hype".
- Answers questions like "what code touches `dbo.Account`?" and "impact of changing this table".
- **Implemented** (R2-ACC-CAP1 for C#, CAP3 for Python; benchmark fixtures Gold).

### (f) Document / manual evidence

Knowledge from imported documentation, READMEs, deployment notes, and (designed) PDF/manual intelligence.
`SourceType.Documentation` / `Readme` / `DeploymentNote`; PDFs are hashed, classified text-vs-scanned, and
summarised extractively (no hallucination) with provenance.

- **Partially implemented:** the PDF analyser + extractive summariser exist and are tested; a real PDF
  parser library and measured quality are still gaps (see the readiness scorecard, Area 14/28).

### (g) Domain knowledge packs

Estate/domain-specific packs (e.g. Mauritius banking context, payments/settlement, leasing, insurance
controls) shipped as additional Knowledge Packs alongside the professional baseline. Same mechanism as
(a) — distinguished by `KnowledgePackId` and the pack's category tags.

- **Implemented** as content within the v1.2 pack; the *delivery* of domain engagements is advisory, not
  shipped capability (scorecard Areas 12/13/21).

### (h) Optional market / news knowledge

Design-stage only. Market data, verified news/events, and human-expert market notes would arrive as
governed knowledge through a dedicated source registry and connector layer (see
`docs/Market-Intelligence-Forecast-Module.md` and `docs/Market-Data-Source-Governance.md`).

- **Design + proposed entity skeleton only.** No live connectors exist. Strong disclaimers apply
  (`docs/Market-Module-Disclaimers.md`): forecast/risk intelligence, **not** investment advice.

## 3. Retrieval precedence

Retrieval assembles context by combining lexical, structural and (optional) semantic modes, then ranks
candidates by the precedence rules below — which directly implement MASTER_VISION §6 ("Approved knowledge
is injected first and weighted highest; more-specific takes precedence over more-general; constraint
knowledge is enforced rather than retrieved").

Precedence, highest first:

1. **Status gate.** `Approved` knowledge is eligible for authoritative injection. `Draft` / `NeedsReview`
   items are excluded from authoritative retrieval (they may surface in a review surface, clearly marked).
   `Deprecated` is excluded.
2. **Authority.** `AuthorityLevel.Binding` (standards/regulations modelled as constraints) is *enforced*,
   not merely retrieved — it is surfaced as a guardrail regardless of similarity score. Then `High`,
   `Normal`, `Low`.
3. **Specificity.** A `ProjectId`-scoped item out-ranks a global item **for that project's questions**.
   For estate-wide or general questions, global (`ProjectId = null`) curated knowledge is the baseline.
4. **Permanence / curation.** Between two otherwise-equal candidates, `Tier = Curated` (human-anchored)
   out-ranks `Tier = Derived` (machine-extracted).
5. **Quality / confidence.** Within a tier, higher `Confidence` / `QualityBand` ranks higher.
6. **Recency.** Used as a tie-breaker, not a primary signal.

**Chat-imported** knowledge participates in retrieval only **after** it has been approved (it enters as a
proposal). Until approved it is review-surface-only and never injected as authoritative context.

> Honesty note: not all of the precedence vocabulary is *behaviourally active* yet. The enums
> (`AuthorityLevel`, `QualityBand`, `KnowledgeScope`) exist and the status gate + scope filtering are in
> place; full authority-order enforcement and outcome-driven confidence are on the Phase-2 path. The
> ranker does not yet weight baseline pack items specially (a known limitation).

## 4. Scoping rules

- **Global (`ProjectId = null`)**: professional baseline, domain packs, and general chat insights.
  Visible across the estate; the default backdrop for any question.
- **Project (`ProjectId = X`)**: everything extracted from or authored about project X. Retrieval for a
  project query unions project-scoped + global, then applies precedence so project specificity wins.
- **Standards / Regulatory (`KnowledgeScope.Standards` / `Regulatory`)**: cross-cutting constraints that
  apply by `AppliesTo` links rather than by project ownership; enforced as guardrails.
- **Access control**: project visibility is governed server-side by RBAC (`UserRole`) and per-project
  `ProjectAccess` grants (deny-by-default, IDOR-guarded). Retrieval must never return project knowledge a
  caller has no grant to — scoping is a security boundary, not just a relevance filter.

## 5. Provenance and the approval lifecycle

Every item carries an append-only `ProvenanceEvent` chain recording **how and why** it came to be or
changed: the `Method` (`Deterministic`, `Semantic`, `Human`, `Import`, `Promotion`, `Consolidation`,
`Autonomous`), the `Actor`, the reason, and — for pack-sourced items — the `OriginPackUid`. Provenance is
distinct from the operational `AuditEvent` trail: provenance is *knowledge lineage* (explainability,
reconciliation, pack origin); audit is *who-did-what-when* for security.

The governed lifecycle is **Draft → NeedsReview → Approved → Deprecated/Superseded**. The critical
invariant, enforced by `IPermanenceGuard`, is **propose-never-overwrite for curated knowledge**:

- Automated processes (extraction, consolidation, re-embedding, chat import, future agents) call
  `IPermanenceGuard.ProposeRevisionAsync(...)` instead of mutating a `Curated` item.
- A human edit/approve always writes a `ProvenanceMethod.Human` event — the reliable signal that an item
  is human-anchored.
- If any `Human` provenance exists, an incoming automated change is routed to a `ProposedRevision`
  (review-required), never written through.
- `Derived` (machine-extracted) knowledge may be freely regenerated, because it is a rebuildable
  projection — losing it is a re-extraction, not data loss.

## 6. Why MSSQL stays the runtime source of truth

- **All knowledge lives in MSSQL.** Vector indexes (Qdrant), the code/SQL graph projections, and model
  outputs are *derived* and rebuildable. No knowledge exists only in a derived store.
- **JSON packs are seed format, not runtime store.** A Knowledge Pack (`manifest.json` + category files)
  is an *import/export* representation. Installation is in-memory-validated, idempotent on `Uid`, and
  transactional; once installed, the **MSSQL rows are authoritative**, and the pack files are no longer
  consulted at query time. Re-installing an unchanged pack is a fast no-op (`SourceManifestHash`).
- **Pack export is export-ready but not yet implemented** — the schema (`Uid`, `OriginPackUid`) already
  supports round-tripping; the reverse direction is a roadmap item.
- **Operable on MSSQL alone.** Disable Qdrant and Ollama and the platform still stores, scopes, retrieves
  (lexically), approves and governs knowledge. Vector and model capability are enhancements layered on
  top, never prerequisites (MASTER_VISION §5, Principle 10).

## 7. Putting it together — a worked retrieval

A developer asks a question about project *BDM*:

1. The query is scoped to project *BDM* (subject to the caller's `ProjectAccess` grant) **unioned with**
   global curated knowledge.
2. Lexical + structural retrieval gather candidates: project business rules, relevant `CodeSymbol`s, any
   `AccessesSql` edges touching the named table, plus matching baseline pack items.
3. The status gate drops `Draft`/`NeedsReview`/`Deprecated`. Binding standards are surfaced as guardrails.
4. Precedence ranks project-scoped, curated, high-confidence items above general derived ones.
5. The assembled context carries its provenance, so any downstream model answer can be traced to the
   rule, the prior fix, or the source artifact that justified it.

This is the architecture that keeps the *memory* — not any model — the centre of gravity, and keeps every
answer grounded, scoped, and accountable.
