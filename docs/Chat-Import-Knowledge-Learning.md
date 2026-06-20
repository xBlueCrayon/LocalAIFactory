# Chat-Import Knowledge Learning

> **Status:** Design. A *minimal* markdown/text chat extractor is being implemented in parallel; this
> document describes the full pipeline and the contract it must honour. Nothing here auto-approves
> knowledge.
> **Authority:** subordinate to `MASTER_VISION.md`.

## 1. Purpose

Exported AI conversations (ChatGPT, Claude) and pasted chat transcripts are a dense, under-exploited
source of engineering knowledge: decisions taken, bugs fixed, reusable rules established, dead-ends to
avoid, and code snippets worth keeping. LocalAIFactory imports them and distils **proposals** — never
authoritative knowledge — that a human curator can approve into durable memory.

This honours MASTER_VISION §7 (the learning lifecycle): the platform learns from explicit curation, and
the expert user's approval is the highest-quality signal. Chat import is a *candidate generator*; the
human is the oracle.

## 2. What goes in, what comes out

**Input** — one of:

- A ChatGPT export (`SourceType.ChatGptExport`).
- A Claude export (`SourceType.ClaudeExport`).
- A pasted/uploaded markdown or plain-text transcript (`SourceType.ConversationTranscript`).

**Output** — zero or more **proposed `KnowledgeItem`s**, each:

- in `KnowledgeStatus.Draft` / routed to `NeedsReview` — **never `Approved`**;
- typed (`KnowledgeType.ConversationInsight`, `FixPattern`, `BusinessRule`, `CodeSymbol`, …);
- scoped (general `ProjectId = null`, or a detected `ProjectId`);
- carrying a `ProvenanceEvent` (`Method = Import`, actor = the importing user, reason naming the source
  conversation) so its lineage back to the transcript is preserved;
- deduplicated against existing knowledge by content hash.

The raw transcript itself is retained as an immutable artifact (a `Raw`-tier source), so re-extraction
with a better extractor later is always possible.

## 3. The honest boundary (what this is *not*)

- It does **not** treat a model's chat output as truth. A confident assistant message is *evidence of a
  claim*, not a verified fact. Proposals are flagged as conversation-derived and ranked accordingly.
- It does **not** invent citations. If the transcript references a file, ticket, or standard, the
  extractor captures the reference verbatim as stated; it never fabricates a source.
- It does **not** auto-approve, auto-merge into curated items, or overwrite anything. The
  `IPermanenceGuard` propose-never-overwrite rule is absolute here.

## 4. Pipeline stages (overview)

The detailed mechanics live in `docs/Chat-Learning-Pipeline.md` (stages, data flow) and
`docs/Conversation-To-Knowledge-Extraction-Rules.md` (classification rules). In brief:

1. **Ingest & retain.** Store the raw transcript as an immutable artifact; record source format.
2. **Normalise & segment.** Split into turns by speaker/heading; preserve order and offsets.
3. **Project detection.** Decide whether the conversation (or a segment) belongs to a known project or to
   general memory.
4. **Candidate extraction.** Classify segments into Decision / FixPattern / Rule / Do-Not-Repeat /
   CodeSnippet / Prompt candidates.
5. **Confidence scoring.** Assign a conservative confidence from signal strength (explicitness,
   corroboration, code presence).
6. **Dedup.** Content-hash each candidate against existing knowledge; drop exact duplicates, flag
   near-duplicates for review.
7. **Propose.** Emit proposals via `IPermanenceGuard` (for any collision with curated knowledge) or as
   fresh Draft items; record provenance.
8. **Review.** A human approves, edits, merges, or rejects. Approval promotes to `Curated`/`Approved`;
   rejection is retained as negative signal.

## 5. Decision/bug-fix/rule/snippet/prompt extraction

The extractor recognises these candidate types (full rules in the rules doc):

- **Decision** — "we'll use X because Y", "decided to…", architecture choices. → `ConversationInsight` /
  `ArchitectureDecision`.
- **Bug-fix / FixPattern** — a stated problem followed by a resolution ("the error was… the fix was…").
  → `FixPattern`. These are high-value reusable playbooks (MASTER_VISION §7).
- **Reusable rule** — a generalisable "always/never" statement. → `BusinessRule` or `Standard`.
- **Do-not-repeat (negative knowledge)** — "this approach didn't work", "avoid…". Retained as negative
  knowledge so the same mistake is not relearned (MASTER_VISION §6/§7).
- **Code snippet** — a fenced code block worth keeping, with its language and surrounding intent. →
  `CodeSnippet` content; may link to a project's code graph if a symbol matches.
- **Prompt** — a reusable, effective prompt worth saving. → a prompt-library proposal (governed by
  `docs/Prompt-Governance.md`).

## 6. Confidence scoring

Confidence is **conservative by default** (a chat claim is weaker than a compiled fact or a human
approval). It rises with:

- **Explicitness** — imperative "always/never/the fix is" beats vague mention.
- **Corroboration** — the same claim appearing across turns or matching existing approved knowledge.
- **Executable proximity** — a fix accompanied by a code block that compiles is stronger than prose.
- **Author** — a human turn asserting a rule outranks an assistant turn proposing one.

Confidence never reaches the band reserved for human-approved or outcome-validated knowledge purely from
chat — that band is earned only by approval or by a passing build/test.

## 7. Dedup (content hash)

Each candidate's normalised content is SHA-256 hashed. An exact hash match against an existing
`KnowledgeItem` (same scope) is dropped as a duplicate (the existing item's review date may be refreshed).
A near-duplicate (reserved for embedding similarity when Qdrant is present) is flagged as a
`DuplicateStatus.Candidate` for human resolution. This mirrors the pack installer's idempotency-on-`Uid`
discipline.

## 8. Propose-not-overwrite (IPermanenceGuard)

If a candidate would change an existing **curated** item, the extractor does **not** write through. It
calls `IPermanenceGuard.ProposeRevisionAsync(...)` with `RevisionSource.Extraction`, producing a
`ProposedRevision` routed to review. Fresh (non-colliding) candidates are written as `Draft`
`KnowledgeItem`s awaiting approval. Either way, **no curated knowledge is mutated without human approval**
— the same invariant the Professional Base Knowledge Pack installer relies on.

## 9. General vs project memory separation

Project detection (see the pipeline doc) decides scope:

- **Confident project match** → `ProjectId` set; the proposal joins that project's knowledge and respects
  its `ProjectAccess` boundary.
- **No/ambiguous match** → `ProjectId = null` (general professional memory), unless the importing user
  explicitly assigns a project.
- A single conversation may yield **both** general and project-scoped proposals (e.g. a generic rule plus
  a project-specific fix). Segments are scoped independently.

This keeps general professional knowledge and project-specific knowledge cleanly separated in the same
MSSQL store, exactly as in `docs/Knowledge-Architecture-General-Project-Chat.md`.

## 10. Current state vs roadmap

- **In progress:** a minimal markdown/text extractor — segment by speaker/heading, classify lines into
  Decision/FixPattern/Rule/CodeSnippet/Prompt, emit proposals, never auto-approve.
- **Roadmap:** ChatGPT/Claude native export parsing, embedding-based near-duplicate detection, project
  detection by code-symbol matching, and outcome-driven confidence updates once a proposal's advice is
  validated by a build/test.

The scorecard records this honestly: chat-imported knowledge is **Partial / design**, with the exact
proof required for 100% being a full segment→classify→propose→approve walkthrough captured with nothing
auto-approved.
