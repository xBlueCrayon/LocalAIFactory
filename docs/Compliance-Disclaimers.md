# Compliance Disclaimers

This document states, plainly and without hedging, what LocalAIFactory **is not** and what its
outputs **must not** be relied upon for. It exists so that no user, reviewer, or stakeholder
mistakes an engineering assistant for an authority it is not. Nothing here should be read as a
claim of certification, affiliation, or legal/regulatory standing.

---

## 1. Not professional advice

LocalAIFactory and any output it produces (including AI-generated text, summaries, extracted rules,
graphs, impact analyses, and suggestions) are **not**, and must not be relied upon as:

- **Legal advice** or a legal opinion.
- **Regulatory or compliance advice**, nor a determination of regulatory status.
- **Tax advice.**
- **Audit advice** or an audit opinion / assurance.
- **Financial, accounting, investment, or actuarial advice.**

For any of the above, consult a qualified professional. The platform is an internal software-
engineering aid for understanding and evolving a code estate — nothing more.

---

## 2. Not a determination engine

The platform does **not** make, and must not be used to make, determinations of:

- **Fraud or forgery** (including any cheque/document authenticity outcome).
- **Medical** conditions or outcomes of any kind.
- Identity, creditworthiness, eligibility, or any decision about a person.

Where the estate includes document-processing components (e.g. OCR/PDF or cheque-related systems),
LocalAIFactory's role is limited to **describing and reasoning about the code**. Any
authenticity/fraud/forgery decision is made by the underlying business system and its accountable
operators, not by this platform, and not by any AI output it surfaces.

---

## 3. Awareness-only references to standards and frameworks

Where the platform surfaces references to standards, regulations, or frameworks — for example
**Mauritius** statutes/guidelines, **IFRS**, **FATF** recommendations, or similar — these are
provided **for awareness and navigation only**. They are:

- **Not** authoritative statements of the law or standard.
- **Not** kept current, complete, or jurisdiction-specific.
- **Not** a substitute for the primary source or professional interpretation.

Always verify against the authoritative primary source. Treat any such reference as a pointer, not
a ruling.

---

## 4. No certification or vendor affiliation

LocalAIFactory is **not certified** by, **not endorsed by, and not affiliated with** any third
party, including but not limited to **SAP, Sage, Oracle, Cisco, Microsoft Dynamics**, or any other
vendor. Product and company names are the property of their respective owners and are used, where
they appear, **only** for identification and interoperability description. No compatibility,
conformance, or certification claim is made or implied for any external product, standard, or
certification scheme.

---

## 5. AI outputs are advisory, never authoritative

- AI-generated content can be **incomplete or wrong** (it can hallucinate, omit, or misread).
- Every AI output is **advisory** and must be **independently verified** by a competent human before
  any action is taken on it.
- The platform is designed to run **without** any AI service present (MSSQL-only mode); AI is an
  optional aid, not a system of record. The system of record is the curated, human-approved
  knowledge and the underlying code.

---

## 6. Source-registry governance

To prevent unverified or improperly sourced material from entering the knowledge base, the platform
applies a source registry and permanence governance:

- Sources are registered; the registry marks each with whether **verbatim copying is allowed**
  (default `verbatimCopyAllowed = false`) and flags research families as **"verification
  required"**.
- The Knowledge Pack installer **rejects references to unregistered sources** — material cannot be
  silently introduced from an unknown origin.
- Knowledge items carry a **permanence tier** (`Derived` vs `Curated`); the permanence guard
  follows a **propose-never-overwrite** rule so curated, human-approved knowledge is not
  clobbered by automated extraction (`IPermanenceGuard`).
- Provenance (`ProvenanceEvent`, including `OriginPackUid`) records where each item came from, so
  the origin of any surfaced statement is traceable.

This governance reduces the risk of unsourced claims, but it does **not** make any surfaced
reference authoritative — §3 and §5 still apply.

---

## 7. Use within scope

LocalAIFactory is a **private, local-first internal tool** for a specific banking middleware
estate. It is not a public service, not a system of record for legal/regulatory obligations, and
not a substitute for the accountable systems and professionals that own those obligations.
