# Unlimited ERP Generation — Convergence Policy

**Date:** 2026-06-21

The fixed `MaxIterations = 5` cap is removed. The loop runs adaptively and stops only on a **documented**
condition.

## Stop conditions

| ID | Condition |
|---|---|
| A | Production-grade ERP gates pass (≥ target). |
| B | **Two consecutive full iterations** produce no measurable improvement. |
| C | A hard host/resource/legal blocker is proven and documented. |
| D | Runtime/resource limits make further iteration unsafe. |

## Progress metrics (the loop records all of these per iteration)

ERPNext-grade parity %, module completion %, production-grade score %, .NET test count + pass rate,
Playwright test count, real-life scenario count, code-review issue count, security-review issue count,
generation autonomy %, knowledge-base item count, generator capability changes.

## Measurable-improvement rule

An iteration "improved" if **any** of {production-grade %, parity %, passing test count, scenario count}
increased versus the previous iteration **without** regressing build/test green. If none increase for two
consecutive iterations → **Stop B**.

## Honesty clause

The loop never stops with "enough." Every stop records which condition fired and the evidence
(`benchmarks/results/laf-erp-unlimited-generation-loop.json`). Hard blockers (no real auth, no MSSQL load
host, external security review) are recorded as **Stop C** with the owner, not faked as solved.
