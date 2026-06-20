# Customer Handover Package

What ships to a customer, what is deliberately excluded, and the checklist to confirm a handover is
complete and safe. This complements `docs/Clean-Machine-Install-Proof.md` (how the package is built
and verified) and `docs/Release-Package-Verification.md` (the automated pass criteria).

The package is a **deployable artifact, not the source tree**. It is assembled by
`scripts/release/package-release.ps1` into a git-ignored output directory and committed nowhere.

---

## 1. What ships

| Component | Source | Purpose |
|---|---|---|
| **Application binaries** | `app/` (from `build-release.ps1` publish) | The runnable Web host |
| **Database scripts** | `database/` | Create, seed, backup, restore, verify, migrate |
| **appsettings examples** | `database/appsettings.*.example.json` | LocalDB / SQL Express / full SQL Server templates |
| **Knowledge packs** | `knowledge-packs/` | professional-base (required) + optional add-on packs |
| **Install / ops scripts** | `scripts/release/`, `scripts/diagnostics/` (as included) | Verify install, health-check, diagnostics |
| **Documentation** | `docs/` subset | Install, SQL, IIS, backup/restore, upgrade/rollback runbooks |
| **Release template files** | `release-template/*` | Any templated top-level ship files |

Knowledge packs ship as folders (`<pack>-v1/`) each with a `manifest.json` (name, version, item
count, source policy, `legalLimitations`, review status) and item files. All pack content is
**original summaries, awareness-level only** — no verbatim regulatory/standard/vendor text — which is
what keeps the pack distributable.

---

## 2. What is excluded

Explicitly **not** in the package:

- **Secrets** — no API keys, no passwords, no connection strings with embedded credentials.
- **Data Protection keys** — the git-ignored `keys/` directory is never shipped; each deployment
  generates/holds its own.
- **The `.git` directory and full source tree** — the package is binaries + ship assets, not source.
- **Build intermediates** — `bin/`, `obj/`, `.tmp-*` working directories.
- **Model weights** — Ollama models are obtained and run by the customer under their own terms; none
  are bundled or redistributed.
- **Large unrelated artifacts** — nothing over the size threshold the verifier enforces (see
  `docs/Release-Package-Verification.md`).

Connection strings and any license configuration are supplied by the customer/operator via
environment variables or a git-ignored local `appsettings` override at the deployment site — never
baked into the package.

---

## 3. Handover checklist

Confirm each item before declaring a handover complete.

### Package integrity
- [ ] Package built from a clean publish via `build-release.ps1` then `package-release.ps1`.
- [ ] `app/`, `database/`, `knowledge-packs/`, and `docs/` are all present in the package.
- [ ] `scripts/release/verify-installation.ps1` passes (validation harness + knowledge-base verify).
- [ ] No secrets, no `keys/`, no `.git`, no build intermediates in the package.
- [ ] No artifact exceeds the verifier's size threshold.

### Deployment readiness
- [ ] The correct `appsettings.*.example.json` for the customer's SQL host is included.
- [ ] Database create/seed path documented for the target host (LocalDB / SQL Express / full server).
- [ ] Connection string + any license config delivered **separately and securely**, not in the
      package.
- [ ] `post-install-healthcheck.ps1` passes against the running instance at the customer URL.

### Documentation
- [ ] Operator Manual, Admin Manual, User Manual, and the relevant deployment guide are included.
- [ ] Backup/restore and upgrade/rollback runbooks are included.
- [ ] `docs/Known-Limitations.md` is included — the customer must know what is **not** claimed.

### Honesty gates
- [ ] No compliance, regulatory, financial, or certification claim is made anywhere in the handover.
- [ ] Edition/license behaviour explained as demo-safe (missing/expired ⇒ Community core).
- [ ] Bare-metal deployment status stated honestly (see `docs/Clean-Machine-Install-Proof.md` §6).

---

## 4. Post-handover

- The customer creates their database and runs the app; it migrates, seeds, and installs the base
  pack on first startup.
- Operators use the diagnostics scripts and the `/Support` page for ongoing health.
- Support and escalation paths: `docs/Support-Runbook.md`. Onboarding: `docs/Customer-Onboarding-Guide.md`.

> Reminder: the durable asset handed over is the **curated, governed memory plus the platform that
> grows it** — not any particular model. Models are replaceable and customer-supplied.
