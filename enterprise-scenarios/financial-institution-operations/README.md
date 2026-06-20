# Financial-Institution Operations Scenario

**Original synthetic operations scenario.** This describes how an operating manager at a financial
institution would use LocalAIFactory's structural understanding of a banking middleware estate to
run day-to-day operations: change-impact triage, control coverage review, and approval routing. It
is **NOT a vendor product**, **NOT a regulatory artifact**, and makes **no compliance,
certification, or legal-advice claims**. Control language is design/awareness only.

It composes the platform's graph-derived capabilities (proven by the `COREBANK`, `ERPCRM`, and
`KYCAML` benchmark fixtures) into an operations narrative. It does not ship its own benchmark
fixture; its validation is a presence/consistency check (see `validation-script.ps1`).

---

## Who this is for

An operations / control owner who needs to answer, quickly and defensibly:

- "If we change this table or proc, which services and reports break?"
- "Which code paths touch a controlled object (posting, mandate, screening, approval)?"
- "Where is the approval/segregation enforced, and where is it only advisory?"

## How the platform supports it

The platform answers structural questions over the imported estate via four query modes — `find`,
`dependents`, `dependencies`, `impact` — each with provenance to a file and line span. Operations
uses these to triage changes before they ship and to evidence control coverage during review.

| Operations need | Backed by | Mode |
|---|---|---|
| Change-impact triage | merged C#↔SQL graph | `impact(target)` |
| "What touches X" coverage | reference edges | `dependents(target)` |
| Service → data lineage | reference + containment edges | `dependencies(target)` |
| Approval routing reference | `approval-matrix.md` | advisory |
| Control coverage view | `controls-matrix.md` | advisory + graph anchors |

## Related fixtures (live proofs)

- `COREBANK` — posting / mandate / claim / settlement integration surface.
- `KYCAML` — KYC → screening → maker/checker transaction approval.
- `ERPCRM` — ERP/CRM service layer.

## Honest split

Graph-derived: dependency, dependents, dependencies, and impact over imported C#/SQL.
Advisory: approval thresholds, segregation policy beyond proc/constraint level, and any regulatory
sufficiency — none of which is asserted as a compliance guarantee.

## How to run validation

```powershell
./validation-script.ps1
```

It verifies the scenario's documents are present and that the underlying graph capabilities it
relies on are described, then exits `0`.
