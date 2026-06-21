# Expanded Tech-Support Failure Patterns

~30 real-world failure patterns relevant to deploying and operating LocalAIFactory (ASP.NET Core
on IIS, Windows auth, SQL Server, EF Core, TLS, Entra, integrations, local LLM/RAG, releases).
Each entry: **symptom**, **root cause**, **diagnostic**, **fix**, **prevention**, **confidence**,
and whether it is **official-source-confirmed**.

Confidence: **High** (official source / directly observed), **Medium** (well-established), **Low**
(plausible / not externally verified). "Official-source-confirmed" = a vendor/standards primary doc
or product docs-repo confirms the mechanism.

---

### 1. IIS 500.19 — configuration data invalid / module not found
- **Symptom:** Page fails immediately with HTTP 500.19; site won't start.
- **Root cause:** Malformed `web.config`, or the ASP.NET Core Module isn't registered (Hosting
  Bundle not installed / installed before IIS).
- **Diagnostic:** Read the 500.19 detail (it names the offending config section); check that
  `aspnetcorev2.dll` is registered; confirm Hosting Bundle present.
- **Fix:** Install/repair the **.NET Hosting Bundle**; if IIS was installed *after* the bundle, run
  the bundle repair so the module registers; correct the `web.config` section.
- **Prevention:** Install IIS first, then the Hosting Bundle; treat `web.config` as a deploy
  artifact, don't hand-edit on the server.
- **Confidence:** High. **Official-source-confirmed:** Yes (Microsoft Learn IIS error reference).

### 2. IIS 500.30 — ANCM in-process start failure
- **Symptom:** "HTTP Error 500.30 - ASP.NET Core app failed to start."
- **Root cause:** An exception during `Program`/`Startup` (bad connection string, failed DI, failed
  startup migration), or a runtime/bitness mismatch.
- **Diagnostic:** Application event log + ANCM **stdout log** (enable `stdoutLogEnabled` briefly);
  reproduce by running the published DLL directly with `dotnet`.
- **Fix:** Resolve the startup exception (most often config/DB reachability); fix the connection
  string; ensure the targeted runtime is installed.
- **Prevention:** Health-gate startup; fail fast with a clear log; keep startup work minimal.
- **Confidence:** High. **Official-source-confirmed:** Yes (Microsoft Learn + IIS support blog).

### 3. IIS 502.5 — ANCM out-of-process process failure
- **Symptom:** HTTP Error 502.5; backend process didn't start or didn't listen on the assigned port.
- **Root cause:** Wrong runtime, missing dependencies, or the app crashed before binding to the
  ANCM-provided port.
- **Diagnostic:** stdout log; run the app standalone; check `dotnet --info` on the server.
- **Fix:** Install correct runtime; resolve missing native deps; verify the app honors the
  `ASPNETCORE_PORT`/handler handshake.
- **Prevention:** Prefer in-process unless out-of-process is required; pin runtime version.
- **Confidence:** High. **Official-source-confirmed:** Yes.

### 4. 500.31 / 500.32 — runtime or bitness mismatch
- **Symptom:** 500.31 (runtime not found) or 500.32 (incompatible architecture).
- **Root cause:** App built for a runtime/arch not installed; app-pool 64-bit vs 32-bit build.
- **Diagnostic:** Compare `dotnet --info` to the target framework; check "Enable 32-Bit
  Applications" on the app pool.
- **Fix:** Install the matching runtime, or align app-pool bitness with the build.
- **Prevention:** Standardize on one architecture; self-contained publish removes runtime ambiguity.
- **Confidence:** High. **Official-source-confirmed:** Yes.

### 5. App pool stops on first request (rapid-fail protection)
- **Symptom:** Site returns 503; app pool disabled.
- **Root cause:** Repeated worker-process crashes trip IIS rapid-fail protection.
- **Diagnostic:** System event log (WAS warnings); identify the crashing exception via ANCM log.
- **Fix:** Fix the crash; re-enable the app pool.
- **Prevention:** Validate startup in staging; monitor app-pool recycles.
- **Confidence:** High. **Official-source-confirmed:** Yes (WAS behavior documented).

### 6. Windows auth falls back to NTLM instead of Kerberos
- **Symptom:** Auth works but Kerberos-only resources fail / double-hop fails; tickets show NTLM.
- **Root cause:** Missing or duplicate **SPN** for the app-pool identity, or non-FQDN host name.
- **Diagnostic:** `setspn -L <account>`; `klist`; check for duplicate SPNs.
- **Fix:** Register the correct SPN for the service account/host; remove duplicates.
- **Prevention:** Document SPN setup in the deployment runbook; use a managed service account.
- **Confidence:** Medium. **Official-source-confirmed:** Partial (Negotiate/Kerberos docs).

### 7. Negotiate behind a load balancer / proxy fails intermittently
- **Symptom:** Sporadic 401s through a proxy.
- **Root cause:** Negotiate requires a 1:1 persistent connection; proxies that multiplex break it.
- **Diagnostic:** Compare direct vs proxied requests; inspect connection reuse.
- **Fix:** Ensure connection affinity, or terminate auth at the proxy.
- **Prevention:** Don't place Negotiate behind connection-pooling proxies.
- **Confidence:** High. **Official-source-confirmed:** Yes (Microsoft Learn warns explicitly).

### 8. 401 loop on intranet despite Windows auth enabled
- **Symptom:** Browser keeps prompting for credentials.
- **Root cause:** Anonymous auth still enabled, site not in the Local Intranet zone, or Anonymous
  ordered before Windows.
- **Diagnostic:** IIS Authentication settings; browser zone settings.
- **Fix:** Disable Anonymous; add site to Intranet zone for transparent SSO.
- **Prevention:** Bake auth settings into the deploy, not manual server clicks.
- **Confidence:** Medium. **Official-source-confirmed:** Partial.

### 9. SQL login lacks permission at runtime
- **Symptom:** App throws "permission denied" / "cannot open database" after a least-priv login swap.
- **Root cause:** App login is correctly non-sysadmin but missing a needed grant (e.g. EXECUTE on a
  proc, or a schema permission).
- **Diagnostic:** SQL error number; query the login's effective permissions.
- **Fix:** Grant the minimal additional permission; never escalate to sysadmin to "make it work."
- **Prevention:** Enumerate required permissions during design; test under the prod login in staging.
- **Confidence:** High. **Official-source-confirmed:** Yes (SQL roles docs).

### 10. Startup migration deadlocks / double-applies on multi-instance
- **Symptom:** Two app instances race to migrate; one fails or schema is left inconsistent.
- **Root cause:** `Database.Migrate()` at startup with >1 instance (no migration lock pre-EF9).
- **Diagnostic:** Correlate startup logs across instances; check `__EFMigrationsHistory`.
- **Fix:** Move to **idempotent SQL scripts** or a **migration bundle** applied once in a change
  window; keep startup-migrate only for the single-instance pilot.
- **Prevention:** Document a non-startup migration path for multi-instance production.
- **Confidence:** High. **Official-source-confirmed:** Yes (Microsoft Learn Applying Migrations).

### 11. Migration succeeds locally but fails on prod data
- **Symptom:** Migration that worked on an empty DB errors on production volume (timeout, lock).
- **Root cause:** Non-additive change on a large table; long-running lock.
- **Diagnostic:** Generate the SQL script; review against table sizes; check lock waits.
- **Fix:** Make changes additive/online; batch backfills; schedule a window.
- **Prevention:** CLAUDE.md rule — additive, backward-compatible migrations; DBA review of scripts.
- **Confidence:** High. **Official-source-confirmed:** Yes.

### 12. TLS — browser warns "not trusted" with self-signed cert
- **Symptom:** Padlock warning; clients must click through.
- **Root cause:** Self-signed cert doesn't chain to a trusted CA (encryption ≠ trust).
- **Diagnostic:** Inspect cert chain; external TLS scan.
- **Fix:** Install a **CA-issued** cert (enterprise PKI or public) and bind it.
- **Prevention:** Treat self-signed as pilot-only; plan CA issuance before GA.
- **Confidence:** High. **Official-source-confirmed:** Yes (TLS fundamentals).

### 13. HTTPS binding works but mixed-content / redirect loop
- **Symptom:** Redirect loop or blocked resources after enabling HSTS/HTTPS redirect.
- **Root cause:** HSTS enabled where the chain isn't trusted, or a proxy terminates TLS and the app
  doesn't see the original scheme.
- **Diagnostic:** Check `X-Forwarded-Proto`; check HSTS header + max-age.
- **Fix:** Configure **Forwarded Headers** middleware; enable HSTS only behind a trusted cert.
- **Prevention:** HSTS in production only (never dev); set forwarded-headers before auth.
- **Confidence:** High. **Official-source-confirmed:** Yes (HTTPS/HSTS + forwarded-headers docs).

### 14. Forwarded-headers ignored — wrong client IP / scheme
- **Symptom:** App logs proxy IP as client; HTTPS detection wrong.
- **Root cause:** `ForwardedHeadersMiddleware` not enabled or `KnownProxies`/`KnownNetworks` not set.
- **Diagnostic:** Log `HttpContext.Connection.RemoteIpAddress` and `Request.Scheme`.
- **Fix:** Enable forwarded headers early in the pipeline; configure known proxies.
- **Prevention:** Order matters — forwarded-headers must run before auth/HTTPS redirection.
- **Confidence:** High. **Official-source-confirmed:** Yes.

### 15. Entra/OIDC — invalid redirect URI
- **Symptom:** AADSTS50011 "redirect URI does not match."
- **Root cause:** App-registration reply URL differs from the app's configured callback (scheme/host).
- **Diagnostic:** Compare app-registration redirect URIs to the running app's callback.
- **Fix:** Register the exact callback (including https + host header).
- **Prevention:** Parameterize callback by environment; document per-tenant setup.
- **Confidence:** High. **Official-source-confirmed:** Yes (Entra OIDC docs).

### 16. Entra/OIDC — claims/roles missing, authorization fails
- **Symptom:** User authenticates but is denied; expected role/group claim absent.
- **Root cause:** App roles/groups not assigned, or token configured to omit group claims (overage).
- **Diagnostic:** Decode the token; inspect claims; check group-overage indicator.
- **Fix:** Assign app roles; emit role claims; use Graph for group overage.
- **Prevention:** Define the claims→RBAC contract up front (see `docs/Claims-Roles-Mapping.md`).
- **Confidence:** Medium. **Official-source-confirmed:** Partial.

### 17. CORS blocks a legitimate client
- **Symptom:** Browser console "No 'Access-Control-Allow-Origin'."
- **Root cause:** Origin not in the CORS policy, or credentials + wildcard origin combination.
- **Diagnostic:** Inspect preflight (OPTIONS) response headers.
- **Fix:** Add the specific origin; with credentials, set an explicit origin (not `*`).
- **Prevention:** Centralize CORS policy; least-origin.
- **Confidence:** High. **Official-source-confirmed:** Yes (ASP.NET Core CORS docs).

### 18. Auth cookie lost — SameSite / Secure mismatch
- **Symptom:** User logged out on navigation; cookie not sent.
- **Root cause:** `SameSite=None` without `Secure`, or scheme mismatch behind a proxy.
- **Diagnostic:** Inspect Set-Cookie attributes; check scheme via forwarded-headers.
- **Fix:** `Secure` + correct `SameSite`; fix scheme detection.
- **Prevention:** Set cookie policy explicitly; test behind the real proxy.
- **Confidence:** High. **Official-source-confirmed:** Yes.

### 19. Large file upload rejected (413 / request limit)
- **Symptom:** HTTP 413 or silent truncation on import of large ZIPs.
- **Root cause:** IIS `maxAllowedContentLength` and/or Kestrel/`MaxRequestBodySize` defaults too low.
- **Diagnostic:** Check the byte size at the failure; inspect both IIS + Kestrel limits.
- **Fix:** Raise both limits to the documented import ceiling; stream rather than buffer.
- **Prevention:** Document the supported import size; enforce it client-side too.
- **Confidence:** High. **Official-source-confirmed:** Yes (request-limits docs).

### 20. Request timeout / page hang under load
- **Symptom:** A page "starts" but never "completes" in `RequestTimingMiddleware` logs.
- **Root cause:** A blocking external call on the request path, or a `GroupBy(_ => 1)` aggregate / a
  list query materializing a large text column (CLAUDE.md known-hang causes).
- **Diagnostic:** Find the "started" with no matching "completed" line; review the query.
- **Fix:** Remove the blocking call (read health from cache); project list queries to lightweight
  records; use separate `CountAsync()` calls.
- **Prevention:** CLAUDE.md rules §5–§7; runtime smoke of all core pages after query/controller/DI
  changes.
- **Confidence:** High. **Official-source-confirmed:** Yes (firsthand + repo guidance).

### 21. SMTP send fails (relay/auth/TLS)
- **Symptom:** Notifications never arrive; SMTP exception.
- **Root cause:** Relay not permitted from the server IP, wrong port/STARTTLS, or bad credentials.
- **Diagnostic:** Test with a minimal SMTP client from the server; check relay logs.
- **Fix:** Allowlist the server, use the correct port + TLS mode, store creds securely.
- **Prevention:** Document SMTP config as an operator input; test from the target host.
- **Confidence:** Medium. **Official-source-confirmed:** No (general).

### 22. SFTP transfer fails (host key / auth)
- **Symptom:** Connection refused or host-key mismatch.
- **Root cause:** Unknown/changed host key, wrong key auth, or firewall.
- **Diagnostic:** Verbose SFTP log; confirm host key fingerprint; test port reachability.
- **Fix:** Pin the correct host key; use key-based auth; open the firewall.
- **Prevention:** Capture host key at onboarding; document the SFTP contract.
- **Confidence:** Medium. **Official-source-confirmed:** No (general).

### 23. WordPress/WooCommerce REST integration — 401/permission
- **Symptom:** REST calls return 401 or empty data.
- **Root cause:** Application-password/consumer-key auth misconfigured, or HTTPS required.
- **Diagnostic:** Reproduce with curl + the documented auth header.
- **Fix:** Correct credential type; ensure HTTPS; check user capabilities.
- **Prevention:** Encode each system's auth model in the integration-expectation library.
- **Confidence:** Medium. **Official-source-confirmed:** Partial (WP/Woo REST docs).

### 24. Magento/Adobe Commerce — token expiry / ACL scope
- **Symptom:** 401 after a while, or 403 on a resource.
- **Root cause:** Bearer token expired, or integration ACL doesn't grant the resource.
- **Diagnostic:** Inspect token lifetime; check integration ACL.
- **Fix:** Refresh tokens; widen ACL to the needed resources only.
- **Prevention:** Track token lifetimes; least-scope ACLs.
- **Confidence:** Medium. **Official-source-confirmed:** Partial (Adobe Commerce REST docs).

### 25. Odoo / Frappe ERP — RPC/REST auth & session
- **Symptom:** Authentication or session errors on XML-RPC/JSON-RPC/REST.
- **Root cause:** Wrong DB name, missing API key/session, or CSRF on REST.
- **Diagnostic:** Reproduce the documented login call; check DB/tenant selection.
- **Fix:** Supply correct DB + key; obtain a session token where required.
- **Prevention:** Capture per-tenant DB + auth in the expectation library.
- **Confidence:** Low. **Official-source-confirmed:** Partial (Odoo/Frappe docs).

### 26. Ollama not present / model not pulled
- **Symptom:** Inference features unavailable; health shows Ollama down.
- **Root cause:** Ollama optional and absent, or model not pulled.
- **Diagnostic:** Health cache (`IServiceHealthCache`); model registry.
- **Fix:** Degrade gracefully (MSSQL-only mode must still render); pull the model where wanted.
- **Prevention:** CLAUDE.md rule — Ollama optional; never block a page on it.
- **Confidence:** High. **Official-source-confirmed:** Yes (firsthand + CLAUDE.md).

### 27. Qdrant absent / REST unreachable
- **Symptom:** Vector retrieval unavailable.
- **Root cause:** Qdrant optional and not running; REST endpoint blocked.
- **Diagnostic:** Health cache snapshot; REST probe.
- **Fix:** Degrade to MSSQL memory; bring up Qdrant when desired (REST only).
- **Prevention:** Never call Qdrant synchronously on the request path.
- **Confidence:** High. **Official-source-confirmed:** Yes (firsthand + CLAUDE.md).

### 28. Large repository clone/import slow or OOM
- **Symptom:** ZIP import stalls or exhausts memory.
- **Root cause:** Buffering whole files / large text columns materialized in memory.
- **Diagnostic:** Profile memory during import; check file sizes.
- **Fix:** Stream extraction; chunk; avoid loading `RawText` into list views.
- **Prevention:** Enforce import-size ceiling; lightweight projections (CLAUDE.md §7).
- **Confidence:** Medium. **Official-source-confirmed:** Partial (firsthand).

### 29. Local-LLM hallucination / over-confident answer
- **Symptom:** Model invents an API, file, or "fact" not in the knowledge base.
- **Root cause:** Generation beyond retrieved/approved context; no grounding/citation.
- **Diagnostic:** Compare answer to retrieved chunks; check provenance.
- **Fix:** Inject approved knowledge first; require citations; cap/confidence-gate outputs
  (mean at the 90/90 cap per `LOCAL_LLM_REASONING_PROOF.md`).
- **Prevention:** Approval lifecycle + provenance (`docs/AI-Output-Provenance-and-Approval.md`);
  human-in-the-loop on agent proposals.
- **Confidence:** Medium. **Official-source-confirmed:** No (well-established RAG practice).

### 30. Benchmark timeout / flaky long-run
- **Symptom:** A benchmark item times out or varies run-to-run.
- **Root cause:** External fetch unavailable, or unbounded operation; some systems are
  metadata-only/unsupported in the benchmark.
- **Diagnostic:** Inspect the failed item; check the unsupported-gaps list.
- **Fix:** Mark unsupported items explicitly; bound per-item timeouts; don't count
  metadata-only coverage as full support.
- **Prevention:** Honest benchmark scoring (see `PUBLIC_SYSTEMS_UNSUPPORTED_GAPS.md`).
- **Confidence:** Medium. **Official-source-confirmed:** Yes (firsthand).

### 31. GitHub release artifact mismatch / checksum failure
- **Symptom:** Downloaded artifact checksum doesn't match the published value.
- **Root cause:** Wrong artifact attached, or checksum generated from a different build.
- **Diagnostic:** Recompute checksum; compare to `checksums/`.
- **Fix:** Re-attach the correct artifact + regenerate checksums together.
- **Prevention:** Generate artifact + checksum in one step; verify by fresh download
  (`POST_RELEASE_ARTIFACT_INTEGRITY.md`).
- **Confidence:** High. **Official-source-confirmed:** Partial (firsthand + GitHub releases docs).

### 32. Backup that never restores (untested RPO/RTO)
- **Symptom:** "Backups succeed" but a restore fails or is too slow when needed.
- **Root cause:** Backup not exercised; missing log chain; restore target untested.
- **Diagnostic:** Perform a timed restore into a clean instance.
- **Fix:** Run a restore drill; record actual RPO/RTO.
- **Prevention:** Treat restore (not backup) as the proof; schedule periodic drills
  (`docs/Database-Backup-Restore-Evidence.md`).
- **Confidence:** High. **Official-source-confirmed:** Yes (general DR practice).

---

## Cross-references

- Page-hang history and the project's hard query rules: `docs/07-Troubleshooting.md`, `CLAUDE.md` §5–§7.
- Existing community/official source notes: `docs/research/COMMUNITY_FAILURE_PATTERNS.md`,
  `docs/research/OFFICIAL_DEPLOYMENT_SECURITY_REFERENCES.md`.
- The Red-Team matrix and human-interaction model show which of these patterns are still **only**
  closeable by an external operator/customer.
