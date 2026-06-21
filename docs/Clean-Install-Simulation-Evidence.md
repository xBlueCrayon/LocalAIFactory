# Clean-Install Simulation Evidence — LocalAIFactory

Evidence that the **release ZIP is self-sufficient** — that a customer can extract it into a clean
folder and the package verifies on the **extracted copy** (not the repo). It also states plainly the
**external proof that remains**: a true fresh Windows machine with .NET 10 + SQL Server.

The simulator is `scripts/release/simulate-clean-install.ps1`. It is read-only with respect to the
repo and writes only into a throwaway, git-ignored clean folder.

---

## 1. What the simulation proves

1. Takes the newest release ZIP (default: newest under `./.tmp-release`).
2. Extracts it into a **fresh** folder (`./.tmp-clean-install`), deleting any prior copy first.
3. Runs `verify-release-package.ps1` against the **extracted copy** — proving the **ZIP**, not the
   repository, contains everything: app binaries + the 4 knowledge packs + database scripts +
   appsettings examples + docs/manuals + scripts + manifest + release notes, and **no** forbidden
   artifacts.

This closes the gap between "the repo can build a package" and "the shipped package is complete on its
own."

---

## 2. Run it

```powershell
# Build + package first if you have not already:
pwsh scripts/release/build-release.ps1      # PUBLISH OK: 151 files
pwsh scripts/release/package-release.ps1    # PACKAGE OK: 16.2 MB / 277 files

# Then simulate a clean install:
pwsh scripts/release/simulate-clean-install.ps1
```

Observed output (abridged):

```
== Extracting LocalAIFactory-release-<stamp>.zip -> ./.tmp-clean-install ==
== Extracted top-level contents ==
  app
  database
  knowledge-packs
  scripts
  docs
  RELEASE_NOTES.md
  RELEASE_MANIFEST.json
  ...
== Verify the EXTRACTED package (not the repo) ==
  ...
VERIFY-RELEASE-PACKAGE: PASS
```

**Result: PASS on the extracted copy.** The verification ran against the clean-folder extraction, not
the source tree.

---

## 3. Evidence summary

| Aspect | Result |
|---|---|
| ZIP extracted to a clean, git-ignored folder | yes |
| Verification target | the **extracted copy** (`./.tmp-clean-install`), not the repo |
| `verify-release-package.ps1` on the extracted copy | **PASS** |
| Package figures | 16.2 MB / 277 files (`docs/Published-Package-Contents.md`) |
| Acceptance against the extracted copy | `customer-acceptance-check.ps1 -Stage ./.tmp-clean-install` → ACCEPTED |

---

## 4. What this does NOT prove (the remaining external proof)

This simulation runs **on the build/dev host**. It proves the package is self-contained; it does
**not** prove a working deployment on a separate machine. The simulator itself prints this caveat:

> A TRUE clean-machine proof still requires extracting on a fresh Windows host with .NET 10 +
> SQL Server/LocalDB, running `database/setup-full-local-demo.ps1`, and starting the app.

**Remaining external proof (open):** on a genuinely fresh Windows VM/server with **.NET 10** and a
**SQL Server** instance (LocalDB/Express/full), and **nothing else from this repo**:

1. Extract the release ZIP.
2. Run `database/setup-full-local-demo.ps1` (or the target-specific setup in
   `docs/Database-Setup-Guide.md`).
3. Start the app and confirm the core pages (`/`, `/Projects`, `/Knowledge`, `/Models`, `/Support`)
   load.
4. Run `scripts/release/customer-acceptance-check.ps1 -AppUrl http://localhost:5000` → ACCEPTED.

Until that fresh-host run is captured, the clean-install evidence is **package self-sufficiency only**,
not an executed clean-machine deployment. This aligns with `docs/Known-Limitations.md` §5
(no production/IIS/Docker/Express/full-SQL deployment executed here).

---

## 5. Honesty notes

- The PASS is real and is on the **extracted** package, not the repo — that distinction is the point
  of the exercise.
- No production, IIS, Docker, Express, or full-SQL deployment has been executed; the fresh-VM proof
  above is the explicit, named gap.

## See also

- `docs/Published-Package-Contents.md`
- `docs/Customer-Acceptance-Test.md`
- `docs/Clean-Machine-Install-Proof.md`, `docs/Known-Limitations.md`
