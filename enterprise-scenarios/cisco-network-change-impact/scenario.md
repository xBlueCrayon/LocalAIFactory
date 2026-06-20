# Scenario: Network Change-Impact and Configuration Governance

> **Inspiration note.** This is an *original, fictional* scenario inspired by the
> general discipline of network infrastructure change management (the broad
> problem space associated with enterprise routing/switching estates). It is a
> **thought experiment**: can LocalAIFactory's dependency- and impact-reasoning
> transfer to a network-change governance context? It is **not** a network
> management product, does **not** talk to network devices, and makes **no**
> claim of compatibility, equivalence, or certification with any vendor's
> platform or tooling.

The fictional enterprise is **Meridian Continental Bank (MCB)**, a mid-size
retail and corporate bank running a private data-center and branch network. The
"factory" they want is a *governance brain* that reasons over a curated model of
their network estate and tells them — **before** anyone touches a device — what a
proposed change could break, who must approve it, and how to back it out.

---

## Business Problem

MCB's network team executes ~40 change requests a month against core, distribution,
and branch-edge devices. Roughly one in eight causes an unplanned side-effect: a
firewall rule change that silently drops a payment-processor heartbeat, a routing
adjustment that re-paths branch traffic through a saturated link, an access-list
edit that locks out a monitoring collector. Each incident is expensive and erodes
auditor confidence.

The root cause is not bad engineers — it is that the *blast radius* of a change
lives only in senior people's heads. There is no governed, queryable model that
says "this VLAN carries the cheque-clearing batch feed" or "these three branches
depend on that distribution switch." Change reviews are manual, inconsistent, and
slow.

MCB wants a system that ingests their network-as-data (configuration exports,
a CMDB-style dependency model, service mappings) and lets an engineer ask, in
plain language, *"what is the impact if I drain distribution switch DSW-07?"* and
get a reasoned, evidence-cited answer plus a governance checklist.

## Current-State Process

1. An engineer drafts a change in a ticketing tool, free-text.
2. A senior reviewer eyeballs it against tribal knowledge and a stale wiki diagram.
3. The Change Advisory Board (CAB) meets weekly; low-context approvals are common.
4. Implementation happens in a maintenance window; rollback is "re-paste the old config."
5. Post-incident reviews rediscover dependencies that were never written down.

Pain points: impact analysis is non-repeatable, dependency knowledge is uncaptured,
audit evidence is assembled after the fact, and rollback plans are improvised.

## Target-State Process

1. The network estate is imported as data: device/config metadata, a dependency
   graph, and service-to-infrastructure mappings, stored in MSSQL.
2. Approved dependency facts and change-governance rules accumulate as **curated
   project memory** with an approval lifecycle.
3. An engineer describes a proposed change. The system computes a **blast-radius
   set** by traversing the dependency graph and surfaces affected services,
   tenants, and downstream devices, each with cited evidence.
4. Governance rules determine the required approver tier, freeze-window conflicts,
   and mandatory pre-checks.
5. A rollback plan is drafted from the captured prior-state and dependency model.
6. Every query, retrieved fact, and approval decision is written to an append-only
   audit trail.

## Users and Roles

- **Network Engineer** — drafts changes, runs impact queries, reads blast-radius reports.
- **Senior Reviewer / CAB Member** — approves/rejects, curates dependency knowledge.
- **Change Manager** — owns freeze windows, governance rules, and the audit record.
- **Security/Compliance Auditor** — read-only; consumes audit and reporting outputs.
- **Service Owner** — confirms which business services map to which infrastructure.
- **Platform Admin** — manages ingestion, model config, and access (RBAC, deny-by-default).

## Data Entities

A CMDB-style change/dependency model in MSSQL. Indicative tables:

- **Device** — id, hostname, role (core/distribution/edge/branch), site, lifecycle state.
- **ConfigArtifact** — device_id, captured_at, artifact_type, raw_text (large; never
  materialized in list views), parsed_summary.
- **Interface** — device_id, name, vlan, link_to_device_id, bandwidth, role.
- **DependencyEdge** — source_entity, target_entity, edge_type (carries/routes/peers/
  monitors/depends-on), confidence, evidence_ref.
- **BusinessService** — id, name, criticality_tier, owner, rto/rpo.
- **ServiceMapping** — service_id, entity_id, relationship.
- **ChangeRequest** — id, summary, target_entities, proposed_state, window_id, status.
- **ImpactAssessment** — change_id, blast_radius_set, affected_services, risk_score, cited_evidence.
- **GovernanceRule** — rule_id, condition, required_approver_tier, freeze_scope, active.
- **FreezeWindow** — id, scope, start, end, reason.
- **ApprovalRecord** — change_id, approver, decision, tier, timestamp.
- **AuditEntry** — append-only; actor, action, target, timestamp, prior_hash.

> Large text columns (e.g. `ConfigArtifact.raw_text`) are projected to lightweight
> rows in list views — never materialized in bulk — consistent with the platform's
> query-performance rules.

## Integrations

All integrations are **optional and degrade gracefully** — the system must remain
useful with **only MSSQL present**.

- **Config export ingestion** — periodic import of read-only config/state exports
  (files), parsed offline. No live device connection; the platform never manages devices.
- **CMDB / asset feed** — optional import of an existing asset inventory.
- **Ticketing reference** — optional change-ticket id linkage (no write-back required).
- **Embeddings/vector retrieval (Qdrant)** — optional; improves semantic recall of
  related config/dependency facts. Absent → keyword/structured retrieval still works.
- **Local model (Ollama)** — optional; drafts narrative impact summaries. Absent →
  structured graph results and rule outputs still render.

## Security and Audit Controls

- **Deny-by-default RBAC**; project-scoped access enforced server-side; IDOR guards.
- **Append-only audit** of queries, retrieved evidence, approvals, and rule changes.
- **Approval lifecycle** on all curated dependency facts and governance rules.
- **No secrets in repo**; credentials/keys handled per platform policy (encrypted at rest).
- **Read-only ingestion** — the system consumes exports, never pushes config.
- Auditors get read-only reporting views; no ability to mutate the model.

## Reporting Requirements

- **Blast-radius report** per change: affected entities, services, tenants, with evidence.
- **Governance summary**: required approvals, freeze conflicts, outstanding pre-checks.
- **Coverage/gap report**: portions of the estate with thin or stale dependency data.
- **Audit export**: chronological, tamper-evident record for a date range.
- **Risk trend**: distribution of change risk scores over time.

## Failure Modes

- **Incomplete model** → blast radius under-reports. Mitigation: explicit coverage/gap
  reporting; confidence scoring; "unknown, not safe" defaults.
- **Stale config** → dependencies wrong. Mitigation: captured_at recency surfaced in answers.
- **Over-broad blast radius** → alert fatigue. Mitigation: tiered/confidence-ranked impact.
- **Hallucinated dependency** (if model narrates) → Mitigation: every claim cites stored evidence;
  ungrounded claims are flagged, not asserted.
- **Service outage of Qdrant/Ollama** → Mitigation: graceful degradation to MSSQL-only.
- **Auditor distrust** → Mitigation: append-only, hash-chained audit; no silent edits.

## Acceptance Criteria

See `acceptance-criteria.md` for the measurable checklist. In summary: the system
must compute a correct, evidence-cited blast-radius set for a seeded change, apply
the right governance rule, draft a rollback, log every step to the audit trail, and
do all of this in MSSQL-only mode without hanging any core page.

## Expected Architecture

Model the estate as a **directed dependency graph** and reuse impact-analysis thinking:

- Nodes = entities (Device, Interface, BusinessService, VLAN-as-node).
- Edges = `DependencyEdge` rows with type + confidence + evidence reference.
- **Blast radius** = forward/transitive closure from the change's target entities,
  bounded by edge type and a confidence/criticality threshold.
- **Reverse impact** = which business services sit upstream of an affected device.
- Governance layer maps the computed impact set onto `GovernanceRule` rows to derive
  approver tier and freeze conflicts.
- Retrieval layer (curated memory first, then optional vector recall) supplies
  cited evidence for each asserted dependency.
- The graph lives in MSSQL; traversal uses simple, reliable queries (no fragile
  group-by-constant aggregation), with vector retrieval purely augmentative.

## Expected Tests

- Graph traversal returns the correct transitive set on a seeded topology.
- Confidence/criticality thresholds prune as specified.
- Governance rule selection is deterministic for a given impact set.
- Every asserted dependency in an answer resolves to a stored evidence row.
- MSSQL-only mode produces a full (non-degraded-to-error) result.
- Audit entries are written for query, retrieval, and approval; chain verifies.
- Core pages load quickly on empty, seeded, and MSSQL-only databases.

## Expected Deployment Concerns

- Runs on the standard local-first footprint; MSSQL mandatory, Qdrant/Ollama optional.
- Ingestion of config exports is offline/batch; no inbound network device access.
- Schema changes are additive and migration-gated; no destructive changes without approval.
- Health of optional services read from a cached snapshot, never synchronously on a request.

## Rollback Considerations

- Each `ChangeRequest` captures `proposed_state`; prior state is retained from the
  last `ConfigArtifact`, enabling a drafted (human-approved) back-out.
- Rollback plans are **advisory artifacts**, not auto-applied — the platform never
  executes device changes.
- The dependency model itself is versioned via the approval lifecycle, so a bad
  curated fact can be reverted without data loss.

## CEO/CTO Summary

MCB's network changes fail because nobody can see the full blast radius before the
window opens. This scenario asks whether LocalAIFactory's core strength — reasoning
over a curated, approved dependency model and governing change against it — transfers
from code/data understanding to network-change governance. It does so *conceptually*:
the platform treats the estate as a queryable dependency graph with evidence-cited
impact analysis, an approval lifecycle, and a tamper-evident audit trail, all running
locally on MSSQL with optional acceleration. It is explicitly **not** a network
management system and never touches a device — it makes the *thinking* repeatable,
auditable, and fast.
