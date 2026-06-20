# Release Checklist

A pre-release gate for LocalAIFactory. Every item must pass before a release is considered ready.
This is a **gate**, not a suggestion: a single failing item blocks the release. The final state of a
release candidate is **pushed but NOT merged** — merge is a separate, human decision.

Run from the repository root. Capture the output of each command; a green build is necessary but
never sufficient (this project's failures are runtime hangs, not compile errors).

---

## 1. Build

- [ ] `dotnet restore` completes.
- [ ] `dotnet build LocalAIFactory.sln -c Release` succeeds with **no errors**.
- [ ] No new warnings introduced that mask real problems.

## 2. Tests — full suite

- [ ] `dotnet test` passes with **all 207 tests green** (zero failures, zero skips that hide gaps).
- [ ] Security suite included: dev-auth guard, deny-by-default project access, IDOR regression,
      no-secrets audit (`tests/LocalAIFactory.Tests/SecurityTests.cs`).

## 3. Validation benchmark

- [ ] The validation benchmark / harness reports **PASS** (e.g. the eShopOnAbp Gold onboarding and
      its proofs).
- [ ] No benchmark regressions versus the prior release.

## 4. UI smoke — core pages must load fast, none may hang

Start the app (`dotnet run --project src/LocalAIFactory.Web`), then in a separate shell confirm each
core page returns quickly (well under one second on SQL Express):

- [ ] `GET /` (Home)
- [ ] `GET /Projects`
- [ ] `GET /Knowledge`
- [ ] `GET /Models`

A hung endpoint logs `-> {path} started` with **no** matching `<- {path} {status}` line
(`RequestTimingMiddleware`). Investigate any "started"-without-"completed".

## 5. POC / verify scripts

- [ ] `scripts/verify.ps1` (verify-poc) passes.
- [ ] Any other gating verify scripts in `scripts/` pass.

## 6. Database & knowledge-base verify

- [ ] App **migrates and seeds** cleanly on startup against a reachable SQL Server.
- [ ] DB verify: expected tables present, no pending/failed migration, `ModelSnapshot` consistent
      with migrations.
- [ ] KB verify: curated knowledge intact; permanence rules respected (no overwrite of curated
      items); provenance present for installed Knowledge Packs.

## 7. Backup / restore drill

- [ ] Take a database backup and **restore it to a clean instance**; confirm the app starts and core
      pages load against the restored DB.
- [ ] Confirm `keys/` (Data Protection key ring) is backed up **securely and out-of-repo**, and that
      restored secrets decrypt with the restored ring.

## 8. No-secrets audit

- [ ] Working tree contains **no** committed secrets: no API keys, passwords, tokens, or
      credential-bearing connection strings.
- [ ] Only `appsettings.*.example.json` (Trusted Connection / placeholders) are committed.
- [ ] `keys/`, `.env`, and local overrides are git-ignored and **not staged**.
- [ ] The automated no-secrets test passes (see §2).

## 9. No-large-artifacts audit

- [ ] No build outputs, packaged ZIPs, model files, vector data, or other large binaries staged or
      committed.
- [ ] `git status` is clean of generated/ignored artifacts; repo size has not ballooned.

## 10. Documentation current

- [ ] `MASTER_VISION.md` and `CLAUDE.md` still accurate for this release.
- [ ] Security/governance docs reflect reality:
      `Security-Model.md`, `RBAC-Matrix.md`, `Audit-Model.md`, `Secrets-Handling.md`,
      `Compliance-Disclaimers.md`, `Known-Limitations.md`.
- [ ] `Known-Limitations.md` updated — any gap closed this release is removed with its proof; no new
      gap is left undocumented.
- [ ] Phase release notes / changelog updated.

## 11. Scorecard

- [ ] The release scorecard is updated with this release's build, test count, benchmark result, and
      smoke results.

## 12. Branch hygiene — push, do NOT merge

- [ ] All work committed on the release branch (not `main`).
- [ ] Branch **pushed** to the remote.
- [ ] **NOT merged** to `main` — merge is a separate, explicit human decision after review.
- [ ] No destructive DB migration included without explicit prior approval (additive only).

---

## Sign-off

A release is ready only when **every** box above is checked. Record who ran the checklist, the
commit hash, the test count (expect 207+), and the benchmark result. If any item fails, stop and fix
the root cause — do not waive a gate to ship.
