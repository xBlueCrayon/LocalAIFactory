# AI Output Provenance and Approval

> **Status:** Architecture / governance reference.
> **Authoritative source:** `MASTER_VISION.md`.

## 1. The contract

Every output from the optional local LLM is, by construction:

1. **Traceable** — it carries provenance: which model, which version/digest, which prompt, with what
   self-reported confidence.
2. **Marked AI-generated** — it is never presented or stored as if a human wrote it.
3. **Non-authoritative until a human approves it** — no AI output is promoted to authoritative /
   curated knowledge without an explicit human approval action.

These are enforced by entities that already exist in the codebase, not by convention alone:
`ProvenanceEvent`, `ProposedRevision`, `KnowledgeVersion`, and the `PermanenceTier` /
`ProvenanceMethod` enums.

## 2. What rides with every AI output

| Provenance fact | Where it lives | Notes |
|-----------------|----------------|-------|
| Model id + tag + **digest** | `ProvenanceEvent.ExtractorOrModelId` | exact build, e.g. `qwen2.5-coder:14b@<digest>` |
| Method = **Semantic** | `ProvenanceEvent.Method` (`ProvenanceMethod.Semantic`) | distinguishes AI from `Human`, `Import`, `Deterministic` |
| Prompt id / version | `ProvenanceEvent.Reason` | the governed `PromptTemplate.Name`, e.g. `kb.summarize.v3` |
| Source artifact link | `ProvenanceEvent.SourceArtifactId` | the imported file the output is grounded in, if any |
| Confidence | proposal metadata / `ChangeReason` | self-reported, **advisory only** |
| Actor = system | `ProvenanceEvent.Actor` | the automated process, not a person |

`ProvenanceEvent` is **append-only** — never mutated, never deleted — so the lineage of any item is
permanently explainable and survives cross-instance reconciliation (`OriginInstanceId`,
`OriginPackUid`).

## 3. AI output is always marked AI-generated

- An AI-originated change enters the system as a `ProposedRevision` with a paired `ProvenanceEvent`
  whose `Method = Semantic`. The `Semantic` method *is* the AI marking.
- It is never written straight into a `Curated` item. `IPermanenceGuard.IsCurated(tier)` blocks that;
  automated processes call `ProposeRevisionAsync(...)` instead of mutating.
- A new AI-authored item (rare, suggestion-only) starts at `KnowledgeStatus.NeedsReview` /
  `PermanenceTier.Derived`, never `Approved` / `Curated`.

## 4. Human approval before any promotion

Promotion to authoritative knowledge is a **human-only** state transition:

```
ProposedRevision (Status = NeedsReview, Method = Semantic)
        │
   human reviewer (KE-014 queue)
        ├── Approve ──► KnowledgeVersion appended; item promoted to Curated;
        │               approval recorded as a Human/Promotion provenance event
        └── Reject  ──► revision Deprecated; provenance retained for the audit trail
```

Guarantees:

- There is **no code path** that lets the model set its own output to `Approved`. Approval is a
  database action performed by an authenticated human, audited separately (`AuditLog`).
- Self-reported confidence **never** auto-approves and never shortens review. A "99% confident"
  proposal sits in the same queue as any other and waits for a person. Local models are small and
  their confidence is not trustworthy.
- On approval, history is preserved (`KnowledgeVersion`); the prior text is never silently lost.

## 5. Refusal policy

The AI layer must decline rather than guess. A compliant output either grounds itself or refuses:

- **No fabricated citations.** If the model cannot point at a real `SourceArtifactId`, it must not
  invent one. The proposal is flagged "unsourced" and the reviewer sees that flag.
- **No invented authoritative claims.** The model condenses and reorganizes existing curated/imported
  material. It does not introduce banking facts, business rules, or regulatory statements absent from
  the source. Output that does is rejected before it reaches the queue.
- **Refuse on insufficient grounding.** If a summarization/consolidation request lacks enough source
  material to be faithful, the correct output is "insufficient context to propose a change", not a
  plausible-sounding guess.
- **No hidden reasoning as fact.** For reasoning models, `<think>` content is stripped and never
  stored as the answer or a citation (see `Prompt-Governance.md`).

## 6. Offline behaviour

- With no reachable model (the default deployment), there are no AI outputs, no proposals, and
  nothing to approve. Curated knowledge in MSSQL serves normally.
- Reachability is read from the cached health snapshot; the approval/review UI never blocks on a model
  being present.

## 7. Honest limits

- AI is an **optional accelerator for curation velocity**, never an authority. MSSQL + human approval
  is the authority.
- There is **no fine-tuned domain model**; general 14B-class models have no special BDM / MCIB /
  ChequeXpert / ETAMS knowledge. Every output is a draft for a human who knows the domain.
- Provenance makes AI output *accountable*, not *correct*. The human reviewer remains the safeguard,
  and removing the AI layer entirely leaves a fully functional MSSQL-only platform.
