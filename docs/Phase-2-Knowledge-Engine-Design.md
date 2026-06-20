# Phase 2 — Knowledge Engine: Complete Design

> **Derives from:** `MASTER_VISION.md` (governing) and the two memory-architecture reviews.
> **Phase theme (per roadmap):** *Understanding — the memory becomes structured.*
> **Form:** design only. No code, no DDL, no schema syntax. Attribute names are conceptual.
> **Note:** This design follows MASTER_VISION and the existing schema. The MASTER_VISION clarifications
> it assumes are formalized by backlog item KE-001 (M0); apply KE-001 before implementing M1 onward.

## What Phase 2 builds, and what it only enables

Phase 2 turns the flat curated store into a **structured, typed, linked, provenance-rich memory** and
lays the data foundations of learning. Concretely, Phase 2 delivers the entity and relationship models,
the typed knowledge taxonomy, the scope/precedence model, the knowledge graph, the matured vector
strategy, the approval workflow, the retrieval pipeline, and the consolidation engine — plus the
**record structures** for fixes and learning and the **capture** of those signals.

Phase 2 deliberately does *not* build the adaptive learning algorithms (Phase 3) or autonomous change
generation (Phase 4). It builds the substrate they require: the records exist and are captured now so
that later phases reason and act over real history. Every choice below preserves the vision's
non-negotiables — MSSQL as truth, raw permanent / derived rebuildable, graceful degradation, human
approval as the gate of trust.

A unifying design decision threads through everything: **a generic knowledge backbone plus typed edges
plus flexible metadata**, with specialization only where structured querying earns it (code, schema,
regulatory, and fix knowledge). This avoids a premature explosion of parallel tables while still giving
the platform structured reasoning.

---

## 1. Entity Model

Entities are organized by memory layer (raw → semantic, with provenance and learning crosscutting).
"Extends" marks an evolution of an existing concept in the current schema; the rest are new.

### Raw layer (immutable truth)

| Entity | Key attributes | Purpose |
|---|---|---|
| **SourceArtifact** *(extends ImportedFile)* | identity, source system, original path, content hash, detected type/language, byte size, import-batch ref, raw-content reference, imported-at | The permanent, immutable record of an imported file. Never edited; everything else derives from it. |
| **ImportBatch** *(extends IngestionJob)* | identity, source, project ref, counts (files/chunks/symbols), phase, started/completed, status | One ingestion episode; the provenance root for everything it produced. |

### Knowledge backbone (semantic layer)

| Entity | Key attributes | Purpose |
|---|---|---|
| **KnowledgeItem** *(extends current)* | identity, knowledge-type ref, scope, status (lifecycle), title, content reference, summary, quality score (computed), confidence, authority, effective/expiry dates, version number, source-artifact ref, created/updated | The generic node. Every piece of knowledge is a KnowledgeItem; typed entities extend it rather than replace it. |
| **KnowledgeVersion** | item ref, version number, content snapshot ref, change reason, author/agent, created | Revision history and supersession trail; makes evolution auditable. |
| **KnowledgeChunk** *(extends current)* | parent item ref, ordinal, chunk type (code/section/clause/object), content, source char range, parent-chunk ref | The retrieval unit, produced by type-aware chunking. Derived and regenerable. |
| **EmbeddingRecord** | chunk ref, embedding-model id, dimension, collection/namespace, vector id, model version, created | Binds a chunk to one of possibly several vector representations; enables multi-model coexistence and clean re-embedding. Derived. |

### Typed knowledge (specializations / projections of KnowledgeItem)

Each carries the backbone fields plus its distinguishing attributes (detailed in §3). High-value
first-class types in Phase 2: **Requirement, RegulatoryObligation, TechnicalStandard, ArchitectureDecision,
DataDictionaryEntry, InterfaceContract, TestCase, GlossaryTerm, BusinessRule** *(exists)*,
**ApprovedCodeSnippet** *(exists)*, **FixPattern**, **ConversationInsight**. Lower-value types remain
generic KnowledgeItems distinguished only by their knowledge-type ref until a need to query them
structurally appears.

### Structural code knowledge

| Entity | Key attributes | Purpose |
|---|---|---|
| **CodeSymbol** | kind (class/method/procedure/table/view/etc.), name, signature, defining artifact ref, project ref, public-surface flag, complexity signal | Structural understanding of code and schema as first-class nodes, extracted deterministically. |
| **CodeCandidate** *(extends ExtractedCodeBlock)* | source-artifact ref, extracted content, candidate type, extraction confidence, status | Extracted code awaiting promotion to an approved snippet (the existing candidate concept, formalized). |

### Organization & scope

| Entity | Key attributes | Purpose |
|---|---|---|
| **Project** *(exists)* | identity, name, code, description | The primary unit of project knowledge. |
| **ProjectProfile / ProjectProfileSection** *(exists)* | project ref, section type, content, status | Generated understanding of a project; curatable. |
| **ProjectRelationship** | from-project, to-project, relationship type (shares-component / depends-on / fork-of / shares-domain), confidence | Connects the estate so patterns learned in one project surface in another. |
| **KnowledgeScope** *(taxonomy)* | scope value, authority level, override behavior | The precedence/applicability vocabulary (see §4). |
| **KnowledgeDomain** *(taxonomy)* | domain value, description | Subject categorization (e.g., a domain system or capability). |

### Provenance & learning (crosscutting)

| Entity | Key attributes | Purpose |
|---|---|---|
| **ProvenanceEvent** | knowledge ref, source-artifact ref, method (deterministic/semantic/human), extractor/model id, actor, reason, created | The chain of why a knowledge item exists or changed; the basis of explainability. |
| **KnowledgeRelationship** *(extends current)* | from node, to node, relationship type, direction, confidence, provenance ref, status | The typed graph edge (see §2 and §7). |
| **FixRecord** | (see §5) | One fix episode, success or failure. |
| **LearningRecord** | (see §6) | One learning signal: retrieval, curation, or outcome. |

**Vision alignment.** Raw artifacts immutable and primary; chunks/embeddings/graph derived and
rebuildable; versions and provenance make the memory auditable; a generic backbone keeps the model
extensible rather than fragmenting it.

---

## 2. Relationship Model

Edges are themselves knowledge: each carries a type, a direction, a confidence, a provenance reference,
and a lifecycle status, so relationships are curated and decayed like nodes. Two layers exist — a
**structural** layer (extracted deterministically from code and schema, high confidence) and a
**semantic** layer (inferred and corroborated, reviewed before becoming authoritative).

| Relationship type | From → To | Semantics | Layer |
|---|---|---|---|
| **DependsOn** | symbol/item → symbol/item | A requires B to function | Structural |
| **References** | symbol → symbol/table | A names or uses B | Structural |
| **DefinedIn / PartOf** | symbol → artifact; chunk → item | Containment | Structural |
| **Implements** | code → contract/requirement | A realizes B | Semantic |
| **Satisfies** | code/test → requirement | A fulfills B | Semantic |
| **GovernedBy** | code/decision → standard/regulation | A is constrained by B | Semantic |
| **AppliesTo** | standard/regulation → project/component | B is in scope of A | Semantic |
| **DerivedFrom** | knowledge → source/knowledge | A was distilled from B | Semantic |
| **Supersedes** | knowledge → knowledge | A replaces B (B deprecated) | Temporal |
| **EvolvedFrom** | knowledge → knowledge | A is a later form of B | Temporal |
| **FixedBy** | defect/problem → fix | B resolves A | Causal |
| **Addresses** | fix → problem/requirement | B targets A | Causal |
| **Caused** | change → defect/incident | A produced B | Causal |
| **Contradicts** | knowledge → knowledge | A and B conflict (needs resolution) | Semantic |
| **DuplicateOf** | knowledge → knowledge | A and B express the same thing | Semantic |
| **RelatesTo** | any → any | Weak association | Semantic |

The most valuable edges for engineering are **causal** and **temporal** — they answer "what will break,"
"why is it this way," and "what went wrong last time." Edges are queryable in both directions.

**Vision alignment.** Relationships are knowledge (confidence + provenance + curation); structural edges
are grounded in deterministic truth; the graph encodes the constraint relationships (GovernedBy /
AppliesTo) that let standards and regulations be enforced rather than merely retrieved.

---

## 3. Knowledge Types

Each type behaves in one of three ways, and the behavior — not the storage — is what matters:
**Context** (retrieved to inform), **Constraint** (enforced as a guardrail and checked against), or
**Executable-linked** (tied to compilable/runnable truth).

| Type | Behavior | Distinguishing attributes | Primary extraction source |
|---|---|---|---|
| **GlossaryTerm / DomainConcept** | Context | term, definition, synonyms, related concepts | Docs, conversations, curation |
| **Requirement** | Context + traceable | statement, kind (functional/non-functional), priority, acceptance criteria, trace links | Requirements docs |
| **BusinessRule** *(exists)* | Constraint (soft) | rule statement, condition, rationale | Docs, code, conversations |
| **RegulatoryObligation** | **Constraint (hard)** | clause, issuing authority, effective/expiry dates, applicability | Regulatory/legal documents |
| **TechnicalStandard / Policy** | **Constraint (hard, exception-gated)** | rule, category (coding/naming/architecture/security), enforcement level | Standards docs, curation |
| **ArchitectureDecision** | Context (rationale) | decision, alternatives, rationale, consequences, status | Docs, conversations, curation |
| **ApprovedCodeSnippet** *(exists)* | Executable-linked | code, language, intent, parameters, usage notes | Promotion of code candidates / fixes |
| **CodeSymbol** | Executable-linked (structural) | kind, signature, dependencies | Deterministic code/SQL parsing |
| **DataDictionaryEntry** | Context + structural | object (table/column/proc), semantics, type, lineage, constraints | SQL/schema parsing + semantic distillation |
| **InterfaceContract** | Constraint + context | boundary, message/format, direction, expectations | Integration code, docs |
| **TestCase / TestKnowledge** | Executable-linked | scenario, expected behavior, known-good output | Test code, docs |
| **FixPattern** | Context + executable-linked | symptom class, root cause, remedy, exemplar | Distilled from successful FixRecords |
| **ConversationInsight** | Context | problem, approach, what worked/failed, lesson | Imported AI conversations |
| **OperationalNote / Runbook** | Context | procedure, environment, configuration | Deployment notes, docs |

Constraint types are retrieved into a separate guardrail channel (see §10) and used to validate
proposals, consistent with the vision's principle that **knowledge constrains as well as informs**.

**Vision alignment.** First-class types where structured reasoning needs them; constraints distinguished
from context; executable-linked types tie knowledge to the compiler/tests that ground reasoning.

---

## 4. Memory Scopes

Because the platform is single-user and local, scope is purely about **relevance, authority, and
applicability** — not isolation or access control. The client/tenant dimension from the production
discussion is intentionally absent. Scopes form a precedence model, with constraint scopes treated
differently from context scopes.

| Scope | Authority | Override behavior | Retrieval treatment |
|---|---|---|---|
| **Regulatory** | Highest (binding) | Never overridden | Injected as hard guardrails; proposals validated against it |
| **Technical Standards** | High (binding) | Only via a recorded, approved exception | Guardrails during generation and review |
| **Project** | Medium-high | Overrides Global within its project | Most-specific functional context |
| **Global** | Medium | Specialized by Project scope | Default cross-project context |
| **Team / Personal (User)** | Lowest | Most local, least authoritative | Hints and conventions |

Two structural points. Precedence is **applied at retrieval time** as ranking and channel assignment,
not as a hard pre-filter, except where authority makes a constraint non-negotiable. And applicability is
explicit: standards and regulations link to the projects/components they govern (via AppliesTo edges),
so a constraint surfaces only where it applies.

**Vision alignment.** More-specific over more-general; constraints enforced; single-user simplification
keeps scope about relevance and authority, exactly as the vision's memory-first, approval-gated model
intends.

---

## 5. Fix Records

The FixRecord is the seed of the self-improving loop. It captures a complete episode — **successes and
failures alike** — so that later phases reason from real history and avoid repeating mistakes. In Phase
2 these are captured from manual and assisted work; in Phase 4 the autonomous loop produces them
directly. The record is grounded in executable truth.

**FixRecord attributes:** identity, project ref, problem statement, symptom signature (for matching),
affected-symbol links (into the graph), diagnosis/hypothesis, change reference (snapshot/diff of what
was done), build result, test result, outcome (success / failure / partial), generalized pattern (the
distilled symptom-class → root-cause → remedy), reuse count, reuse outcomes, confidence, status,
created/updated.

**FixRecord lifecycle:** **Captured → Validated** (build and tests run, results recorded) **→ Reviewed →
Approved → Generalized** (distilled into a FixPattern and, where applicable, an ApprovedCodeSnippet, with
FixedBy / Addresses / Caused edges created). A failed attempt is **retained as negative knowledge** —
linked to the problem and to the approach that did not work — so a future proposal resembling it is
flagged.

Generalization is essential: the platform stores the *transferable* remedy, not only the literal diff,
so a FixPattern retrieves for *similar* future problems. Each reuse's outcome feeds back into the
pattern's confidence (Phase 3 learning), so good patterns strengthen and weak ones fade.

**Vision alignment.** Grounded in compile/test truth; negative knowledge first-class; outcome validation
as the confidence anchor; approval gates a fix becoming reusable.

---

## 6. Learning Records

LearningRecords are the append-only ledger that Phase 3 will learn from. Three record kinds correspond
to the vision's three signals of increasing strength.

| Record kind | Captures | Signal strength | Drives (later phases) |
|---|---|---|---|
| **RetrievalEvent** | query, context, returned knowledge ids, which were used/cited, helpfulness | Weak, plentiful | Adapting retrieval relevance |
| **CurationEvent** | the user's approve / reject / edit / merge action, target, before/after, reason | Strong (expert oracle) | Quality scoring; improving extraction from corrections |
| **OutcomeEvent** | a task result (build/test pass/fail) linked to the knowledge that informed it | Objective ground truth | Confidence/quality updates; playbook formation |

A fourth, derived record — **DistilledLesson** — is produced by consolidation from many events (for
example, "this approach repeatedly fails on this component") and becomes curatable knowledge in its own
right.

The ledger is append-only for auditability; it never mutates the knowledge directly. Batch processes
(consolidation, §11) read it to recompute quality and confidence. **Negative signals are first-class** —
rejections and failures are as valuable as approvals and successes. In Phase 2 the ledger is built and
populated; the algorithms that consume it to adapt ranking and extraction are Phase 3.

**Vision alignment.** The expert user as oracle (CurationEvent is the gold signal); outcome validation as
the truth anchor; learning that is auditable and accumulates before it adapts.

---

## 7. Knowledge Graph Design

The graph is the reasoning fabric and turns retrieval into understanding. It has two layers — a
**structural** layer (CodeSymbols and their dependencies, schema relationships) built deterministically
at ingest, and a **semantic** layer (requirements, standards, regulations, decisions, fixes and their
relationships) inferred and corroborated. Nodes are KnowledgeItems, CodeSymbols, Projects, and
SourceArtifacts; edges are the typed relationships of §2.

**Storage direction:** the graph lives in MSSQL as the durable, always-available representation,
traversed relationally; a dedicated graph store is a *future optional accelerator* only if traversal
depth and scale demand it — exactly the role the vector store plays for vectors. This preserves MSSQL as
the source of truth and keeps the platform operable on MSSQL alone.

**Edge lifecycle:** **Proposed** (from a parser at high confidence, or inferred by a model at lower
confidence) **→ Reviewed → Active → Decayed**. Structural edges are typically auto-active; semantic and
causal edges with low confidence are queued for the user. Edges carry confidence and provenance so the
reasoning layer can weight them and so contradictions can be surfaced.

**Primary uses:** impact analysis (what is affected by a change), traceability (requirement → code →
test → fix → regulation), contradiction detection (conflicting approved knowledge), graph-augmented
retrieval (§10), onboarding maps, and cross-project pattern recognition.

**Vision alignment.** MSSQL-resident graph (truth-preserving, degradable); relationships as curated
knowledge; the structural layer grounded in deterministic parsing; causal/temporal edges enabling the
engineering questions the vision targets.

---

## 8. Vector Strategy

Vectors find knowledge by meaning and are one of three retrieval modes, never the whole of retrieval.
They are **derived from MSSQL and always rebuildable**; the EmbeddingRecord tracks which model and
dimension produced each vector so multiple representations coexist and re-embedding is clean.

**Direction:**

- **Payload-rich and filterable.** Each vector carries scope, knowledge type, status, authority,
  project, and effective date, so the search filters on these *inside* the nearest-neighbor query —
  enforcing precedence and keeping deprecated or low-quality knowledge out of authoritative results.
- **Namespaced by domain and by model/dimension.** Vectors of different embedding models or dimensions
  are never mixed in one collection; domains with different retrieval semantics are separated.
- **Type-aware chunking with small-to-big retrieval.** Code is chunked by symbol, SQL by object,
  documents by section, regulations by clause; matching happens on small chunks, but the larger parent
  is injected for context.
- **Multiple representations per item where useful** — for example a summary embedding, a raw-content
  embedding, and a signature embedding — serving different query intents.
- **Hybrid retrieval.** Dense semantic vectors are combined with lexical exact-identifier matching and
  structured filters, then reranked by a capable model. Pure similarity misses precise names (tables,
  error codes, regulation numbers) that engineering needs.
- **Boosts, not cutoffs.** Approved-first and specific-over-general are scoring boosts blended with
  semantic score, authority, and recency — never hard gates.
- **Hygiene.** On deprecate, delete, or supersede, the corresponding vectors are updated or removed
  (closing the current gap). Re-embedding on model upgrade runs as a background job; MSSQL remains the
  source of truth so vectors are always reconstructable.
- **Graceful degradation.** With the vector store absent, retrieval falls back to lexical and structural
  modes; the platform never depends on vectors to function.

**Vision alignment.** Vectors derived and rebuildable; MSSQL-only operability preserved; filtering
enforces scope/authority; hybrid + rerank reflects the three-mode retrieval principle.

---

## 9. Approval Workflow

Approval is the gate of trust and the highest-quality learning signal. It governs **promotion, not
participation**: low-risk activity flows freely, while becoming authoritative requires human approval.
The single expert user is the approver, so **low friction is a first-order requirement**.

**States:** **Draft → NeedsReview → Approved → (Deprecated / Superseded)**, with **Rejected** as a
terminal-but-retained state (negative knowledge) and, for fixes, an intermediate **Validated** state
(build/tests passed) before review.

**Transitions and triggers:**

- Extraction produces **Draft** knowledge and **Proposed** edges.
- Corroboration across sources, or a positive outcome, can auto-advance an item to **NeedsReview** and
  place it on a prioritized queue.
- The user **Approves** (item becomes authoritative and enters high-priority retrieval), **Rejects**
  (retained as negative knowledge), **Edits** (creates a new version), or **Merges** duplicates.
- A superseding item moves the prior one to **Deprecated**, and vector hygiene removes its stale vectors.

**What approval gates:** knowledge becoming authoritative; edges becoming Active; FixRecords becoming
reusable FixPatterns; and, in later phases, high-risk autonomous change. Constraint knowledge
(regulations, standards) always requires explicit approval and is then enforced, not merely stored.

**Friction reduction is part of the design:** review queues prioritized by quality and impact, batch
approval, and one-glance review that presents provenance and (for fixes) build/test evidence. Every
approval, rejection, edit, and merge is recorded as a CurationEvent — the gold learning signal — and the
payoff of approval is made visible so the curation discipline sustains itself.

**Vision alignment.** Approval gates promotion not participation; the expert user as oracle; constraints
enforced; curation captured as the strongest learning signal.

---

## 10. Retrieval Pipeline

Retrieval delivers grounded, cited knowledge at the moment of need, and it always works on MSSQL alone,
degrading capability rather than failing when AI infrastructure is absent.

1. **Query understanding.** Classify the request — factual, impact, how-to, debugging, architecture — to
   select strategy and graph-traversal policy.
2. **Scope and constraint resolution.** Determine the active project/context and always load the
   applicable constraints (regulations, standards) into a separate guardrail channel.
3. **Multi-mode candidate retrieval, in parallel.** Semantic (vector search filtered by scope, type, and
   status), lexical (exact identifiers — symbols, error codes, clause numbers), and structural (graph and
   relational lookups — dependencies, definitions).
4. **Graph expansion.** From the top candidates, traverse typed edges per the question's policy
   (dependency edges for impact questions; causal and decision edges for "why" questions) to assemble a
   connected subgraph rather than isolated fragments.
5. **Fusion and reranking.** Merge and deduplicate candidates; rerank with a capable model; apply
   authority, quality, recency, and approved-first boosts.
6. **Constraint injection.** Attach the applicable constraints as guardrails, distinct from the retrieved
   context, so generation and review are bound by them.
7. **Context assembly.** Budget-aware, provenance-tagged, small-to-big (matched chunk plus its parent),
   so every grounded claim is traceable.
8. **Feedback capture.** Log a RetrievalEvent recording what was returned and, after the fact, what was
   used — feeding the learning ledger.

**Degradation:** without vectors, the pipeline uses lexical and structural modes; without any model,
keyword plus graph traversal. The output is grounded, cited context ready for reasoning.

**Vision alignment.** Three equal retrieval modes; constraints enforced via a guardrail channel;
provenance-tagged context for traceable reasoning; graceful degradation to MSSQL-only.

---

## 11. Consolidation Pipeline

Consolidation is the background "overnight engine" that keeps the living memory healthy — the
single-user, local setting makes heavy offline work practical. All steps are idempotent and
truth-preserving (they regenerate derived data; they never compromise MSSQL as the source of truth).

1. **Re-extraction.** Re-run extraction from retained raw artifacts with improved extractors or models;
   reconcile results with existing knowledge, raising candidates for changed understanding.
2. **Embedding maintenance.** Embed new or changed chunks, re-embed on model upgrades, and remove
   vectors for deprecated or deleted knowledge; keep EmbeddingRecords versioned.
3. **Graph refresh.** Rebuild structural edges from parsers; infer and corroborate semantic and causal
   edges; queue low-confidence edges for review.
4. **Quality recomputation.** Recompute quality and confidence from provenance strength, corroboration,
   currency, consistency, and validated outcomes drawn from the learning ledger.
5. **Contradiction and duplication detection.** Use the graph and embeddings to surface conflicting or
   duplicate knowledge; route conflicts to the review queue and auto-merge only high-confidence
   duplicates.
6. **Consolidation and summarization.** Distill clusters of related items — for example many FixRecords
   on one component — into higher-level synthesized knowledge (DistilledLessons, FixPatterns) with
   back-links to sources.
7. **Decay and review flagging.** Flag stale, unused, or superseded items as NeedsReview so the memory
   stays current.
8. **Proactive analysis.** Speculatively identify fragile areas or improvement opportunities, with
   evidence, ready for the user — exploiting idle local time to produce value before it is asked for.

**Vision alignment.** The memory is alive (consolidation, decay, contradiction resolution); derived data
regenerated from immutable truth; quality driven by outcomes; the overnight engine turning idle local
capacity into compounding memory.

---

## Vision alignment summary

| MASTER_VISION principle | Where honored |
|---|---|
| Memory-first | Generic backbone + typed graph as the center of the design; models serve retrieval/reranking |
| MSSQL is the source of truth | Graph and provenance in MSSQL; vectors/embeddings/chunks derived and rebuildable |
| Raw permanent, extracted regenerable | SourceArtifact immutable; re-extraction and re-embedding from raw in consolidation |
| Grounded in executable truth | FixRecords carry build/test results; executable-linked knowledge types |
| Expert user as oracle | CurationEvent as the gold signal; low-friction approval workflow |
| Knowledge constrains as well as informs | Constraint types + guardrail channel + GovernedBy/AppliesTo edges |
| Local-first, model-pluggable, graceful degradation | Multi-mode retrieval degrades to MSSQL-only; EmbeddingRecord abstracts models |
| Outcome + human anti-drift | OutcomeEvents anchor quality; approval gates authority; negative knowledge retained |
| Approval gates promotion, not participation | Lifecycle states and gated transitions in §9 |

## Suggested implementation sequencing (within Phase 2)

A practical order that keeps each step shippable and reversible: first the **scope/type taxonomies and
the knowledge backbone with provenance and versioning**; then **deterministic extraction** (code and
schema symbols, structural edges) since it is high-confidence and immediately useful; then the
**typed-knowledge promotion paths** (candidates, profiles, requirements/standards/regulations) with the
**approval workflow**; then the **matured vector strategy and the retrieval pipeline**; then the
**FixRecord and LearningRecord ledgers with capture**; and finally the **consolidation engine** that ties
them together. Build one full pass narrowly — one knowledge type end to end through extraction, approval,
retrieval, and consolidation — before broadening, and let the memory's own growth indicate where to
deepen the model next.
