# Conversation-to-Knowledge Extraction Rules

> **Status:** Design (classification rules). Companion to `docs/Chat-Import-Knowledge-Learning.md` (intent)
> and `docs/Chat-Learning-Pipeline.md` (stages). The minimal extractor implements a first cut of these
> rules over markdown/text transcripts.
> **Authority:** subordinate to `MASTER_VISION.md`.

These rules define **how a conversation segment is classified into a knowledge candidate**. They are
written to be deterministic and precision-favouring: when a segment is ambiguous, prefer *no candidate*
(it can be re-extracted later from the retained raw transcript) over a noisy proposal. Every candidate is
a **proposal**, never authoritative knowledge.

## 1. Segmentation precondition

Classification runs over the segments produced by the pipeline's Stage 1:

- Split by **speaker** (`User` / `Assistant` / `System` turns).
- Sub-split by **heading** (`#`, `##`, …) within a turn.
- Isolate **fenced code blocks** (```) as code segments with their declared language.

Each segment carries its `ChatRole` and its character offsets into the raw transcript. The **author role
matters**: a `User` assertion of a rule is a stronger signal than an `Assistant` suggestion of one.

## 2. Candidate types and their triggers

For each segment, evaluate the triggers below. A segment may match more than one type (e.g. a fix plus a
code snippet); emit each independently.

### 2.1 Decision → `KnowledgeType.ConversationInsight` / `ArchitectureDecision`

**Triggers (any):**
- Decision verbs: "decided", "we'll use", "going with", "chose", "settled on", "let's standardise on".
- Rationale connector following a choice: "… because …", "… so that …".
- A heading like "Decision", "Approach", "Plan".

**Capture:** the decision statement + its stated rationale + any alternatives mentioned. Map to
`ArchitectureDecision` when it concerns structure/technology; otherwise `ConversationInsight`.

### 2.2 Bug-fix → `KnowledgeType.FixPattern`

**Triggers (any):**
- A problem marker ("error", "exception", "failing", "bug", "broken", a stack-trace line) **followed by**
  a resolution marker ("fixed by", "the fix was", "resolved", "turned out", "root cause").
- A before/after code pair.

**Capture (the playbook shape):** *symptom → root cause → fix → verification (if stated)*. These are the
highest-value reusables (MASTER_VISION §7) — a problem-to-fix trace becomes a playbook. If a code block
accompanies the fix, link it and raise confidence.

### 2.3 Reusable rule → `KnowledgeType.BusinessRule` / `Standard`

**Triggers (any):**
- Imperative generality: "always", "never", "must", "should", "prefer", "avoid", "by convention".
- A normative statement not tied to one incident ("connection strings go in environment variables").

**Capture:** the rule as an imperative statement + applicability/scope. Map to `Standard` when it reads as
a cross-cutting engineering standard; `BusinessRule` when domain/business-specific. A binding-sounding
rule is still proposed at normal authority — **elevation to `AuthorityLevel.Binding` is a human decision**,
not the extractor's.

### 2.4 Do-not-repeat → negative knowledge

**Triggers (any):**
- Failure-of-approach: "didn't work", "doesn't work", "broke things", "regretted", "avoid this", "dead
  end", "rolled back".

**Capture:** the failed approach + why it failed. Stored as a `FixPattern` flagged **negative**, so the
same mistake is not relearned. Negative knowledge is first-class (MASTER_VISION §6/§7) — never discarded.

### 2.5 Code snippet → `CodeSnippet` content

**Triggers:**
- A fenced code block of non-trivial size with surrounding intent (the prose explaining what it is for).

**Capture:** the code verbatim + declared language + the intent sentence(s). If the snippet's symbols
match a known project's `CodeSymbol` `NormalizedKey`, scope it to that project and note the linkage;
otherwise it is a general snippet. Trivial one-liners and obvious boilerplate are dropped.

### 2.6 Prompt → prompt-library candidate

**Triggers:**
- A segment that *is* a reusable instruction/prompt and is described as effective ("this prompt worked
  well", "use this prompt to…"), or an explicitly labelled "Prompt:" block.

**Capture:** the prompt text + its stated purpose. Routed to the prompt library under
`docs/Prompt-Governance.md`. Prompts are proposals like any other knowledge.

## 3. Scoping each candidate

Apply the pipeline's project detection per segment:

- Explicit user assignment > identifier/symbol match > (optional) embedding similarity > fallback to
  general (`ProjectId = null`).
- A candidate referencing a project's symbol, table, or file path is scoped to that project (subject to
  the caller's `ProjectAccess`).

## 4. Confidence assignment (rule-level)

Start each candidate at a conservative base by type (fixes with code highest among chat-derived; bare
mentions lowest), then adjust:

| Signal | Effect |
|---|---|
| Imperative/explicit phrasing | + |
| Asserted by a `User` turn (vs proposed by `Assistant`) | + |
| Accompanying code block (fix/decision) | + |
| Corroborated by another segment or existing approved knowledge | + |
| Hedged language ("maybe", "I think", "possibly") | − |
| Single passing mention, no rationale | − |

Chat-derived confidence is **capped below** the band reserved for human-approved or outcome-validated
knowledge. Only human approval or a passing build/test can earn that band.

## 5. Dedup before emission

Normalise (trim, collapse whitespace, strip volatile tokens) and SHA-256 hash each candidate. Exact
same-scope match ⇒ drop. Near match (when embeddings available) ⇒ `DuplicateStatus.Candidate` for human
resolution. Never auto-merge into a curated item.

## 6. Emission contract (the non-negotiables)

For every surviving candidate:

1. **Emit as a proposal.** New item ⇒ `Draft` / `NeedsReview`. Collision with a curated item ⇒
   `IPermanenceGuard.ProposeRevisionAsync(...)` (`RevisionSource.Extraction`). **Never** overwrite curated
   knowledge.
2. **Never auto-approve.** No path in extraction sets `KnowledgeStatus.Approved` or `Tier = Curated`.
   Those transitions belong to a human (writing a `ProvenanceMethod.Human` event).
3. **Record provenance.** `ProvenanceEvent` with `Method = Import`, the importing actor, and a reason that
   names the source conversation and the segment offsets.
4. **No fabricated sources.** References are captured exactly as the transcript states them. The extractor
   never invents a citation, file, ticket, or standard number.
5. **Audit it.** The operational `AuditEvent` trail records the import action and who performed it.

## 7. Worked examples

| Segment (paraphrased) | Author | Classification | Scope | Notes |
|---|---|---|---|---|
| "We decided to use `FromSqlRaw` here because EF couldn't express the hierarchy." | User | Decision (`ArchitectureDecision`) | project (matches a symbol) | rationale captured |
| "The 500 was a null `ProjectId`; fixed by defaulting to global. ```c# … ```" | Assistant | FixPattern + CodeSnippet | project | code raises confidence |
| "Always store connection strings in environment variables, never in config." | User | Rule (`Standard`) | general | proposed at normal authority |
| "Tried caching the whole graph in memory — it blew up on large estates, rolled it back." | User | Do-not-repeat (negative) | general | retained as negative knowledge |
| "This prompt reliably extracts SQL objects from a proc: …" | User | Prompt | general | to prompt library |

## 8. Current state

The minimal extractor implements speaker/heading/code-fence segmentation and a first cut of the Decision
/ FixPattern / Rule / CodeSnippet / Prompt triggers, with exact-hash dedup and proposal emission honouring
the §6 contract. Native export parsing, embedding-based scope/dedup, and symbol-linked snippet scoping are
roadmap. The scorecard records chat import as **Partial / design** accordingly.
