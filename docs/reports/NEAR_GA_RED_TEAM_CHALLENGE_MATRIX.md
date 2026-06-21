# Near-GA Red-Team Challenge Matrix

This document **adversarially challenges** every production-readiness claim LocalAIFactory makes.
The goal is to be **brutally honest** and to **downgrade** any claim whose evidence is internal,
emulated, sampled, or environment-limited. A claim is only "GA-grade" when a third party
(operator, customer, or independent tester) could reproduce it.

**Confidence-after-challenge scale:** High (external/reproducible), Medium (solid internal proof,
external step pending), Low (emulated/sampled/simulated only), **Downgraded** label called out
explicitly where the headline claim oversells the evidence.

Authoritative gate: `docs/Production-Readiness-Gate.md` — **V2 = PRODUCTION_READY_WHEN_EXTERNAL_
PROOFS_SUPPLIED**. This matrix is the honest justification for *why* the gate is conditional.

---

| # | Claim | Evidence (in repo) | Weakness / challenge | What could invalidate it | Test to verify | Remaining risk | Confidence after challenge |
|---|-------|--------------------|----------------------|--------------------------|----------------|----------------|----------------------------|
| 1 | **Code-complete** | Solution builds; 240/240 tests pass | Tests are our own; coverage of edge/error paths not externally audited | An external reviewer finds untested critical paths | Independent coverage + mutation review; re-run `dotnet test` on a clean clone | Hidden untested branches | **Medium** |
| 2 | **Local-technical works (MSSQL-only)** | Core pages load on empty/seeded/MSSQL-only DB; health degrades gracefully | Proven on dev workstation only | Server-only env (ACLs, GPO) breaks a page | Fresh-clone run + core-page smoke under MSSQL-only | Env-specific startup issues | **Medium** |
| 3 | **IIS deployment proof** | `MODE_A_IIS_*`, `DEPLOYMENT_IIS_EXECUTION_PROOF.md` | **Workstation IIS, not Windows Server** | Server app-pool identity / DNS host header differs | Operator cold start on provisioned Server + smoke | Server-only ANCM/identity issues | **Medium (Downgraded from "IIS proven")** |
| 4 | **HTTPS proof** | `IIS_HTTPS_BINDING_PROOF.md` | **Self-signed cert** — proves encryption, not trust | Client without manual trust rejects the cert | CA-issued cert + external TLS scan | No trusted chain yet | **Low (Downgraded)** |
| 5 | **Windows-auth proof** | `IIS_WINDOWS_AUTH_PROOF.md`, `MODE_A_IIS_HTTP_AUTH_HEALTHCHECK.md` | **Dev-auth path behind IIS**, not a real AD domain; Kerberos vs NTLM unproven | No SPN / NTLM fallback / double-hop failure on real domain | Domain-joined server + `setspn`/`klist` Kerberos check | Real-domain auth behavior unknown | **Low (Downgraded)** |
| 6 | **SQL least-privilege** | `is_sysadmin = 0` (`MODE_A_SQL_EXPRESS_IIS_DB_PROOF.md`) | Proven on **SQL Express**, not prod instance/account | Prod login missing a needed grant, or over-privileged service account | Run under prod login on prod instance; enumerate effective perms | Permission gaps at prod | **Medium** |
| 7 | **Knowledge packs** | 6 packs / 520 items (`Knowledge-Pack-Validation-Report.md`) | Curation quality is self-assessed; banking-domain accuracy not externally reviewed | A domain expert finds incorrect/outdated rules | Customer SME review of a sample pack | Knowledge correctness | **Medium** |
| 8 | **Local-LLM safe** | `LOCAL_LLM_REASONING_PROOF.md`, mean at 90/90 cap | Cap + grounding tested by us; hallucination on unseen banking inputs not externally validated | Model fabricates a banking API/rule under real prompts | Adversarial prompt suite + provenance assertion | Hallucination on novel inputs | **Medium** |
| 9 | **Workflow learning** | `ENTERPRISE_WORKFLOW_LEARNING_REPORT.md`, pattern library | "Learning" demonstrated on emulated/curated workflows | Real customer workflow doesn't generalize | Pilot capture of a real workflow end-to-end | Generalization gap | **Low** |
| 10 | **Support pack** | Support bundle + runbooks + dashboard spec | Bundle contents not exercised by a real support incident | A real incident needs data the bundle omits | Operator runs the bundle during a simulated incident | Missing diagnostics | **Medium** |
| 11 | **100+ system benchmark** | 113-system benchmark (`PUBLIC_SYSTEMS_UNDERSTANDING_*`) | **Largely metadata-level; some systems unsupported** (`PUBLIC_SYSTEMS_UNSUPPORTED_GAPS.md`) | Treating metadata coverage as full functional support | Re-score: separate "metadata-only" from "functionally exercised" | Overstated breadth | **Low (Downgraded — breadth ≠ depth)** |
| 12 | **Docs/API cross-check** | `PUBLIC_SYSTEMS_DOC_API_CROSSCHECK_RESULTS.md` | **Sampled only**, not exhaustive | An unsampled system's API contract is wrong | Expand sample / full cross-check on pilot-relevant systems | Unsampled inaccuracies | **Low (Downgraded — sampled)** |
| 13 | **Production gate** | `PRODUCTION_READINESS_GATE_V2_RESULT.md` | Gate explicitly **conditional** on external proofs | Mistaking "conditional pass" for "GA" | Re-read gate; confirm V2 = conditional | Misreading the gate | **Medium (correctly conditional)** |
| 14 | **Fresh-clone pullable** | `FRESH_CLONE_PULLABLE_REPO_PROOF.md` | Cloned + built by us, same toolchain | Different machine/toolchain fails restore/build | Clone on a clean machine, `dotnet restore/build/test` | Toolchain drift | **Medium** |
| 15 | **Release draft** | `GitHub-Release-Instructions.md`, checksums, post-release verification | **No published release cut by an operator** | Wrong artifact / checksum mismatch at publish | Operator publishes + verifies by fresh download | Publish-time errors | **Medium** |
| 16 | **Operator emulation** | `OPERATOR_EMULATION_PACK_RESULT.md`, integration-expectation library | **Emulation, not real operators** | Real operator hits a step the emulation skipped | Real operator dry-run against the runbooks | Emulation blind spots | **Low (emulated by definition)** |
| 17 | **Pilot readiness** | `Commercial-Pilot-Package.md`, `Customer-Acceptance-Test.md` | **No signed pilot** | Customer rejects scope/criteria | Signed pilot scope + run | No customer commitment | **Low** |
| 18 | **High-volume readiness** | 29,540 req / 0 HTTP 500s (`LOAD_TEST_IIS_RESULTS.md`) | **Workstation simulation**, not prod hardware/concurrency | Prod hardware/network behaves differently at peak | Re-run load on prod-representative server; capture p95/p99 | Scale behavior on real hardware | **Medium (Downgraded — workstation sim)** |
| 19 | **Security readiness** | Threat model, hardening, pen-test *readiness* | **No external penetration test** | An external tester finds an exploitable flaw | Independent pen-test + fix + retest | Unknown vulnerabilities | **Low (Downgraded — no pen-test)** |
| 20 | **GA readiness (overall)** | All of the above + handover package | Aggregate is gated on the external/operator/customer items above | Any single external blocker (TLS, Entra, pen-test, pilot, license) remaining open | Close items 3–6, 11–12, 15–19 with real external proof | Multiple external blockers open | **Low–Medium (conditional, ~low-60s% GA)** |

---

## Honest downgrades called out

The following headline claims were **explicitly downgraded** because the evidence is environment-
limited, sampled, simulated, or self-assessed:

- **IIS proof** → workstation, not Windows Server.
- **HTTPS proof** → self-signed (encryption, not trust).
- **Windows-auth proof** → dev-auth behind IIS, not real AD/Kerberos.
- **100+ system benchmark** → mostly metadata-level; some systems unsupported.
- **Docs/API cross-check** → sampled, not exhaustive.
- **High-volume readiness** → workstation simulation, not production hardware.
- **Security readiness** → no external pen-test exists.
- **Pilot / operator** → emulated and unsigned, not real.

## What would move the matrix to mostly-High

1. Provision a real **Windows Server** + run IIS cold start (items 3, 6).
2. Install a **CA-issued TLS cert** + pass an external scan (item 4).
3. Stand up a **real Entra/OIDC tenant** + verify claims→RBAC end-to-end (related to item 5/auth).
4. Run an **external penetration test**, fix findings, retest (item 19).
5. Sign and run a **customer pilot** to **acceptance** (items 9, 17, 20).
6. Re-run **load on production hardware** with p95/p99 (item 18).
7. Re-score the **benchmark** separating metadata-only from functional support (items 11–12).

Until those exist, the honest position is: **strong internal/emulated readiness; GA-conditional.**
See `docs/reports/HUMAN_INTERACTION_GA_IMPACT_MODEL.md` for the GA-% progression tied to each task.
