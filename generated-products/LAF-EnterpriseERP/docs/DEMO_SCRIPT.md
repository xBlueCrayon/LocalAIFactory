# LAF Enterprise ERP — 5-Minute Demo Script

**Audience:** stakeholder reviewing the generated ERP proof.

1. **Frame it (30s).** "This is *LAF Enterprise ERP* — a clean-room ERP core built from ERPNext's *public
   docs*, not its code. It's a proof that we can study a large business system and generate a real,
   tested application. It is ~36% ERPNext-grade — strong core, several modules absent. No overclaim."

2. **Dashboard (30s).** Show KPIs: customers, invoices, **pending approvals**, **AR 350 / AP 1200**.
   "These numbers come from real posted transactions, not mock data."

3. **General Ledger (60s).** Open it. "Every transaction posts **double-entry**. Look at the footer —
   **total debit equals total credit**. The system *refuses* to post an unbalanced voucher."

4. **Stock Balance (30s).** "Inventory uses an immutable stock ledger with moving-average valuation and a
   **negative-stock guard** — you can't sell what you don't have."

5. **Workflow + Audit (60s).** Open Workflow Inbox then Audit Log. "Approvals run **maker/checker**: a
   submitter can't approve their own document, and amounts over a threshold need a separate approver.
   Every action is **audited** with who and when."

6. **Switch User (30s).** Use the dev-auth login to become `alice (Sales User)`. "Auth here is a dev
   stand-in; production binds to Windows/SSO. Roles drive what each user can do."

7. **Tests (30s).** "All of this is backed by **86 automated tests** — 74 .NET + 12 Playwright browser
   tests — all green, and it runs on SQLite with zero external services."

8. **Honest close (30s).** "What's missing: manufacturing, HR, POS, website, real auth, and the UI is
   read-oriented today. This is a credible POC foundation, not a finished ERPNext replacement."
