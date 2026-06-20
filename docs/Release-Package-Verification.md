# Release Package Verification

How a release package is inspected before handover, and the pass criteria each check enforces. The two
verification scripts are **read-only** — they never modify the package, the database, or a running
instance.

This document pairs with `docs/Clean-Machine-Install-Proof.md` (how the package is built) and
`docs/Customer-Handover-Package.md` (what ships and what is excluded).

---

## 1. The two verifiers

### `scripts/release/verify-installation.ps1`
Inspects an installation's artifacts and knowledge base. It runs:

1. `scripts/poc/verify-poc.ps1 -Fast` — the fast validation harness (build/extraction/benchmark
   smoke against fixtures).
2. `database/verify-knowledge-base.ps1` — confirms the curated knowledge base is present and seeded
   in the target database (default instance `(localdb)\MSSQLLocalDB`, database `LocalAIFactory`;
   override with `-Instance` / `-Database`).

It exits with the verification's status code. **Pass** = both the harness and the knowledge-base check
succeed.

### `scripts/release/post-install-healthcheck.ps1`
Probes a **running** instance (read-only) via the deploy health-check at a given URL (default
`http://localhost:8080`, override with `-Url`). **Pass** = the running instance responds healthy.

---

## 2. Pass criteria

A package is handover-ready when **all** of the following hold:

| # | Criterion | How it is checked |
|---|---|---|
| 1 | **Binaries present** | `app/` exists with the published Web host; the validation harness builds/runs |
| 2 | **Knowledge packs present** | `knowledge-packs/` contains professional-base (+ contracted add-ons); `verify-knowledge-base.ps1` confirms seeded items |
| 3 | **DB scripts present** | `database/` contains create/seed/backup/restore/verify scripts and the matching `appsettings.*.example.json` |
| 4 | **Docs present** | the install/SQL/IIS/backup/upgrade runbooks are in `docs/` |
| 5 | **No secrets** | no API keys, passwords, credentialed connection strings, or `keys/` in the package |
| 6 | **No oversized artifacts** | no file exceeds the **5 MB** ship threshold (catches stray binaries/dumps) |
| 7 | **Validation harness PASS** | `verify-poc.ps1 -Fast` succeeds |
| 8 | **Running instance healthy** | `post-install-healthcheck.ps1` passes against the deployed URL |

Criteria 5 and 6 are the **safety gates**: a package that contains a secret or an unexpected large
artifact must fail the handover regardless of functional status.

---

## 3. Running the verification

```powershell
# Build + package first (see Clean-Machine-Install-Proof.md)
scripts/release/build-release.ps1
scripts/release/package-release.ps1

# Verify artifacts + knowledge base (read-only)
scripts/release/verify-installation.ps1 `
  -Instance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory"

# After deploying + starting the package, verify the running instance
scripts/release/post-install-healthcheck.ps1 -Url http://localhost:8080
```

Interpret a non-zero exit code as a **failed** verification — do not hand over.

---

## 4. Manual safety sweep (criteria 5–6)

The scripts cover functional and knowledge-base checks; complete the safety gates with a quick manual
sweep of the assembled package directory before zipping/shipping:

- [ ] No file under `keys/` is present.
- [ ] No `appsettings*.json` contains a real connection string with credentials (only `.example.`
      templates ship).
- [ ] No API key, password, or token string is present in any shipped file.
- [ ] No `.git` directory, `bin/`, `obj/`, or `.tmp-*` working folder is present.
- [ ] No file exceeds 5 MB except the intentionally shipped application binaries.

Record the sweep result alongside the script outputs as the verification evidence for the release.

---

## 5. Relationship to the handover checklist

These pass criteria are the automated/manual core of the broader handover checklist in
`docs/Customer-Handover-Package.md` §3. A handover requires both: the verification here **and** the
documentation/honesty gates in the handover checklist.
