# Known Limitations

An honest, comprehensive inventory of what LocalAIFactory does **not** do yet, organized by
capability area. Each limitation lists the **proof required to close it** — the concrete, observable
evidence that would let us remove the limitation rather than just assert it away.

This list is a feature, not an apology: over-claiming is the failure mode we are guarding against.

---

## 1. Language / extraction coverage

- **Only C#, SQL, and Python are structurally extracted.** Deterministic symbol/structure
  extraction targets these three. Other languages in the estate are imported as text but not given
  first-class structural understanding.
  - *Close with:* a deterministic extractor for the additional language, plus extraction-accuracy
    proofs on a real repo (precision/recall against a known symbol set).

- **OCR / PDF handling is a prototype, not an engine.** Document-processing paths are exploratory
  scaffolds, not production extraction engines, and produce no authoritative output.
  - *Close with:* a measured accuracy benchmark on representative documents and an explicit
    production-readiness sign-off; until then, treat as prototype only.

---

## 1a. Enterprise reasoning benchmark scope

- **The enterprise giant-pattern reasoning benchmark is synthetic and pattern-level, not vendor-equivalent.**
  `benchmarks/fixtures/enterprise-giant-patterns` models **public, high-level** CRM/ERP/ITSM/core-banking/
  reporting patterns. It is **not** a clone of, and makes **no** certified-compatibility claim for, SAP,
  Dynamics, Salesforce, ServiceNow, Oracle, NetSuite, Temenos, Finastra, Mambu, FIS, Fiserv, Jira, Confluence,
  Power BI, Tableau, Copilot, or Sourcegraph.
  - *Close with:* a delivered, measured advisory/solution engagement on a real customer system — not a synthetic
    fixture.
- **Structural answers cover statically-named SQL only; advisory answers are design, not execution.** The 14
  structural questions are graph-proven (find/dependents/dependencies/impact) but exclude dynamic/ORM-generated
  SQL (reported as a gap). The 17 advisory questions (controls, audit evidence, lifecycle, risk) are grounded
  design reasoning scored at most 90 — they are **not** executed workflows and carry **no** compliance,
  regulatory, financial, or fraud guarantee.
  - *Close with:* delivered, executed workflows with measured outcomes and, where regulated, an external review.

---

## 2. Estate-level understanding

- **No estate model.** The platform reasons per-project; there is no cross-project / whole-estate
  dependency or impact model that links BDM, MCIB, ChequeXpert, ETAMS, etc. together.
  - *Close with:* an estate-level graph spanning multiple imported projects and an impact query that
    crosses project boundaries, validated against a known cross-system dependency.

---

## 3. Identity & access

- **No enterprise SSO / external IdP.** Authentication is Windows/Negotiate only — no SAML/OIDC
  federation, no SCIM provisioning, no MFA layer beyond the Windows domain itself.
  - *Close with:* an integrated OIDC/SAML flow against a real IdP, plus provisioning/deprovisioning
    tests.

- **`AccessLevel.Write` is reserved, not behavioral.** Project grants operate at `Read`; write-level
  differentiation is modeled but not enforced as distinct behavior.
  - *Close with:* enforced write-vs-read differentiation in the controller layer plus tests.

---

## 4. Audit & assurance

- **Audit is append-only by convention, not tamper-evident.** No hash chaining, no cryptographic
  sealing (see `Audit-Model.md` §4).
  - *Close with:* a per-row hash chain + verifier that detects any edit/removal.

- **No external security audit and no penetration test.** No third-party offensive assessment or
  certification has been performed.
  - *Close with:* a completed pen-test / external review report and remediation of findings.

---

## 5. Deployment & runtime proof

- **No production deployment is proven here.** The IIS deployment path is documented but a real
  production deploy has not been demonstrated as part of this work.
  - *Close with:* a deployment to a representative IIS host with the core-page smoke checks passing
    on that host.

- **Docker and SQL Express were not exercised on the build host.** The build/validation host did not
  run the Docker-based or SQL-Express scenarios; those paths are documented but not host-verified
  here.
  - *Close with:* the same smoke + benchmark run executed on a SQL Express instance and (where
    relevant) in the Docker scenario, captured in logs.

---

## 6. Autonomous execution

- **The controlled executor runs, but there is no real fix/rollback loop yet.** The autonomous path
  enforces an allow/deny command policy, **dry-run by default**, and **commit/push gated** behind
  approval, and the `ControlledExecutor` never self-promotes. But it does not yet close a genuine
  "diagnose → apply fix → verify → roll back on failure" loop against real code.
  - *Close with:* an end-to-end run on a real defect that applies a change, verifies it via build +
    tests, and automatically rolls back on failure — all within the existing gating and audit
    constraints.

---

## 7. Data governance

- **No formal PII / data-retention / DLP policy.** Retention is operational guidance
  (`Audit-Model.md` §3), not enforced controls; there is no PII classification or data-loss
  prevention layer.
  - *Close with:* a documented, enforced retention/classification policy with mechanisms to apply
    it.

- **Source-registry governance reduces but does not eliminate unsourced material.** The installer
  rejects unregistered sources and permanence rules protect curated knowledge, but registration is a
  human act and awareness-only references remain non-authoritative (see `Compliance-Disclaimers.md`).
  - *Close with:* automated source-attestation checks plus periodic provenance audits.

---

## 8. AI dependence & quality

- **AI outputs are advisory and unverified by default.** They can be wrong or incomplete and require
  human verification before any action (`Compliance-Disclaimers.md` §5). The platform runs fully in
  MSSQL-only mode, so AI quality is never on the critical path for the system of record — but it is
  also not measured/guaranteed.
  - *Close with:* task-level accuracy benchmarks for each AI-assisted feature with published
    thresholds.

---

## Status anchor

At the time of writing: **207 tests passing**, **validation benchmark PASS**. These cover the
implemented security, ingestion-robustness, and knowledge-governance behaviors — they do **not**
close any of the gaps above, which require their own stated proofs.
