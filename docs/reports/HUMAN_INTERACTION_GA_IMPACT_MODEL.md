# Human-Interaction GA-Impact Model

Every remaining blocker to General Availability (GA) for LocalAIFactory is **external, operator-,
or customer-owned** — it requires a human with the right access to act in the real world (provision
a server, issue a certificate, stand up a tenant, run a pen-test, sign a pilot). This document
models each such task: who owns it, how long it realistically takes, its risk, its GA-% impact,
its current vs emulated status, the proof artifact that closes it, and how to validate it.

The machine-readable companion is `benchmarks/results/human-interaction-ga-impact.json`.

**Honesty constraints (do not inflate):**
- Current overall GA is modeled at **~62%** (low-60s%). Internal/emulated readiness is high, but
  the GA number is held down because the load-bearing **external proofs are not yet supplied**.
- No stage is allowed to exceed **~90%** until the external security test **and** a signed customer
  pilot are real. The final climb to GA is gated on commercial/legal signoff.
- "Emulated status = Complete" means *we have a dry-run/emulation/proof-of-readiness*, **not** that
  the real-world artifact exists.

Time estimates are **human effort/elapsed** ranges (optimistic / realistic / pessimistic), not
machine time. Skill = the role typically required.

---

## Per-task model

### 1. Provision Windows Server
- **Owner:** Customer IT / infrastructure. **Skill:** Windows sysadmin.
- **Time:** optimistic 2h / realistic 6h / pessimistic 24h.
- **Risk:** Medium (hardware/licensing/AD-join delays).
- **GA-% impact:** +6%. **Current status:** Not started. **Emulated status:** Complete (workstation IIS).
- **Proof artifact:** Server build sheet + OS/IIS feature inventory.
- **Expected input:** Server spec, IIS role enabled. **Expected output:** Reachable Windows Server with IIS.
- **Validation command:** `Get-WindowsFeature Web-Server` (Installed) + reach the host.
- **Failure modes:** AD-join blocked, IIS role missing, firewall closed.

### 2. Domain / DNS configuration
- **Owner:** Customer IT (network/AD). **Skill:** AD/DNS admin.
- **Time:** 1h / 4h / 16h. **Risk:** Medium.
- **GA-% impact:** +3%. **Current:** Not started. **Emulated:** Partial (host header only).
- **Proof artifact:** DNS record + host-header binding evidence.
- **Expected input:** FQDN, A/CNAME record. **Expected output:** App reachable by FQDN.
- **Validation command:** `Resolve-DnsName <fqdn>` resolves to the server.
- **Failure modes:** Split-horizon DNS, wrong host header on the IIS binding.

### 3. CA-issued TLS certificate
- **Owner:** Customer PKI / security. **Skill:** PKI admin.
- **Time:** 2h / 8h / 40h (CA queue). **Risk:** High (issuance bureaucracy).
- **GA-% impact:** +6%. **Current:** Not started (self-signed only). **Emulated:** Complete (self-signed binding).
- **Proof artifact:** Cert chain to trusted CA + external TLS scan report.
- **Expected input:** CSR for the FQDN. **Expected output:** Trusted cert bound to the IIS site.
- **Validation command:** External scan / `openssl s_client -connect <fqdn>:443` shows a trusted chain.
- **Failure modes:** Chain incomplete, SAN mismatch, weak ciphers, HSTS on an untrusted chain.

### 4. Production IIS deployment
- **Owner:** Operator / DevOps. **Skill:** ASP.NET Core + IIS.
- **Time:** 2h / 6h / 16h. **Risk:** Medium.
- **GA-% impact:** +5%. **Current:** Not started on server. **Emulated:** Complete (`MODE_A_IIS_*`).
- **Proof artifact:** Server cold-start with clean ANCM event log + core-page smoke.
- **Expected input:** Published artifact + Hosting Bundle installed. **Expected output:** Clean cold start.
- **Validation command:** `curl -s -o NUL -w "%{http_code} %{time_total}s\n" https://<fqdn>/` for each core page.
- **Failure modes:** 500.19/500.30/502.5; bitness mismatch; missing runtime.

### 5. Production SQL account
- **Owner:** Customer DBA. **Skill:** SQL Server admin.
- **Time:** 1h / 3h / 8h. **Risk:** Medium.
- **GA-% impact:** +4%. **Current:** Not started. **Emulated:** Complete (`is_sysadmin=0` on Express).
- **Proof artifact:** Least-priv login on prod instance + successful app run under it.
- **Expected input:** Service account, scoped grants. **Expected output:** App runs least-priv on prod DB.
- **Validation command:** `SELECT IS_SRVROLEMEMBER('sysadmin')` returns 0; app pages load.
- **Failure modes:** Missing EXECUTE grant; over-privileged account; wrong connection string.

### 6. Entra / OIDC tenant + app registration
- **Owner:** Customer identity team. **Skill:** Entra/IdP admin.
- **Time:** 2h / 6h / 20h. **Risk:** High.
- **GA-% impact:** +6%. **Current:** Not started. **Emulated:** Complete (`SSO_ENTRA_ID_PROOF_PACK.md`).
- **Proof artifact:** Real tenant token signing in to the running app.
- **Expected input:** Tenant, app registration, redirect URI, secret/cert. **Expected output:** Successful OIDC sign-in.
- **Validation command:** Sign in as a tenant user; decode token; confirm `iss`/`aud`.
- **Failure modes:** AADSTS50011 redirect mismatch; missing consent.

### 7. App-RBAC binding (claims → roles)
- **Owner:** Operator + identity team. **Skill:** App + IdP config.
- **Time:** 1h / 4h / 12h. **Risk:** Medium.
- **GA-% impact:** +3%. **Current:** Not started. **Emulated:** Complete (`Claims-Roles-Mapping.md`).
- **Proof artifact:** Real user with a role claim getting the correct authorization decision.
- **Expected input:** App roles assigned in the IdP. **Expected output:** Correct allow/deny per role.
- **Validation command:** Sign in as users of two roles; confirm differing access.
- **Failure modes:** Group-claim overage; missing role assignment.

### 8. SMTP configuration
- **Owner:** Customer messaging/IT. **Skill:** Mail admin.
- **Time:** 0.5h / 2h / 8h. **Risk:** Low–Medium.
- **GA-% impact:** +1%. **Current:** Not started. **Emulated:** N/A. 
- **Proof artifact:** A test email delivered from the server.
- **Expected input:** Relay host, port, TLS mode, creds. **Expected output:** Email delivered.
- **Validation command:** Send a test message from the server; confirm receipt.
- **Failure modes:** Relay denied; wrong port/STARTTLS; auth failure.

### 9. SFTP configuration
- **Owner:** Customer IT. **Skill:** SFTP/network admin.
- **Time:** 0.5h / 2h / 8h. **Risk:** Low–Medium.
- **GA-% impact:** +1%. **Current:** Not started. **Emulated:** N/A.
- **Proof artifact:** A test file transferred + host key pinned.
- **Expected input:** Host, port, key, host-key fingerprint. **Expected output:** Successful transfer.
- **Validation command:** `sftp` round-trip of a test file.
- **Failure modes:** Host-key mismatch; firewall; auth failure.

### 10. Monitoring / alerting
- **Owner:** Customer SRE/ops. **Skill:** Observability engineer.
- **Time:** 2h / 8h / 24h. **Risk:** Medium.
- **GA-% impact:** +3%. **Current:** Not started. **Emulated:** Partial (in-app health + dashboard spec).
- **Proof artifact:** A dashboard + at least one alert proven to fire to on-call.
- **Expected input:** Telemetry endpoint, alert thresholds, on-call rota. **Expected output:** Live alerting.
- **Validation command:** Trigger a synthetic failure; confirm the alert pages.
- **Failure modes:** No paging integration; noisy/missing thresholds.

### 11. Backup retention policy
- **Owner:** Customer DBA/ops. **Skill:** DBA.
- **Time:** 1h / 3h / 8h. **Risk:** Medium.
- **GA-% impact:** +2%. **Current:** Not started. **Emulated:** Complete (`Database-Backup-Restore-Evidence.md`).
- **Proof artifact:** A timed restore drill into a clean instance + agreed RPO/RTO.
- **Expected input:** Retention schedule, RPO/RTO targets. **Expected output:** Verified restore within RTO.
- **Validation command:** Restore the latest backup; verify row counts + app start; record time.
- **Failure modes:** Broken log chain; restore exceeds RTO.

### 12. External penetration test
- **Owner:** Independent security firm. **Skill:** Pen-tester.
- **Time:** 16h / 40h / 120h (engagement). **Risk:** High.
- **GA-% impact:** +8%. **Current:** Not started. **Emulated:** Readiness only (`Security-Pentest-Readiness.md`).
- **Proof artifact:** Pen-test report with findings + severities.
- **Expected input:** Scope, test environment, rules of engagement. **Expected output:** Findings report.
- **Validation command:** N/A (human deliverable); confirm report received + scoped.
- **Failure modes:** Critical/high findings; scope too narrow.

### 13. Fix pen-test findings
- **Owner:** Engineering. **Skill:** AppSec-aware developer.
- **Time:** 4h / 24h / 160h (depends on findings). **Risk:** High.
- **GA-% impact:** +4%. **Current:** Not started. **Emulated:** N/A.
- **Proof artifact:** Remediation + a clean **retest**.
- **Expected input:** Findings list. **Expected output:** Retest with no open critical/high.
- **Validation command:** Re-run `dotnet test`; obtain retest sign-off from the tester.
- **Failure modes:** Regression from fixes; incomplete remediation.

### 14. Sign pilot scope
- **Owner:** Customer sponsor + vendor. **Skill:** Product/commercial.
- **Time:** 1h / 8h / 40h (negotiation). **Risk:** Medium.
- **GA-% impact:** +3%. **Current:** Not started. **Emulated:** Package ready (`Commercial-Pilot-Package.md`).
- **Proof artifact:** Countersigned pilot scope + acceptance criteria.
- **Expected input:** Scope, success criteria. **Expected output:** Signed scope doc.
- **Validation command:** N/A (signature artifact present).
- **Failure modes:** Scope creep; unclear criteria.

### 15. Run pilot
- **Owner:** Customer + vendor. **Skill:** Operator + SME.
- **Time:** 40h / 120h / 320h (pilot period). **Risk:** High.
- **GA-% impact:** +6%. **Current:** Not started. **Emulated:** Workflow learning emulated only.
- **Proof artifact:** Pilot run log against real data + criteria.
- **Expected input:** Real project/data, signed scope. **Expected output:** Pilot results vs criteria.
- **Validation command:** Core-page smoke + workflow completion on customer data.
- **Failure modes:** Real workflow doesn't generalize; data issues.

### 16. Acceptance sign-off
- **Owner:** Customer sponsor. **Skill:** Decision-maker.
- **Time:** 1h / 8h / 40h. **Risk:** Medium.
- **GA-% impact:** +5%. **Current:** Not started. **Emulated:** Test pack ready (`Customer-Acceptance-Test.md`).
- **Proof artifact:** Countersigned acceptance.
- **Expected input:** Pilot results vs criteria. **Expected output:** Signed acceptance.
- **Validation command:** N/A (signature artifact present).
- **Failure modes:** Criteria not met; conditional acceptance.

### 17. Approve publication
- **Owner:** Vendor release owner. **Skill:** Release manager.
- **Time:** 0.5h / 2h / 8h. **Risk:** Low.
- **GA-% impact:** +1%. **Current:** Not started. **Emulated:** Instructions + checksums ready.
- **Proof artifact:** Published GitHub release verified by fresh download.
- **Expected input:** Approved artifacts + checksums. **Expected output:** Public release.
- **Validation command:** Download artifact; recompute checksum vs `checksums/`.
- **Failure modes:** Artifact/checksum mismatch.

### 18. Approve license enforcement
- **Owner:** Vendor commercial/legal. **Skill:** Product/legal.
- **Time:** 1h / 8h / 40h. **Risk:** Medium.
- **GA-% impact:** +2%. **Current:** Not started (design only, `License-Enforcement-Design.md`). **Emulated:** Design only.
- **Proof artifact:** Enabled, tested license enforcement + commercial approval.
- **Expected input:** Licensing policy, keys. **Expected output:** Enforcement active + tested.
- **Validation command:** Run with valid/invalid license; confirm correct gating.
- **Failure modes:** False lockout; bypassable enforcement.

---

## GA-readiness progression (conservative)

The percentages below are deliberately conservative and **additive within stages** (not literal
sums of every per-task impact, since some tasks overlap and the final climb is commercially gated).

| Stage | GA readiness % | Rationale |
|-------|----------------|-----------|
| **Current** | **~62%** | Strong internal/emulated proof; all external proofs outstanding. |
| **+ After operator infra** (server, DNS, prod IIS, prod SQL, backup) | **~72%** | Real environment stands the app up; transport trust + identity still open. |
| **+ After Entra/OIDC** (real tenant, claims→RBAC) | **~78%** | Real identity + authorization verified end-to-end. |
| **+ After external security** (CA TLS, pen-test, fixes, retest) | **~85%** | Trusted transport + independent security evidence. Capped below 90 until a real customer validates. |
| **+ After customer pilot** (signed scope, run, acceptance) | **~92%** | Real customer on real data accepts the system. |
| **+ After commercial signoff** (publication, license enforcement) | **~97%** | GA: publishable, enforceable, accepted. Reserve ~3% for the inevitable first-week production findings. |

**Caveat.** No stage above is claimed as reached. These are *forward* estimates of what each
external proof, once genuinely supplied, would unlock. Until then the honest position remains:
**internal readiness high, GA-conditional at ~62%**, consistent with gate
**V2 = PRODUCTION_READY_WHEN_EXTERNAL_PROOFS_SUPPLIED** and the per-claim downgrades in
`docs/reports/NEAR_GA_RED_TEAM_CHALLENGE_MATRIX.md`.
