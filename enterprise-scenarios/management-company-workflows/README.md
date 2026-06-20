# Management-Company Workflows Scenario

**Original synthetic management scenario.** This describes how a management company (operating the
banking middleware estate on behalf of the business) runs its recurring oversight workflows using
LocalAIFactory's structural understanding of the codebase. It is **NOT a vendor product**, **NOT a
regulatory artifact**, and makes **no compliance, certification, or legal-advice claims**. Control
language is design/awareness only.

Like `financial-institution-operations`, this scenario composes graph-derived capabilities
(`COREBANK`, `ERPCRM`, `KYCAML` fixtures) into a management narrative rather than shipping its own
benchmark fixture. Its validation is a presence/consistency check.

---

## Who this is for

An operating / management-company manager responsible for oversight rather than line operations:
change governance, control-coverage assurance, approval-authority maintenance, and management
reporting.

## Recurring workflows supported

| Workflow | Platform support | Mode |
|---|---|---|
| Change governance review | `impact(target)` per proposed change | Graph-derived |
| Control-coverage assurance | `dependents(target)` over controlled objects | Graph-derived |
| Lineage / data-flow review | `dependencies(target)` | Graph-derived |
| Approval-authority maintenance | `approval-matrix.md` | Advisory |
| Management reporting | `operating-manager-dashboard.md` views | Advisory + graph anchors |

## How the platform helps a management company

The management company does not write the code; it oversees it. The platform lets a manager ask
structural questions and get deterministic, provenance-backed answers without reading the source —
"what changed, what does it reach, is a control affected, who must approve it."

## Honest split

Graph-derived: dependency, dependents, dependencies, and impact over imported C#/SQL. Advisory:
approval authorities, segregation beyond proc/constraint level, and regulatory sufficiency — none
asserted as a compliance guarantee.

## How to run validation

```powershell
./validation-script.ps1
```

It verifies the scenario documents are present and that the backing fixtures it relies on are
declared in the manifest, then exits `0`.
