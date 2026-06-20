# Knowledge Separation and Retrieval Rules

> **Status:** Design + implemented-core reference. The canonical rules for separating general / project /
> chat knowledge and for ordering retrieval. Companion to
> `docs/Knowledge-Architecture-General-Project-Chat.md` and `docs/Knowledge-Pack-Authoring-Guide.md`.
> **Authority:** subordinate to `MASTER_VISION.md`.

## 1. The separation model

All knowledge lives in one MSSQL `KnowledgeItem` table. Separation is by **discriminator columns and
provenance**, not by separate stores. The three primary classes:

| Class | Discriminator | Tier (typical) | Enters retrieval |
|---|---|---|---|
| **General** (professional baseline, domain packs) | `KnowledgePackId != null`, `ProjectId == null` | `Curated` | as global backdrop |
| **Project** (extracted/authored per project) | `ProjectId != null`, `KnowledgePackId == null` | `Derived` (machine) or `Curated` (human) | scoped to that project |
| **Chat-imported** | `SourceType ∈ {ChatGptExport, ClaudeExport, ConversationTranscript}` | `Draft` until approved | **only after approval** |

Supporting structural classes (`CodeSymbol`/`CodeEdge`, `AccessesSql` SQL-graph edges) live in their own
lean tables and act as **evidence** joined into retrieval by `NormalizedKey`, not as curated assertions.

## 2. Separation rules (invariants)

1. **General never silently becomes project, and vice versa.** Scope (`ProjectId`) is set at creation
   (pack install ⇒ null; project extraction ⇒ the project; chat import ⇒ detected or null) and changes
   only by human action.
2. **Baseline is overwrite-protected.** `KnowledgePackId != null` items are `Tier = Curated`; automated
   processes propose revisions via `IPermanenceGuard`, never overwrite.
3. **Chat-imported is quarantined until approved.** It is review-surface-only and excluded from
   authoritative retrieval until a human approves it (writing a `ProvenanceMethod.Human` event).
4. **Project scope is also a security boundary.** Project knowledge is visible only to callers with a
   `ProjectAccess` grant (deny-by-default, IDOR-guarded, enforced server-side by RBAC). Separation here
   is enforced, not advisory.
5. **Provenance always travels with the item.** Every item can answer *how/why/since-when* via its
   append-only `ProvenanceEvent` chain — distinct from the operational `AuditEvent` trail.

## 3. Retrieval precedence (canonical order)

When assembling context, candidates are gathered across lexical + structural + (optional) semantic modes,
then ordered by the following precedence — highest first. This implements MASTER_VISION §6.

1. **Status gate.** Only `Approved` items are eligible for authoritative injection. `Draft` /
   `NeedsReview` are review-surface-only; `Deprecated`/`Superseded` are excluded.
2. **Constraint enforcement.** `KnowledgeScope.Standards` / `Regulatory` items at `AuthorityLevel.Binding`
   are surfaced as **guardrails** regardless of similarity — enforced, not merely retrieved.
3. **Authority order.** `Binding` > `High` > `Normal` > `Low`.
4. **Specificity.** For a project question, `ProjectId`-scoped items out-rank global items. For a general
   question, global curated knowledge is the baseline.
5. **Permanence.** Between equals, `Curated` (human-anchored) out-ranks `Derived` (machine-extracted).
6. **Quality / confidence.** Higher `Confidence` / `QualityBand` ranks higher within a tier.
7. **Recency.** Tie-breaker only.

## 4. Scoping resolution at query time

```
query (caller, optional projectId)
  │  access check: caller's UserRole + ProjectAccess grant
  ▼
candidate set = project-scoped (if projectId & granted)  ∪  global curated
  │  status gate (Approved only for authoritative)
  ▼
apply constraint enforcement (Binding standards as guardrails)
  │  order by authority → specificity → permanence → quality → recency
  ▼
assembled context (each item carries its provenance)
```

- If `projectId` is supplied but the caller lacks a grant, project knowledge is **excluded** (the query
  does not silently leak it).
- If no `projectId`, only global knowledge is in scope (plus structural evidence that is itself global).

## 5. How the three classes interact in one answer

A typical project query returns a blend:

- **Project-scoped curated** business rules and architecture decisions (highest specificity).
- **Project-scoped derived** code symbols and `AccessesSql` edges as supporting evidence (lower
  confidence, clearly machine-extracted).
- **Global curated** baseline pack items as general backdrop (e.g. an EF Core or RBAC standard).
- **Binding standards** surfaced as guardrails irrespective of score.
- **Approved chat-imported insights**, if any, ranked by their (conservative) confidence.

Unapproved chat proposals do **not** appear in this authoritative answer — they live on the review
surface only.

## 6. MSSQL-only behaviour

Every rule above holds with Qdrant and Ollama absent:

- Semantic retrieval degrades to lexical + structural; the **precedence ordering is unchanged**.
- The status gate, scope filtering, authority enforcement and access control are pure MSSQL operations.
- No retrieval path depends on an external service being present (Principle 10).

## 7. Honest limitations (current)

- The ranker does **not yet weight baseline pack items specially** — baseline items participate via normal
  retrieval but are not boosted (a known limitation noted in the pack design).
- Full **authority-order enforcement** and **outcome-driven confidence** are on the Phase-2 path; the
  enums (`AuthorityLevel`, `QualityBand`) exist and the status gate + scope filtering are active, but not
  every precedence rule is fully behavioural yet.
- **Near-duplicate detection** and **embedding-based scoping** require Qdrant; without it, only exact-hash
  dedup and lexical/identifier scoping apply.

These limitations are recorded so retrieval behaviour is never overclaimed — consistent with "no silent
blind spots" and "honesty over hype".
