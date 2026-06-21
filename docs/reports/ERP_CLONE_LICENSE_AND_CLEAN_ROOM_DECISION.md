# ERP Clean-Room and License Decision Report

**Product:** LAF Enterprise ERP (a.k.a. "LocalAIFactory ERP Suite")
**Date:** 2026-06-21
**Status:** Decision record — governs the clean-room reimplementation effort
**Scope:** Licensing posture and clean-room methodology for building an ERPNext-grade ERP inside LocalAIFactory
**Companion artifact:** `benchmarks/erpnext-study/erpnext-parity-target-matrix.json` (behavioral parity targets)

---

## 1. Purpose

LAF Enterprise ERP is an original, clean-room ERP implemented in C#/.NET for the LocalAIFactory
platform. It is **functionally inspired by** the public, documented behavior of ERPNext and the
Frappe Framework, but it is **not** a fork, port, repackaging, or derivative of either. This document
records (a) the exact licenses of the reference projects, (b) the clean-room method used, (c) whether
any GPL/AGPL obligations are triggered, and (d) the naming/branding and "must-not-claim" guardrails.

---

## 2. Confirmed licenses of the reference projects (verified from official sources)

Licenses were verified directly from the official GitHub repositories on 2026-06-21 via the GitHub
API and the repositories' own license files, **not** from memory or third-party summaries.

| Project | Repository | Confirmed license | How verified |
|---|---|---|---|
| **ERPNext** | `github.com/frappe/erpnext` | **GNU General Public License v3 (GPL-3.0)** | `license.txt` begins `GNU GENERAL PUBLIC LICENSE / Version 3, 29 June 2007`; GitHub license detector reports `GPL-3.0`; repo sidebar shows "GPL-3.0 license". |
| **Frappe Framework** | `github.com/frappe/frappe` | **MIT License** (current repo, verified at `develop`, `version-14`, `version-13`) | Top-level `LICENSE` file reads "The MIT License", README badge shows "License: MIT", GitHub license detector reports `MIT`. |

### 2.1 Important nuance on Frappe's licensing (honest finding)

A widely repeated claim is "Frappe Framework is AGPLv3." **The current `frappe/frappe` repository does
not bear an AGPL license** — its top-level license file and README badge state **MIT** across the
`develop`, `version-14`, and `version-13` branches as inspected on 2026-06-21. The AGPL association
comes from Frappe Technologies' **stated app-licensing strategy**, articulated by Frappe's founders on
the official forum: end-user *applications* are released under **AGPLv3**, while pure developer
tooling and the framework core trend toward **MIT**. Examples verified via the GitHub API:

- `frappe/helpdesk` → **AGPL-3.0**
- `frappe/lms` → **AGPL-3.0**
- `frappe/hrms` (HR/Payroll) → **GPL-3.0**
- `frappe/erpnext` → **GPL-3.0**
- `frappe/frappe` (framework core) → **MIT**

Founder statement (official Frappe forum): *"applications that are used by end customers are AGPL and
pure developer tools (like Frappe UI) are MIT,"* and apps built on GPL code such as ERPNext that are
merely *delivered over a network* do not inherit AGPL's network-distribution ("viral over the wire")
obligation, because GPL — unlike AGPL — does not treat network delivery as distribution.

**Net licensing picture that governs this project:** the ERP feature behavior we target is documented
in ERPNext, which is **GPL-3.0**; some adjacent Frappe end-user apps are **AGPL-3.0**; the Frappe
framework core itself is **MIT**. We therefore treat the strongest applicable copyleft — **GPLv3 (and
AGPLv3 where an inspected app is AGPL)** — as the constraint to design around. We do not rely on the
MIT classification of the framework core as a loophole, and we copy nothing from any of them.

### 2.2 Official sources consulted

- ERPNext license: `github.com/frappe/erpnext` → `license.txt` (GPLv3 full text).
- Frappe license: `github.com/frappe/frappe` → `LICENSE` (MIT) and README license badge.
- Frappe app-licensing strategy: official Frappe community forum thread *"Frappe Licensing & Software
  Architecture"* (`discuss.frappe.io`), statements by Frappe founders/maintainers (rmehta, Ankush).
- Per-app licenses (`helpdesk`, `lms`, `hrms`): GitHub license API for each `frappe/*` repository.

---

## 3. Clean-room methodology

LAF Enterprise ERP was specified and is being implemented under a strict **clean-room** discipline:

1. **Specification derived from public documentation and domain theory only.** The parity targets in
   `erpnext-parity-target-matrix.json` were written from:
   - Official end-user documentation at **docs.erpnext.com** (e.g. Journal Entry, Payment Entry,
     General Ledger, Trial Balance, P&L, Balance Sheet, Stock Entry, Sales/Purchase Order, Delivery
     Note, Purchase Receipt, BOM, Work Order, Quality Inspection).
   - Official framework documentation at **frappeframework.com / docs.frappe.io** (REST API
     conventions, role-based permissions, naming series, workflow/maker-checker, submittable-document
     lifecycle, data import).
   - **Standard, non-proprietary accounting and inventory theory** in the public domain (double-entry
     bookkeeping, FIFO/moving-average valuation, AR/AP ageing, depreciation) — concepts no party owns.
2. **No source code was copied.** No ERPNext or Frappe Python, JavaScript, JSON DocType definitions,
   SQL, HTML/Jinja templates, CSS, icons, or other UI assets were copied, transliterated, machine-
   translated, or pasted into LAF Enterprise ERP.
3. **No repository was cloned into the LocalAIFactory tree.** Research used official documentation
   websites and read-only GitHub metadata/license queries. Nothing from ERPNext/Frappe source trees
   was vendored, embedded, or committed.
4. **Public source inspected only for behavioral understanding — never copied.** Where public source
   or documentation was read, it was used solely to understand *observable behavior and conventions*
   (e.g. that a Sales Order tracks delivered/billed quantity per line; that a Payment Entry allocates
   against references; that the REST API authenticates with an `api_key:api_secret` token). Such
   facts about *what a system does* are ideas/behaviors, not protected expression, and were re-
   expressed as original requirements and original C#/.NET code.
5. **Original expression.** All entity models, EF Core mappings, posting engines, controllers, views,
   and tests are written from scratch in the LocalAIFactory architecture (C#/.NET, MSSQL, EF Core).
   Naming reflects standard accounting/ERP vocabulary that predates and is independent of ERPNext.

---

## 4. Are GPL/AGPL obligations triggered?

**No.** GPLv3 and AGPLv3 copyleft attach to **copying, modifying, or distributing the licensed
work or a derivative of it.** Because LAF Enterprise ERP:

- contains **no** ERPNext or Frappe source code or assets,
- does **not** link against, bundle, embed, or dynamically load any ERPNext/Frappe code or library,
  and
- is independently authored original C#/.NET expression built from public documentation and public-
  domain domain theory,

it is **not a derivative work** of ERPNext or Frappe under copyright law. Therefore **GPLv3 / AGPLv3
copyleft is not triggered**, and LAF Enterprise ERP is **not** obligated to be released under GPL or
AGPL on account of these reference projects. Implementing the *same business behavior* (e.g.
double-entry GL posting, FIFO valuation, maker-checker approval) does not create a derivative work —
copyright protects expression, not ideas, methods, or functional behavior.

### 4.1 The hard line (stated explicitly)

This conclusion holds **only** while the clean-room discipline is maintained. **If any GPL/AGPL-
licensed code or asset were ever copied** (even a snippet, a DocType JSON, a query, or a template)
into LAF Enterprise ERP, then:

- the resulting component would become a **derivative work**, and
- it would have to be **released under the same copyleft license** (GPLv3 for ERPNext-derived code;
  AGPLv3 for any AGPL Frappe-app-derived code, with AGPL's network-use obligation), and
- distributing it under any more permissive or proprietary terms would be a **license violation**.

To prevent this, contributors **must not** copy from ERPNext/Frappe sources, and any future code
review should reject pasted upstream code. When in doubt, re-derive from documentation and theory.

---

## 5. Naming, branding, and trademarks

- The product is named **"LAF Enterprise ERP"** (alternatively "LocalAIFactory ERP Suite"). These are
  neutral, original names.
- **"ERPNext", "Frappe", and associated logos/marks are trademarks of Frappe Technologies Pvt. Ltd.**
  and are **not used** as branding, product names, module names, screen titles, or marketing for LAF
  Enterprise ERP.
- ERPNext/Frappe names appear **only** in internal engineering artifacts (this report and the parity
  matrix) for **nominative, factual reference** — to cite the documentation that informed our targets.
  They are not used to imply origin, sponsorship, endorsement, affiliation, or compatibility.

---

## 6. MUST NOT be claimed (hard guardrails)

The following statements are **prohibited** in any documentation, UI, marketing, commit message,
release note, or customer communication for LAF Enterprise ERP:

1. **Do NOT** call it an "ERPNext clone", "ERPNext replacement", "ERPNext port", or "Frappe clone".
2. **Do NOT** claim "100% parity", "full parity", or "feature-complete vs ERPNext". Parity scores in
   the matrix are an internal, self-assessed engineering metric only.
3. **Do NOT** claim "ERPNext compatible", "drop-in compatible", "data-compatible", or "API-compatible
   with ERPNext/Frappe".
4. **Do NOT** claim any ERPNext/Frappe "certification", "validation", "approval", "endorsement", or
   "official" status.
5. **Do NOT** use the ERPNext or Frappe names or logos as product branding, in the UI chrome, or in
   any way that implies origin, sponsorship, affiliation, or endorsement.
6. **Do NOT** state or imply that LAF Enterprise ERP contains, reuses, or is built on ERPNext/Frappe
   code. It does not.

---

## 7. Summary

- **ERPNext = GPL-3.0** (verified). **Frappe Framework core = MIT** (verified); several Frappe
  end-user apps are **AGPL-3.0** and `frappe/hrms` is **GPL-3.0** — so we design against the strongest
  applicable copyleft (GPLv3/AGPLv3) and rely on no permissive-license loophole.
- LAF Enterprise ERP is a **clean-room, original C#/.NET implementation** built from official
  documentation and public-domain domain theory. **No source code or UI assets were copied; no repo
  was cloned into the tree.**
- Because nothing copyleft is copied or linked, the work is **not a derivative** and **GPL/AGPL
  copyleft is not triggered** — but copying any upstream snippet would immediately impose the
  upstream copyleft on the derived component.
- Neutral branding (**"LAF Enterprise ERP"**) is used; **ERPNext/Frappe trademarks are not used as
  branding**, and the prohibited-claims list in §6 is binding.
