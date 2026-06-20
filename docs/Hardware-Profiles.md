# Hardware Profiles

Recommended host profiles for running LocalAIFactory. The platform is **MSSQL-authoritative and
local-first**: it must run with only SQL Server present (no GPU, no internet, no Ollama, no Qdrant).
Everything beyond the minimal profile buys optional capability, never correctness of the system of
record.

These profiles are **guidance**, not certified minimums. The only host measured this sprint is the
"full-AI" class machine documented in `docs/Resource-and-Performance-Evidence.md`; the minimal and
standard profiles below are reasoned recommendations, not separately benchmarked. Size up if you
import large repositories or run several concurrent users.

---

## 1. Minimal — MSSQL-only, no GPU

The baseline the product guarantees. AI features are off; everything else works.

| Resource | Recommendation |
|---|---|
| CPU | 4 physical cores (x64) |
| RAM | 8 GB |
| Disk | 50 GB free (app, database, imports, backups) — SSD strongly preferred |
| GPU | None |
| SQL Server | SQL Express, LocalDB, or a shared MSSQL instance |
| Network | None required (air-gapped supported) |

**Available:** repository import and deterministic C#/T-SQL/Python understanding, C#↔SQL bridge,
coverage/gap reporting, curated knowledge base with the approval lifecycle, Windows-auth RBAC +
append-only audit, backup/restore.

**Not available:** local AI assistance (chat, embeddings, autonomy). Those paths detect the absent
service and stay disabled — pages still load (CLAUDE.md §3–§4).

---

## 2. Standard — LocalDB/MSSQL + a small local model

Adds optional local AI on a developer-class machine without a discrete GPU, or with a modest one.
Small models run on CPU; expect higher latency than the full-AI profile.

| Resource | Recommendation |
|---|---|
| CPU | 8 cores / 16 threads |
| RAM | 16–32 GB (models compete with the database and app for RAM on CPU inference) |
| Disk | 100 GB+ free SSD (model files are several GB each) |
| GPU | Optional; a small GPU accelerates inference but is not required |
| SQL Server | LocalDB or a local MSSQL instance |
| Inference | Ollama with a small/quantised model |

**Available:** everything in Minimal, plus optional local chat and embeddings (subject to model
quality — AI output is advisory and unverified by default; see `docs/Compliance-Disclaimers.md`).

**Caveat:** CPU-only inference of a 14B-class model is slow. For interactive AI use, prefer the
full-AI profile.

---

## 3. Full-AI — discrete GPU (proven configuration)

The configuration measured this sprint. This is the **only** profile with live evidence behind it
(`docs/Resource-and-Performance-Evidence.md`).

| Resource | Proven value (this sprint) |
|---|---|
| CPU | AMD Ryzen 7 9800X3D — 8 cores / 16 threads |
| RAM | 31.1 GB |
| Disk | C: 285/476 GB free, D: 1404/1863 GB free (SSD) |
| GPU | NVIDIA RTX 5070 Ti, 16 GB VRAM, driver 596.36 |
| SQL Server | LocalDB |
| Inference | Ollama online with `qwen2.5-coder:14b` + `deepseek-r1:14b` |

**Available:** everything, including responsive local AI assistance with 14B-class models, with VRAM
headroom on a 16 GB card.

**VRAM guidance:** a single quantised 14B model fits comfortably in 16 GB. Running two large models
concurrently, or a larger model, may exceed 16 GB and spill to slower paths — size VRAM to the
largest model you intend to keep resident.

---

## 4. Choosing a profile

| If you need… | Choose |
|---|---|
| The system of record only; air-gapped; no AI | Minimal |
| Occasional local AI on existing hardware, latency-tolerant | Standard |
| Interactive local AI on real repositories | Full-AI |

Regardless of profile, the security model (Windows-auth RBAC, append-only audit, Data Protection for
secrets) is identical and does not depend on any external AI service (`docs/Security-Model.md` §7).

---

## 5. Sizing notes and unknowns (honest)

- **Disk grows with imports and backups.** Imported repositories, extracted text, and database
  backups (a backup was 69.5 MB in a prior sprint, for a small dataset) accumulate. Budget headroom.
- **RAM under large-repo import is not yet characterised.** Memory behaviour while importing a very
  large repository under load has not been measured (`docs/Known-Limitations.md`,
  `docs/Load-and-Reliability-Test-Report.md`). Start generous and watch `system-snapshot.ps1`.
- **Multi-user concurrency is unmeasured.** All profiles above assume a small number of users. Sizing
  for many concurrent users requires the load proof that does not yet exist.
- **SQL Express limits.** SQL Express caps database size and RAM. For larger estates, use a full
  MSSQL edition. The validation host used LocalDB; SQL Express was not exercised this sprint.
