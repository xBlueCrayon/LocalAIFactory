# Chat-Learning Pipeline

> **Status:** Design (pipeline mechanics). Companion to `docs/Chat-Import-Knowledge-Learning.md` (intent)
> and `docs/Conversation-To-Knowledge-Extraction-Rules.md` (classification rules). A minimal extractor is
> being implemented in parallel.
> **Authority:** subordinate to `MASTER_VISION.md`.

This document specifies the **stages and data flow** of importing an AI/chat conversation and turning it
into knowledge proposals. It is deliberately deterministic where it can be (segmentation, hashing,
dedup), and conservative where it cannot (classification, confidence). No stage promotes knowledge to
authoritative — promotion is human approval only.

## Stage 0 — Source intake

Inputs and their `SourceType`:

| Source | `SourceType` | Notes |
|---|---|---|
| ChatGPT export (JSON/markdown) | `ChatGptExport` | roadmap parser for the native export shape |
| Claude export (JSON/markdown) | `ClaudeExport` | roadmap parser for the native export shape |
| Pasted/uploaded markdown or text | `ConversationTranscript` | **the minimal extractor's first target** |

The intake step:

1. Persists the **raw transcript** as an immutable source artifact (`Raw` permanence tier). Re-extraction
   later (with a better extractor) re-reads this artifact; the raw record is never edited.
2. Records the importing user (becomes the provenance `Actor`), the timestamp, and the source format.
3. Computes a transcript-level hash for idempotency (re-importing the identical transcript is a no-op).

## Stage 1 — Normalise & segment

The transcript is split into ordered **segments**. Segmentation is structural and deterministic:

- **By speaker** — turns are delimited by role markers (e.g. `User:` / `Assistant:`, or the export's
  role field). Each turn becomes a segment tagged with its `ChatRole` (`User` / `Assistant` / `System`).
- **By heading** — within a long turn, markdown headings (`#`, `##`) and horizontal rules sub-divide the
  segment, so a single answer covering several topics yields several candidate segments.
- **By fenced code block** — code fences (```) are isolated as code segments, carrying their declared
  language; the surrounding prose is kept as the snippet's intent.

Each segment records its character offsets into the raw transcript so every downstream proposal can point
back to the exact span it came from (traceability, MASTER_VISION §8).

## Stage 2 — Project detection

Decide the scope of the conversation (and of individual segments where they differ):

1. **Explicit assignment.** If the importing user assigned a project at upload, that wins.
2. **Lexical/identifier match.** Segments naming a known project's identifiers — project name, a
   `CodeSymbol.FullName`, a SQL `schema.object`, a file path — are tentatively scoped to that project.
3. **Embedding similarity (optional).** When Qdrant is present, segment embeddings may be compared to a
   project's knowledge centroid for a soft signal. Absent Qdrant, this step is skipped — the pipeline
   degrades gracefully (Principle 10).
4. **Fallback.** No confident match ⇒ `ProjectId = null` (general professional memory).

Detection is **per segment**, so one conversation can produce both general and project-scoped proposals.
Low-confidence project matches are surfaced for the curator to confirm rather than silently committed.

## Stage 3 — Candidate extraction & classification

Each segment is classified into zero or more candidate types using the rules in
`docs/Conversation-To-Knowledge-Extraction-Rules.md`:

- Decision → `ConversationInsight` / `ArchitectureDecision`
- Bug-fix → `FixPattern`
- Reusable rule → `BusinessRule` / `Standard`
- Do-not-repeat → negative knowledge (`FixPattern` flagged negative)
- Code snippet → code content (`CodeSnippet`), language-tagged
- Prompt → prompt-library candidate

A segment with no recognisable candidate is dropped (not every chat line is knowledge). Classification is
intentionally **precision-favouring**: a missed candidate is cheap (re-extract later from the retained
raw transcript); a wrong proposal wastes curator attention.

## Stage 4 — Confidence scoring

Each candidate gets a conservative confidence (see the intent doc §6 for the factors). Scoring is a pure
function of observable signals — explicitness, corroboration, executable proximity, author role — so it
is reproducible and explainable. No model "felt confident" inflates the score.

## Stage 5 — Dedup

For each candidate:

1. Normalise content (trim, collapse whitespace, strip volatile tokens) and SHA-256 hash it.
2. **Exact match** against an existing same-scope `KnowledgeItem` → drop as duplicate (optionally refresh
   the existing item's review date).
3. **Near match** (embedding similarity, when available) → emit a `DuplicateStatus.Candidate` for human
   resolution rather than auto-merging.

## Stage 6 — Proposal emission

The pipeline never writes authoritative knowledge. It emits:

- **Fresh candidate, no curated collision** → a new `KnowledgeItem` in `Draft` (routed to `NeedsReview`),
  typed and scoped, with a `ProvenanceEvent` (`Method = Import`, actor = importer, reason = source
  conversation + segment span).
- **Candidate that would change a curated item** → `IPermanenceGuard.ProposeRevisionAsync(...)` with
  `RevisionSource.Extraction`, producing a `ProposedRevision` routed to review. The curated item is
  untouched.

Every emission is **audited** (the operational `AuditEvent` trail) and **provenanced** (the knowledge
lineage trail) — the two trails are distinct, as elsewhere in the system.

## Stage 7 — Human review & promotion

The curator reviews proposals on the existing knowledge/review surfaces and may:

- **Approve** → status `Approved`, tier `Curated` (writes a `ProvenanceMethod.Human` event — the
  human-anchor signal that protects it from future automated overwrite).
- **Edit then approve** → same, with the human's edits recorded as a new `KnowledgeVersion`.
- **Merge** into an existing item → consolidation, provenance preserved.
- **Reject** → retained as **negative signal** (rejections teach the extractor what not to propose;
  MASTER_VISION §7), never silently discarded.

## Data-flow summary

```
raw transcript (Raw artifact, immutable)
      │  Stage 0 intake (hash, actor, format)
      ▼
segments (by speaker / heading / code fence, with offsets)
      │  Stage 2 project detection (per segment)
      ▼
candidates (Decision / FixPattern / Rule / Negative / Snippet / Prompt)
      │  Stage 4 confidence  +  Stage 5 dedup (content hash)
      ▼
proposals  ──►  Draft KnowledgeItem      (no collision)
           └─►  ProposedRevision via IPermanenceGuard   (curated collision)
      │  Stage 7 human review
      ▼
Approved + Curated  (ProvenanceMethod.Human)   |   Rejected (negative knowledge)
```

## Degradation & determinism

- **MSSQL-only mode:** every stage runs without Qdrant or Ollama. Embedding-based project detection and
  near-duplicate detection simply switch off; lexical/identifier detection and exact-hash dedup remain.
- **Determinism:** intake hashing, segmentation, and exact-hash dedup are fully deterministic. Re-running
  the pipeline on the same transcript yields the same candidates and drops the same duplicates.

## Current state

The minimal extractor implements Stage 0–1 (text/markdown intake + speaker/heading/code-fence
segmentation), a first cut of Stage 3 line classification, exact-hash dedup (Stage 5), and Stage 6
proposal emission with **no auto-approval**. Native ChatGPT/Claude export parsing, embedding-based
project detection and near-dup, and outcome-driven confidence are roadmap.
