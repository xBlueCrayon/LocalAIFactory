# Expected Capabilities — Honest Today vs. Future

> **What LocalAIFactory is, and is not, here.** LocalAIFactory is a local-first
> software/code + data **understanding and governance** platform. It does **NOT**
> manage, configure, monitor, or connect to network devices. It does not push
> configuration, does not poll equipment, and is not a substitute for a network
> management system or change-orchestration tool. There is **no claim** of
> compatibility, equivalence, or certification with any vendor's networking product.
>
> The **transferable capability** under test is *dependency/impact reasoning over a
> curated model* and *change governance with an approval lifecycle and audit trail*.
> The network scenario is a conceptual transfer test, not a product capability.

---

## The transferable core

The platform already reasons over imported software/data estates: it builds a
curated, approval-gated knowledge model, retrieves cited evidence, and answers
questions about structure and impact. A network estate, modeled **as data** (a
dependency graph plus governance rules in MSSQL), is the same shape of problem.
The reasoning — transitive impact closure, evidence citation, rule-driven
governance, append-only audit — transfers. The *acting on devices* does not, and
is out of scope by design.

---

## Today (achievable now, with modeling effort)

- **Ingest network-as-data**: import config/state **exports as files** and a
  CMDB-style dependency model; store in MSSQL; project large text columns to
  lightweight list rows.
- **Curated dependency memory**: dependency facts and governance rules captured
  with an **approval lifecycle**; approved facts injected first into reasoning.
- **Impact / blast-radius reasoning**: traverse the dependency graph to a transitive
  affected-set, bounded by edge type and confidence/criticality — reusing existing
  impact-analysis thinking.
- **Evidence-cited answers**: every asserted dependency resolves to a stored evidence
  row; ungrounded claims are flagged, not asserted.
- **Change governance**: deterministic mapping of an impact set to required approver
  tier and freeze-window conflicts via `GovernanceRule` rows.
- **Coverage/gap reporting**: surface thin or stale parts of the model so blast radius
  is never silently under-reported.
- **Append-only audit**: queries, retrieved evidence, and approval decisions recorded
  in a tamper-evident chain; read-only auditor views.
- **MSSQL-only operation**: full functionality with no Qdrant and no Ollama; optional
  services accelerate but are never required, and never block a request path.
- **Advisory rollback drafting**: assemble a back-out plan from prior `ConfigArtifact`
  state — as a human-reviewed artifact, never auto-applied.

## Near future (modest extension)

- **Semantic dependency recall** via optional embeddings to surface implicit relationships
  not captured as explicit edges (still evidence-gated before assertion).
- **Local-model narrative summaries** of blast-radius reports for non-expert readers.
- **Confidence-tiered impact ranking** to combat alert fatigue on large changes.
- **Freeze-calendar reasoning** across overlapping windows and recurring blackout periods.
- **Diff-aware ingestion** to flag dependency drift between successive config exports.

## Future (larger investment, still in-scope as understanding/governance)

- **What-if simulation** over hypothetical topology edits (model-only, never device).
- **Cross-change conflict detection** when multiple pending changes overlap in blast radius.
- **Risk-trend analytics** and learned governance suggestions from historical outcomes.

## Explicitly out of scope (by design, not by limitation to be "fixed")

- Connecting to, polling, or authenticating against network devices.
- Pushing, applying, or rolling back configuration on equipment.
- Real-time monitoring, alerting, or telemetry collection.
- Acting as a network management, orchestration, or assurance system.
- Any claim of vendor compatibility, equivalence, or certification.

---

## Honesty summary

The platform brings **repeatable, auditable impact reasoning and change governance
over a curated model** to a network-change context. It does **not** bring device
management. The value is making the *decision-support thinking* — what breaks, who
approves, how to back out, with evidence — fast, consistent, and locally auditable.
