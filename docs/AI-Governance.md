# AI Governance — LocalAIFactory

How AI is governed in LocalAIFactory. The single rule that everything below operationalises: **AI is
an optional accelerator, never an authority.** MSSQL plus human approval is the authority; AI output
is a **proposal**, not truth, and has no write path to curated knowledge.

> This is the governance-policy view. The mechanism detail lives in
> [`AI-Output-Provenance-and-Approval.md`](AI-Output-Provenance-and-Approval.md); the proposal queue
> in [`Agent-Proposal-Approval-Workflow.md`](Agent-Proposal-Approval-Workflow.md); prompt rules in
> [`Prompt-Governance.md`](Prompt-Governance.md). Authoritative source: `MASTER_VISION.md`.

---

## 1. Ollama is optional; MSSQL works without an LLM

- The local LLM (Ollama, e.g. `qwen2.5-coder:14b`) is **optional and off by default**
  (`Ollama.Enabled=false` in the example configs).
- With **no reachable model**, there are **no AI outputs, no proposals, and nothing to approve** —
  curated knowledge in MSSQL serves normally. Reachability is read from a cached health snapshot; the
  approval/review UI never blocks on a model being present
  ([`Offline-Mode-Guide.md`](Offline-Mode-Guide.md)).
- Therefore AI quality is **never on the critical path** for the system of record. Removing the AI
  layer entirely leaves a fully functional MSSQL-only platform.

---

## 2. AI outputs are PROPOSALS, not truth

Every output from the optional LLM is, by construction:

1. **Traceable** — it carries provenance (which model + tag + **digest**, which prompt, with what
   self-reported confidence).
2. **Marked AI-generated** — `ProvenanceEvent.Method = Semantic` *is* the AI marking. It is never
   presented or stored as if a human wrote it.
3. **Non-authoritative until a human approves it** — no AI output is promoted to authoritative /
   curated knowledge without an explicit human approval action.

These are enforced by entities that already exist: `ProvenanceEvent`, `ProposedRevision`,
`KnowledgeVersion`, and the `PermanenceTier` / `ProvenanceMethod` enums. A new AI-authored item starts
at `NeedsReview` / `Derived`, **never** `Approved` / `Curated`.

---

## 3. Provenance + approval (propose-never-overwrite)

The chokepoint is **`IPermanenceGuard`**:

- `IPermanenceGuard.IsCurated(tier)` **blocks** any direct write to a `Curated` item. Automated
  processes call `ProposeRevisionAsync(...)` instead of mutating — the **propose-never-overwrite**
  rule.
- An AI-originated change enters as a `ProposedRevision` paired with a `Semantic` `ProvenanceEvent`,
  and lands in the human review queue.
- Promotion to authoritative knowledge is a **human-only** state transition. There is **no code
  path** that lets the model set its own output to `Approved`.
- Self-reported confidence **never** auto-approves and never shortens review — a "99% confident"
  proposal waits in the same queue as any other. Local models are small and their confidence is not
  trustworthy.
- On approval, `KnowledgeVersion` is appended (history preserved); on rejection, the revision is
  retained as a negative signal. `ProvenanceEvent` is **append-only** — never mutated or deleted.

The same path is used by chat import and the knowledge-pack installer — there is no separate overwrite
path anywhere.

---

## 4. Refusal policy (the AI must decline rather than guess)

- **No fabricated citations.** If the model cannot point at a real `SourceArtifactId`, it must not
  invent one; the proposal is flagged "unsourced" for the reviewer.
- **No invented authoritative claims.** The model condenses and reorganizes existing curated/imported
  material; it does not introduce banking facts, business rules, or regulatory statements absent from
  the source. Output that does is rejected before the queue.
- **Refuse on insufficient grounding.** The correct output for an under-sourced request is
  "insufficient context to propose a change", not a plausible guess.
- **No hidden reasoning as fact.** For reasoning models (e.g. `deepseek-r1:14b`), `<think>` content is
  stripped and never stored as the answer or a citation.

---

## 5. No AI authority over curated knowledge

This is the boundary, stated plainly:

- AI can **propose**. It cannot **approve**, **overwrite**, or **delete** curated knowledge.
- There is **no fine-tuned domain model**; general 14B-class models have no special BDM / MCIB /
  ChequeXpert / ETAMS knowledge. Every output is a draft for a human who knows the domain.
- Provenance makes AI output **accountable**, not **correct**. The human reviewer remains the
  safeguard.

---

## 6. Audit

Approvals are audited separately from provenance (`AuditEvent` / `AuditLog`): who approved what, when.
The audit is **append-only by convention**; it is **not yet tamper-evident** (no hash chaining) — see
[`Audit-Model.md`](Audit-Model.md) §4 and [`Known-Limitations.md`](Known-Limitations.md) §4 for the
proof required to close that gap.

---

## 7. Multi-agent and other AI surfaces (status)

- The **multi-agent knowledge factory** is **design + entity skeleton only**; every proposed agent
  emits proposals + evidence routed through `IPermanenceGuard`, with nothing auto-approved
  ([`Multi-Agent-Knowledge-Factory.md`](Multi-Agent-Knowledge-Factory.md)).
- The **market intelligence** module is design-only and pre-committed to strict disclaimers — no
  advice, no guarantees, no live trading ([`Market-Module-Disclaimers.md`](Market-Module-Disclaimers.md)).
- **Autonomous engineering** runs under a command policy with approval gates and never self-promotes
  ([`Autonomous-Engineering-Status.md`](Autonomous-Engineering-Status.md)).

In all cases the governing rule is identical: AI proposes under provenance; humans approve; MSSQL is
the authority. No regulatory, financial, fraud, or certification claim is made about any AI output.
