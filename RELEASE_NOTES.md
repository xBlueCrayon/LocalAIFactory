# LocalAIFactory — Release Notes

## v1.0.0-rc — Customer Handover / Release Candidate (2026-06-21)

**Branch:** `ke-008-code-symbols` · **Status:** controlled paid-pilot ready · **NOT** commercial GA.

LocalAIFactory is a private, local-first, MSSQL-authoritative AI software-engineering platform for a banking
middleware estate. This release candidate is a customer-handover deliverable: a published app, the included
knowledge base, database setup, validation/verification scripts, manuals, and an honest readiness statement.

### Highlights

- **Included knowledge base (4 packs, 438 items)** — `professional-base-v1` (390) + `financial-institution-
  operations-v1`, `kyc-aml-transaction-approval-v1`, `market-intelligence-forecasting-v1` (16 each). All packs
  install automatically on startup (idempotent; propose-never-overwrite). Live-verified: **4 packs / 438 items**.
- **Structural understanding** — C#/T-SQL/Python symbols + graph + bidirectional impact; **C#↔SQL bridge**.
- **Benchmark fixtures (Gold)** — ERP/CRM 6/6, core-banking 6/6, **KYC/AML→transaction-approval 7/7**.
- **/Support** read-only operations dashboard; **edition/license** skeleton (demo-safe); **safe local fix loop**.
- **Chat-import learning** — deterministic chat→knowledge *proposals* (never auto-approved).
- **Release tooling** — build/package/verify/manifest/clean-install-simulation/customer-acceptance scripts;
  diagnostics, security audit, knowledge install/verify/catalog scripts; **real product screenshots**.

### Validation (this release)

Build **0 errors** · Tests **235/235** · Benchmark smoke + standard **PASS** · UI smoke **PASS** (11 pages) ·
verify-poc **PASS** · KB **VERIFIED** (4 packs / 438 items) · full-install verify **PASS** · security audit
**0 HIGH** · release package **verified** · clean-install simulation **PASS** · `dotnet publish` **~151 files**.

### Honest limitations (not in this release)

No executed production deployment (IIS/Docker/Express/full-SQL); Docker not installed on the build host; no
enterprise SSO/IdP; no trained OCR/CV model (deterministic prototypes only); no cross-repository estate model;
fix loop proven on a synthetic workspace, not a real repo; no commercial licensing enforcement; **no external
security/penetration review**. See `docs/Known-Limitations.md` and `docs/Gap-Closure-Roadmap-To-100.md`.

### No claims

No regulatory/compliance certification, no investment advice, no fraud-detection certainty, no production
certification. Knowledge content is original professional summaries; no proprietary/regulatory text is vendored.

### Install (quick)

```powershell
dotnet build LocalAIFactory.sln -c Release
database/setup-full-local-demo.ps1     # create LocalDB + seed all packs + verify
dotnet run --project src/LocalAIFactory.Web
```

See `docs/FINAL_CUSTOMER_HANDOVER_INDEX.md` and `docs/FINAL_LOCAL_DEPLOYMENT_GUIDE.md`.
