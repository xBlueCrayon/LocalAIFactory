# Expected Capabilities — VB6-to-ASP.NET Migration Scenario

This document states, honestly, what LocalAIFactory **can** and **cannot** do for the ClaimDesk
modernization scenario. It exists to prevent overclaiming.

---

## The hard truth first: there is no VB6 parser

**LocalAIFactory has no VB6 or VB.NET source extractor today. Legacy parsing for this stack is a
gap.** The ingestion pipeline profiles repositories and extracts code structure for the stacks it
supports; `.frm`, `.bas`, `.cls`, `.vbp` files and Crystal Reports `.rpt` definitions are **not**
parsed into a structured symbol model. Do not claim, imply, or simulate automatic VB6 comprehension.

Concretely, the platform **cannot** today:

- Parse VB6 forms, modules, or classes into symbols, call graphs, or dependency maps.
- Read or interpret `modBusinessRules.bas` and recover the settlement-cap logic automatically.
- Translate or transpile VB6/ADO/DAO code into C# / EF Core.
- Parse Crystal Reports `.rpt` files or regenerate the reports automatically.
- Connect to the legacy Access/JET cache or diff it against MSSQL.

If asked "can it auto-migrate the VB6 app?", the correct answer is **no**.

---

## Where the value actually is

The platform's contribution is **knowledge-level reasoning**, not code transformation. For this
scenario it provides:

### 1. A migration playbook (strangler-fig)
A reusable, written method for incrementally replacing a thick-client desktop app with ASP.NET Core +
EF Core + MSSQL: characterize-before-change, shared-database seam, lowest-risk-slice-first sequencing,
and legacy retirement criteria. This is curated, approvable project knowledge — exactly what
LocalAIFactory is built to store and inject first into prompts.

### 2. Risk assessment
A structured catalogue of the failure modes specific to VB6 modernization — hidden business rules,
silent last-write-wins sync, encoding/locale drift, report divergence, authorization gaps, big-bang
risk — with the mitigation for each. This is reasoning the platform can hold, retrieve, and apply.

### 3. Target architecture design
Reasoned target-state architecture: MVC + EF Core over the existing MSSQL schema, additive-only schema
evolution, Windows/AD authentication, deny-by-default role authorization, separation of duties,
append-only audit, and server-side reporting to replace Crystal. The platform reasons about *what good
looks like* for the target, consistent with its own architectural rules.

### 4. Test and parity strategy
A test strategy emphasizing **golden-master / behavioural-parity** testing for financial logic, rules
unit tests with boundary cases, report-parity comparison, authorization matrices, and conflict-aware
data integrity tests. The platform can describe and justify this strategy and help draft the cases.

---

## What a human (not the platform) must still do

- **Read the VB6 source by hand** (or with separate tooling) to recover the actual rules; the platform
  reasons about *how to do this safely*, but does not do the parsing.
- **Extract the golden-master datasets** from the legacy system's real outputs.
- **Validate report parity** against the existing Crystal output.
- **Implement** the new MVC/EF Core code and run the tests.

---

## Honesty checklist (applies to every answer about this scenario)

- [ ] Did the answer state plainly that there is **no VB6 parser / no auto-migration**?
- [ ] Did it avoid implying the platform "read" or "understood" the VB6 code automatically?
- [ ] Did it frame the platform's value as **playbook + risk + target design + test strategy**?
- [ ] Did it keep schema changes additive and the strangler-fig cutover incremental?
- [ ] Did it avoid claiming any certification, vendor capability, or guaranteed parity?
