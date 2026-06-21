# Load Test — IIS HTTPS/Windows-auth Pilot (local high-volume simulation)

**Date:** 2026-06-21 · `scripts/load/run-iis-smoke-load.ps1` (+ search wrapper) against `https://localhost:8443`
(HTTPS + Windows/Negotiate auth, self-signed pilot cert, SQL Express backend).

> **This is a LOCAL workstation simulation, NOT a production high-volume claim.** Real high-volume readiness
> requires a server-class host, a real load generator, and production infrastructure.

| Suite | Concurrency | Duration | Requests | HTTP 200 | HTTP 500 | RPS | p50 | p95 | p99 | max | App pool |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| smoke (mixed pages) | 25 | 60 s | **19,300** | 19,300 | **0** | 321.7 | 54.8 ms | 81.7 ms | 96.8 ms | 216 ms | Started→Started |
| search (DB-backed) | 20 | 60 s | **10,240** | 10,240 | **0** | 170.7 | 74.5 ms | 116 ms | 128.9 ms | 190 ms | Started→Started |
| **Total** | — | — | **29,540** | **29,540** | **0** | — | — | — | — | — | healthy |

## What this proves

- **~29,540 authenticated HTTPS requests** through IIS + ANCM against SQL Express with **zero HTTP 500s** and
  **zero connection errors**, with the **least-privilege** app-pool SQL login.
- **Sub-130 ms p99** for DB-backed Base Knowledge search under 20 concurrent clients; sub-100 ms p99 for mixed
  pages under 25 concurrent.
- The **app pool stayed `Started`** (no crash/recycle) across both runs — stable under sustained concurrency.
- Each request completed a **Windows/Negotiate handshake over TLS** — the load is authenticated, not anonymous.

## Honest scope / limitations

- **Workstation simulation only** — a single Windows 11 box, self-signed TLS, a PowerShell/.NET `HttpClient`
  load loop (not a calibrated load generator like k6/wrk). Not a production high-volume proof.
- A `run-iis-sustained-load.ps1` (5-minute) wrapper exists for a longer soak; the 60-second suites are the
  committed evidence here.
- No memory/CPU instrumentation under load beyond the app-pool state check — see
  `PERFORMANCE_RESOURCE_PROFILE.md`.
