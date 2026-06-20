# Clean-Machine Install Proof

This document describes the approach used to prove that LocalAIFactory installs and runs from a
**clean folder** — i.e. a published artifact plus ship assets, with no dependency on the source
tree — and is honest about what remains for a true bare-metal proof on a fresh Windows Server.

The goal is a deterministic, reproducible handover: a customer (or a clean machine) receives a
package, follows the steps, and the app runs against MSSQL with seeded knowledge.

---

## 1. What "clean machine" means here

Two distinct levels of proof, not to be conflated:

1. **Clean-folder simulation (achievable on the build host).** Publish the app to an isolated output
   directory and assemble a self-contained package that contains everything needed to run, then
   verify it without referencing the source tree. This proves the **package is complete and
   self-describing**.
2. **Bare-metal proof (still outstanding).** Take that package to a **fresh Windows Server** with only
   the OS and a SQL Server instance, install, and run the core-page smoke on that host. This proves
   the **host prerequisites and deployment** end to end.

This document delivers (1) and is explicit that (2) is not yet demonstrated.

---

## 2. Publish output

Build a self-contained publish into a git-ignored output directory:

```powershell
scripts/release/build-release.ps1     # produces a publish under ./.tmp-publish (git-ignored)
```

The publish output is the application binaries and content required to run the Web host — it does
**not** contain the source projects, the `.git` directory, build intermediates, or developer tooling.

---

## 3. Release package layout

```powershell
scripts/release/package-release.ps1   # assembles a timestamped zip under ./.tmp-release (git-ignored)
```

The package (`LocalAIFactory-release-<timestamp>.zip`) is assembled from the publish plus ship assets
and has this layout:

```
app/                 published application (binaries + content)
database/            create/seed/backup/restore/verify scripts + appsettings examples
knowledge-packs/     professional-base + optional add-on packs (manifests + items)
docs/                installation, SQL, IIS, backup/restore, upgrade/rollback guides
release-template/*   any templated ship files (overlaid into the package root)
```

It is a **deployable artifact, not source-controlled** — the packaging script commits nothing and
writes only to git-ignored output directories.

---

## 4. What a clean folder must contain

For the package to be self-sufficient on a clean machine it must contain:

- `app/` — the published Web host (runnable with the .NET 10 runtime present, or self-contained).
- `database/` — the create/seed scripts for the target SQL host plus an `appsettings.*.example.json`
  matching that host (LocalDB / SQL Express / full SQL Server).
- `knowledge-packs/` — at least the professional-base pack so seeding installs curated knowledge;
  optional add-on packs as contracted.
- `docs/` — the install and runbook guides needed to operate without the source tree.

It must **not** contain: secrets, Data Protection keys (`keys/`), connection strings with embedded
credentials, the `.git` directory, build intermediates, or large unrelated artifacts.

---

## 5. Deterministic clean-folder simulation

The simulation verifies the assembled package without touching the source tree:

```powershell
# 1. Build + package (sections 2–3)
scripts/release/build-release.ps1
scripts/release/package-release.ps1

# 2. Verify the installation artifacts + knowledge base (read-only)
scripts/release/verify-installation.ps1

# 3. After deploying the package to an isolated folder and starting it against MSSQL,
#    run the running-instance health check
scripts/release/post-install-healthcheck.ps1 -Url http://localhost:8080
```

`verify-installation.ps1` runs the fast validation harness (`scripts/poc/verify-poc.ps1 -Fast`) and
`database/verify-knowledge-base.ps1`. `post-install-healthcheck.ps1` probes a running instance via the
deploy health-check. See `docs/Release-Package-Verification.md` for the pass criteria.

The simulation is **deterministic**: the same publish + ship assets produce the same package layout
and the same verification outcome, so a handover is reproducible rather than a one-off.

---

## 6. What remains for a true bare-metal proof

Honest gap list — none of these are closed by the simulation:

1. **Run on a fresh Windows Server.** Deploy the package to a server with only the OS + SQL Server,
   confirm the .NET 10 runtime requirement is met (or ship self-contained), and run the core-page
   smoke on that host.
2. **IIS path.** Exercise the IIS deployment (`docs/Windows-Server-IIS-Deployment-Guide.md`); a
   dry-run script exists (`scripts/release/install-windows-server-iis-dryrun.ps1`) but a real IIS
   deploy has not been demonstrated.
3. **SQL Express / full SQL Server on the target host.** The build host validated LocalDB; the SQL
   Express and full-server create/seed paths are documented but not host-verified end to end here.
4. **No-internet / air-gapped run.** Confirm the package runs with no outbound network, including the
   CDN-served frontend assets if the deployment must be fully offline.

Until these are demonstrated on a representative host and captured in logs, treat the clean-folder
simulation as proof of **package completeness**, not proof of **bare-metal deployment**. See
`docs/Known-Limitations.md` §5 and `docs/Customer-Handover-Package.md`.
