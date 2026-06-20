# UI / UX Product Review

An honest review of the current LocalAIFactory user interface — what is strong, what to improve, and
a prioritised plan. This is a working review of the shipped UI, not a redesign proposal. The modern
card/table UI, the left navigation, status badges, and the readiness and support dashboards already
exist; the goal here is to confirm strengths and target concrete, incremental improvements.

---

## 1. What the UI is today

- **Framework:** Bootstrap 5 + bootstrap-icons, server-rendered ASP.NET Core MVC views, `marked.js`
  for client-side markdown. No SPA, no heavy client framework.
- **Shell:** a left navigation grouping pages by purpose (Overview, Ask, Projects, Knowledge, Graph,
  Quality, Ops, Admin, Runtime).
- **Patterns:** cards for summaries, tables for lists, status badges for state, dashboards for the
  Home, Readiness, and Support pages.
- **Performance posture:** pages render from MSSQL and never block on an external service; health is
  read from a cached snapshot. This is the UI's biggest hidden strength — it stays responsive.

---

## 2. Strengths

1. **Consistent, familiar shell.** Bootstrap 5 + a single left-nav give a predictable, enterprise
   look that needs no learning curve for an analyst.
2. **Honest state, surfaced in the UI.** Coverage/gap, confidence, evidence links, the Readiness
   scorecard, and the Support warnings all make the platform's actual state visible rather than
   hiding it. This is rare and valuable — the UI does not over-claim.
3. **Operations at a glance.** The Support dashboard (build/version, edition/license, cached health,
   DB counts, last import/audit, disk, warnings) is a genuinely useful single pane for an operator.
4. **Fast, non-blocking pages.** The "never wait on Qdrant/Ollama" rule shows up as a UI that does
   not spin or stall — core pages return well under a second.
5. **Clear separation of curated vs imported knowledge.** Users always know whether an item came from
   a curated pack or from their own repository, which builds trust.
6. **Role- and grant-aware.** Deny-by-default access produces clean 403 / AccessDenied states rather
   than confusing partial views.

---

## 3. Areas to improve

Grouped by impact. None require a redesign; all are incremental.

### 3.1 List ergonomics (high impact)
- **Filtering and search** on the larger list pages (Knowledge, Code Candidates, Imported files,
  Audit Trail). Long lists are hard to scan without server-side filters.
- **Pagination or virtualised lists.** Lightweight `record` projections keep queries cheap, but the
  rendered table can still be long. Add paging to keep the DOM and scan-cost bounded.
- **Sorting** on key columns (date, status, confidence).

### 3.2 Export (medium impact)
- **CSV / JSON export** of coverage/gap reports, impact results, and audit slices. Operators and
  reviewers frequently need to take a result off-platform for a meeting or a ticket.

### 3.3 Loading and feedback (medium impact)
- **Loading indicators** for the few genuinely async actions (import progress, benchmark runs,
  consolidation). Even fast pages benefit from a clear "working…" affordance on submit.
- **Success toasts / confirmations** after mutating actions (pack install, grant/revoke, role
  change, approve) so the user gets unambiguous feedback.

### 3.4 Empty and error states (medium impact)
- **First-run empty states** with a next-step call to action (e.g. "No projects yet — Import a
  repository"). A new Viewer with no grants should get a friendly explanation, not a blank table.
- **Consistent error cards.** Where a tile degrades to "unavailable" (as Support already does well),
  apply the same pattern everywhere a data source can fail.

### 3.5 Evidence and impact presentation (medium impact)
- The graph and impact results carry confidence and evidence; make the **confidence legend** and the
  **evidence drill-down** consistent and always one click away.

### 3.6 Responsive sanity (low impact)
- The product targets desktop operators, but the left-nav + tables should **not break** on a tablet
  width. A pass for collapse/overflow behaviour is worthwhile.

### 3.7 Polish (low impact)
- Consistent icon usage, button hierarchy (primary vs secondary), and typographic scale across pages.
- No unstyled debug or developer pages reachable in a normal build.

---

## 4. Prioritised plan

| Priority | Item | Why |
|---|---|---|
| P1 | Filtering + pagination + sorting on long list pages | Biggest day-to-day friction for analysts/admins |
| P1 | Success toasts / confirmations on mutating actions | Removes ambiguity after approve/grant/install |
| P2 | CSV/JSON export of coverage, impact, audit | Frequent off-platform need |
| P2 | Loading indicators on async actions | Clarity during import/benchmark/consolidation |
| P2 | Friendly first-run empty states with CTAs | Better onboarding for new Viewers |
| P3 | Consistent confidence legend + evidence drill-down | Reinforces the trust story |
| P3 | Tablet-width responsive sanity pass | Avoids layout breakage |
| P3 | Icon/button/typography consistency pass | Final polish |

---

## 5. What NOT to do

- Do **not** redesign the shell or replace Bootstrap. The current UI is a strength.
- Do **not** introduce a client-side SPA framework for these incremental gains.
- Do **not** add blocking calls to make a page "richer" — the non-blocking render rule is paramount.

See `docs/UI-UX-Polish-Checklist.md` for the actionable checklist that operationalises this review.
