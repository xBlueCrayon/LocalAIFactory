# LAF Self-Generated ERP — Human Affirmation Pack

**Date:** 2026-06-21 · **For:** the repository owner to review and (later) affirm. **Not signed.**

## Product

- **Name:** LAF Enterprise ERP V2 (clean-room, **generated** by `tools/LocalAIFactory.Generator`)
- **URL when running:** http://localhost:5081
- **DB:** SQLite (zero external services) or SQL Server via `ConnectionStrings:Default`.

## How to run it yourself

```powershell
# 1. (Re)generate from the requirement + governed local-LLM proposal
dotnet run --project tools/LocalAIFactory.Generator -- --target generated-products/LAF-EnterpriseERP-LAFGenerated --product-name "LAF Enterprise ERP V2" --prefer-local-llm

# 2. Build, test, run
dotnet build generated-products/LAF-EnterpriseERP-LAFGenerated/LAF-EnterpriseERP-LAFGenerated.slnx -c Release
dotnet test  generated-products/LAF-EnterpriseERP-LAFGenerated/tests/LafErp.Tests/LafErp.Tests.csproj -c Release
dotnet run   --project generated-products/LAF-EnterpriseERP-LAFGenerated/src/LafErp.Web --no-launch-profile --urls http://localhost:5081

# 3. Browser proof (login + navigation + screenshots)
cd generated-products/LAF-EnterpriseERP-LAFGenerated/playwright; npx playwright test
```

**Login:** open `/Home/Login`, sign in as `admin` / `System Manager` (or `alice` / `Sales User`, `bob` /
`Accounts Manager` to see maker/checker).

## What was demonstrated

| Item | Result |
|---|---|
| LocalAIFactory generated V2 | **Yes** — 70 product files, 100% file autonomy |
| Local model used | **Yes** — `qwen2.5-coder:14b`, governed + collision-guarded |
| App runs | **Yes** — 26/26 pages+APIs 200, 0 HTTP 500s |
| Webpage opened in a browser | **Yes** — Playwright Chromium |
| Login tested | **Yes** — dev-auth login + redirect verified |
| Real-life scenario passed | **Yes** — buy→sell→maker/checker→pay→balanced GL |
| Tests | **82 .NET + 13 Playwright, all pass** |
| Screenshots | 11 PNGs in `playwright/screenshots/` (git-ignored) |
| Parity vs ERPNext | ~37% (honest) |
| Production-grade | **No** — PILOT-grade (~35%) |

## What you should review

1. The generator: `tools/LocalAIFactory.Generator/` (templates + `Program.cs`).
2. The attribution: `benchmarks/results/laf-erp-v2-generation-attribution.json` (autonomy 100%).
3. The local-LLM evidence: `generated-products/LAF-EnterpriseERP-LAFGenerated/generation-evidence/`.
4. The generated product source + tests.
5. The comparison + production-grade assessment.

## Go / No-Go checklist (you fill in)

- [ ] Generator + templates reviewed
- [ ] Attribution autonomy (100%) accepted as honest
- [ ] Local-LLM usage + collision guard accepted
- [ ] Ran the app and logged in
- [ ] Reviewed screenshots
- [ ] Agree V2's advance is **autonomy**, parity is ~flat (~37%)
- [ ] Accept it is **PILOT-grade, not production-grade**

## Risk acceptance (you fill in)

| Risk | Level | Accepted? |
|---|---|---|
| Dev-auth only (no real auth) | High | ☐ |
| Partial module coverage (~37% parity) | Medium | ☐ |
| No EF migrations / production ops | Medium | ☐ |
| Generator engine is template-based (LLM drives only catalog) | Medium | ☐ |

## Decision

- [ ] **GO** (accept as a generation-autonomy + pilot milestone) ☐ **NO-GO** ☐ **DEFER**

| Name | Role | Date | Signature |
|---|---|---|---|
| ____________ | ____________ | ____________ | ____________ |

*Status: NOT SIGNED — awaiting human affirmation.*
