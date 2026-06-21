# External-Proof Emulation Engine

**Generated:** 2026-06-21
**Engine:** `scripts/production/verify-external-proof-emulation.ps1`
**Model:** `operator-emulation/external-proof-model.json`
**Result:** `benchmarks/results/external-proof-emulation.json`

## Problem

Several proofs needed for commercial GA cannot be produced from a developer workstation: a real
Windows Server host, a CA-issued TLS certificate on a real domain, a real Entra/OIDC tenant, an
independent penetration test, a signed customer pilot on sanitized data, and production
SMTP/SFTP/monitoring. The dishonest move is to fake them. The honest move is to **model** each
one precisely so the operator can execute it, and to **prove we have not faked any of them**.

## What the engine asserts

For every external proof in the model, the engine requires all of:

1. a classification from a closed set — `BLOCKED_OPERATOR` / `BLOCKED_EXTERNAL` /
   `BLOCKED_CUSTOMER` / `EMULATED_PROOF` (and rejects `REAL_PROOF`, which this host may not assert);
2. an **emulated input file** that actually exists in `operator-emulation/`;
3. a trusted-source **expected output**;
4. a **validation command** the owner runs to confirm the real proof;
5. a named human **owner** (operator / external firm / customer);
6. a binary **pass criterion**;
7. `realSecret: false` — and a regex sweep confirms no real secret pattern leaked into the pack.

Exit is non-zero only if a proof is un-modelled (`MISSING_PROOF`) or a rule is violated.

## Current result

| Classification | Count |
|---|---:|
| `BLOCKED_OPERATOR` | 6 |
| `BLOCKED_EXTERNAL` | 2 |
| `BLOCKED_CUSTOMER` | 1 |
| **Total modelled** | **9** |
| **Faked as real** | **0** |

`EXTERNAL-PROOF-EMULATION: PASS` — every external proof is modelled, owned, and validatable, and
none is counted as real.

## Relationship to the gates

Gate V3 (`scripts/production/verify-production-readiness-v3.ps1`) consumes this result. It can only
reach `NEAR_GA_READY_WITH_EXTERNAL_PROOF_MODEL` when `fail = 0` **and** `noRealProofClaimed = true`.
This is what keeps "near-GA" honest: the remaining distance to commercial GA is fully enumerated as
externally-owned work with concrete validation commands, not hand-waved and not faked.

## Operator runbook

Each modelled proof maps to an `operator-emulation/*.example.json` input and a validation command.
To convert an emulated proof into a real one, the named owner supplies the real input and runs the
validation command; the proof then moves from `BLOCKED_*` to a genuinely satisfied state. Until
then the system is correctly classified as near-GA, **not** GA.
