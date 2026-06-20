# UI / UX Polish Checklist

An actionable, page-by-page checklist that operationalises `docs/UI-UX-Product-Review.md`. Use it as a
pre-handover sweep. Each item is a concrete yes/no. None of these require a redesign or a new
framework — they are consistency and finishing items on the existing Bootstrap 5 UI.

> Rule that overrides everything below: **no change may add a blocking external-service call to the
> render path.** Health stays on the cached snapshot; lists stay on lightweight `record` projections.

---

## 1. Consistency

- [ ] Icons: every nav item and primary action uses a bootstrap-icon from a consistent set; no mixed
      or missing icons.
- [ ] Buttons: clear primary vs secondary hierarchy; one primary action per view; destructive actions
      visually distinct.
- [ ] Typography: consistent heading scale and body size across pages; no ad-hoc inline font sizes.
- [ ] Badges: status badges use a consistent colour mapping (e.g. Approved / Draft / NeedsReview /
      Deprecated) across every knowledge page.
- [ ] Spacing: consistent card padding and table density across list pages.

## 2. Empty states

- [ ] Projects: a new Viewer with no grants sees an explanatory empty state ("No projects granted —
      ask an administrator"), not a blank table.
- [ ] Knowledge / Business Rules / Approved Code / Code Candidates: empty state explains the lifecycle
      and the next step.
- [ ] Imports: empty state points to **Import Project**.
- [ ] Chat: empty/first-message state explains it is grounded in approved knowledge and notes when no
      model is configured.
- [ ] Graph: empty state when a project has no extracted symbols yet.

## 3. Error states

- [ ] Every data tile that can fail degrades to a clear "unavailable" state (as Support already does)
      rather than 500-ing the page.
- [ ] Access denied renders the friendly `AccessDenied` view with a clear "request access" message.
- [ ] Optional-service-down (Ollama/Qdrant) is shown as a status, never as a broken page.
- [ ] DB-unreachable surfaces a single clear message, not a stack trace.

## 4. Feedback

- [ ] Mutating actions (approve, grant/revoke, role change, pack install, import submit) show a
      success toast or confirmation.
- [ ] Async actions (import, benchmark run, consolidation) show a loading/progress affordance on
      submit.
- [ ] Failed actions show an actionable error message, not a silent no-op.

## 5. Lists

- [ ] Long list pages have filtering/search.
- [ ] Long list pages have pagination (or a bounded, virtualised render).
- [ ] Key columns are sortable (date, status, confidence).
- [ ] Confidence and evidence are one click away on impact/graph results, with a consistent legend.

## 6. Navigation and links

- [ ] No broken links in the left nav or in-page links (every route resolves).
- [ ] Active nav item is highlighted on every page.
- [ ] Breadcrumbs / back-links present where a page is reached by drill-down (project → file → graph).
- [ ] Admin-only items are hidden for non-admins **and** enforced server-side (UI hiding is never the
      control).

## 7. No debug surface

- [ ] No unstyled debug or developer-only pages reachable in a Release build.
- [ ] No raw exception pages exposed to end users in production configuration.
- [ ] No placeholder "lorem ipsum" or TODO text visible in any shipped view.

## 8. Responsive sanity

- [ ] Left nav collapses or remains usable at tablet width.
- [ ] Tables overflow/scroll gracefully rather than breaking the layout at narrow widths.
- [ ] The Support and Readiness dashboards remain readable on a smaller screen.

## 9. Trust and honesty cues

- [ ] Curated vs imported knowledge is visually distinguished on every knowledge page.
- [ ] Coverage/gap shows honest gaps (no silent zeros) with the skip-reason buckets visible.
- [ ] Limitation notes are visible on Base Knowledge and pack items.
- [ ] Readiness and Known-Limitations are linked from where users might over-trust a result.

---

## Sign-off

A handover-ready UI passes every box above on the core analyst and admin journeys: Home → Projects →
import → graph/impact → Chat → Knowledge approval → Support. Record exceptions and their justification
rather than silently shipping them.
