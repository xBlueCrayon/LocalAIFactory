# Screenshots

This folder holds **real product screenshots** of LocalAIFactory, captured from a running instance by
`scripts/docs/capture-screenshots.ps1` (Playwright/Chromium, headless, read-only GET navigation only — no forms
are submitted and no data is changed). They are generated, not hand-made or fabricated.

---

## Current status (honest)

**Screenshots ARE generated.** The build host now has **Node v24.17.0 + Playwright Chromium**, so the capture
script produced the set below from a live app. On a host **without** Node, the script detects this and exits
cleanly (printing the enabling commands) rather than failing — so the blocker, where it exists, is documented,
not faked.

## Captured set (11 pages, 1440×900 viewport, ~1.5 MB total, largest ≈ 191 KB)

| File | Route | Page |
|---|---|---|
| `01-home-dashboard.png` | `/` | Home / Dashboard |
| `03-readiness.png` | `/Readiness` | Enterprise Readiness scorecard |
| `04-base-knowledge.png` | `/BaseKnowledge` | Base Knowledge (included packs) |
| `05-knowledge.png` | `/Knowledge` | Knowledge |
| `06-projects.png` | `/Projects` | Projects |
| `08-coverage.png` | `/Coverage` | Import coverage / gap report |
| `09-graph-explorer.png` | `/Graph` | Explore Graph / impact |
| `11-benchmarks.png` | `/Benchmarks` | Benchmarks / validation harness |
| `13-audit.png` | `/Audit` | Audit Trail (admin) |
| `14-users.png` | `/Users` | Users & Access (admin) |
| `15-support-health.png` | `/Support` | Support / health dashboard |

CLI/benchmark-evidenced capabilities (symbol impact, ERP/CRM, core-banking, KYC/AML) are demonstrated by the
benchmark fixtures and `docs/` evidence rather than by a UI screenshot.

---

## Regenerating

```powershell
# One-time: install Playwright + a headless browser (Node 18+ required)
mkdir .tmp-playwright; cd .tmp-playwright; npm init -y; npm i -D playwright; npx playwright install chromium; cd ..
# Start the app (Development), then capture:
scripts/docs/capture-screenshots.ps1 -BaseUrl http://localhost:60398
```

## Conventions

- Viewport-only PNG (1440×900) — keeps each image small enough to commit as a documentation asset.
- File names are number-prefixed so they sort in walkthrough order.
- Re-running overwrites the pages it reaches; pages it cannot reach are **skipped (logged), never faked**.
- **Never** hand-edit or synthesize a screenshot to imply a state the app did not produce.
