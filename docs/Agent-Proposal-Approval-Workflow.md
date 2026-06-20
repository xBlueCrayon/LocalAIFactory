# Agent Proposal & Approval Workflow

> **Status: DESIGN.** How a knowledge-factory agent's output travels from finding → evidence → proposal →
> human approval → authoritative knowledge — **without ever overwriting approved truth**. Companion to
> `docs/Multi-Agent-Knowledge-Factory.md` and `docs/Agent-Roles-and-Contracts.md`.
> **Authority:** subordinate to `MASTER_VISION.md`.

## 1. The one rule this workflow exists to enforce

> **An agent never makes its own output authoritative.** Promotion to `Approved`/`Curated` knowledge is a
> human decision (or a clearly-gated, audited policy), routed through `IPermanenceGuard` whenever it
> touches curated knowledge.

Everything below is the machinery that guarantees that rule while still letting agents do useful work in
parallel.

## 2. The states

A finding moves through these states; each transition is recorded and (on promotion) audited.

```
AgentFinding (candidate)
   │  worth promoting?  ── no ──► retained as run output (not knowledge)
   ▼ yes
AgentProposal
   ├─ Kind = NewItem    → creates a Draft KnowledgeItem proposal
   └─ Kind = Revision   → calls IPermanenceGuard.ProposeRevisionAsync → ProposedRevision
   │
   ▼  human review (or gated policy)
   ├─ Approved  → Approved + Curated KnowledgeItem  (+ ProvenanceEvent, Method = Autonomous, agent id)
   ├─ Rejected  → retained as negative signal (teaches the agent)
   └─ Superseded → a later proposal replaced this one
```

`ProposalStatus`: `Pending` → `Approved` / `Rejected` / `Superseded`.

## 3. Step by step

### Step 1 — Finding + evidence

The agent produces an `AgentFinding` (proposed type/scope/title/content/confidence/hash) with one or more
`AgentEvidence` rows. Evidence is **real and verbatim** — a `CodeSymbol` `NormalizedKey`, a SQL object, a
build result, a transcript span, or a declared registry source. No evidence, no proposal.

### Step 2 — Dedup

The finding's `ContentHash` is checked against existing knowledge (same scope). Exact match ⇒ the finding
is dropped or attached to the existing item as corroboration (raising its confidence), **not** duplicated.
Near-match (embeddings, when available) ⇒ flagged for the reviewer.

### Step 3 — Proposal creation

- **No curated collision** → `AgentProposal { Kind = NewItem }` creates a **Draft** `KnowledgeItem`
  (routed to `NeedsReview`), scoped and typed, carrying provenance that names the run and agent.
- **Curated collision** (the finding would change an existing `Curated` item) → `AgentProposal
  { Kind = Revision }` calls `IPermanenceGuard.ProposeRevisionAsync(targetType, targetId,
  originalKnowledgeItemId, proposedTitle, proposedContent, changeReason, RevisionSource, ct)` and stores
  the returned `ProposedRevision` id. **The curated item is not touched.**

### Step 4 — Conflict consolidation

The Final Consolidation agent records `AgentConflict`s for contradictory or duplicate findings. Provisional
ordering favours stronger provenance + higher confidence; the conflict is **escalated to the reviewer**,
not auto-resolved into curated knowledge.

### Step 5 — Human review

The reviewer sees proposals on the existing knowledge/review surfaces, each with its evidence and
provenance, and may:

- **Approve** → the proposal becomes an `Approved`, `Curated` `KnowledgeItem`. A `ProvenanceEvent` is
  written (`Method = Autonomous`, `ExtractorOrModelId` = the agent id, `Actor` = the approver, reason =
  the run). The human approval is the trust anchor (MASTER_VISION §14).
- **Edit then approve** → as above, with the human's edits stored as a new `KnowledgeVersion`.
- **Reject** → retained as **negative signal** so the agent learns what not to propose. Never silently
  discarded.

### Step 6 — Audit

The factory run and every approval/rejection are recorded in the operational `AuditEvent` trail
(who/what/when/which-project). Provenance (knowledge lineage) and audit (operational action) are kept
distinct, as elsewhere in the system.

## 4. Conflict-resolution policy

| Situation | Resolution |
|---|---|
| Two findings assert contradictory rules | `AgentConflict` (contradiction); escalate; human decides; loser retained as context |
| Two findings are duplicates | dedup by hash; keep one; the other becomes corroboration |
| A finding contradicts an **approved** curated item | proposed revision via `IPermanenceGuard`; the approved item stands until a human approves the change |
| Findings differ only in scope | `AgentConflict` (scope-mismatch); reviewer assigns scope |

The ordering signals are **provenance strength** then **confidence**, but the **final arbiter is human
approval**. No agent and no automated rule promotes a contested finding to authoritative on its own.

## 5. Why approved truth is safe

- The **only** write path to curated knowledge is `IPermanenceGuard` — the same chokepoint the pack
  installer and chat import already use. Agents have no alternative route.
- Approved/curated items are protected by the `ProvenanceMethod.Human` anchor signal: once a human has
  touched an item, automated changes are *proposed*, never applied.
- Agent outputs are **derived and rebuildable**; if a run is wrong, its proposals are rejected and the
  authoritative store is unchanged (no rollback crisis).

## 6. Gated-policy promotion (future, optional)

MASTER_VISION §9 allows light-touch flow for low-risk work. A future, **clearly-gated and audited**
policy could auto-approve narrowly-scoped, low-risk proposal classes (e.g. a new derived `CodeSymbol`
insight that matches a passing benchmark). Even then:

- the gate is explicit, configured, and audited;
- it never applies to `Curated` knowledge revisions (those always need a human);
- it never relaxes a security control;
- it is off by default.

This is the autonomy ladder applied to knowledge promotion — advanced only by demonstrated success, never
assumed.

## 7. Status

This workflow is **design**. The promotion chokepoint (`IPermanenceGuard`), the lineage record
(`ProvenanceEvent`, including `Method = Autonomous`), the audit trail (`AuditEvent`), and the review
surfaces exist today. The agent layer that feeds them — `AgentFinding`/`AgentEvidence`/`AgentProposal`/
`AgentConflict` and the run orchestration — is the proposed skeleton path described here and in
`docs/Multi-Agent-Knowledge-Factory.md`. Scorecard: **Low / design**.
