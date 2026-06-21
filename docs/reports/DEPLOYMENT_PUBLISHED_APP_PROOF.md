# Deployment Published-App Proof — Mode C (Phase 5)

**Date:** 2026-06-21 · **Mode:** C — published app + **SQL Server Express**, no IIS

This is the **executed deployment proof** for this phase. It runs the **published application binaries**
(not `dotnet run` from source) from the publish folder, against a **real server database engine**
(SQL Server Express 2022), using a **fresh, isolated deployment database**.

## How it was launched

```
# Env override points the published app at SQL Express (fresh deployment DB):
$env:ConnectionStrings__DefaultConnection =
  "Server=.\SQLEXPRESS;Database=LocalAIFactory_DeploymentProof;Trusted_Connection=True;..."
$env:ASPNETCORE_ENVIRONMENT = "Development"   # dev-auth, so all secured pages are reachable for the probe
dotnet LocalAIFactory.Web.dll --urls http://localhost:8095   # run from ./.tmp-publish
```

- App became ready in **~4 s**; on first run it **migrated (14) + seeded + installed 4 packs / 438 items**
  into SQL Express (see `DEPLOYMENT_DATABASE_PROOF.md`).
- Process PID 30164, listening on port 8095.

## HTTP probe results (all live, against SQL Express)

| Route | Status | Time | Matches |
|---|---:|---:|---:|
| `/` | **200** | 42 ms | — |
| `/Readiness` | **200** | 38 ms | — |
| `/Support` | **200** | 41 ms | — |
| `/BaseKnowledge` | **200** | 68 ms | 400 |
| `/BaseKnowledge?q=OCR` | **200** | 44 ms | **57** |
| `/BaseKnowledge?q=Mauritius` | **200** | 29 ms | **90** |
| `/BaseKnowledge?q=market` | **200** | 27 ms | **23** |
| `/Coverage` | **200** | 30 ms | — |
| `/Graph` | **200** | 30 ms | — |
| `/Benchmarks` | **200** | 25 ms | — |
| `/Projects` | **200** | 13 ms | — |
| `/Models` | **200** | 19 ms | — |
| `/Audit` | **200** | 20 ms | — |

**HTTP 500 count: 0.** Every probed route returned 200 against the SQL Express-backed published app, with
DB-backed knowledge search returning real matches.

> Note on the search parameter: the app's real query parameter is `q` (e.g. `/BaseKnowledge?q=OCR`), used
> above to return real match counts. (`?search=` is not the route's parameter name.)

## What this proves — and what it does NOT

**Proves:**
- The **packaged/published binaries run** as a deployed app (not a dev `dotnet run`).
- The app runs against a **real server SQL engine** (SQL Server Express 2022), not just LocalDB.
- A **fresh deployment database** was created, migrated, and seeded end-to-end by the deployed app.
- All pages and DB-backed knowledge search work over HTTP with **zero 500s**.

**Does NOT prove (honest limits):**
- This is **not** an IIS deployment (IIS is not installed — see `DEPLOYMENT_IIS_EXECUTION_PROOF.md`). The
  app self-hosts on **Kestrel**, not behind IIS/ANCM.
- This is **not** production: it ran with `ASPNETCORE_ENVIRONMENT=Development` so dev-auth made secured
  pages reachable for the probe. A production posture would use **Windows/Negotiate auth** (the app's
  default outside Development), with RBAC/deny-by-default — enforcement proven by the security unit tests,
  not by this anonymous probe.
- This is **not** commercial GA.

**Classification:** Mode C — published-app + SQL-Express **pilot** deployment proof.
