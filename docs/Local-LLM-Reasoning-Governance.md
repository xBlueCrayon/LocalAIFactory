# Local LLM Reasoning Governance

Rules that bound how local models may be used in LocalAIFactory. **Authority:** subordinate to
`MASTER_VISION.md`.

## Cardinal rules

1. **MSSQL is the source of truth.** The model never replaces it.
2. **Every model output is a proposal.** It carries `authoritative=false` and `reviewStatus=PENDING_REVIEW`
   and is **not** installed into memory until approved.
3. **Propose-never-overwrite.** Approval creates a new versioned item; it never silently overwrites curated
   knowledge.
4. **Optional + replaceable.** The model is off the request path; swapping models changes nothing
   authoritative. The platform runs with no model present.
5. **Grounded or refuse.** The model must say "insufficient evidence" rather than fabricate — verified by the
   hallucination-refusal test (`scripts/ai/test-local-llm-reasoning.ps1`, test 8).
6. **No secrets / no internet dependency.** Inference is local; prompts contain no secrets; no external call
   is required once a model is present.

## Review flow (approval gate)

| State | Meaning |
|---|---|
| `PENDING_REVIEW` | LLM proposal generated; not authoritative; not installed |
| `APPROVED` | a reviewer accepted it → becomes a versioned MSSQL knowledge item |
| `REJECTED` | discarded; recorded for audit |
| `EDITED` | reviewer modified before approval |

## Scoring discipline (proof harness)

```
0   hallucinated / unsafe
25  generic
50  partially useful
75  grounded but incomplete
90  grounded + safe + reviewable        <-- maximum for an unreviewed proposal
100 deterministic evidence + reviewed + approved
```

A model output may score **at most 90** until a human review converts it into approved, versioned knowledge.
The executed proof scored **mean 90** (all 8 tasks grounded; hallucination-refusal verified) — see
`reports/LOCAL_LLM_REASONING_PROOF.md`. **No LLM output is marked 100** in this work, because none has been
through the human approval gate into versioned memory.

## Audit

Proposals, approvals, edits, and rejections are auditable. The deterministic graph/benchmark evidence — not
the model — remains the authority for any capability claim.
