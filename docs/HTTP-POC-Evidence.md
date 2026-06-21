# HTTP POC Evidence

**Phase:** R2-ACC-POC-COMPLETE · **Date:** 2026-06-21 · **App:** `http://localhost:60398`
(ASP.NET Core, Release, `ASPNETCORE_ENVIRONMENT=Development` for dev-auth=admin) against the configured
**`(localdb)\MSSQLLocalDB / LocalAIFactory`** database.

The app was started locally, probed over HTTP, and stopped. **App became ready in ~2 s.** Every route
below was captured live; the `matches` column counts `/BaseKnowledge/Details/` links in the response
(i.e. real knowledge hits). **No route returned HTTP 500** (HTTP_500_COUNT = **0**).

## Core pages

| Route | Status | Time | Note |
|---|---:|---:|---|
| `/` (Home/Dashboard) | **200** | 16 ms | contains `LocalAIFactory` |
| `/BaseKnowledge` | **200** | 89 ms | 400 item links listed |
| `/BaseKnowledge/Details/2113` | **200** | 40 ms | a baseline knowledge item detail page |
| `/Coverage` | **200** | 32 ms | |
| `/Graph` | **200** | 61 ms | |
| `/Projects` | **200** | 15 ms | |
| `/Knowledge` | **200** | 55 ms | |
| `/Models` | **200** | 20 ms | |
| `/Readiness` | **200** | 18 ms | |
| `/Support` | **200** | 46 ms | read-only health |
| `/Audit` (admin) | **200** | 24 ms | reachable under dev-auth admin |
| `/Users` (admin) | **200** | 33 ms | reachable under dev-auth admin |

## Base Knowledge search (`/BaseKnowledge?q=<term>`)

| Query | Status | Time | Matches |
|---|---:|---:|---:|
| OCR | **200** | 38 ms | **57** |
| Mauritius banking | **200** | 27 ms | **52** |
| financial market prediction | **200** | 29 ms | **3** |
| direct debit | **200** | 19 ms | **7** |
| insurance | **200** | 20 ms | **20** |
| leasing | **200** | 18 ms | **13** |
| PDF summarizer | **200** | 20 ms | **1** |
| Qdrant | **200** | 19 ms | **9** |
| VB6 | **200** | 20 ms | **1** |

## What this proves

1. Home/dashboard returns **200** ✅
2. Base Knowledge page returns **200** ✅
3. Base Knowledge search returns **200** for all required terms (OCR, Mauritius banking, financial
   market prediction, direct debit, insurance, leasing, PDF summarizer, Qdrant, VB6) with real,
   non-trivial match counts ✅
4. A baseline item details page returns **200** ✅
5. Coverage and Graph pages return **200** ✅
6. Admin/audit routes return **200** under dev-auth admin (the test/dev identity) ✅
7. **No core route returned 500** ✅
8. Every page responded in **well under a second** (16–89 ms) on LocalDB — consistent with the
   project's "core pages complete in well under one second" requirement.

## Honesty notes

- This is an **HTTP-level** proof captured by `Invoke-WebRequest` (status, timing, and response-text
  match counts). It runs the real app against the real LocalDB, but it is not full browser rendering.
  Browser-level UI is covered separately by the committed Playwright screenshot capture (11 real
  screenshots in `docs/screenshots/`) and the `scripts/poc/ui-smoke-test.ps1` gate.
- Admin routes return 200 because the app ran in Development with dev-auth as an admin identity
  (`DEV\developer`). In production (Windows/Negotiate auth) these routes are protected by RBAC and
  deny-by-default; that enforcement is proven by the security/IDOR unit tests, not by this anonymous
  HTTP probe.
- Reproduce with: `pwsh scripts/poc/ui-smoke-test.ps1` (starts the app, asserts no 500s, checks
  searches), which returns a non-zero exit code on any failure.
