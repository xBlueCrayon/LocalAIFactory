# Phase 2 Knowledge Engine — Refinements & MASTER_VISION Alignment

> **Companion to** `MASTER_VISION.md` (governing) and `Phase-2-Knowledge-Engine-Design.md` (the design).
> **Purpose:** a depth pass. The design established breadth; this document resolves the open tensions
> inside it, makes the under-specified decisions, and proposes surgical clarifications to MASTER_VISION
> where the design revealed ambiguity. No code — decisions and principles only.

Each section states the open question, makes the decision, and notes the vision alignment. The
decisions are intended to be authoritative for Phase 2 implementation.

---

## 1. The permanence model (the most important refinement)

**Open question.** MASTER_VISION says "raw is permanent; extracted is regenerable," and the design has a
consolidation engine that re-extracts and re-embeds in the background. Taken literally, that engine could
regenerate — and therefore overwrite — knowledge a human edited or approved. That is a catastrophic
failure mode: the platform would forget what its expert user taught it. The two-tier framing is missing a
middle tier.

**Decision.** Adopt three permanence tiers, with an explicit reconciliation rule between them.

| Tier | Examples | Permanence | Who may change it |
|---|---|---|---|
| **Raw** | imported source artifacts | Immutable, permanent | Nothing — never edited |
| **Derived** | chunks, embeddings, structural edges, machine-extracted Draft knowledge | Fully regenerable, disposable | Re-extraction / re-embedding / consolidation, freely |
| **Curated** | human-edited or approved knowledge, approved fix patterns, approved edges | Durable — promoted to truth by human judgment | Only by human approval |

The reconciliation rule: **re-derivation proposes, it never overwrites curated knowledge.** When
re-extraction produces content that differs from a curated item, it creates a *suggested revision* — a
new candidate linked to the original, routed to the review queue — and the human decides. Curated items
carry a human-anchored marker and their own version history; the overnight engine may enrich around them
(new links, new related items) but may not silently rewrite them.

**Vision alignment.** Sharpens "raw permanent / extracted regenerable" into a coherent three-tier model;
keeps the expert user as the oracle by making curation durable; lets the consolidation engine run
aggressively without endangering trust.

## 2. Knowledge identity & deduplication

**Open question.** If re-extraction and consolidation run repeatedly, what stops them from multiplying
knowledge (a new item every pass) or losing it (failing to update the right item)? Convergence depends on
stable identity.

**Decision.** Identity is explicit at every tier. Raw artifacts are identified by content hash plus
source path, so re-importing identical content de-duplicates and changed content becomes a new artifact
version. Machine-extracted items have a stable logical identity keyed to their source locus (artifact
plus extraction type and position), so re-extraction *updates the same logical item* rather than spawning
a duplicate. Curated items keep their identity permanently. Across items, near-duplicates are detected by
embedding similarity and recorded as DuplicateOf edges; consolidation merges only high-confidence
duplicates automatically and routes the rest to review, and every merge preserves provenance and links.

**Vision alignment.** Guarantees the consolidation engine *converges* (updates and merges in place)
rather than *diverges* (accumulates duplicates), which a living memory requires; keeps provenance intact.

## 3. The quality model

**Open question.** The design names quality dimensions but not how they combine, how they stay
interpretable, or how they behave when AI is absent.

**Decision.** Quality is an interpretable trust band — for example **Provisional → Corroborated →
Trusted → Authoritative** — not an opaque number, so both the user and the reasoning models can weight
knowledge meaningfully. Composition follows a floor-and-adjust principle: **provenance tier sets the
floor** (human-authored or approved outranks corroborated extraction, which outranks single-source
extraction), and corroboration, currency, consistency, and validated outcomes adjust upward within and
above that floor; contradiction or a failed outcome demotes an item to NeedsReview. Crucially, the model
is **degradation-safe**: when no AI or outcome history is available, quality is computed from provenance,
corroboration, and currency alone — all derivable without a model — so no capability depends on
outcome-model availability. Quality (a trust band) and confidence (a probabilistic estimate) are distinct
but linked; the band drives retrieval tiering and review priority.

**Vision alignment.** Outcome and human curation anchor quality; the band is auditable; degradation
safety preserves MSSQL-only operability.

## 4. Retrieval precedence & constraint enforcement (Phase 2 form)

**Open question.** The design says "boosts, not cutoffs," but a system needs a deterministic ranking, and
"enforce constraints" is ambiguous before any autonomous change exists.

**Decision.** Ranking is deterministic given a fixed memory state. Applicable hard constraints
(regulations, binding standards) are always assembled into the guardrail channel first. Context
candidates are then ordered by a blended score combining semantic relevance, scope authority, quality
band, and recency, with approved-first and specific-over-general applied as boosts and ties broken by
specificity then recency.

"Enforcement" in Phase 2 — before autonomy — means three things: constraints are surfaced prominently to
the user; they are injected into the model's context as an explicit guardrail set, kept separate from
retrieved context; and a review or critique step flags any proposal that appears to violate one. Hard
blocking and automatic correction belong to Phase 4, when the platform changes code itself. Phase 2
enforces by **visibility and flagging**, not by gating change, because there is no autonomous change yet.

**Vision alignment.** Realizes "knowledge constrains as well as informs" at the maturity Phase 2 has
reached, with a clear path to hard enforcement later; makes retrieval reproducible.

## 5. Cold-start and no-model behavior

**Open question.** How does the engine behave with an empty memory, or with no AI model present?

**Decision.** With an empty memory, retrieval returns nothing or low-confidence results *gracefully* and
states that no relevant approved knowledge exists rather than fabricating it; ingestion is the bootstrap.
With no model present, retrieval runs in lexical and structural modes (keyword plus graph and relational
lookups); extraction runs deterministically only (code and schema symbols via parsers), and semantic
extraction is queued as pending work for when a model becomes available; quality is computed from
provenance and corroboration. The platform remains useful on MSSQL alone — browsing, structural
understanding, exact-identifier retrieval, and curation all work.

**Vision alignment.** Graceful degradation and MSSQL-only operability hold at the engine's coldest start.

## 6. Memory stability under model change

**Open question.** "Memory-first, model-pluggable" is in tension with the fact that extraction and
distillation quality depend on the model — so changing models could change the knowledge.

**Decision.** The curated tier is the stability anchor: it does not change when models change. A model
swap re-derives only the regenerable tier — embeddings (versioned per model, so a swap is a re-index that
does not alter the meaning of stored knowledge) and machine-extracted Draft knowledge — and any
divergence from curated items is raised as a *proposed* revision, never an overwrite (per §1). Models thus
change the derived layer and *propose* to the curated layer; they never silently rewrite truth.

**Vision alignment.** Confirms that models are replaceable engines while the memory remains the durable,
stable asset.

## 7. Human as gate, not bottleneck

**Open question.** A single user cannot review everything; if the queue floods, the curation flywheel
stalls — yet the human must remain the gate of trust.

**Decision.** Two mechanisms keep the human the gate without making them the bottleneck. First,
**bounded auto-advancement**: high-confidence, corroborated, outcome-validated Draft items may
auto-advance to NeedsReview and be queued, but not to Approved without human action — except for low-risk
classes the user has explicitly pre-authorized as a policy. Authority always traces to human judgment,
directly or by standing policy. Second, **prioritized, batched review**: the queue is ordered by impact
times uncertainty so the user sees the few items that matter most first; similar items are batched for
approve-many; and high-confidence structural extractions can be bulk-accepted. The payoff is made visible
— surfacing how often approved knowledge was used and how many successful outcomes it contributed to — so
curation stays rewarding.

**Vision alignment.** Approval gates promotion, not participation; friction is minimized; the payoff of
curation is visible, sustaining the loop.

## 8. Suggested MASTER_VISION clarifications

The depth pass surfaced wording in MASTER_VISION that should be sharpened so the vision and the design do
not drift. These are surgical, not directional.

- **Principle 3 (raw/extracted).** Restate as three tiers: raw is permanent; *machine-extracted*
  knowledge is regenerable; *human-curated* knowledge is durable and changes only by approval.
- **Add a derivation rule.** State explicitly that re-derivation and consolidation *propose* changes to
  curated knowledge and never overwrite it silently.
- **Clarify "constrains."** Note that constraint enforcement is progressive — by visibility and flagging
  before autonomy exists, by hard gating once the platform changes code itself.
- **Clarify quality.** State that quality is an interpretable trust band and is degradation-safe (computed
  from provenance and corroboration when AI is absent).

These keep the governing document precise without changing its direction.

## 9. Phase 2 Definition of Done

Phase 2 is complete — and the gate to Phase 3 (adaptive learning) opens — when each component meets its
exit criterion and passes its vision check.

| Component | Done when | Vision check |
|---|---|---|
| Entity model | Typed backbone with provenance and versioning live; raw immutable; identity stable across re-extraction | MSSQL truth; auditable |
| Extraction | Deterministic code/SQL extraction at high confidence; semantic extraction yields Draft with byte-level provenance; re-runnable; reconciliation proposes, never overwrites | Raw→derived; curation durable |
| Knowledge types | Code, schema, regulatory, and fix types first-class; constraints separated from context | Constrains as well as informs |
| Scopes | Precedence and applicability live; deterministic ranking | Specific over general |
| Approval | Lifecycle states and gated transitions; prioritized low-friction queue; curation captured | Approval gates promotion; oracle |
| Retrieval | Three modes plus guardrail channel; degrades to MSSQL-only; RetrievalEvents captured | Three modes; graceful degradation |
| Vectors | Payload-filtered, type-aware chunking, hygiene on deprecate, versioned, rebuildable, optional | Derived/rebuildable; degradation |
| Graph | Structural edges auto, semantic edges reviewed, MSSQL-resident, supports impact and traceability | MSSQL graph; relationships as knowledge |
| Records | FixRecord and LearningRecord ledgers populated (capture only) | Outcome and curation anchors |
| Consolidation | Dedup, quality recomputation, and decay run idempotently and never overwrite curated knowledge | Living memory; truth-preserving |

The exit state: the memory is structured, linked, curated, and *capturing* learning signals — everything
Phase 3 needs to begin adapting retrieval and confidence from real history.

## 10. Recommended first vertical slice

**Decision.** Build the **deterministic structural core first** — code symbols and database-schema
(data-dictionary) knowledge — end to end through extraction, identity/dedup, graph edges, retrieval, and
consolidation, before adding the fuzzier semantic types.

The rationale is risk and value. This slice is parser-driven and high-confidence, so it exercises the
entire pipeline with the least dependence on fuzzy model judgment, which de-risks the plumbing. It is
also the highest daily value for a banking, MSSQL-heavy estate: understanding the code and the database is
the constant need. And it produces the structural graph that everything else — impact analysis,
traceability, the semantic layer, and the fix loop — builds on. Once that core is proven, layer the
semantic types (requirements, standards, regulations, architecture decisions), then the conversation and
fix knowledge that feed the learning loop.

**Vision alignment.** Grounds the first slice in deterministic, executable truth; delivers immediate value
on the actual estate; and lays the structural foundation the rest of the memory depends on.

---

## Summary of decisions

The depth pass settled ten things: a three-tier permanence model with a propose-never-overwrite rule;
explicit knowledge identity and convergent deduplication; an interpretable, degradation-safe quality
band; deterministic retrieval ranking with Phase-2 constraint enforcement by visibility and flagging;
graceful cold-start and no-model behavior; memory stability under model change; a human-as-gate model
that avoids the bottleneck; surgical MASTER_VISION clarifications; a per-component Phase 2 definition of
done; and a deterministic-structural-core first vertical slice. The throughline is the one that matters
most: the platform may regenerate everything derived, but it never forgets what its expert taught it.
