# Administrator Manual

For administrators who manage users, access, knowledge packs, models, licensing, and the audit trail
for a LocalAIFactory instance. Admin actions are all enforced **server-side** and are **audited
identically** to any other user's actions.

This manual assumes the database is already created and the app is running (see the Operator Manual
for install/run). It complements `docs/Admin-Guide.md`, `docs/RBAC-Matrix.md`, and
`docs/Security-Model.md`.

---

## 1. Roles and access (Users & Access)

Roles are a total order: **Viewer (0) < Analyst (1) < Admin (2)**. Higher roles inherit lower-role
capabilities. The defining rule is **deny-by-default**: a new account is a Viewer, enabled, with no
project grants, and can see nothing project-scoped until you grant access.

| Capability | Viewer | Analyst | Admin |
|---|:---:|:---:|:---:|
| Sign in | ✅ | ✅ | ✅ |
| View / query a granted project | ✅¹ | ✅¹ | ✅ |
| Import a repository / run consolidation | ❌ | ✅¹ | ✅ |
| Install / update a knowledge pack | ❌ | ❌ | ✅ |
| Grant / revoke project access | ❌ | ❌ | ✅ |
| Manage users (create, change role, disable) | ❌ | ❌ | ✅ |
| View the audit trail | ❌ | ❌ | ✅ |

¹ Project-scoped: requires the role **and** an explicit `ProjectAccess` grant for that project.
Admins bypass the per-project allow-list but are audited identically.

On the **Users & Access** page you can:

- **Create / view users** and see their role and enabled state.
- **Change a role** (`RoleChanged` audit event).
- **Disable an account** (`UserDisabled`) — disabled accounts are treated as unauthorized for every
  gated action.
- **Grant / revoke per-project access** (`AccessGranted` / `AccessRevoked`).

Enforcement is in the controller (`RequireAdminAsync` / `RequireProjectAsync`) before the action body
runs. UI hiding is never the control. Denials return HTTP 403 with an `AccessDenied` view and are
audited as `AuthDenied`.

> Note: `AccessLevel.Write` exists in the model but is **reserved** — current grants operate at
> `Read`. There is no external IdP / AD-group sync; roles and grants are managed in-app. See
> `docs/Known-Limitations.md`.

---

## 2. Installing knowledge packs

Knowledge packs are curated, versioned bundles of professional summaries. The core
**professional-base** pack is installed automatically on first startup. Three optional add-on packs
are available (48 items total):

- `financial-institution-operations-v1` (16 items)
- `kyc-aml-transaction-approval-v1`
- `market-intelligence-forecasting-v1`

Each pack ships as a folder under `knowledge-packs/<pack>-v1/` with a `manifest.json` plus one or
more item files. The manifest records the pack name, version, item count, files, source policy,
review status, and an explicit `legalLimitations` note.

**Install path:** installation is an **Admin-only** action (`KnowledgePackInstalled` audit event).
The installer is **idempotent** — re-running does not duplicate items — and validates the pack's
source policy. Use the validated installer (Admin UI action, or the seed/verify scripts under
`database/`). To verify after install:

```powershell
database/verify-knowledge-base.ps1 -ServerInstance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory"
```

Pack content is **awareness-level only** and carries a per-item limitation note. It is **not** legal,
regulatory, financial, or compliance advice. See `docs/Knowledge-Pack-Authoring-Guide.md` to author
new packs.

---

## 3. Models configuration

The **Models** page configures optional local AI. Models are **optional and pluggable** — the
platform runs fully in MSSQL-only mode without any model.

- Inference and embeddings run through **Ollama** (optional). Models verified this sprint:
  `qwen2.5-coder:14b` and `deepseek-r1:14b`; embeddings via `nomic-embed-text` (768-dim).
- Vector storage uses **Qdrant** (REST only, optional). `projectId=0` denotes global knowledge.
- Both are gated behind config flags and degrade gracefully when absent. The health monitor probes
  them on a background timer; the request path never calls them synchronously.

When a model is unavailable, AI-assisted features (Chat) are disabled and the rest of the platform is
unaffected. The Home dashboard and the Support page show whether chat/AI is currently available.

---

## 4. Edition and licensing (Support page)

The **Support** page reports the current **edition** and **license status**, evaluated
deterministically and **demo-safe**:

- Editions: **Community**, **ProfessionalPilot**, **Enterprise**.
- A **missing or expired license degrades to Community core** — it never blocks read access to a
  customer's own knowledge or corrupts data.
- License inputs are read from configuration (`Licensing:Edition`, `Licensing:ExpiryUtc`,
  `Licensing:CustomerId`, `Licensing:CustomerName`). An absent or `Community` edition resolves to
  Community core (no license file required).
- A grace-period, expired, or invalid license raises a **warning** tile on the Support page rather
  than failing.

See `docs/Edition-and-Licensing-Strategy.md` for the broader strategy and its honest gaps.

---

## 5. Audit Trail

The **Audit Trail** is **Admin-only** and **append-only by convention**. Every gated action is
recorded with the acting principal, action, and timestamp. Examples: `AuthDenied`, `AccessGranted`,
`AccessRevoked`, `RoleChanged`, `UserDisabled`, `KnowledgePackInstalled`.

Use it to answer "who did what, when". Limitation: audit is append-only **by convention, not
tamper-evident** — there is no hash chaining or cryptographic sealing yet (see `docs/Audit-Model.md`
and `docs/Known-Limitations.md`).

---

## 6. Health monitoring

Service health is read from a **cached snapshot** (`IServiceHealthCache`) updated by a background
monitor — the request path never blocks on Qdrant or Ollama. The snapshot drives:

- The environment **mode** label (Minimal / Standard / FullAi).
- The optional-service status tiles (Qdrant, Ollama, embeddings) on Home and Support.
- The `ChatAvailable` flag.

For deeper, on-demand checks run the read-only diagnostics scripts (see the Operator Manual):
`scripts/diagnostics/sql-health-check.ps1`, `ollama-health-check.ps1`, `gpu-health-check.ps1`,
`system-snapshot.ps1`.

---

## 7. Backup and restore (pointers)

MSSQL is the source of truth; backing it up backs up everything authoritative. Vector and graph
projections are rebuildable.

- Backup: `database/backup-database.ps1`
- Restore: `database/restore-database.ps1`, then verify with `database/restore-verify-database.ps1`
- Rebuild derived indexes after restore: `database/reset-derived-indexes.ps1`

Full procedures: `docs/Backup-Restore-Runbook.md`. Upgrade / rollback:
`docs/Upgrade-Rollback-Runbook.md`.

---

## 8. Operational guardrails

- **No destructive database changes without explicit approval.** Schema changes are additive and
  backward-compatible by default.
- **No secrets in the repo.** API keys are encrypted at rest via Data Protection (keys live in a
  git-ignored `keys/`); connection strings with credentials go in environment variables or a
  git-ignored local override. See `docs/Secrets-Handling.md`.
- Core pages (Home, Projects, Knowledge, Models) must always load — on an empty DB, a seeded DB, or
  MSSQL-only. If a page hangs, consult `docs/07-Troubleshooting.md`.
