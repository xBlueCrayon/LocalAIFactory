# Test Questions — Leasing & Arrears Platform

Questions to probe whether a solution (or an AI assistant reasoning about it) actually
understands this domain. Each lists what a strong answer must contain. None of these
ask for, or accept, a compliance/certification claim; staging/default/ECL answers must
carry the awareness-only caveat.

## 1. How do you guarantee an amortization schedule is reproducible?

A strong answer describes a **pure, deterministic** amortization domain service driven
only by contract terms (no hidden state, no clock), a stored schedule, and a
**recompute-and-compare** check against a defined rounding tolerance. It mentions where
the residual rounding difference is posted (last installment).

## 2. A contract is restructured mid-term. What happens to the old schedule?

Must explain **versioning**: a new schedule version is created and the prior version is
**preserved** and linked (supersedes/superseded-by), never overwritten. Bonus: why this
matters for audit and rollback.

## 3. The same bank statement file is imported twice. What prevents double payment?

Must describe **idempotent import** keyed on receipt identity, with allocation records,
so a re-import allocates nothing additional. Should mention journaling for traceability.

## 4. An installment is exactly 30 days past due, another exactly 31. Where do they go?

Must show awareness of **bucket boundary semantics** (e.g., 1–30 vs 31–60) and that an
off-by-one in day-count is a real failure mode. A strong answer names the calendar /
day-count convention and time-zone pinning as the source of correctness.

## 5. How is a collections case created, and how is it closed?

Must explain **auto-open exactly one case** when a contract enters arrears (no
duplicates) and **auto-close on cure**. Should mention the orphaned-case failure mode
and how it is prevented.

## 6. What is a "staging signal" here, and what is it NOT?

Must state it is a **candidate / advisory** indicator (e.g., significant-increase-in-
credit-risk flag, default-candidate flag) that a qualified reviewer dispositions. Must
explicitly say it does **not** finalize impairment stage, default classification, or
ECL — those are **accounting/regulatory interpretations requiring sign-off; not advice**.
An answer that has the platform decide staging on its own is wrong.

## 7. A Collections Agent guesses a contract id they aren't assigned to. What happens?

Must describe **object-level authorization** and a blocked **IDOR** attempt under a
**deny-by-default** server-side policy. Should note it is enforced server-side, not in
the UI.

## 8. Who can propose and who can approve a write-off?

Must describe **segregation of duties**: the proposing role cannot also approve. Should
mention both actions are captured in the **append-only audit log**.

## 9. The bank, GL, and valuation feeds are all offline. What still works?

Must assert **MSSQL-only operation**: every page renders, contracts display, schedules
compute. The request path **never blocks** on an external feed. Integration adapters are
optional behind interfaces.

## 10. How do you keep the arrears report fast on a large book?

Must describe **lightweight projections** that never materialize large text columns,
simple reliable queries, and sub-second targets. Bonus: the daily job does the heavy
ageing in batch so reads are cheap.

## 11. A release ships a bad amortization engine. How do you recover?

Must describe **engine versioning** and pinning back, plus **recompute-and-compare** of
affected schedules against the prior version, and **additive migrations** so the binary
can roll back without a destructive down-migration.

## 12. How would you extend this toward a full ECL model later — and what's the line?

Must frame ECL (PD/LGD/EAD, 12-month vs lifetime) as **future** work, and hold the line
that even then the platform produces inputs for **qualified human staging decisions**.
Must repeat the **awareness-only / not advice** caveat and make **no compliance claim**.
