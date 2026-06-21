# Mode A — IIS HTTP / Auth / Support Healthcheck (Phase 6)

**Date:** 2026-06-21 · **Endpoint:** `http://localhost:8095` (served **through IIS** — `Server: Microsoft-IIS/10.0`)

`scripts/deployment-drill/11-iis-mode-a-healthcheck.ps1` — read-only; HTTP + SQL + IIS state. **PASS.**

## HTTP through IIS (all 200, 0 HTTP 500s)

| Route | Status | Time |
|---|---:|---:|
| `/` | 200 | 52 ms |
| `/Support` | 200 | 46 ms |
| `/Readiness` | 200 | 41 ms |
| `/BaseKnowledge` | 200 | 70 ms |
| `/Coverage` | 200 | 33 ms |
| `/Graph` | 200 | 55 ms |
| `/Benchmarks` | 200 | 18 ms |

| Search (DB-backed, through IIS) | Matches |
|---|---:|
| `/BaseKnowledge?q=OCR` | **57** |
| `/BaseKnowledge?q=Mauritius` | **90** |
| `/BaseKnowledge?q=market` | **23** |

DB (SQL Express `LocalAIFactory_IISProof`): packs **4**, items **438**, migrations **14**.
IIS: site `LocalAIFactoryPilot` = **Started**, app pool `LocalAIFactoryPilotPool` = **Started**.
**HTTP 500 count: 0.**

## Windows / Negotiate authentication (validated at the IIS layer)

| Step | Result |
|---|---|
| `IIS-WindowsAuthentication` feature | **Enabled** (DISM: State = Enabled) |
| Site with Windows Auth on + Anonymous off, probe `/` **without** credentials | **HTTP 401** — IIS enforces the Negotiate challenge ✅ |
| Probe `/` **with** default Windows credentials | Could not complete via `Invoke-WebRequest` — PowerShell refuses to send default credentials over **plain HTTP** ("cannot protect plain text secrets"). A client over **HTTPS** (or a browser) completes the handshake. **Tooling limitation, not an auth failure.** |
| Revert to Anonymous | **HTTP 200** restored (clean dev-auth proof intact) |

**Honest summary:** IIS Windows/Negotiate authentication is **installed and demonstrably enforcing**
(401 challenge when anonymous is disabled). The full authenticated round-trip + the app's RBAC/deny-by-default
under Negotiate is the **production posture** (needs HTTPS for credential transport and a seeded Windows
bootstrap admin) and was **not** completed in this pilot. The committed pilot runs `Development` dev-auth for
full page reachability — the same auth-posture limitation as Mode C, now behind real IIS.

## Reproduce

```powershell
pwsh scripts\deployment-drill\11-iis-mode-a-healthcheck.ps1 -AppUrl http://localhost:8095 -Database LocalAIFactory_IISProof
```
