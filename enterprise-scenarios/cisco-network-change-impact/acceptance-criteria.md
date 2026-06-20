# Acceptance Criteria — Measurable Checklist

Each item is pass/fail and independently verifiable against a seeded fictional
topology for Meridian Continental Bank (MCB).

## Ingestion and model

- [ ] A config/state **export file set** imports without crashing on a malformed file;
      bad files are reported, not silently dropped.
- [ ] At least one CMDB-style dependency model (Device, Interface, DependencyEdge,
      BusinessService, ServiceMapping) is populated in MSSQL.
- [ ] Large text columns (`ConfigArtifact.raw_text`) are **not** materialized in any
      list view; list queries return lightweight projected rows.
- [ ] A coverage/gap report identifies at least one seeded "thin/stale" region of the model.

## Impact / blast-radius reasoning

- [ ] For a seeded change targeting distribution switch `DSW-07`, the computed
      blast-radius set equals the expected transitive affected-set (exact match).
- [ ] Confidence/criticality threshold pruning changes the result deterministically
      and as specified when the threshold is raised.
- [ ] Reverse impact correctly lists the upstream business services for an affected device.
- [ ] Every asserted dependency in the answer resolves to a stored evidence row;
      no dependency is asserted without an evidence reference.
- [ ] An ungrounded/uncertain dependency is **flagged**, not asserted as fact.

## Governance

- [ ] The correct `GovernanceRule` is selected for the seeded impact set (deterministic).
- [ ] A required approver tier is derived and shown.
- [ ] A freeze-window conflict is detected when the change overlaps a seeded `FreezeWindow`.
- [ ] Curated dependency facts and governance rules pass through an **approval lifecycle**
      (draft → approved); only approved facts are injected first into reasoning.

## Rollback

- [ ] A rollback/back-out plan is drafted from the prior `ConfigArtifact` state.
- [ ] The rollback artifact is advisory only — there is **no** code path that applies
      changes to a device.

## Security and audit

- [ ] Access is deny-by-default and project-scoped, enforced server-side; an IDOR
      attempt on another project's change is rejected.
- [ ] An append-only `AuditEntry` is written for query, evidence retrieval, and approval.
- [ ] The audit chain verifies (prior_hash linkage intact); auditor view is read-only.
- [ ] No secrets appear in committed config or repo.

## Resilience and performance

- [ ] Full result is produced in **MSSQL-only** mode (Qdrant absent, Ollama absent),
      not a degraded error.
- [ ] With optional services present, results are augmented but never blocked on them;
      health is read from a cached snapshot, never a synchronous request-path call.
- [ ] Home, Projects, Knowledge, and Models pages load quickly (well under one second
      target) on empty, seeded, and MSSQL-only databases — none hang.
- [ ] Schema changes required by this scenario are additive only; no destructive
      migration is introduced.
