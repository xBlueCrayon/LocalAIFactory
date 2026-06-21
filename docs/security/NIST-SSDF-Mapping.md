# NIST SSDF Mapping (Self-Assessment)

This maps LocalAIFactory's **actual, evidenced** practices to the NIST Secure Software Development
Framework (SSDF, SP 800-218) practice groups. It is a **self-assessment** in original wording; SSDF
practice text is **not** reproduced verbatim. Verify exact practice identifiers against the current
publication before any formal citation.

Status legend: **PASS** / **PARTIAL** / **GAP**, each with evidence and a fix path.

> Honest baseline gaps: no external penetration test; self-signed TLS in the pilot; app-level auth
> is dev-grade behind the IIS Windows-auth boundary; no formal vulnerability-disclosure process yet.

---

## PO — Prepare the Organization

| Practice theme | Status | Evidence | Fix path |
|---|---|---|---|
| Define security requirements for the software | PASS | CLAUDE.md is an enforced engineering contract (no blocking calls on request path, least-privilege DB, no secrets, additive migrations). | Add a standalone security-requirements register. |
| Define roles and toolchain | PARTIAL | Agent roles/approval workflow documented; build/run/migrate scripts standardised. | Formalise toolchain security settings (signing, scanning). |
| Criteria for software security checks | PARTIAL | Runtime-validation gates + security audit before release. | Make security checks continuous in the build gate. |

## PS — Protect the Software

| Practice theme | Status | Evidence | Fix path |
|---|---|---|---|
| Protect code from unauthorized access/tampering | PASS | Git-tracked source; no secrets in repo; `keys/` git-ignored; Data Protection encryption at rest. | Add commit/signing and integrity checks. |
| Provide a mechanism to verify integrity | PARTIAL | Release packaging with checksums (`checksums/`) and release docs. | Publish and verify checksums/signatures for every artifact. |
| Archive and protect each release | PARTIAL | Release-candidate packaging; rollback runbook references retained artifacts. | Guarantee previous-artifact retention in automation. |

## PW — Produce Well-Secured Software

| Practice theme | Status | Evidence | Fix path |
|---|---|---|---|
| Secure design / threat modeling | PARTIAL | Layered, dependency-cycle-free architecture; degradation rules. | Add a recorded threat model. |
| Reuse secure components | PARTIAL | Controlled dependency set; parameterised EF Core (no string SQL). | Add automated dependency-vulnerability scanning. |
| Secure coding practices | PASS | Documented anti-patterns (no `GroupBy(_ => 1)`, no large-text on request path, no blocking external calls). | Keep the runtime-validation checklist enforced. |
| Configuration with secure defaults | PASS | MSSQL-only safe mode; optional services gated off; no secrets in config. | Continue startup config-source logging. |
| Review and test code | PARTIAL | Build + runtime validation; security audit (0-HIGH); benchmark/golden tests. | Add coverage for security-relevant paths. |

## RV — Respond to Vulnerabilities

| Practice theme | Status | Evidence | Fix path |
|---|---|---|---|
| Identify and confirm vulnerabilities | PARTIAL | Security audit; support-issue learning registry; autonomous fix-loop proof. | Add scheduled dependency/vuln scanning. |
| Assess, prioritize, and remediate | PARTIAL | Fix-loop runbook + incident rollback patterns documented. | Define a vuln-response SLA and severity policy. |
| Analyze root causes to prevent recurrence | PASS | Firsthand findings captured in the registry with prevention rules and `detectBy`. | Promote firsthand findings to official-confirmed as cross-checked. |
| Vulnerability disclosure process | GAP | No formal external disclosure channel. | Establish a disclosure/contact policy. |

---

## Overall posture

LocalAIFactory shows **strong secure-coding and secure-default practices** and a real
**learn-from-incidents** loop, with **PARTIAL** marks where automation/formalisation is missing
(dependency scanning, threat modeling, integrity verification, vuln-response SLA) and a **GAP** on a
formal vulnerability-disclosure process and independent testing. This is a self-assessment, not an
SSDF attestation.
