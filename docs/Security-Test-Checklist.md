# Security Test Checklist

A practical checklist combining the **runnable safe checks** that exist today with a **manual
checklist** for review. This is for internal verification and pre-assessment hygiene; it is **not** a
substitute for an external penetration test (`docs/Security-Pentest-Readiness.md`).

---

## 1. Runnable safe checks

These make no system changes and require no network. Run from the repository root in PowerShell.

### 1.1 Static security self-audit

`scripts/security/security-audit.ps1` — read-only static scan over **tracked** files. It flags:

- **Forbidden / large tracked artifacts** — `bin/obj`, `.mdf/.ldf/.bak`, model files
  (`.gguf/.onnx/.safetensors`), key material (`.pfx/.pem/.key`, anything under `keys/`), and any
  tracked file over 5 MB.
- **Potential secrets / hardcoded credentials** — conservative regexes for password assignments,
  API-key/secret/token literals, AWS access keys, and private-key blocks. Obvious placeholders and
  `Integrated Security`/`Trusted_Connection` strings are allowlisted to avoid noise.
- **Dangerous shell commands in committed scripts** — destructive patterns (`rm -rf /`,
  `git reset --hard`, `git clean -fdx`, `DROP DATABASE`, drive format, recursive force-delete).

```powershell
pwsh scripts/security/security-audit.ps1                 # report only
pwsh scripts/security/security-audit.ps1 -FailOnFindings # non-zero exit on any HIGH (gate a release)
```

**Expected (clean) result:** `0 HIGH` findings. INFO findings for guarded destructive patterns in
operator scripts are acceptable when reviewed — confirm they are intentional and guarded.

### 1.2 Security unit tests

The security behaviours are covered by `tests/LocalAIFactory.Tests/SecurityTests.cs` and related
suites. Run the full suite:

```powershell
dotnet test tests/LocalAIFactory.Tests/LocalAIFactory.Tests.csproj -c Release
```

Behaviours asserted include: the dev-auth guard (throws outside Development), deny-by-default project
access, the IDOR cross-project regression (403), and the no-secrets audit.

---

## 2. Manual checklist

Work through this before any assessment, after adding endpoints, and as a periodic review. Tick each
item only when verified by reading the code path, not by assumption.

### 2.1 Authorization on every admin / privileged endpoint

- [ ] Every controller that mutates state or exposes admin function derives from `SecuredController`.
- [ ] Each admin action calls `RequireAdminAsync(...)` **before** the action body and returns the
      denial result.
- [ ] Each project-scoped action calls `RequireProjectAsync(projectId, ...)` before doing work.
- [ ] Denials return HTTP 403 + the shared `AccessDenied` view and are audited as `AuthDenied`.
- [ ] No control relies on hiding a button/nav link as the enforcement (server-side only).
- [ ] Newly added endpoints since the last review are covered by the above.

### 2.2 IDOR / object-reference safety

- [ ] No action trusts an id because it parsed — every project/resource id is authorised against the
      caller's grants.
- [ ] A cross-project id returns 403 (regression test present and passing).
- [ ] List queries never leak rows the caller has no grant for.

### 2.3 File-upload / import handling

- [ ] ZIP import rejects path-traversal / zip-slip entries (no write outside the intended target).
- [ ] Oversized or malformed archives are handled without crashing the request path.
- [ ] Hostile content is treated as data, never executed.
- [ ] Import is an Analyst+/authorised action, not anonymous.

### 2.4 Audit coverage

- [ ] Every privileged action and every denial writes an `AuditEvent`.
- [ ] Audit writes are wrapped so a failure cannot break the request path.
- [ ] Grant/revoke of project access is audited (`AccessGranted` / `AccessRevoked`).
- [ ] Known gap acknowledged: audit is append-only by convention, **not** tamper-evident
      (hash-chaining is a future proof — `docs/Known-Limitations.md` §4).

### 2.5 Secrets & configuration

- [ ] `keys/`, `.env`, and local config overrides are git-ignored and untracked.
- [ ] Committed `appsettings.*.example.json` use placeholders or Trusted Connection only.
- [ ] API keys are stored via Data Protection, never in plaintext config.
- [ ] `security-audit.ps1` reports 0 HIGH.

### 2.6 Optional-AI boundary

- [ ] The app renders core pages with Ollama and Qdrant absent (MSSQL-only mode).
- [ ] No controller/view calls an external AI service synchronously on the request path
      (health read from the cached snapshot only).

### 2.7 Autonomous execution boundary

- [ ] The fix loop / executor defaults to dry-run; execution requires an explicit flag.
- [ ] Denied (destructive/production/history-rewriting) commands never run.
- [ ] commit/push/deploy/migrations are approval-gated, never run autonomously.
- [ ] The executor reports `Promoted: false` / `Committed: false` — it cannot self-promote.

---

## 3. What this checklist does not cover

This is internal hygiene. It does **not** include offensive testing (fuzzing, session abuse,
privilege-escalation chains), which require an independent assessor against a deployed target — see
`docs/Security-Pentest-Readiness.md` §3.
