# Data Protection Plan

How LocalAIFactory protects data at rest and in transit, handles secrets, and where its data-governance
posture currently stands — including the honest gap that a **formal PII / retention policy is not yet
implemented**. Read with `docs/Secrets-Handling.md`, `docs/Security-Model.md`, `docs/Audit-Model.md`,
and `docs/Threat-Model.md`.

> Scope: a private, local-first, MSSQL-authoritative pilot on an internal banking network. Several
> protections below are **deployment responsibilities** (TLS, disk encryption, DB hardening) rather
> than controls the application itself enforces — this document marks which is which.

---

## 1. Data inventory

| Data | Sensitivity | Where it lives |
|---|---|---|
| Curated knowledge base | High (business IP) | MSSQL |
| Imported source code & extracted text | High (system internals) | MSSQL |
| API keys | High (secret) | Encrypted at rest via Data Protection; key ring under git-ignored `keys/` |
| DB connection strings | High (secret) | Environment variables / secret store — never committed |
| Audit trail | High (accountability) | MSSQL (`AuditEvent`, append-only) |
| Windows identities | Medium (PII-adjacent) | Referenced in `UserAccount` / audit; no passwords stored |

---

## 2. Data at rest

- **System of record:** Microsoft SQL Server is authoritative. Database-level encryption (e.g., TDE),
  disk encryption (e.g., BitLocker), and file-system ACLs are **deployment responsibilities** — the
  application assumes a properly administered MSSQL instance and host (`docs/SQL-Server-Deployment-Guide.md`).
- **API keys:** Encrypted at rest using ASP.NET Core **Data Protection**. The key ring is persisted to
  a git-ignored `keys/` directory with `SetApplicationName("LocalAIFactory")` (`Program.cs`). The
  `keys/` directory must be protected by OS file-system permissions and backed up securely; losing it
  means encrypted secrets cannot be decrypted.
- **No secrets in the repository:** No tracked keys, database files, model weights, or `keys/`
  content. Enforced by tests and the release-time static audit
  (`scripts/security/security-audit.ps1`, 0 HIGH this sprint).

---

## 3. Data in transit

- **Browser ↔ app:** Transport encryption (TLS/HTTPS) is terminated at **IIS** in a deployed
  environment — a **deployment responsibility** to configure with a valid certificate
  (`docs/Windows-Server-IIS-Deployment-Guide.md`). The application is internal and not internet-facing.
- **App ↔ MSSQL:** Use an encrypted SQL connection (`Encrypt=True`) appropriate to the instance and
  certificate trust on the host; this is a connection-string / deployment setting.
- **App ↔ optional AI (Ollama / Qdrant):** Local, loopback, optional. No customer data is sent to any
  external/internet service — the platform runs fully in MSSQL-only mode and does not depend on these.

---

## 4. Secrets handling

- API keys: Data Protection encrypted; never logged, never in committed config.
- Connection strings with credentials: environment variables or a secret store; committed
  `appsettings.*.example.json` use placeholders or Trusted Connection only.
- `.env`, `keys/`, and local overrides are git-ignored.
- Full guidance: `docs/Secrets-Handling.md`. Verification: `scripts/security/security-audit.ps1`.

---

## 5. Retention and PII posture (honest gap)

- **No formal PII / data-retention / DLP policy is implemented.** Retention today is **operational
  guidance** (`docs/Audit-Model.md` §3), not an enforced control. There is no PII classification
  scheme and no data-loss-prevention layer.
- **What is true now:** the audit trail is append-only and references Windows identities (PII-adjacent).
  Imported source and knowledge are retained until an authorised user removes them. No automatic
  expiry or purge exists.
- **Closing proof** (from `docs/Known-Limitations.md` §7): a documented, enforced retention and
  classification policy with the mechanisms to apply it — e.g., a retention schedule with an
  enforced purge/archive job, a PII classification on stored fields, and tests proving the policy is
  applied.

Until that exists, **do not represent the platform as having a PII or data-retention control**. It
has secure storage primitives (Data Protection, no-secrets-in-repo, append-only audit) but not a
governed retention/classification regime.

---

## 6. Backup and recovery

- Database backups are supported and verified: a prior-sprint backup was 69.5 MB with
  `RESTORE VERIFYONLY` returning OK (`docs/Backup-Restore-Runbook.md`).
- Backups contain sensitive data and must be stored with the same protections as the live database
  (access-controlled, encrypted storage). The `keys/` directory must be backed up alongside, securely,
  or Data-Protection-encrypted secrets become unrecoverable.

---

## 7. Responsibilities summary

| Control | Owned by |
|---|---|
| Data Protection encryption of API keys | Application |
| No secrets in repo | Application + release audit |
| Append-only audit | Application |
| TLS / HTTPS termination | Deployment (IIS) |
| Disk / database encryption (TDE/BitLocker) | Deployment / DBA |
| `keys/` file-system protection & secure backup | Operator |
| Formal PII / retention / DLP policy | **Not yet implemented** (gap) |
