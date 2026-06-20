# Industrial Ship-Readiness Audit

Honest audit of the 30 industrial-shipping areas. Columns: **Impl** (in code/scripts), **Test** (automated
test), **Demo** (run live with captured output), **Doc** (documented). Confidence is conservative. "Proof for
100%" states the exact evidence that would close the gap. Reviewed 2026-06-21 at R2-ACC-INDUSTRIAL-SHIP.

Legend: ✅ yes · ◑ partial · ❌ no.

| # | Area | Impl | Test | Demo | Doc | Confidence | Gap → Proof for 100% |
|---|---|---|---|---|---|---|---|
| 1 | Installation | ◑ | ◑ | ◑ | ✅ | Medium | release/install scripts exist + dry-run; **proof**: full operator install on a clean Windows host captured |
| 2 | Database creation | ✅ | ◑ | ✅ (LocalDB) | ✅ | High (LocalDB) | `database/create-*` run live on LocalDB; **proof**: run on Express + full SQL Server |
| 3 | Knowledge-base seed/install | ✅ | ✅ | ✅ | ✅ | High | 390 items verified live; `KnowledgePackTests`; idempotent install proven |
| 4 | Backup/restore | ✅ | ◑ | ✅ | ✅ | High | backup 69.5 MB + RESTORE VERIFYONLY live; **proof**: full restore-to-verify drill on a server edition |
| 5 | Upgrade/migration path | ✅ | ✅ | ✅ | ✅ | High | additive EF migrations; `dotnet ef`; auto-migrate on startup; rollback runbook |
| 6 | Security / RBAC / audit | ✅ | ✅ | ✅ | ✅ | High | R2-P0B Windows auth + RBAC + project access + append-only audit + IDOR test |
| 7 | Deployment scripts | ✅ | ✅ | ◑ | ✅ | Medium | compose + 5 scripts validated; **proof**: a real staged rollout |
| 8 | Windows Server / IIS | ◑ | ❌ | ❌ | ✅ | Low-Med | `windows-deploy.ps1` dry-run + guide; **proof**: actual IIS site stood up on a server |
| 9 | Docker | ◑ | ◑ | ❌ | ✅ | Low | compose(cpu/gpu)+Dockerfile, YAML validated; **Docker not installed here** → **proof**: `docker compose up` on a Docker host |
| 10 | SQL Express readiness | ✅ | ❌ | ❌ | ✅ | Medium | script + appsettings exist; **proof**: run against a real Express instance |
| 11 | Full MSSQL readiness | ✅ | ❌ | ❌ | ✅ | Medium | script + appsettings exist; **proof**: run against a real server |
| 12 | App health checks | ✅ | ✅ | ✅ | ✅ | High | `health-check.ps1`, `IServiceHealthCache`, UI smoke 10 pages 200 |
| 13 | Logging / diagnostics | ✅ | ◑ | ✅ | ✅ | Medium-High | structured logging + `RequestTimingMiddleware`; **proof**: diagnostics bundle export + supportability dashboard |
| 14 | Admin guide | ✅ | n/a | n/a | ✅ | Medium | `docs/Admin-Guide.md` |
| 15 | User guide | ✅ | n/a | n/a | ✅ | Medium | `docs/User-Guide.md` |
| 16 | Demo guide | ✅ | n/a | ✅ | ✅ | High | `docs/POC-Demo-Script.md` + UI smoke |
| 17 | Support / troubleshooting | ✅ | n/a | n/a | ✅ | Medium | `docs/Support-Runbook.md`, `docs/Troubleshooting-Guide.md` |
| 18 | Commercial pilot readiness | ◑ | n/a | n/a | ✅ | Medium | `docs/Commercial-Pilot-Package.md`; **proof**: a signed pilot |
| 19 | License / edition strategy | ◑ | n/a | n/a | ✅ | Low-Med | `docs/Edition-and-Licensing-Strategy.md`; no enforcement yet |
| 20 | ERP/CRM capability | ✅ | ✅ | ✅ | ✅ | Medium-High | `erp-crm-industrial` fixture answered by the bridge (benchmark PoV) |
| 21 | Core-banking integration | ✅ | ✅ | ✅ | ✅ | Medium-High | `core-banking-integration` fixture + middleware; bridge PoV |
| 22 | SMTP capability | ◑ | ◑ | ❌ | ✅ | Low-Med | `deploy/smtp` templates + health/test scripts; **proof**: a real test-send to a dev sink |
| 23 | SFTP capability | ◑ | ◑ | ❌ | ✅ | Low-Med | `deploy/integrations/sftp` templates + scripts; **proof**: a real upload/download to a test SFTP |
| 24 | SDK integration | ◑ | ✅ | ✅ | ✅ | Medium | sample adapter interface + mock adapter + test (mockable boundary) |
| 25 | LLM / local AI runtime | ✅ | ◑ | ✅ | ✅ | Medium | Ollama check + tiny live inference (`deepseek-r1:14b`); optional, not authoritative |
| 26 | CNN / OCR / doc intelligence | ◑ | ✅ | ◑ | ✅ | Low-Med | PDF analyzer + cheque risk skeleton tested; **no real CV model** → proof: trained+validated model with metrics |
| 27 | Autonomous engineering | ◑ | ✅ | ✅ | ✅ | Low-Med | command policy + dry-run planner + **controlled local executor** (allowlist only, no commit) tested |
| 28 | Blockers to paid pilot | n/a | n/a | n/a | ✅ | — | see `docs/Known-Limitations.md` / certificate |
| 29 | Blockers to commercial release | n/a | n/a | n/a | ✅ | — | SSO, scale testing, packaging/licensing enforcement, external audit |
| 30 | Blockers to full autonomous eng | n/a | n/a | n/a | ✅ | — | real execution loop with proven rollback on real repos + sign-off |

## Summary

**Industrially strong (ship-grade today):** knowledge-base install + verification, security/RBAC/audit,
migrations/upgrade, backup/restore (verified), health checks, repository understanding incl. the C#↔SQL +
Python↔SQL bridge, the benchmark harness, and the ERP/CRM + core-banking capability fixtures.

**Pilot-grade with operator gating:** deployment scripts, Windows/IIS and Docker (validated but not executed
here — Docker is not installed on this host), SQL Express / full SQL Server (scripts ready, not run here),
SMTP/SFTP (templates + scripts, not connected to live relays here), LLM runtime (Ollama live but optional).

**Prototype/design (not production):** real CV/OCR engines, autonomous *execution at scale*, commercial
licensing enforcement. These are documented, scored low, and never claimed as complete.

The biggest honest gaps are environmental (no Docker/Express/full-SQL/SMTP/SFTP endpoints on this workstation)
and capability-depth (no trained CV model, no production deployment). Each is listed above with the exact proof
required to close it.
