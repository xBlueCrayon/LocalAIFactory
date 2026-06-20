# Agent Roles and Contracts

> **Status: DESIGN.** Defines the remit, inputs, outputs and hard constraints of each knowledge-factory
> agent. None of these agents is implemented yet; the existing `AgentTask`/`AgentStep` is chat
> orchestration, not a factory agent. Companion to `docs/Multi-Agent-Knowledge-Factory.md` and
> `docs/Agent-Proposal-Approval-Workflow.md`.
> **Authority:** subordinate to `MASTER_VISION.md`.

## 1. The contract every agent honours

Before the per-role contracts, the **universal contract** (binding on all agents):

1. **Output is proposals + evidence only.** An agent emits `AgentFinding`s with `AgentEvidence`. It never
   writes an `Approved`/`Curated` knowledge item directly.
2. **No overwrite of approved truth.** A revision to a curated item goes through
   `IPermanenceGuard.ProposeRevisionAsync(...)`. There is no other write path to curated knowledge.
3. **No fabricated evidence.** Evidence references real artifacts/symbols/spans/sources, captured
   verbatim. No invented citations, metrics, or sources.
4. **Conservative confidence.** Confidence reflects observable signal strength, not model self-assurance.
5. **Stay in remit.** An agent proposes only within its declared scope; out-of-remit observations are
   handed to the Orchestrator, not acted on.
6. **Degrade gracefully.** If an optional dependency (Ollama/Qdrant) is absent, the agent reduces scope or
   skips — it never blocks the run.
7. **Traceable + audited.** Every finding links to its run/task/evidence; runs and approvals are audited.

## 2. Per-role contracts

For each agent: **Inputs → Proposes → Evidence → Hard constraints.**

### Orchestrator
- **Inputs:** a run scope (estate / project / artifact).
- **Proposes:** a run plan and task fan-out; a consolidated proposal set at fan-in. **Produces no
  knowledge of its own.**
- **Evidence:** the task graph and consolidation decisions.
- **Constraints:** owns fan-out/fan-in only; cannot approve; records `AgentConflict`s for the reviewer.

### Code Graph agent
- **Inputs:** imported source artifacts (C#/T-SQL/Python today).
- **Proposes:** `CodeSymbol`/`CodeEdge` evidence and code-structure insights (e.g. high-complexity hot
  spots, dependency clusters).
- **Evidence:** deterministic extraction keyed on `SourceLocusKey`/`NormalizedKey`.
- **Constraints:** syntactic only (no semantic model); reports `ExtractionStatus` honestly
  (NotAttempted/Extracted/NoSymbols/Unsupported/ParseError) — never a silent zero.

### SQL / DB agent
- **Inputs:** SQL scripts/objects; C#/Python members with SQL string literals.
- **Proposes:** SQL `CodeSymbol`s, `AccessesSql` bridge edges, data-dictionary candidates.
- **Evidence:** the SQL object, the literal span, the resolved `schema.object` → `NormalizedKey` join.
- **Constraints:** bridge confidence is **< 1.0** (syntactic string evidence) and must say so.

### Knowledge Pack agent
- **Inputs:** curated baseline + domain knowledge gaps.
- **Proposes:** new/updated pack items (manifest + category files) per the authoring guide.
- **Evidence:** the source summary + declared registry sources (no verbatim proprietary text).
- **Constraints:** original summaries only; user-edited baseline items are proposed-not-overwritten.

### Chat Learning agent
- **Inputs:** ChatGPT/Claude exports, pasted transcripts.
- **Proposes:** Decision/FixPattern/Rule/Do-not-repeat/CodeSnippet/Prompt candidates.
- **Evidence:** transcript span offsets; author role; accompanying code.
- **Constraints:** never auto-approves; scopes per segment (general vs project); honours the chat-import
  rules doc exactly.

### Documentation agent
- **Inputs:** imported docs, READMEs, PDFs.
- **Proposes:** documentation knowledge + **extractive** summaries (no hallucination).
- **Evidence:** the source document hash + the extracted spans.
- **Constraints:** summaries are extractive; PDF text needs a real parser (current limitation noted).

### UI/UX agent
- **Inputs:** the running app surfaces; demo script; screenshot inventory.
- **Proposes:** usability findings, missing-screenshot gaps, demo-flow improvements.
- **Evidence:** the page, the timing, the captured observation.
- **Constraints:** advisory; never changes UI on its own.

### ERP/CRM agent
- **Inputs:** ERP/CRM knowledge + synthetic fixtures (`erp-crm-industrial`).
- **Proposes:** advisory designs + integration contracts (API/migration/extensibility).
- **Evidence:** the fixture answers (benchmark proof-of-vision) + named knowledge.
- **Constraints:** explicitly **not** vendor-equivalent or certified; fixtures are synthetic.

### Core Banking agent
- **Inputs:** banking knowledge + synthetic fixtures (`core-banking-integration`).
- **Proposes:** advisory banking-workflow designs + integration contracts (posting/mandate/claim/
  settlement/reconciliation) with maker-checker/idempotency controls.
- **Evidence:** fixture answers + contracts.
- **Constraints:** **awareness-only on regulation; not compliance-certified.** No regulatory certainty.

### Management Workflow agent
- **Inputs:** management-company workflow playbooks/scenarios.
- **Proposes:** workflow designs.
- **Evidence:** named playbooks/scenarios.
- **Constraints:** advisory; no delivered workflow claimed.

### SMTP/SFTP/SDK agent
- **Inputs:** deploy templates + the adapter boundary.
- **Proposes:** integration templates and adapter findings.
- **Evidence:** the template + the mockable adapter test.
- **Constraints:** no live relay/transfer claimed until a real endpoint test is captured.

### LLM/RAG agent
- **Inputs:** the retrieval/ranking path (optional Ollama/Qdrant).
- **Proposes:** retrieval/ranking improvements; model-routing findings.
- **Evidence:** measured retrieval comparisons where available.
- **Constraints:** optional services; models are replaceable engines, never authoritative truth.

### OCR/CNN agent
- **Inputs:** PDF analyser + cheque-risk skeleton.
- **Proposes:** document-intelligence findings — **detection, never verification**.
- **Evidence:** OCR DTOs (which a future CV service would populate) + analyser output.
- **Constraints:** never a fraud verdict; human-review-required; no real CV model yet.

### Market Intelligence agent
- **Inputs:** governed market sources (design only).
- **Proposes:** market signals/forecasts as **heavily-disclaimed proposals**.
- **Evidence:** source registry entries (reliability/freshness) + captured data.
- **Constraints:** forecast/risk intelligence, **not investment advice**; respects API terms/robots/
  rate-limits; no fragile scraping as authoritative truth.

### Security/Audit agent
- **Inputs:** the security model, RBAC config, audit trail.
- **Proposes:** findings (gaps, anomalies).
- **Evidence:** the control, the audited event.
- **Constraints:** **never relaxes a control on its own**; deny-by-default is sacrosanct.

### Deployment agent
- **Inputs:** release scripts + runbooks.
- **Proposes:** deployment/runbook findings.
- **Evidence:** the script + the (dry-run or executed) result.
- **Constraints:** distinguishes dry-run from executed; no production rollout claimed unless captured.

### QA/Benchmark agent
- **Inputs:** the benchmark harness (pinned repos, golden snapshots, tiers).
- **Proposes:** benchmark results, snapshot diffs, regressions.
- **Evidence:** exit-code-gated runs, golden snapshots.
- **Constraints:** grounds understanding in **executable truth**; no invented metrics.

### Performance agent
- **Inputs:** request timing, query shapes.
- **Proposes:** profiling findings, regressions.
- **Evidence:** `RequestTimingMiddleware` traces.
- **Constraints:** enforces bounded-query rules (no `GroupBy`-constant, lightweight list projections).

### Final Consolidation agent
- **Inputs:** all agents' findings.
- **Proposes:** a merged, de-duplicated proposal set for review, with `AgentConflict`s flagged.
- **Evidence:** dedup decisions, conflict records.
- **Constraints:** consolidates only; cannot approve.

## 3. Conflict handling between agents

When two findings disagree, the Final Consolidation agent records an `AgentConflict` (reason:
contradiction / duplicate / scope-mismatch). Provisional resolution favours **stronger provenance** and
**higher confidence**, but the conflict is escalated to the reviewer — the human is the arbiter. Nothing
is auto-resolved into curated knowledge.

## 4. Status

All roles above are **design**. The deterministic capabilities several roles would wrap (code/SQL/Python
extraction, the C#↔SQL bridge, the chat-import pipeline, the benchmark harness, RBAC/audit) exist today;
the **agent layer that orchestrates them as a fleet does not**. See the scorecard (multi-agent factory:
Low / design).
