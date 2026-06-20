# Multi-Agent Knowledge Factory

> **Status: DESIGN + skeleton path. NOT yet implemented.** Today the repository has a single-task
> `AgentTask`/`AgentStep` chat orchestration (Plan/Retrieve/Generate/Review steps) — that is **not** the
> knowledge factory described here. The factory entities and orchestration below are a proposed,
> additive design with an entity skeleton; nothing in it overwrites approved truth or runs autonomously
> at scale.
> **Authority:** subordinate to `MASTER_VISION.md`. Where this conflicts, the vision prevails.

## 1. The idea

The knowledge factory is a fleet of **specialised agents that work in parallel** to enrich the platform's
memory — extracting structure, distilling fixes, authoring documentation, evaluating domain workflows —
and that **only ever produce proposals plus evidence**. Approval by a human (or a clearly-gated policy)
is the sole path by which any agent output becomes authoritative. This is MASTER_VISION §8 ("a division
of cognitive labour across models, all grounded in the same memory and the same executable checks") and
§14 (human approval as the gate of trust), expressed as an architecture.

The factory's defining rule, repeated everywhere below: **agents never overwrite approved truth
directly.** MSSQL-approved knowledge is the source of truth; JSON packs are seed format; conflicts are
resolved by provenance + confidence + approval; every output is traceable.

## 2. The agents (roles)

Each agent has a narrow remit, its own contracts (see `docs/Agent-Roles-and-Contracts.md`), and a
proposal+evidence output (see `docs/Agent-Proposal-Approval-Workflow.md`). They run in parallel and never
write to each other's approved outputs.

| Agent | Remit (proposes…) | Grounded in |
|---|---|---|
| **Orchestrator** | the run plan; fans out tasks; consolidates; never produces knowledge itself | the task graph |
| **Code Graph** | `CodeSymbol`/`CodeEdge` evidence + code-structure insights | deterministic extraction (C#/T-SQL/Python today) |
| **SQL / DB** | SQL objects + `AccessesSql` bridge edges + data-dictionary candidates | deterministic SQL extraction |
| **Knowledge Pack** | new/updated pack items (general + domain) | curated baseline; pack authoring rules |
| **Chat Learning** | Decision/FixPattern/Rule/Snippet/Prompt proposals from transcripts | the chat-import pipeline |
| **Documentation** | doc/manual knowledge + summaries (extractive, no hallucination) | imported docs/PDFs |
| **UI/UX** | usability findings, demo/screenshot gaps | the running app surfaces |
| **ERP/CRM** | advisory designs + integration contracts (synthetic fixtures) | ERP/CRM knowledge + fixtures |
| **Core Banking** | advisory banking-workflow designs + contracts (synthetic) | banking knowledge + fixtures |
| **Management Workflow** | management-company workflow designs | workflow playbooks |
| **SMTP/SFTP/SDK** | integration templates + adapter findings | deploy templates + adapter boundary |
| **LLM/RAG** | retrieval/ranking improvements; model-routing findings | the RAG path (optional Ollama/Qdrant) |
| **OCR/CNN** | document-intelligence findings (detection ≠ verification) | PDF analyser + cheque-risk skeleton |
| **Market Intelligence** | market signals/forecasts as proposals (heavily disclaimed) | governed market sources (design) |
| **Security/Audit** | RBAC/audit findings; never relaxes a control on its own | the security model |
| **Deployment** | deployment/runbook findings | release scripts + runbooks |
| **QA/Benchmark** | benchmark results, golden-snapshot diffs | the benchmark harness |
| **Performance** | profiling findings; query/timing regressions | `RequestTimingMiddleware`, bounded-query rules |
| **Final Consolidation** | a merged, de-duplicated proposal set for review | all of the above |

> Naming note: the existing `AgentTask`/`AgentStep` entities are for **chat task orchestration**, not the
> factory. To avoid collision, the factory introduces its own `AgentRun` (and related) entities — see §5.

## 3. Operating rules (non-negotiable)

1. **Parallel, isolated.** Agents work in parallel and do not mutate one another's outputs. The
   Orchestrator owns fan-out/fan-in.
2. **Proposals + evidence only.** Every agent emits `AgentFinding`s carrying `AgentEvidence`; promotion to
   knowledge is always a *proposal*.
3. **Never overwrite approved truth.** Any change to a `Curated` knowledge item goes through
   `IPermanenceGuard.ProposeRevisionAsync(...)`. No agent has a write path to approved knowledge.
4. **MSSQL-approved knowledge is the source of truth.** Agent outputs are derived and rebuildable; JSON
   packs are seed format.
5. **Conflicts resolved by provenance + confidence + approval.** When two agents disagree, an
   `AgentConflict` is recorded; resolution favours stronger provenance and higher confidence, but the
   final arbiter is human approval.
6. **Everything traceable.** Each finding/proposal links to its run, its agent, its evidence, and (on
   approval) to a `ProvenanceEvent` — so any approved item can be traced back to the agent and inputs
   that produced it.
7. **Graceful degradation.** Agents that need Ollama/Qdrant degrade or skip when those are absent; the
   factory runs (with reduced reach) on MSSQL alone.
8. **Audited.** Factory runs and approvals are recorded in the operational `AuditEvent` trail.

## 4. Lifecycle of a factory run

```
Orchestrator: create AgentRun (scope: estate / project / artifact)
      │  fan out AgentTasks to role agents (parallel)
      ▼
each agent: gather inputs → produce AgentFindings (+ AgentEvidence)
      │  Final Consolidation: dedup, detect AgentConflicts
      ▼
AgentProposals: for each finding worth promoting
      │   - fresh        → Draft KnowledgeItem proposal
      │   - curated hit  → IPermanenceGuard.ProposeRevisionAsync
      ▼
Human review (or clearly-gated policy)
      │  approve → Approved + Curated + ProvenanceMethod.Human-equivalent provenance
      │  reject  → retained as negative signal
      ▼
MSSQL = source of truth (agents never wrote here directly)
```

## 5. Proposed entity skeleton

Additive, schema-frozen-respecting design (a real migration would be additive only). These are
**proposed**, not yet present.

```csharp
// A factory run: one orchestrated fan-out/fan-in pass over a scope.
class AgentRun {
    int Id; Guid Uid;
    int? ProjectId;                 // null = estate-wide
    string Scope;                   // "estate" | "project" | "artifact"
    AgentRunStatus Status;          // Pending/Running/Consolidating/Completed/Failed/Cancelled
    DateTime CreatedUtc; DateTime? CompletedUtc;
    string CreatedBy;               // actor
}

// A unit of work handed to one role agent within a run.
class AgentTaskItem {              // distinct from the existing chat AgentTask
    int Id; int AgentRunId;
    string AgentRole;               // "CodeGraph" | "SqlDb" | "ChatLearning" | ...
    string Input;                   // scope/parameters
    AgentTaskStatus Status;         // reuse existing enum (Pending..Cancelled)
    DateTime CreatedUtc; DateTime? CompletedUtc;
}

// A single thing an agent found (proposal candidate, pre-promotion).
class AgentFinding {
    int Id; Guid Uid; int AgentTaskItemId;
    KnowledgeType ProposedType;     // reuse Core enum
    KnowledgeScope ProposedScope;
    int? ProjectId;
    string Title; string Content;   // rendered proposal body
    double Confidence;              // conservative
    string ContentHash;             // dedup
}

// Evidence backing a finding (executable/structural/source — never fabricated).
class AgentEvidence {
    int Id; int AgentFindingId;
    string Kind;                    // "CodeSymbol" | "SqlObject" | "BuildResult" | "TranscriptSpan" | "Source"
    string Reference;               // NormalizedKey / artifact id / span offsets / registry source id
    string Detail;                  // verbatim, captured as-is
}

// A finding promoted toward knowledge (the proposal record).
class AgentProposal {
    int Id; Guid Uid; int AgentFindingId;
    int? TargetKnowledgeItemId;     // set when proposing a revision to an existing curated item
    ProposalKind Kind;              // NewItem | Revision
    ProposalStatus Status;          // Pending/Approved/Rejected/Superseded
    int? ProposedRevisionId;        // links to IPermanenceGuard's ProposedRevision when Kind = Revision
}

// A recorded disagreement between findings/proposals.
class AgentConflict {
    int Id; Guid Uid; int AgentRunId;
    int FindingAId; int FindingBId;
    string Reason;                  // "contradiction" | "duplicate" | "scope-mismatch"
    ConflictResolution Resolution;  // Unresolved/PreferA/PreferB/Merged/Escalated
    string? ResolvedBy;             // human actor on approval
}
```

## 6. How it maps to existing mechanisms

- **`IPermanenceGuard`** is the chokepoint for any revision to curated knowledge. An `AgentProposal` of
  `Kind = Revision` calls `ProposeRevisionAsync(...)` and stores the returned `ProposedRevision` id — the
  same path chat import and the pack installer already use. No new overwrite path is created.
- **`ProvenanceEvent`** records lineage on approval: `Method` could be `Autonomous` (already in the enum)
  with the agent id in `ExtractorOrModelId` and the run in the reason, so an approved item traces back to
  the exact agent and inputs.
- **`AuditEvent`** records factory runs and approvals (who/what/when), distinct from provenance.
- **Benchmark harness** is how the QA/Benchmark agent grounds its findings in executable truth (golden
  snapshots, exit-code gating) — proposals about understanding are checked, not asserted.

## 7. Honest status

- **Implemented:** single-task chat orchestration (`AgentTask`/`AgentStep`), the deterministic extractors
  the Code Graph / SQL agents would wrap, the chat-import pipeline (minimal), `IPermanenceGuard`,
  `ProvenanceEvent`, the benchmark harness, RBAC + audit.
- **Design only (this document):** the `AgentRun`/`AgentTaskItem`/`AgentFinding`/`AgentEvidence`/
  `AgentProposal`/`AgentConflict` skeleton, the role fleet, parallel fan-out/fan-in, and conflict
  resolution.

The scorecard records the multi-agent factory as **Low / design**. The exact proof to advance it: the
skeleton entities created plus **one** role agent producing proposals + evidence routed through
`IPermanenceGuard`, captured, with **nothing auto-approved**.
