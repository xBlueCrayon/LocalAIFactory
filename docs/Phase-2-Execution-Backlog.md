# Phase 2 Knowledge Engine — Execution Backlog

> **Source documents:** `MASTER_VISION.md`, `Phase-2-Knowledge-Engine-Design.md`, `Phase-2-Refinements-and-Alignment.md`.
> **Purpose:** an implementation-ready GitHub backlog. Issues are listed in the **exact order Claude Code
> should execute them**. No code; no architectural changes — this is execution planning only.
> **ID scheme:** `KE-NNN` (stable backlog ids; GitHub will assign its own issue numbers on import).
> **Complexity:** S (small, contained) · M (moderate) · L (large, multi-part). Anything larger is split.
> **How to use:** create the labels below, create milestones M0–M7, then create issues in order. Each
> milestone boundary is a natural review checkpoint. An issue is "done" only when every acceptance
> criterion is met.

## Epics (labels: `epic:*`)

- **E1 — Memory Foundation & Permanence:** the backbone, permanence tiers, identity, scopes, quality.
- **E2 — Extraction & Understanding:** raw layer, deterministic and semantic extraction.
- **E3 — Knowledge Graph:** structural and semantic/causal edges, traversal.
- **E4 — Vector & Retrieval:** embeddings, chunking, the multi-mode retrieval pipeline.
- **E5 — Approval & Curation:** lifecycle, review queue, curation capture.
- **E6 — Knowledge Types:** typed knowledge (code, schema, regulatory, semantic types).
- **E7 — Learning & Fixes:** fix and learning ledgers.
- **E8 — Consolidation & Memory Health:** the background engine.

## Milestones (ordered)

- **M0 — Alignment Pre-flight:** lock the governing document so execution and vision agree.
- **M1 — Memory Foundation:** the contracts everything depends on.
- **M2 — Deterministic Structural Core (vertical slice):** prove extraction → graph → retrieval →
  consolidation end-to-end on high-confidence, MSSQL-only data.
- **M3 — Approval & Curation:** make the Draft→Approved loop real and low-friction before fuzzy
  knowledge arrives.
- **M4 — Vector Retrieval Maturation:** layer semantic retrieval onto the working base.
- **M5 — Semantic Knowledge Types:** broaden horizontally into requirements, standards, regulations, etc.
- **M6 — Fix & Learning Records:** capture the signals the learning loop will later consume.
- **M7 — Consolidation Engine:** tie it together with the background memory-health engine.

---

# M0 — Alignment Pre-flight

### KE-001 · Apply MASTER_VISION clarifications
**Epic:** E1 · **Complexity:** S
**Description.** Apply the four surgical wording refinements identified in the alignment review so the
governing document matches the design before any build begins.
**Acceptance Criteria.**
- [ ] Principle 3 restated as three permanence tiers (raw permanent; machine-extracted regenerable; human-curated durable, changed only by approval).
- [ ] A "re-derivation proposes, never overwrites" rule added.
- [ ] "Constrains" clarified as progressive enforcement (visibility/flagging now, hard gating once autonomy exists).
- [ ] Quality clarified as an interpretable, degradation-safe trust band.
**Dependencies.** None.

---

# M1 — Memory Foundation

### KE-002 · Permanence tiers & propose-never-overwrite contract
**Epic:** E1 · **Complexity:** M
**Description.** Establish the three-tier permanence model and the rule that no automated process may
overwrite curated knowledge.
**Acceptance Criteria.**
- [ ] Raw artifacts are immutable after import.
- [ ] Machine-extracted items are marked regenerable; items edited or approved by the user are marked curated/human-anchored.
- [ ] No automated process can modify a curated item; an automated change is recorded as a *proposed revision* routed to review.
- [ ] The marker and rule are enforced uniformly across extraction, consolidation, and re-embedding.
**Dependencies.** KE-001.

### KE-003 · Knowledge backbone with provenance & versioning
**Epic:** E1 · **Complexity:** L
**Description.** Establish the generic KnowledgeItem backbone (identity, type, scope, status, content
reference, summary, quality band, confidence, authority, effective/expiry, version, source link,
timestamps) with a provenance chain and version history. Extend the existing item.
**Acceptance Criteria.**
- [ ] Every knowledge item records provenance (source artifact, method, actor/model, reason, time).
- [ ] Edits create a new version with a change reason; full history retrievable.
- [ ] The backbone supports all typed knowledge as extensions, not parallel tables.
**Dependencies.** KE-002.

### KE-004 · Stable identity & deduplication foundation
**Epic:** E1 · **Complexity:** M
**Description.** Define stable identity at each tier and a duplicate-detection foundation so later
re-extraction and consolidation converge rather than multiply.
**Acceptance Criteria.**
- [ ] Raw identified by content hash + path; re-importing identical content de-duplicates; changed content becomes a new artifact version.
- [ ] Machine-extracted items keyed to source locus so re-extraction updates the same logical item.
- [ ] Curated items keep identity permanently; near-duplicates recordable as DuplicateOf edges.
**Dependencies.** KE-003.

### KE-005 · Scope & domain taxonomies with precedence
**Epic:** E1 · **Complexity:** M
**Description.** Implement scope (Global, Project, Standards, Regulatory, Team/Personal) and domain
vocabularies with authority levels and applicability links. Single-user: no tenant isolation.
**Acceptance Criteria.**
- [ ] Every item carries a scope and domain.
- [ ] Authority order defined (Regulatory > Standards > Project > Global > Team/Personal).
- [ ] Standards/regulations can link to the projects/components they apply to; precedence is queryable.
**Dependencies.** KE-003.

### KE-006 · Quality band model (degradation-safe)
**Epic:** E1 · **Complexity:** M
**Description.** Implement the interpretable trust band (Provisional → Corroborated → Trusted →
Authoritative) with floor-and-adjust composition, distinct from confidence, computable without AI.
**Acceptance Criteria.**
- [ ] Each item has a computed quality band; provenance tier sets the floor; corroboration/currency/consistency/outcomes adjust.
- [ ] Contradiction or a failed outcome demotes an item to NeedsReview.
- [ ] The band computes correctly with no model present (provenance + corroboration only) and drives retrieval tiering and review priority.
**Dependencies.** KE-003, KE-005.

---

# M2 — Deterministic Structural Core (vertical slice)

### KE-007 · Raw artifact layer & ingestion provenance
**Epic:** E2 · **Complexity:** M
**Description.** Formalize the immutable SourceArtifact (extend ImportedFile) with a full metadata
envelope, and the ImportBatch provenance root. Raise the import ceiling for large project ZIPs.
**Acceptance Criteria.**
- [ ] Each imported file stored as an immutable SourceArtifact with content hash + metadata (source, path, type/language, size, batch).
- [ ] Everything derived links back to its artifact; re-import dedup via hash.
- [ ] The multipart/import ceiling handles large project archives (open audit item closed).
**Dependencies.** KE-002, KE-004.

### KE-008 · Deterministic code symbol extraction (C#)
**Epic:** E2 · **Complexity:** L
**Description.** Parse C# into CodeSymbols (kind, name, signature, public surface, complexity) with
byte-level provenance. Deterministic and high-confidence.
**Acceptance Criteria.**
- [ ] C# files yield CodeSymbols with defining artifact and project.
- [ ] Extraction is re-runnable and idempotent (updates the same logical symbols, no duplicates).
- [ ] Confidence marked high (deterministic); provenance to source offset retained.
**Dependencies.** KE-007.

### KE-009 · Deterministic schema / data-dictionary extraction (T-SQL)
**Epic:** E2 · **Complexity:** L
**Description.** Parse T-SQL/schema scripts into DataDictionaryEntry and schema objects (tables,
columns, procedures, views, constraints) with referenced-object detection.
**Acceptance Criteria.**
- [ ] SQL yields schema objects with types, constraints, and referenced tables/columns.
- [ ] DDL vs DML classified; extraction re-runnable/idempotent; provenance retained.
**Dependencies.** KE-007.

### KE-010 · Structural graph edges
**Epic:** E3 · **Complexity:** L
**Description.** Build the structural graph layer (DependsOn, References, DefinedIn, PartOf) from the
deterministic extraction, MSSQL-resident, with confidence and provenance. Extend KnowledgeRelationship.
**Acceptance Criteria.**
- [ ] Structural edges generated automatically at high confidence and auto-active, with type/direction/confidence/provenance.
- [ ] Graph traversable relationally for "what depends on X" / "what references table Y."
- [ ] Edges rebuildable from extraction.
**Dependencies.** KE-008, KE-009.

### KE-011 · Multi-mode retrieval — lexical + structural base
**Epic:** E4 · **Complexity:** L
**Description.** Implement the retrieval pipeline's lexical (exact-identifier) and structural
(graph/relational) modes plus query classification, on MSSQL alone (no vectors yet), with provenance-
tagging and RetrievalEvent capture scaffolding.
**Acceptance Criteria.**
- [ ] Queries return results by exact identifier and by graph traversal; results provenance-tagged.
- [ ] Pipeline runs with no vector store and no model; impact/traceability queries return connected results.
- [ ] A RetrievalEvent is logged per query (capture only).
**Dependencies.** KE-010, KE-006.

### KE-012 · Thin consolidation pass for the structural core
**Epic:** E8 · **Complexity:** M
**Description.** A minimal idempotent maintenance job over the structural slice that re-extracts from
raw, updates symbols/edges in place, dedups, and recomputes quality — proving the consolidation pattern
end to end while honoring propose-never-overwrite.
**Acceptance Criteria.**
- [ ] Re-running ingestion/extraction converges (no duplicate symbols/edges).
- [ ] Curated items are untouched (proposed revisions only).
- [ ] Quality bands recomputed; job idempotent and rebuildable from raw.
**Dependencies.** KE-008, KE-009, KE-010, KE-004, KE-006, KE-002.

---

# M3 — Approval & Curation

### KE-013 · Lifecycle states & gated transitions
**Epic:** E5 · **Complexity:** M
**Description.** Implement Draft → NeedsReview → Approved → Deprecated/Superseded (+ Rejected retained,
+ Validated for fixes) with gated transitions; approval makes an item authoritative.
**Acceptance Criteria.**
- [ ] Every item has a lifecycle status; transitions follow the defined rules.
- [ ] Only Approved items enter high-priority retrieval; rejected items retained as negative knowledge.
- [ ] Superseding deprecates the predecessor and flags it for vector hygiene; extraction produces Draft, edges Proposed.
**Dependencies.** KE-003, KE-005.

### KE-014 · Prioritized, low-friction review queue
**Epic:** E5 · **Complexity:** L
**Description.** A review queue ordered by impact × uncertainty with batch approve-many, bulk-accept for
high-confidence structural extractions, and bounded auto-advancement (never auto-Approved except
pre-authorized low-risk classes).
**Acceptance Criteria.**
- [ ] Queue ranked by impact × uncertainty; similar items batchable; high-confidence structural items bulk-acceptable.
- [ ] Auto-advancement to NeedsReview works; nothing reaches Approved without human action unless a pre-authorized policy applies.
- [ ] One-glance review shows provenance (and evidence for fixes).
**Dependencies.** KE-013, KE-006.

### KE-015 · Curation capture (CurationEvent ledger)
**Epic:** E7 · **Complexity:** S
**Description.** Capture every approve/reject/edit/merge as an append-only CurationEvent — the gold
learning signal. Capture only; adaptation is Phase 3.
**Acceptance Criteria.**
- [ ] All curation actions logged immutably with before/after and reason, linked to the affected item.
- [ ] Ledger append-only.
- [ ] Payoff metrics surfaced (how often approved knowledge was used / contributed to outcomes).
**Dependencies.** KE-013.

---

# M4 — Vector Retrieval Maturation

### KE-016 · Embedding records & multi-model abstraction
**Epic:** E4 · **Complexity:** M
**Description.** Implement EmbeddingRecord (chunk → model, dimension, collection, vector id, version) so
multiple embedding models coexist and re-embedding is clean. Vectors derived from MSSQL, rebuildable.
**Acceptance Criteria.**
- [ ] Each vector tracked to model/dimension/collection/version; different models/dimensions never mixed in one collection.
- [ ] Vectors fully rebuildable from MSSQL; embedding optional (system works without it).
**Dependencies.** KE-007, KE-003.

### KE-017 · Type-aware chunking & small-to-big
**Epic:** E4 · **Complexity:** M
**Description.** Replace fixed-character chunking with type-aware chunking (code by symbol, SQL by
object, docs by section, regulations by clause) and parent/child small-to-big retrieval.
**Acceptance Criteria.**
- [ ] Chunks align to natural units per type; small chunk matched, larger parent injected.
- [ ] Chunking re-runnable/idempotent; chunk provenance retained.
**Dependencies.** KE-016, KE-008, KE-009.

### KE-018 · Payload-filtered semantic retrieval
**Epic:** E4 · **Complexity:** L
**Description.** Add the semantic (vector) mode with payload filtering on scope, type, status, authority,
project, and effective date, enforcing precedence at the vector layer.
**Acceptance Criteria.**
- [ ] Vector search filters by scope/type/status/authority inside the query; deprecated/low-quality items excluded.
- [ ] Semantic mode runs in parallel with lexical/structural; degrades gracefully if the vector store is absent.
**Dependencies.** KE-016, KE-011, KE-006.

### KE-019 · Hybrid fusion, reranking & guardrail channel
**Epic:** E4 · **Complexity:** L
**Description.** Fuse semantic + lexical + structural candidates, rerank (model when available,
deterministic boosts otherwise), apply approved-first / specific-over-general boosts, dedupe, and
assemble the constraint guardrail channel separately. Deterministic ranking.
**Acceptance Criteria.**
- [ ] Candidates from all three modes fused and deduped; ranking deterministic for a fixed memory state.
- [ ] Reranking applies authority/quality/recency/approved-first boosts; degrades to boosts-only when no model.
- [ ] Applicable constraints assembled into a separate guardrail channel; context budget-aware and provenance-tagged.
**Dependencies.** KE-018, KE-011.

### KE-020 · Vector hygiene on deprecate/delete/supersede
**Epic:** E4 · **Complexity:** S
**Description.** Close the known gap — remove or update vectors when knowledge is deprecated, deleted, or
superseded, in lifecycle transitions and consolidation.
**Acceptance Criteria.**
- [ ] Deprecating/deleting/superseding an item removes or updates its vectors; no stale vectors surface.
- [ ] Hygiene runs on transition and in consolidation; idempotent.
**Dependencies.** KE-016, KE-013.

---

# M5 — Semantic Knowledge Types

### KE-021 · Semantic extraction pipeline (provenance-anchored)
**Epic:** E2 · **Complexity:** L
**Description.** Add the model-driven semantic extraction pass producing structured Draft knowledge from
documents/code/conversations with byte-level provenance and confidence; queue when no model; hybrid with
deterministic parsers where they exist.
**Acceptance Criteria.**
- [ ] Semantic extraction yields typed Draft knowledge with provenance to source offset and a confidence.
- [ ] Runs as a background pass; deferred/queued when no model present; outputs flow to the review queue.
- [ ] Re-runnable with reconciliation (propose-never-overwrite).
**Dependencies.** KE-007, KE-013, KE-002.

### KE-022 · Promote code candidates & project profiles
**Epic:** E6 · **Complexity:** M
**Description.** Promotion paths for ExtractedCodeBlock → ApprovedCodeSnippet and ProjectProfileSection
curation (open audit items), through the approval lifecycle.
**Acceptance Criteria.**
- [ ] Code candidates (NeedsReview) reviewable and promotable to approved snippets.
- [ ] Project profile sections curatable through the lifecycle.
- [ ] Promotions write provenance, audit, and a CurationEvent.
**Dependencies.** KE-013, KE-014.

### KE-023 · Constraint knowledge — standards & regulations
**Epic:** E6 · **Complexity:** L
**Description.** First-class TechnicalStandard and RegulatoryObligation types (clause, authority,
effective/expiry, applicability, enforcement level), surfaced via the guardrail channel, with
GovernedBy/AppliesTo edges.
**Acceptance Criteria.**
- [ ] Standards/regulations stored as constraint types with effective dates, authority, and applicability.
- [ ] Surfaced in the guardrail channel; GovernedBy/AppliesTo edges link them to code/projects.
- [ ] Flagged for explicit approval; retrieval is effective-date aware.
**Dependencies.** KE-021, KE-005, KE-019.

### KE-024 · Other semantic types
**Epic:** E6 · **Complexity:** L
**Description.** Add Requirement, ArchitectureDecision, GlossaryTerm, InterfaceContract, TestCase, and
ConversationInsight with their distinguishing attributes; distill imported AI conversations into
structured insights, not transcripts.
**Acceptance Criteria.**
- [ ] Each type stored with its distinguishing attributes and flowing through Draft→approval.
- [ ] Conversation imports produce ConversationInsight (problem/approach/worked/failed/lesson), not raw transcripts.
- [ ] Glossary feeds back into extraction (ontology-aware).
**Dependencies.** KE-021.

### KE-025 · Semantic & causal graph edges (reviewed)
**Epic:** E3 · **Complexity:** L
**Description.** Add the semantic graph layer (Implements, Satisfies, GovernedBy, DerivedFrom,
Supersedes, FixedBy, Caused, Contradicts, AppliesTo), inferred and corroborated, with low-confidence
edges queued for review.
**Acceptance Criteria.**
- [ ] Semantic/causal edges inferred with confidence and provenance; low-confidence edges queued (not auto-active).
- [ ] Traceability queries (requirement → code → test → fix → regulation) return connected paths.
- [ ] Contradictions surfaced as Contradicts edges for resolution.
**Dependencies.** KE-010, KE-021, KE-014.

---

# M6 — Fix & Learning Records

### KE-026 · FixRecord ledger & capture
**Epic:** E7 · **Complexity:** L
**Description.** Implement FixRecord (problem, symptom signature, affected-symbol links, diagnosis,
change reference, build result, test result, outcome) capturing successes and failures, grounded in
build/test truth. Capture from manual/assisted work now.
**Acceptance Criteria.**
- [ ] A fix episode records problem/symptom/affected symbols/diagnosis/change/build+test results/outcome.
- [ ] Failures retained as negative knowledge linked to the failed approach; build/test results attached as evidence.
- [ ] FixRecords link into the graph (FixedBy/Addresses/Caused).
**Dependencies.** KE-010, KE-013.

### KE-027 · Fix generalization & pattern scaffolding
**Epic:** E7 · **Complexity:** M
**Description.** On approved, build/test-passing fixes, distill a FixPattern (symptom class → root cause
→ remedy) with an exemplar and reuse-tracking fields. Distillation + storage only; reuse-confidence
learning is Phase 3.
**Acceptance Criteria.**
- [ ] An approved successful fix can be distilled into a FixPattern with a generalized remedy and exemplar.
- [ ] Reuse count/outcome fields exist; pattern linked to its source FixRecord; retrievable by symptom similarity.
**Dependencies.** KE-026, KE-014.

### KE-028 · Learning ledger — retrieval & outcome events
**Epic:** E7 · **Complexity:** M
**Description.** Implement RetrievalEvent (query, context, returned ids, used/cited, helpfulness) and
OutcomeEvent (task result linked to informing knowledge) as append-only ledgers. Capture only; Phase 3
consumes them.
**Acceptance Criteria.**
- [ ] Every retrieval logs returned and used knowledge; outcomes (build/test pass/fail) logged and linked to the informing knowledge.
- [ ] Ledgers append-only; negative signals captured.
- [ ] The ledger is the substrate Phase 3 will read (no adaptation logic in Phase 2).
**Dependencies.** KE-011, KE-019.

---

# M7 — Consolidation Engine

### KE-029 · Re-extraction with reconciliation
**Epic:** E8 · **Complexity:** L
**Description.** Background re-extraction from raw with improved extractors/models; reconcile against
existing knowledge — updating regenerable items in place and proposing revisions to curated items.
**Acceptance Criteria.**
- [ ] Re-extraction updates derived items in place (convergent, no duplicates).
- [ ] Curated items get proposed revisions routed to review, never overwritten.
- [ ] Idempotent and rebuildable from raw.
**Dependencies.** KE-021, KE-002, KE-004, KE-012.

### KE-030 · Contradiction & duplication detection
**Epic:** E8 · **Complexity:** L
**Description.** Use the graph and embeddings to surface contradictions and duplicates; auto-merge
high-confidence duplicates (preserving provenance/links); route conflicts and uncertain cases to review.
**Acceptance Criteria.**
- [ ] Conflicting approved items surfaced as Contradicts for resolution.
- [ ] High-confidence duplicates auto-merged with provenance and links preserved; uncertain cases queued.
- [ ] Merges reversible and auditable.
**Dependencies.** KE-025, KE-018, KE-014.

### KE-031 · Quality recomputation, decay & summarization
**Epic:** E8 · **Complexity:** L
**Description.** Periodically recompute quality bands from current signals; decay/flag stale, unused, or
superseded items to NeedsReview; consolidate clusters (e.g., many fix records on one component) into
DistilledLessons with back-links.
**Acceptance Criteria.**
- [ ] Quality recomputed from provenance + corroboration + outcomes; stale/unused/superseded items flagged NeedsReview.
- [ ] Clusters distilled into higher-level knowledge with source back-links; curated items untouched (proposals only).
- [ ] Degradation-safe when no model is present.
**Dependencies.** KE-028, KE-006, KE-027.

### KE-032 · Proactive analysis (overnight engine)
**Epic:** E8 · **Complexity:** M
**Description.** Background speculative analysis (fragile areas, improvement opportunities) with
evidence, ready for the user — using idle local capacity. Optional and degrades gracefully.
**Acceptance Criteria.**
- [ ] A scheduled job produces evidence-backed observations surfaced in the review queue.
- [ ] It never modifies curated knowledge.
- [ ] Fully optional (no effect on core function when disabled or when no model is present).
**Dependencies.** KE-029, KE-030, KE-031.

---

## Execution order (quick reference)

```
M0  KE-001
M1  KE-002 → KE-003 → KE-004 → KE-005 → KE-006
M2  KE-007 → KE-008 → KE-009 → KE-010 → KE-011 → KE-012      ← vertical slice; full pipeline proven, MSSQL-only
M3  KE-013 → KE-014 → KE-015
M4  KE-016 → KE-017 → KE-018 → KE-019 → KE-020
M5  KE-021 → KE-022 → KE-023 → KE-024 → KE-025
M6  KE-026 → KE-027 → KE-028
M7  KE-029 → KE-030 → KE-031 → KE-032
```

Within M2, KE-008 and KE-009 may run in parallel (both depend only on KE-007); likewise within M4,
KE-017 depends on KE-016 while KE-020 depends only on KE-016 and KE-013. Otherwise the chain is linear.

## Definition of done for Phase 2

Phase 2 — and the gate to Phase 3 (adaptive learning) — is complete when all milestones close: the
memory is structured, typed, linked, and curated; retrieval runs in three modes and degrades to
MSSQL-only; the permanence rule holds (nothing overwrites curated knowledge); and the fix and learning
ledgers are capturing real signals for Phase 3 to consume.
