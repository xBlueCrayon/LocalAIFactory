# LocalAIFactory

**A private, local-first, MSSQL-authoritative AI software-engineering platform — with a data-driven
product generator and a governed knowledge engine.** It imports legacy projects (C#/.NET, T-SQL, Python),
understands them *structurally*, accumulates an **approval-gated knowledge base** injected first into every
prompt context, and can **generate clean-room sample applications** (ERP, screen-share) from that knowledge.
Everything runs locally and keeps working with **only SQL Server present** — no GPU, no internet, no Ollama,
no Qdrant required (the local LLM is optional and used only as a reviewer/planner).

> `MASTER_VISION.md` is the authoritative vision; `CLAUDE.md` is the contributor contract.
> **Authoritative current status:** [`docs/reports/CURRENT_STATUS.md`](docs/reports/CURRENT_STATUS.md).
> This is **not** a general chatbot and makes **no** commercial-GA, vendor-certification, ERPNext-parity,
> or internet-ready claims.

---

## Current status (verified, commit `96fbbc4`)

| Area | Status |
|---|---|
| **Factory** build / tests | 0 errors · **240 / 240** |
| Production-readiness gate V3 | `NEAR_GA_READY_WITH_EXTERNAL_PROOF_MODEL` (near-GA **local** proof, not commercial GA) |
| Security audit | PASS (no HIGH findings) |
| **Knowledge engine** | **20 packs / 852 items**, validated, no UID collisions |
| **ERP V5** (generated) | `ERP_PILOT_READY` · 134 .NET + 14 Playwright tests · ~48% ERPNext parity · ~57% production-grade |
| **ScreenStream** (generated) | `LAN_READY` · 12 .NET + 4 Playwright tests · real local server EXE |
| Release | `v1.0.0-rc` **draft + prerelease** (unpublished); no `v1.0` tag |

## What it is

.NET 10 / ASP.NET Core MVC. MSSQL + EF Core is the primary memory store; Qdrant (vectors) and Ollama (local
inference) are optional and degrade gracefully when absent. Three pillars:

1. **Code understanding** — deterministic C# (Roslyn), T-SQL (ScriptDom) and Python symbol extraction into a
   structural graph with C#↔SQL bridging and bidirectional impact analysis.
2. **Knowledge engine** — governed `KnowledgePack`s (propose-never-overwrite), 20 packs / 852 items, validated
   and default-installed. See [`docs/knowledge-engine/`](docs/knowledge-engine/).
3. **Product generator** — `tools/LocalAIFactory.Generator`: a data-driven, knowledge + template emitter that
   generates clean-room sample applications at 100% file autonomy, with attribution + knowledge-usage reports.

## Generated products (samples)

Source kept in-repo; build artifacts/EXEs are git-ignored — publish locally to run. See
[`generated-products/README.md`](generated-products/README.md).

- **LAF Enterprise ERP V5** — clean-room .NET/MSSQL ERP (double-entry GL + P&L + Balance Sheet, stock ledger,
  maker/checker + audit + RBAC, generated create UI forms, REST APIs). **ERP_PILOT_READY, not ERPNext-grade.**
- **LAF ScreenStream Assist** — consent-based Windows screen-share with a real server EXE + client-EXE
  generator. **LAN_READY, not internet/production-grade.**

ERP V1–V4 remain as **historical** generation artifacts (version progression evidence).

## Quick start

```powershell
git clone --branch ke-008-code-symbols https://github.com/xBlueCrayon/LocalAIFactory.git
cd LocalAIFactory
dotnet restore
dotnet build LocalAIFactory.sln -c Release          # 0 errors
dotnet test                                          # 240/240
.\scripts\knowledge\verify-all-knowledge-packs.ps1   # 20 packs / 852 items, PASS
.\scripts\production\verify-production-readiness-v3.ps1
```

Run the factory web app (MSSQL-only mode is fine):

```powershell
dotnet run --project src/LocalAIFactory.Web
```

### Run the generated products locally

```powershell
# ERP V5 (publishes to C:\LAFEnterpriseERP-V5, runs on SQLite; set ConnectionStrings:Default for MSSQL)
pwsh scripts/erp-v5/publish-local-production.ps1
C:\LAFEnterpriseERP-V5\LafErp.Web.exe        # then open http://localhost:5000

# ScreenStream (publishes to C:\LAFScreenStreamAssist)
pwsh generated-products/LAF-ScreenStreamAssist/scripts/publish-local-test-folder.ps1 -OutputRoot C:\LAFScreenStreamAssist
C:\LAFScreenStreamAssist\Server\Start-Server.bat   # dashboard at http://localhost:5090
```

### Regenerate a product (proves the generator)

```powershell
dotnet run --project tools/LocalAIFactory.Generator -- `
  --module-spec tools/LocalAIFactory.Generator/specs/erpnext-production-suite.json `
  --target generated-products/LAF-EnterpriseERP-V5-New --product-name "LAF Enterprise ERP V5"
```

## Repository structure

```
src/        LocalAIFactory factory (Core/Data/Rag/Agent/Ingestion/Workspaces/Terminal/Web)
tests/      LocalAIFactory.Tests (240)
tools/      LocalAIFactory.Generator (data-driven product generator) + benchmark
scripts/    knowledge/ production/ generator/ erp-v5/ screenstream/ diagnostics/ poc/ security/
knowledge-packs/        20 default knowledge packs (852 items)
generated-products/     ERP V5 + ScreenStream (current) + V1-V4 (historical)
docs/       architecture/setup/operations/knowledge-engine/generated-products/reports
```

## What is production-ready vs pilot-ready

- **Factory:** near-GA **local** proof; commercial GA needs external proofs (real Entra/OIDC, CA TLS,
  independent pen-test, signed pilot) — modelled + owned, **not faked**.
- **ERP V5:** high **pilot**; not ERPNext-grade / not production-grade. Local gaps: EF migrations, edit/delete
  UI, backup/restore, module depth. External gaps: real auth, TLS, security review, customer acceptance.
- **ScreenStream:** **LAN_READY**; internet/production needs TLS/WSS, code-signing, operator network setup.

## Known limitations & release

See [`docs/Known-Limitations.md`](docs/Known-Limitations.md). The draft release `v1.0.0-rc` is **not**
published; there is **no** final `v1.0` tag. **No commercial GA, no ERPNext parity, no internet-ready
ScreenStream, no fake 100%.**

## License & contributing

See `CLAUDE.md` for the operating contract (MSSQL-only must work; Qdrant/Ollama optional; no blocking
external calls on the request path; no destructive DB changes without approval; no secrets in the repo).
Knowledge-pack content and generated code are original/clean-room — no third-party proprietary text or
source is vendored.
