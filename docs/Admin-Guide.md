# Administrator Guide

For LocalAIFactory administrators. Covers users & access, audit review, the knowledge pack, project import,
health/readiness, backup/restore, configuration, and Data Protection keys. Admin actions are enforced
**server-side** and **audited**; the UI hides what a user cannot reach, but the server is authoritative.

---

## 1. Users & access (RBAC)

- **Bootstrap admin** — set in config; provisioned Admin on first login and re-asserted every login (recovery):

  ```json
  "Security": { "BootstrapAdmin": "DOMAIN\\your-admin" }
  ```

- **Deny-by-default** — every other first-seen user is provisioned **Viewer** with **no project access**
  until you grant it.
- **Roles:**

  | Role | Import | Consolidate / maintenance | Manage users & access | View granted projects | Audit |
  |---|---|---|---|---|---|
  | **Admin** | ✅ | ✅ | ✅ | ✅ (all) | ✅ |
  | **Analyst** | ❌ | ❌ | ❌ | ✅ (granted only) | ❌ |
  | **Viewer** | ❌ | ❌ | ❌ | ✅ read-only (granted only) | ❌ |

- **Manage users at `/Users`** (Admin-only): assign roles, grant/revoke per-project access, disable users.
- A non-admin sees **only** granted projects. Direct-URL access to an ungranted project returns **403** and is
  audited as `AuthDenied`. An IDOR regression test pins that symbol detail does not leak across projects.

See `docs/08-Security.md` for the full model.

---

## 2. Audit trail review

- The audit trail is **append-only** — never updated or deleted.
- View at **/Audit** (Admin-only); filter by user or event type.
- Each `AuditEvent` records **who / what / when / which project / IP / denied?**.
- Event types include: `AuthSuccess, AuthDenied, ImportStarted, ImportCompleted, ProjectViewed,
  SymbolQueried, DependencyViewed, ImpactQueried, CoverageViewed, AccessGranted, AccessRevoked, RoleChanged,
  UserDisabled, ConsolidationStarted, ConsolidationCompleted, KnowledgePackInstalled`.
- Use it to answer access questions during a pilot: who viewed which project, what was denied, who changed a
  role or grant.

---

## 3. Knowledge pack — install & verify

The Professional Base Knowledge Pack is **390 curated items, 22 categories**, with a validated source
registry. Pack install is Admin-only and audited (`KnowledgePackInstalled`).

```powershell
# Install / re-assert the pack (propose-never-overwrite; idempotent)
./database/seed-professional-knowledge-base.ps1

# Verify (read-only; safe against any environment)
./database/verify-knowledge-base.ps1 -ServerInstance "(localdb)\MSSQLLocalDB" -Database LocalAIFactory
```

The verifier checks: pack row present, baseline item count, **no duplicate Uids**, all baseline items
**curated**, pack-origin provenance present, and the **source registry referenced** (`src:` tags). It exits
non-zero on any failure. Items reference only **registered** sources; an item that references an unregistered
source causes the whole pack to be rejected with **no DB writes**.

---

## 4. Import projects

- Use the **Import** wizard (Admin-only; audited `ImportStarted` / `ImportCompleted`).
- Upload a repository ZIP. The pipeline detects binary content, decodes text honestly, and processes each file
  in a bulletproof loop — **a bad repository never crashes the platform**; bad files are recorded as skips.
- Watch the job to **Completed**, then review the coverage/gap report.
- Supported extraction: **C#/.NET (Roslyn), T-SQL (ScriptDom), Python**. Other languages are reported as
  honest gaps, not failures.
- The `IngestionBackgroundService` drains the import queue; imports do not block the request path.

---

## 5. Health & readiness

- **Health** is read from a **cached snapshot** (`IServiceHealthCache`) populated by `HealthMonitorService` —
  controllers and views never call Qdrant/Ollama synchronously, so pages never hang on an external service.
- **Readiness** scorecard renders live at **/Readiness** (overall ≈49% and rising). Scores are deliberately
  conservative (0/25/50/75/90/100); 100 requires implemented + tested + demonstrated + documented +
  reviewable. See `docs/Readiness-Maturity-Model.md`.
- Run the post-install health check against a running instance:

  ```powershell
  ./scripts/release/post-install-healthcheck.ps1 -Url http://localhost:8080
  ```

- `RequestTimingMiddleware` logs `-> {path} started` and `<- {path} {status} in {ms} ms` (Warning > 1 s). A
  "started" line with no matching "completed" line locates a stalled endpoint.

---

## 6. Backup & restore

MSSQL is the authoritative store — back it up like any production database.

```powershell
# Back up
./database/backup-database.ps1

# Restore
./database/restore-database.ps1

# Restore into a scratch DB and verify it (non-destructive check)
./database/restore-verify-database.ps1
```

See `docs/Backup-Restore-Runbook.md` for cadence and retention guidance. **No destructive DB change** (drop,
rename, lossy type change, data deletion) should be made without explicit approval — default to additive,
backward-compatible changes.

---

## 7. Configuration (appsettings & optional AI)

- **Connection string** — point at the reachable SQL Server (LocalDB / Express / full). Credentials go in
  environment variables or a git-ignored local override, **never** in committed config.
- **Security** — `BootstrapAdmin` (above). `Security:UseDevAuth` is a **dev-only** scheme; startup **fails**
  if it is set in a non-Development environment (`SecurityStartup.GuardDevAuth`). Never trust the `X-Dev-User`
  header in production — it is unreachable there.
- **Optional Ollama** — config-gated; degrades gracefully when absent. Verified with `qwen2.5-coder` /
  `deepseek-r1`. Setup helper: `scripts/setup-ollama.ps1`; check: `scripts/ai/check-ollama.ps1`.
- **Optional Qdrant** — config-gated, REST-only, optional; `projectId=0` denotes global knowledge. Setup
  helper: `scripts/start-qdrant-docker.ps1`. The platform must keep working in **MSSQL-only mode** with both
  absent.

---

## 8. Data Protection keys

- API keys and secrets are **encrypted at rest** via ASP.NET Data Protection.
- Keys live in `keys/`, which is **git-ignored** — they must never be committed.
- For a multi-server or reinstall scenario, preserve the `keys/` directory so encrypted values remain
  decryptable. Losing the keys means re-entering any encrypted secrets.
- The POC verifier confirms no `keys/` material is tracked in git.

---

## 9. Admin honesty notes

- Authentication is **Windows (Negotiate)** only — **no SSO/IdP**.
- Security is **pilot-grade**, not a full bank-production accreditation (see `docs/08-Security.md`).
- OCR/PDF/forecasting are prototypes/design; the autonomous workspace is **dry-run + allowlisted** and
  executes nothing destructive.
- No commercial license enforcement exists yet — access is governed by RBAC and contract, not licensing code.
