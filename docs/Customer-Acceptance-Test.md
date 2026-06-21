# Customer Acceptance Test — LocalAIFactory

The acceptance checklist a customer/operator runs to confirm a release package is **complete, safe,
and installable**, with the **pass criteria** for each item and the **observed result**. The checklist
is automated by `scripts/release/customer-acceptance-check.ps1`, which prints a clear
`ACCEPTED` / `NOT ACCEPTED` verdict. It is **read-only** — no destructive actions.

---

## 1. Run it

```powershell
# Default: checks the staged/extracted package and the live system where possible.
pwsh scripts/release/customer-acceptance-check.ps1

# Against a specific extracted package and a running app:
pwsh scripts/release/customer-acceptance-check.ps1 -Stage "./.tmp-clean-install" -AppUrl "http://localhost:5000"
```

Run after `build-release.ps1` → `package-release.ps1` (and, for the extracted-copy check, after
`simulate-clean-install.ps1`).

---

## 2. The checklist (what it verifies)

| # | Check | Backed by | Pass criterion |
|---|---|---|---|
| 1 | Release package complete and safe | `verify-release-package.ps1` | App binaries, ≥4 knowledge packs, ≥5 DB scripts, appsettings examples, the three manuals, manifest, release notes — **and no** secrets/keys/DB/backup/model files, no >5 MB doc assets |
| 2 | Knowledge base validates | `verify-all-knowledge-packs.ps1` | **4 packs / 438 items**, all UIDs valid GUIDs, **no within/cross-pack UID collisions**, every item has limitation+tags |
| 3 | Database full install verifies | `verify-full-install.ps1` | DB reachable, migrations applied (**14**), `KNOWLEDGE-BASE: VERIFIED`, source packs match live counts — *(skipped gracefully if `sqlcmd`/DB absent)* |
| 4 | Support dashboard reachable | `GET <AppUrl>/Support` | HTTP **200** — *(only when `-AppUrl` is supplied)* |

Each item prints `[PASS]` or `[FAIL]`; checks 3–4 print `[SKIP]` when their prerequisite is absent.
The final verdict is `ACCEPTED` only if **no** item failed.

---

## 3. Observed result

On the build host:

```
== Customer Acceptance Checklist ==
  [PASS] Release package complete and safe (verify-release-package)
  [PASS] Knowledge base validates (4 packs, 438 items, no UID collisions)
  [PASS] Database full install verifies (migrations + KB)
  ...
== Result ==
CUSTOMER-ACCEPTANCE: ACCEPTED
```

This corroborates the supporting evidence:

- Package: 16.2 MB / 277 files, `VERIFY-RELEASE-PACKAGE: PASS`
  (`docs/Published-Package-Contents.md`).
- Knowledge base: `VERIFY-ALL-KNOWLEDGE-PACKS: PASS` (`docs/Knowledge-Pack-Validation-Report.md`).
- Database: `VERIFY-FULL-INSTALL: PASS`, 14 migrations, `KNOWLEDGE-BASE: VERIFIED`
  (`docs/Database-Setup-Guide.md`).

---

## 4. Manual acceptance steps (for an operator)

Beyond the automated checks, an operator accepting a deployment should confirm:

1. The app starts: `dotnet run --project src/LocalAIFactory.Web` (or run the published `app/`).
2. Core pages return quickly (none may hang): `/`, `/Projects`, `/Knowledge`, `/Models`, `/Support`.
   The UI smoke run covers **11 pages** including `/Support` — PASS on the build host.
3. The knowledge base is present in the UI (Base Knowledge browser shows the 438-item base).
4. A backup + restore-verify drill succeeds (`docs/Database-Backup-Restore-Evidence.md`).

---

## 5. Honesty notes

- `ACCEPTED` here means the **package and local install** meet the checklist criteria on the build
  host. It is **not** a production sign-off: it does not assert an executed production/IIS/Express
  deploy, HA/DR, SSO, or an external pen-test (`docs/Known-Limitations.md`).
- Checks degrade gracefully: where `sqlcmd`, the database, or a running app is absent, the relevant
  item is **skipped** (not faked) and the verdict reflects only what was actually checked.
- The package under test is version **1.0.0-rc**, a release candidate, not a GA build.

## See also

- `docs/Published-Package-Contents.md`
- `docs/Clean-Install-Simulation-Evidence.md`
- `docs/Release-Checklist.md`, `docs/Release-Package-Verification.md`
