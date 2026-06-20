# Integration Contracts — ERP/CRM Industrial

> **Original synthetic contracts.** Nothing here copies a vendor API surface, OData schema, SDK, or
> manual. The shapes are illustrative of *good practice*, not of any commercial ERP/CRM product, and
> imply no compatibility or certification.

These contracts describe how an external system would integrate with the scenario's entity model
(`Customer`, `Contact`, `Lead`, `Opportunity`, `PriceList`, `SalesOrder`, `SalesOrderLine`,
`Invoice`, `PurchaseOrder`, `InventoryItem`, `InventoryMovement`, `DiscountApproval`).

## 1. REST API surface (sample)

Convention: `/api/v1/{resource}`, plural nouns, JSON, ISO-8601 dates, decimal as string. All writes
are idempotent on a caller-supplied `Id`. Reads support `?page=&pageSize=` and `?expand=`.

| Method | Path | Maps to (service) | Purpose |
|---|---|---|---|
| GET | `/api/v1/customers/{id}` | `CustomerService.GetCustomer` | Read a customer |
| GET | `/api/v1/customers/{id}/contacts` | `CustomerService.ListContacts` | Contacts for a customer |
| POST | `/api/v1/leads/{id}/convert` | `OpportunityService.ConvertLead` | Promote a lead |
| POST | `/api/v1/opportunities/{id}/advance` | `OpportunityService.AdvanceStage` | Move pipeline stage |
| GET | `/api/v1/sales-orders?status=Open` | `SalesOrderService.GetSalesOrders` | List open orders |
| POST | `/api/v1/sales-orders` | `SalesOrderService.CreateSalesOrder` | Create order + lines |
| POST | `/api/v1/invoices` | `InvoiceService.GenerateInvoice` | Generate + post invoice |
| POST | `/api/v1/inventory/movements` | `InventoryService.RecordMovement` | Record stock movement |
| POST | `/api/v1/discounts/{opportunityId}/approve` | `ApprovalService.ApproveDiscount` | Record discount approval |
| GET | `/api/v1/reports/inventory` | `ReportingService.InventoryReport` | Net movement per item |

**Versioning:** path-based (`/v1`). Additive fields are non-breaking; removing/renaming a field is a
new version. **Errors:** RFC-7807 `application/problem+json` with a stable `type` URI.

## 2. Data-migration contract (staging + validation)

Legacy ERP/CRM imports land in a **staging schema** (`stg.*`) mirroring target columns plus a
`SourceSystem` and `BatchId`. Promotion to `dbo.*` is gated by validation; nothing writes a target
master/transaction table until its batch passes.

```
Source extract → stg.Customer / stg.SalesOrder / ...   (raw, typed loosely)
        │
        ├─ Validate:  required fields, FK resolvability (e.g. SalesOrder.CustomerId
        │             must resolve to a staged/known Customer), decimal/precision,
        │             enum domains (Status, Stage), no duplicate Id within batch.
        │
        ├─ Quarantine: rows that fail land in stg.*_Reject with a Reason. Batch
        │             reports a pass/fail count; a partial batch never half-loads
        │             a referential set (Customer before its SalesOrders).
        │
        └─ Promote:   validated rows MERGE into dbo.* in FK order
                      (Customer → Contact/Opportunity/SalesOrder → SalesOrderLine/Invoice).
```

Contract guarantees: **idempotent** (re-running a `BatchId` is a no-op), **referentially ordered**,
**non-destructive** to existing `dbo.*` rows, and **auditable** (every promote writes an audit row,
§5).

## 3. Extensibility / plugin-SDK-style boundary (interface seam)

Customisation happens behind interface seams, never by editing core services. A plugin is discovered
via DI and invoked at defined extension points. (Original seam; not any vendor SDK.)

```csharp
namespace ErpCrm.Extensibility
{
    // Implemented by core; consumed by plugins. Read-only context, no direct DbContext leak.
    public interface IErpCrmContext
    {
        T? GetEntity<T>(int id) where T : class;
        void Emit(DomainEvent e);          // raise a workflow/automation event (§4)
    }

    // A plugin contributes behaviour at named extension points.
    public interface IErpCrmPlugin
    {
        string Name { get; }
        // Called after a core operation completes; plugin may validate, enrich, or veto-by-throw.
        void OnExtensionPoint(ExtensionPoint point, IErpCrmContext ctx);
    }

    public enum ExtensionPoint
    {
        BeforeSalesOrderCreate, AfterSalesOrderCreate,
        BeforeInvoiceGenerate,  AfterInvoiceGenerate,
        OnOpportunityStageChange, OnDiscountApprovalRequested
    }
}
```

Rules: plugins get a **read-only** context, may **veto** by throwing a typed exception, and may
**not** open their own DB transaction against `dbo.*`. The seam is stable across `/v1` API versions.

## 4. Workflow-automation hooks

Domain events let automations react without polling. Events are emitted by core, fan out to
subscribers (in-process handlers or, optionally, an outbox for external delivery).

| Event | Emitted when | Typical automation |
|---|---|---|
| `OpportunityStageChanged` | `AdvanceStage` runs | notify owner; if `Won`, open a `SalesOrder` draft |
| `DiscountApprovalRequested` | discount exceeds policy threshold | route to approver per role matrix |
| `SalesOrderCreated` | `CreateSalesOrder` succeeds | reserve inventory; pricing recheck vs `PriceList` |
| `InvoiceGenerated` | `GenerateInvoice` posts | trigger dunning timer; sync to ledger |
| `InventoryMovementRecorded` | `RecordMovement` runs | low-stock alert; reorder suggestion |

Delivery contract: **at-least-once**, handlers must be **idempotent** (keyed by entity `Id` +
event type), ordering guaranteed only per-entity.

## 5. Audit

Every state-changing operation and every migration promote writes an **append-only** audit record:
`{ Timestamp, Actor, Operation, Entity, EntityId, BeforeHash, AfterHash, Source }`. Audit is
write-only to callers (no update/delete API), aligning with the platform's append-only audit
posture. Discount approvals additionally capture the resolved `ApproverRole` written to
`dbo.DiscountApproval`, so the *who/what/when* of an approval is reconstructable from audit even
though the *policy* lives in the RBAC matrix (see `expected-answers.md` #5).
