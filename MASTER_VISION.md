# MASTER_VISION.md — LocalAIFactory

> **Status:** Canonical source of truth. This document governs the direction of the entire platform.
> **Repository:** https://github.com/xBlueCrayon/LocalAIFactory
> **Audience:** every contributor, human or AI (Claude Code), working on LocalAIFactory.
> **Scope:** principles, architecture, and direction only. It deliberately contains no implementation
> detail — code, schemas, and APIs live in the codebase and in `CLAUDE.md`/`docs/`, and they must
> conform to this document, never the reverse.

LocalAIFactory is built around one conviction: in an AI-assisted engineering platform, **the memory is
the product and the model is a replaceable engine.** Models will change every few months. The
accumulated, governed, outcome-validated memory of a software estate is the durable asset. Every
decision in this document follows from that conviction.

---

## Foundational Principles

These are non-negotiable. Future work — human or autonomous — must honor them. When a feature and a
principle conflict, the principle wins.

1. **Memory-first.** The platform's value is its curated, linked, evolving knowledge. Models are
   orchestrated around the memory; the memory is never subordinate to a model.
2. **MSSQL is the source of truth.** Everything else — vectors, graph projections, model outputs,
   caches — is derived and rebuildable from it.
3. **Three permanence tiers.** Raw imported artifacts are permanent and immutable. Machine-extracted
   knowledge is regenerable — a projection that can be re-derived as extraction improves, so no early
   extraction decision is load-bearing. Human-curated knowledge — anything a person edited or approved —
   is durable and changes only by human approval. Re-derivation and consolidation may freely regenerate
   the machine-extracted tier; against curated knowledge they **propose** a revision for review and
   **never overwrite it silently**.
4. **Reasoning is grounded in executable truth.** A proposal is correct because it compiles, the tests
   pass, and the query runs — not because a model is confident.
5. **The expert user is the oracle.** Human curation is the highest-quality signal in the system and
   the anchor of trust. The platform is designed around dense, low-friction human judgment.
6. **Knowledge constrains as well as informs.** Standards and regulations are guardrails, not merely
   context to retrieve. Enforcement is progressive: by visibility and flagging while the platform only
   advises, and by hard gating once the platform changes code itself. Fixes teach. Requirements trace.
   Different knowledge behaves differently.
7. **Local-first and model-pluggable.** The platform runs fully on local infrastructure. AI services
   are optional and interchangeable; the platform must always remain usable with MSSQL alone.
8. **Outcome validation and human curation prevent drift.** The compiler, the tests, and the user are
   the objective anchors that keep the memory from believing its own output.
9. **Approval gates promotion, not participation.** Light-touch flow for low-risk work; explicit human
   approval before knowledge becomes authoritative or before high-risk autonomous change.
10. **Graceful degradation is mandatory.** No capability may make a core experience depend on an
    external service being present.

---

## 1. What LocalAIFactory Is

LocalAIFactory is a **local-first, memory-centric software-engineering platform** that understands a
software estate, accumulates governed knowledge about it, reasons over that knowledge with local and
remote AI models, and — under human approval — assists with and ultimately performs engineering work,
learning from every outcome.

It is a system that ingests projects, documentation, source code, database scripts, exported AI
conversations, requirements, and regulatory material; turns them into structured, linked, curated
knowledge; retrieves the right knowledge at the moment of need; and grows more capable the more it is
used. Its purpose is to become the durable engineering memory and reasoning surface for the estate it
serves — initially a banking middleware estate, but the architecture is domain-general.

It is, in its mature form, an engineer's externalized brain: a place where understanding is captured
once and reused forever, where successful fixes become reusable patterns, and where new work begins
from everything the platform already knows.

## 2. What LocalAIFactory Is Not

It is **not a chatbot.** Conversation is an interface, not the product; the product is the memory and
the reasoning grounded in it.

It is **not a stateless code generator.** It does not produce throwaway answers detached from a
project's history, standards, and prior decisions. Every output is grounded in curated knowledge and,
where possible, in executable verification.

It is **not a wrapper around a single model.** No model is central. The platform is architected so
that local models, large open models, and future hosted models are interchangeable engines serving a
permanent memory.

It is **not a search box over documents.** Retrieval is a means; understanding, relationships, and
reasoning are the ends. Flat text retrieval is the floor, not the ceiling.

It is **not dependent on the cloud, on a GPU, or on the internet** to deliver its core value. It runs
locally and degrades gracefully when optional AI infrastructure is absent.

It is **not a system that changes things without accountability.** Knowledge does not become
authoritative, and code does not change in consequential ways, without human approval and a traceable
chain of evidence.

## 3. The Long-Term Vision

The destination is a self-improving engineering platform that has internalized an entire software
estate — its projects, their relationships, the domain language, the applicable standards and
regulations, every architectural decision, and every fix ever made — and that reasons over this memory
with whatever model is best for the task, grounded at every step in what compiles, what passes, and
what the expert user has judged true.

In that state, the platform assists across the full engineering lifecycle: it onboards new projects in
days rather than months because it already knows the patterns and constraints; it debugs by recalling
the causes and remedies of similar past problems; it advises on architecture by tracing decisions and
their consequences; and it generates change that is grounded, verified, and consistent with the
estate's standards. The knowledge compounds. Each engineering episode leaves the memory richer, which
makes the next episode faster and safer — a flywheel whose output is institutional engineering memory
that outlives any individual or any model.

## 4. The Memory-First Philosophy

Memory is the center of gravity. The platform is organized as a layered memory with three strata. A
**raw layer** holds immutable source artifacts — the permanent record of what was imported. An
**episodic layer** holds working context: the current task, recent interactions, and reasoning in
progress. A **semantic layer** holds the durable, curated, linked knowledge graph — the long-term
memory. Knowledge flows upward: raw is extracted into candidate knowledge, validated knowledge is
consolidated into long-term memory, and consolidation distills clusters of detail into higher-level
understanding while preserving links back to sources.

Memory is queryable in three equal modes — semantic (meaning), structural (relationships and
identity), and lexical (exact terms) — because engineering questions need all three, often at once.
And memory is alive: it versions, supersedes, decays, deduplicates, detects its own contradictions,
and revises its confidence from outcomes. A static knowledge store rots; a living memory compounds.

Because the machine-extracted layer is derived, it is rebuildable, which makes aggressive automated
curation safe: if extraction or consolidation goes wrong, it is regenerated rather than lost. The
curated layer is the exception — it is durable, and re-derivation proposes changes to it rather than
overwriting it (Principle 3).

## 5. MSSQL as the Source of Truth

MSSQL is the durable, authoritative store of all knowledge, relationships, provenance, and history.
Vector indexes, graph accelerators, and model outputs are projections of it and can be reconstructed
from it at any time. This is a deliberate architectural commitment with several consequences.

It means the platform is **always operable on MSSQL alone.** Vector search and local models are
performance and capability enhancements layered on top, never prerequisites for core function.

It means **resilience and portability.** Re-embedding to a new model, rebuilding a graph, or replacing
an accelerator is a re-index, not a migration crisis, because the truth never left MSSQL.

It means **a single, auditable record.** Provenance, versions, approvals, and outcomes live in one
authoritative place, which is essential for an engineering memory that must explain not only what it
knows but why and since when.

The discipline to maintain: derived stores must be treated as disposable and reconstructable, and no
knowledge may exist only in a derived store.

## 6. The Knowledge Lifecycle

Knowledge moves through a governed lifecycle — **Draft → NeedsReview → Approved → Deprecated /
Superseded** — that exists to protect quality. Newly extracted knowledge enters as Draft and is kept
out of authoritative retrieval until it earns trust through corroboration, outcome validation, or human
approval. Approved knowledge is injected first and weighted highest; more-specific knowledge takes
precedence over more-general; and constraint knowledge — standards and regulations — is enforced
rather than merely retrieved.

Quality is treated as a first-class, evolving property rather than a fixed label — an interpretable
trust band, not an opaque number, so the user and the models can weight knowledge meaningfully. It is
shaped by provenance strength, corroboration across independent sources, currency, specificity,
consistency with existing knowledge, and — most powerfully — validated outcomes. It is degradation-safe:
when no model or outcome history is present, quality is computed from provenance and corroboration alone,
so no capability depends on AI being available. Knowledge that proves wrong is not
silently deleted; it is retained as negative knowledge so the same mistake is not relearned. The
lifecycle's purpose is singular: keep the signal high as the volume grows.

## 7. The Learning Lifecycle

The platform learns from three signals of increasing strength: implicit usage (what was retrieved and
used versus ignored), explicit curation (the expert user's approvals, rejections, edits, and merges),
and outcome validation (whether the build and tests passed when knowledge was relied upon). These
signals feed a continuous loop in which retrieval adapts to what actually helps, confidence and quality
update from outcomes, extraction improves from the user's corrections, and successful problem-to-fix
traces become reusable playbooks.

Negative learning is first-class: failed approaches, rejected suggestions, and contradicted knowledge
are retained as signals to avoid repetition. The anchor that keeps learning honest is objective
outcome — because compilation and tests are ground truth, the memory cannot indefinitely reinforce its
own errors. The direction of travel is from a system that is configured to a system that is *taught*,
and, with a single expert user, taught deeply and personally.

## 8. The Reasoning Lifecycle

Reasoning is a grounded loop: **Retrieve → Ground → Plan → Act → Verify → Reflect.** It begins by
assembling relevant, trustworthy knowledge across all three retrieval modes; grounds the task in that
knowledge and in the estate's constraints; plans an approach; acts; verifies against executable truth;
and reflects, leaving new knowledge behind. Every reasoning step is traceable to the knowledge it
used, so any proposal arrives with its rationale — the rule, the prior fix, the requirement, the
dependency that justifies it. This traceability is what makes the platform's reasoning auditable and,
therefore, trustworthy.

Reasoning is not a single model thinking in isolation; it is a division of cognitive labor across
models, each in the role it is best suited to, all grounded in the same memory and the same executable
checks. Successful reasoning is itself captured, so the next similar problem starts from a proven path
rather than from nothing.

## 9. The Autonomous Engineering Lifecycle

The platform's engineering work follows a guarded loop: **Understand → Snapshot → Propose → Build →
Test → Verify → Approve → Capture.** It identifies the relevant parts of a project, takes a reversible
snapshot, proposes a change grounded in memory, builds and tests it against executable truth,
self-critiques against the applicable standards, and presents a verified, explained result for human
approval — after which the successful fix is distilled into reusable knowledge and the outcome updates
the memory's confidence.

Autonomy advances along a ladder, gated by demonstrated success and risk: from suggesting, to proposing
with evidence, to running the full propose-build-test loop and presenting a result, to acting
semi-autonomously on low-risk, well-patterned change classes. Two rules hold at every rung: change
happens only inside a reversible sandbox, never silently against a live system; and every consequential
change is approved and recorded. The goal is for the platform to do the work and present a grounded,
verified, one-glance-approvable result, with the human's judgment as the gate — not the bottleneck for
every step.

## 10. The Knowledge Graph Vision

Relationships are the reasoning fabric. The platform models knowledge as a typed graph in two layers: a
**structural layer** capturing how code and data fit together — dependencies, references, schema
relationships — and a **semantic layer** linking requirements, code, tests, standards, regulations, and
fixes. Edges are typed and meaningful (depends-on, implements, governed-by, supersedes, derived-from,
fixed-by, contradicts, applies-to), and the most valuable edges are temporal (how something came to be)
and causal (this change caused this defect; this fix resolved this incident), because the questions
engineers most want answered are causal.

The graph turns retrieval into reasoning: relevant context is found by meaning and then expanded by
following relationships, assembling a connected, grounded picture rather than a bag of fragments. The
graph also powers impact analysis, traceability, and contradiction detection, and it connects the
estate across projects so that a pattern learned once is recognized everywhere. Relationships are
themselves knowledge — they carry confidence and provenance and are curated like any other knowledge.

## 11. The Vector Search Vision

Vector search exists to find knowledge by meaning, and it is one mode of retrieval among three, not the
whole of retrieval. Its direction is toward semantic indexes that are filterable by the knowledge's
scope, type, status, and authority, so that relevance and precedence are honored at retrieval time;
toward representations shaped by the kind of knowledge — code understood by structure, documents by
section, data and regulations by their natural units — rather than by arbitrary fragmentation; and
toward hybrid retrieval that blends meaning with exact-identifier precision, because engineering work
demands both the gist and the precise name.

Crucially, vector indexes are derived from the authoritative store and are always rebuildable, which
makes evolving the underlying representation a re-index rather than a risk. Pure semantic similarity is
treated as a strong signal to be combined with structure, authority, and validated outcomes — and
reranked by capable models — never as the sole arbiter of relevance.

## 12. The Local AI Runtime Vision

The platform's intelligence runs locally by default. Local models provide reasoning, generation, and
embedding without dependence on the cloud, and the platform remains fully functional when no model is
present — losing AI assistance, never losing its memory or its core workflows. The runtime is built so
that adding or removing AI capability is a configuration choice, not a code change, and so that the
absence of a GPU, a model, or the internet degrades the experience gracefully rather than breaking it.

The local runtime is the foundation; remote models are an extension of it for the hardest problems. The
platform's job is to keep intelligence close to the memory and the code, fast and private, and to make
the memory — not any particular model — the constant.

## 13. The Multi-Model Vision

No single model is sufficient or permanent, so the platform orchestrates many. It routes work to the
model best suited to it: fast local models for routine extraction and generation, large reasoning
models for deep analysis, reranking, and architecture, and future hosted models for the most demanding
problems — all grounded in the same memory and the same executable verification. Model choice is a
policy over a permanent knowledge substrate, and that policy itself evolves as new models arrive and as
outcomes reveal which models serve which roles best.

The strategic stance is constant: models are rented and replaceable; the memory is owned and durable.
The platform is designed to absorb better models as they appear without disturbing the asset that makes
it valuable.

## 14. The Human Approval Philosophy

Human approval is not bureaucracy; it is the trust anchor and the highest-quality learning signal in
the system. The expert user is the oracle, and the platform is designed to make their judgment dense,
fast, and consequential. Approval governs two transitions: knowledge becoming authoritative, and change
becoming real. Low-risk activity flows with light-touch review so the platform stays useful; promotion
to authoritative knowledge and high-risk autonomous change require explicit human approval and a
traceable chain of evidence.

Because the approval loop is the gate through which quality and trust pass, reducing its friction is a
first-order architectural priority. Equally important is making the payoff of approval visible — the
user must see that knowledge they approved makes later work faster and safer — because that visible
return is what sustains the curation discipline on which the entire flywheel depends. Every approval,
rejection, and correction is captured as gold-standard signal that teaches the platform what good looks
like.

## 15. Success Metrics

Success is measured by engineering outcomes and memory health, not by activity or model benchmarks.
The platform should demonstrate, over time, improvement in: retrieval relevance (the right knowledge
surfaces at the moment of need); fix success rate (proposed changes that build and pass); reduction in
time-to-fix and time-to-onboard; knowledge reuse (how often prior understanding and past fixes are
applied again); and curation efficiency (high-signal approval at low friction).

The memory itself is measured for coverage and quality: growth of trustworthy, corroborated,
outcome-validated knowledge; a declining rate of unresolved contradictions and stale items; and the
strength and usefulness of the relationship graph. Above all, the platform should be able to show its
own learning curve — evidence that it is getting better as it is used. A self-improving system that
cannot demonstrate its improvement has not earned the discipline it requires.

## 16. Phase Roadmap: Phase 1 → Phase 5

The arc is deliberate: the platform first **exists**, then **understands**, then **reasons**, then
**acts**, then **self-improves.** Each phase deepens the memory's maturity.

| Phase | Theme | Direction | Outcome |
|---|---|---|---|
| **1 — Foundation** | The memory exists | MSSQL-primary curated knowledge with a governed approval lifecycle; project ingestion; keyword and optional vector retrieval; local chat; fully operable in MSSQL-only mode; runtime-stable. | A reliable curated memory that can be trusted to run anywhere, with or without AI infrastructure. *(Achieved.)* |
| **2 — Understanding** | The memory becomes structured | Distinct knowledge types promoted to first-class concepts (requirements, regulations, standards, data and schema knowledge, code structure, fixes); provenance-rich extraction blending deterministic structure with semantic meaning; richer scope and precedence; the typed knowledge graph and a maturing vector strategy begin. | The platform understands code, documents, SQL, and conversations as structured, linked knowledge rather than flat text. |
| **3 — Reasoning** | The memory reasons and begins to learn | Graph-augmented, multi-mode, reranked retrieval; grounded, cited reasoning; the feedback loop turns on, with outcome-driven confidence and retrieval that adapts to the user. | Relevant knowledge surfaces precisely, reasoning is traceable, and the memory improves from use. |
| **4 — Autonomy** | The memory acts | The full guarded engineering loop — understand, snapshot, propose, build, test, verify, approve, capture — with failure memory, reusable playbooks, and measured progression along the autonomy ladder for low-risk change. | The platform fixes and evolves real projects under human approval, learning from every outcome. |
| **5 — Self-Improvement** | The memory compounds | A continuous, background engine that re-extracts with better models, consolidates, resolves contradictions, and analyzes proactively; cross-project intelligence; a demonstrable learning curve. | Institutional engineering memory: new projects onboard in days because the platform already knows the domain, the patterns, and the constraints. |

---

## Using This Document

This document defines *why* and *what direction*. The codebase, `CLAUDE.md`, and `docs/` define *how*.
When they diverge, this document and its Foundational Principles prevail, and the lower-level documents
should be corrected to match.

Before beginning any significant work, a contributor — human or Claude Code — should confirm that the
work advances the current phase without violating a foundational principle, and in particular that it
preserves memory as the center of gravity, MSSQL as the source of truth, executable grounding,
graceful degradation, and human approval as the gate of trust. Features come and go; this direction
holds.
