# API Reference

Base path: `/api`. All responses are JSON. Default identity is dev-auth `admin` (all roles) unless
`erp_user`/`erp_roles` cookies are set. Business-rule violations return **`400`** with
`{ "error": "<message>" }`; see [API_DESIGN.md](API_DESIGN.md).

Ports: `http://localhost:5027` under `dotnet run` (Development profile); `http://localhost:5080`
under the Playwright harness.

---

## Health

### `GET /api/health`
```json
{ "status": "ok", "product": "LAF Enterprise ERP" }
```

---

## Masters (read)

### `GET /api/customers`
Ordered by `Code`.
```json
[ { "id": 1, "code": "CUST-0001", "name": "Acme Industries", "email": "ap@acme.test", "creditLimit": 100000 } ]
```

### `GET /api/suppliers`
```json
[ { "id": 1, "code": "SUPP-0001", "name": "Globex Supplies", "email": "ar@globex.test" } ]
```

### `GET /api/items`
```json
[ { "id": 1, "code": "WIDGET", "name": "Widget", "standardRate": 100, "isStockItem": true } ]
```

### `GET /api/warehouses`
```json
[ { "id": 1, "code": "MAIN", "name": "Main Store" } ]
```

---

## Documents (read)

### `GET /api/sales-invoices`
Newest first.
```json
[ { "id": 1, "docNo": "SALE-00001", "customerId": 1, "grandTotal": 550, "outstandingAmount": 350, "status": "Submitted" } ]
```

### `GET /api/purchase-invoices`
```json
[ { "id": 1, "docNo": "PURC-00001", "supplierId": 1, "grandTotal": 600, "status": "Submitted" } ]
```

### `GET /api/payments`
```json
[ { "id": 1, "docNo": "PAYM-00001", "partyType": "Customer", "partyId": 1, "amount": 200, "status": "Submitted" } ]
```

### `GET /api/journal-entries`
```json
[ { "id": 1, "docNo": "JOUR-00001", "totalDebit": 5000, "totalCredit": 5000, "status": "Submitted" } ]
```

### `GET /api/stock-ledger`
Newest first, capped at 200.
```json
[ { "id": 1, "itemId": 1, "warehouseId": 1, "qtyChange": 10, "qtyAfter": 10, "valuationRate": 60, "voucherType": "PurchaseInvoice", "voucherNo": "PURC-00001" } ]
```

---

## Sales invoice lifecycle (write)

### `POST /api/sales-invoices`
Request:
```json
{
  "companyId": 1,
  "customerId": 1,
  "warehouseId": 1,
  "lines": [ { "itemId": 1, "qty": 1, "rate": 100, "taxRatePercent": 0 } ]
}
```
Response `201 Created` (`Location: /api/sales-invoices/{id}`):
```json
{ "id": 5, "docNo": "SALE-00005", "grandTotal": 100 }
```
Validation errors (no lines, non-positive qty, negative rate, unknown customer) → `400`.

### `POST /api/sales-invoices/{id}/submit`
Submits the draft. If the grand total is within the workflow threshold it auto-approves and posts;
otherwise it moves to `PendingApproval`. Posting-time domain rules (e.g. insufficient stock) → `400`.
```json
{ "id": 5, "action": "submitted" }
```

### `POST /api/sales-invoices/{id}/approve`
Approves a pending invoice (subject to approver-role and maker≠checker rules in the service layer).
```json
{ "id": 5, "action": "approved" }
```

### `POST /api/sales-invoices/{id}/cancel`
Reverses GL + stock and sets the invoice to `Cancelled` (only valid on a `Submitted` invoice).
```json
{ "id": 5, "action": "cancelled" }
```

> Purchase invoices, payments, and journal entries are **read-only over the API** in this build.

---

## Reports (read)

### `GET /api/reports/general-ledger?companyId=1`
GL rows over the last 12 months (through tomorrow):
```json
[ { "postingDate": "2026-06-21T00:00:00", "account": "Debtors", "debit": 550, "credit": 0, "voucherType": "SalesInvoice", "voucherNo": "SINV-00001", "party": "Customer:1" } ]
```

### `GET /api/reports/trial-balance?companyId=1`
Per-account debit/credit totals (sum of debits equals sum of credits):
```json
[ { "account": "Cash", "debit": 0, "credit": 0 }, { "account": "Debtors", "debit": 550, "credit": 200 } ]
```

### `GET /api/reports/stock-balance`
Current quantity/value/valuation per item+warehouse combination present in the ledger:
```json
[ { "itemId": 1, "warehouseId": 1, "qty": 5, "value": 300, "valuationRate": 60 } ]
```

### `GET /api/reports/ar-ap?companyId=1`
```json
{ "receivable": 350, "payable": 600 }
```

---

## Governance (read)

### `GET /api/workflows`
Latest 100 workflow instances:
```json
[ { "id": 1, "docType": "SalesInvoice", "documentId": 1, "currentState": "Approved", "submittedBy": "admin", "amount": 550 } ]
```

### `GET /api/audit`
Latest 200 audit events:
```json
[ { "id": 1, "entityType": "SalesInvoice", "entityId": 1, "action": "Submit", "performedBy": "admin", "eventUtc": "2026-06-21T10:00:00Z", "details": "amount=550; autoApprove=True" } ]
```

> Field values above are illustrative (shapes are exact; document numbers/amounts depend on seeded
> demo data). `docNo` prefixes come from `NumberingService`: the demo seed does **not** pre-create
> `NumberingSeries` rows, so a series is auto-created on first use with a prefix of the **first four
> letters of the doctype, uppercased, + `-`**. Therefore `SalesInvoice` → `SALE-`,
> `PurchaseInvoice` → `PURC-`, `PaymentEntry` → `PAYM-`, `JournalEntry` → `JOUR-`, with a 5-digit
> zero-padded number (e.g. `SALE-00001`).
