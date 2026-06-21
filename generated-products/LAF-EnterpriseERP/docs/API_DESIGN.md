# API Design

LAF Enterprise ERP exposes a JSON REST API built with **ASP.NET Core minimal APIs**, defined in one
place: `ApiEndpoints.Map(app)` (`src/LafErp.Web/ApiEndpoints.cs`). All endpoints live under the
`/api` route group, registered alongside the MVC/Razor UI in the same host.

The endpoint catalogue is in [API_REFERENCE.md](API_REFERENCE.md).

## Conventions

### Route group and shape

- Every endpoint is under `/api` (`app.MapGroup("/api")`).
- **GET** endpoints return projected, lightweight JSON (anonymous objects / record DTOs), not full EF
  entities — only the columns the caller needs. List endpoints order by a natural key and several cap
  results (e.g. stock ledger `Take(200)`, workflows `Take(100)`, audit `Take(200)`).
- **POST** endpoints accept a JSON body bound to a record DTO (e.g. `CreateInvoiceDto`) and return a
  `201 Created` (with a `Location` header) on resource creation, or `200 OK` for lifecycle actions.

### `DomainException` → HTTP 400

Business-rule violations are modelled as `DomainException` in the domain/service layer. Write
endpoints wrap their work in a `Guard` helper:

```csharp
Task<IResult> Guard(Func<IResult> f)
{
    try { return Task.FromResult(f()); }
    catch (DomainException ex) { return Task.FromResult(Results.BadRequest(new { error = ex.Message })); }
}
```

So a domain violation returns **`400 Bad Request`** with `{ "error": "<message>" }`, while unexpected
(non-domain) exceptions bubble up as **500**. This is a deliberate convention: *expected* rule
violations (unbalanced journal, insufficient stock, maker approving own document, unknown customer,
non-positive payment, submitting a non-draft) are 400s, not crashes. The API test
`Insufficient_stock_returns_400_not_500` pins this behaviour.

> Read (GET) endpoints are not wrapped in `Guard`; they are simple projections and are not expected to
> throw domain exceptions.

### Identity / dev-auth

The API runs under the same `ICurrentUser` as the rest of the app. With no `erp_user` / `erp_roles`
cookies, `HttpCurrentUser` resolves to **`admin` with all roles**, so API calls execute with full
privileges by default. The API endpoints themselves do **not** perform an explicit RBAC `Demand`;
maker/checker and posting rules are still enforced because they live inside the workflow/service layer
that the create/submit/approve/cancel endpoints invoke. See [SECURITY_MODEL.md](SECURITY_MODEL.md).

This is **dev-auth**: there is no authentication on the API. Do not expose it to untrusted callers
as-is.

### Reports take `companyId`

The financial report endpoints require a `companyId` query parameter (the GL/Trial Balance/AR-AP
reports are per-company). The seeded demo company is id `1`. The General Ledger endpoint reports a
fixed window of "last 12 months through tomorrow."

## Surfaces

| Surface | Purpose |
| --- | --- |
| `GET /api/{entity}` | List masters and documents (customers, suppliers, items, warehouses, sales/purchase invoices, payments, journal entries, stock ledger) |
| `POST /api/sales-invoices` + `…/submit` `…/approve` `…/cancel` | Create and drive a sales invoice through its lifecycle |
| `GET /api/reports/*` | General Ledger, Trial Balance, Stock Balance, AR/AP totals |
| `GET /api/workflows`, `GET /api/audit` | Inspect workflow instances and the audit trail |
| `GET /api/health` | Liveness/identity probe (`{ status: "ok", product: "LAF Enterprise ERP" }`) |

## Notes / limitations

- **Write coverage is intentionally narrow.** Only **sales invoices** have create + lifecycle POST
  endpoints. Purchase invoices, payments, and journal entries are **read-only over the API** in this
  build; they are created/driven through the service layer (and the startup demo data). Other masters
  have no create endpoints over the API.
- There is no pagination, filtering DSL, or versioning on the API yet; list caps are fixed `Take(n)`.
