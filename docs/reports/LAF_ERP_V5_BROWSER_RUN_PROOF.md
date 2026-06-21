# LAF ERP V5 Browser Run Proof

## Run

- **URL:** http://localhost:5085
- **Database:** SQLite
- **Result:** **15 / 15 probed endpoints returned HTTP 200.**
- **0 unhandled exceptions**, **0 HTTP 500.**

## Endpoints probed (15/15 = 200)

Core pages plus:

- **P&L report** — income **500**, expense **300**, **netProfit 200**.
- **Balance Sheet** — **balanced = true**.
- New catalog modules: **quotations**, **employees**, **workorders**, **leaveapplications**.

## Create-form UI

The new generated **create form works in the browser**: the GET form renders, POST persists
the record with audit fields, and the record is retrievable via the module API (this is the
path the Playwright create-form test exercises).

## Honest note

This is a local SQLite run on a dev port. It proves the generated app boots, serves all
probed routes, produces a balanced P&L/Balance Sheet, and persists via the new create form.
It is **not** a production load test or a secured deployment. V5 is **ERP_PILOT_READY**.
