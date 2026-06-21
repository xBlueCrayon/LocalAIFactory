# Supportability Dashboard Guide — LocalAIFactory

An **operator's guide** to the read-only `/Support` page: what each tile tells you, the diagnostics
scripts that back the same facts, and when to export a support bundle. This is the page you open
**when something is already wrong** — it is designed to render even when the database is unreachable
and no optional service is up.

For the implementation contract, see `docs/Supportability-Dashboard-Spec.md`.

---

## 1. Open the page

Navigate to `/Support` (e.g. `http://localhost:5000/Support`). It is a read-only operations view for
admins/support engineers. The UI smoke run covers **11 pages including `/Support`** (PASS on the build
host), and `customer-acceptance-check.ps1 -AppUrl <url>` checks that `/Support` returns **200**.

The page obeys the platform's non-blocking rules: health comes from a **cached snapshot**, every DB
read is a guarded lightweight `CountAsync`, and license/edition resolves demo-safe. A failing tile
degrades to "unavailable" rather than breaking the page.

---

## 2. Tiles — how to read them

| Tile | What it tells you | If it looks wrong |
|---|---|---|
| **Build / version** | Environment, machine, framework, OS, assembly + informational version, server UTC time, process uptime | Confirm the deployed version matches the release you expect (`RELEASE_MANIFEST.json` version **1.0.0-rc`) |
| **Edition / license** | Effective edition (Community / ProfessionalPilot / Enterprise), license status + reason, enabled feature count | A missing license resolves to **Community core** — that is expected, not an error |
| **Service health** | Mode (Minimal / Standard / FullAi), chat availability, last-checked UTC, and Qdrant / Ollama / Embeddings labels — all from the cached snapshot | "Health never probed" means the first background probe is pending; optional services being down is normal in MSSQL-only mode |
| **Database facts** | Guarded counts: projects, knowledge items, knowledge packs, code symbols, imported files, chat messages, audit events | A `-1` / "unavailable" count means that single read failed; if the whole tile is gone, the DB is unreachable |
| **Last import / last audit** | Most recent ingestion job and audit event (lightweight columns only) | Empty is normal on a fresh install |
| **Disk** | Content-root drive, free / total GB (best effort) | < 5 GB free raises a low-disk warning |
| **Warnings** | Derived alerts (DB unreachable, license grace/expired/invalid, low disk, health not yet probed) | An **empty** warnings list means the install looks healthy on every dimension the page can cheaply check |

The knowledge-base counts should reconcile with the included base: **4 packs / 438 items** plus any
imported-project items.

---

## 3. Diagnostics scripts behind the same facts

When you need point-in-time captures (or the app is not running), the committed read-only scripts
provide the same signals:

```powershell
pwsh scripts/diagnostics/system-snapshot.ps1     # OS / hardware / runtime
pwsh scripts/diagnostics/gpu-health-check.ps1    # GPU presence / driver
pwsh scripts/diagnostics/ollama-health-check.ps1 # Ollama reachability + models (optional)
pwsh scripts/diagnostics/sql-health-check.ps1    # SQL reachability / version
pwsh scripts/diagnostics/process-monitor.ps1     # process snapshot
pwsh scripts/knowledge/verify-all-knowledge-packs.ps1  # KB validation (4 packs / 438 items)
pwsh scripts/security/security-audit.ps1         # static self-audit (0 HIGH / 2 INFO)
```

For a hung page, also consult `RequestTimingMiddleware` logs: a request that logs `-> {path} started`
with **no** matching `<- {path} ... in {ms} ms` line locates the stall (`docs/07-Troubleshooting.md`).

---

## 4. When to export a support bundle

Export a bundle to send all of the above diagnostics to support in one safe, ~3 KB zip:

```powershell
pwsh scripts/support/export-support-bundle.ps1
# -> SUPPORT-BUNDLE: .../LocalAIFactory-support-bundle.zip (~3 KB)
```

Export when:

- A core page hangs or the app fails to start.
- An optional service shows unhealthy on `/Support` and you want a captured snapshot.
- A deploy / acceptance check fails.
- Any escalation, as a routine attachment.

The bundle contains **no secrets, no database contents, and no source** — only read-only health
snapshots and non-secret environment facts. See `docs/Support-Bundle-Contents.md` for the exact list.

---

## 5. Operator triage flow (quick reference)

1. Open `/Support`. Read the **Warnings** tile first.
2. **DB unreachable?** Core pages depend on MSSQL — check the connection string and SQL instance, then
   `pwsh database/verify-full-install.ps1`.
3. **Optional service down?** Expected in MSSQL-only mode; only matters if you rely on chat/embeddings.
4. **Low disk?** Free space on the content-root drive.
5. Still stuck? Export a support bundle (§4) and attach it to the ticket with the relevant timing-log
   lines.

## See also

- `docs/Supportability-Dashboard-Spec.md` — the implementation contract.
- `docs/Support-Bundle-Contents.md`
- `docs/Support-Runbook.md`, `docs/07-Troubleshooting.md`
