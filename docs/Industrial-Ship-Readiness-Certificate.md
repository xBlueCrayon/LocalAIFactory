# Industrial Ship-Readiness Certificate

**Product:** LocalAIFactory · **Branch:** `ke-008-code-symbols` · **As of:** R2-ACC-INDUSTRIAL-SHIP, 2026-06-21
**Scope:** evidence-based readiness for a *paid industrial pilot*. This is an honest certificate — every claim
below is backed by a reproducible command, test, benchmark, or live capture. It asserts **no** vendor
certification, compliance, or production guarantee.

## Evidence summary (all reproduced this phase)

| Category | Evidence | Result |
|---|---|---|
| Build | `dotnet build LocalAIFactory.sln -c Release` | **0 errors** |
| Tests | `dotnet test` | **207 / 207 pass** |
| Benchmark (full) | `--inmemory` | **PASS** (8 items) |
| Benchmark (smoke) | `--suite smoke` | **PASS** (2 bridge fixtures) |
| Benchmark (standard) | `--suite standard` | **PASS** (7 items) |
| verify-poc | `scripts/poc/verify-poc.ps1` | **PASS** |
| UI smoke | `scripts/poc/ui-smoke-test.ps1` | **PASS** (10 pages 200, no 500s) |
| Database creation | `database/create-localdb.ps1` | LocalDB live (create-if-absent) |
| Knowledge base | `database/verify-knowledge-base.ps1` | **VERIFIED** (390 items, no dup Uids, curated, provenance) |
| Backup / restore | `database/backup-database.ps1` + `restore-verify-database.ps1` | backup **69.5 MB**, **VERIFY OK** |
| Release package | `dotnet publish` | **142 files / 45 MB**, knowledge-packs + scorecard bundled |
| Security / RBAC / audit | `SecurityTests` + AccessDenied 403 + IDOR test | enforced server-side, audited |
| AI runtime (optional) | `scripts/ai/test-installed-model.ps1` | qwen2.5-coder:14b "OK" in **10.2 s** (RTX 5070 Ti) |
| GPU | `nvidia-smi` | RTX 5070 Ti, 16 GB |
| ERP/CRM capability | `erp-crm-industrial` fixture | **Gold 6/6** (bridge answers ERP impact questions) |
| Core-banking capability | `core-banking-integration` fixture | **Gold 6/6** (posting/mandate/claim/settlement) |
| SDK integration | `VendorSdkAdapterTests` | **4/4** (mock + retry + error-map + availability gate) |
| Autonomous (controlled) | `AutonomousExecutorTests` | **4/4** (allowlist-only, halts on failure, never promotes) |
| Repo hygiene | tracking audit | no bin/obj/cache/db/model/keys; no file > 5 MB |
| Secrets | audit | no secrets committed; `.env`/`keys/` ignored; example configs only |

## Capability statements (honest)

- **Real & shipped:** deterministic C#/T-SQL/Python understanding; C#↔SQL and Python↔SQL bridges with
  both-direction impact + confidence + evidence; 390-item governed knowledge base with source registry;
  Windows-auth RBAC + append-only audit; ingestion robustness; benchmark harness with tiers; DB
  create/seed/verify + backup/restore; release publish/packaging; controlled autonomous executor.
- **Templates / design (operator-gated or prototype):** Docker/IIS deployment (validated, not executed on this
  host — Docker absent); SQL Express / full SQL Server (scripts ready, not run here); SMTP/SFTP (templates +
  health scripts, no live relay/endpoint here); LLM enhancement + CNN/OCR (architecture + deterministic
  prototypes, **no trained CV model**); commercial licensing (strategy, **no enforcement**).
- **Not present:** enterprise SSO/IdP; cross-repository estate model; production deployment; real OCR/CV engine;
  autonomous real fix→test→rollback loop; penetration test / external audit; commercial licensing enforcement.

## Readiness scores (authoritative: `readiness-scorecard.json`, mean ≈ 55%)

Technical POC **85** · Controlled Pilot **70** · Security **75** · Audit **70** · Data/Knowledge Governance **80**
· Repository Understanding **80** · Benchmark **80** · Deployment **65** · Banking/Finance **65** · ERP/Infra
Advisory **65** · Vendor-Style Design **55** · UX/Demo **60** · Supportability **50** · Autonomous **45** ·
Scalability **40** · Document/OCR **35** · Cross-System/Estate **45** · Legacy **30** · Enterprise Product **30**
· Commercial Product **25** · Packaging/Licensing **15** · Commercial Pilot-readiness: see decision below.
**No area is at 100.** Each carries an explicit `proofRequiredFor100`.

## Risk & confidence

- **Technical risk:** LOW for the proven core (build/test/benchmark/DB/bridge/security all green and reproducible).
- **Deployment risk:** MEDIUM — scripts are validated and LocalDB-proven, but no production/IIS/Docker/Express
  rollout was executed on this host.
- **Domain/compliance risk:** MEDIUM-HIGH if misrepresented — banking/finance/OCR are advisory/prototype and
  **must not** be sold as compliant, certified, or fraud-proof. Disclaimers are documented.
- **Overall confidence (1–10): 7/10** for a *controlled, technically-grounded paid pilot*; lower (≈4/10) for an
  unattended production or commercial-GA claim.

## Pilot-readiness decision

**YES — sellable as a controlled, paid industrial PILOT**, on these conditions: (1) the pilot is scoped to the
proven core (repository + C#/SQL/Python understanding + impact, governed knowledge base, security/audit, the
ERP/CRM and core-banking *analysis* capability), with deployment operator-assisted; (2) OCR/PDF, banking
compliance, autonomy-at-scale, and SSO are presented as roadmap, not delivered; (3) the buyer can reproduce the
evidence above with the committed scripts. **NOT yet ready** for unattended production deployment or commercial
general availability — see `Known-Limitations.md` and the per-area `proofRequiredFor100`.

> Signed off by the engineering process, not an external auditor. Independent technical + security review is the
> single highest-value step to raise Technical POC to 90+ and de-risk a first paid pilot.
