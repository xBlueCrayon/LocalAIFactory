# Code Review Report — LAF Enterprise ERP V2 (LAF-generated)

**Subject:** `generated-products/LAF-EnterpriseERP-LAFGenerated`
**Nature of the artifact:** 100%-generator-emitted .NET 10 ERP. 66 engine files came from the
LocalAIFactory ERP-knowledge templates (`tools/LocalAIFactory.Generator/templates/erp-core`); 4
catalog files were generated from a governed `qwen2.5-coder:14b` proposal. The generator never
hand-edits the product — any product bug is fixed in the template/generator and re-emitted.

---

## 1. Layered architecture

```
Core  ──►  Data  ──►  Services  ──►  Web
                              ▲
                            Tests (references all)
```

- **`LafErp.Core`** — POCO entities, enums, `EntityBase`/`DocumentBase`, `DomainException`. No
  framework dependencies beyond BCL.
- **`LafErp.Data`** — `ErpDbContext` with model configuration (indexes, soft-delete filters,
  decimal precision, provider-conditional `RowVersion`).
- **`LafErp.Services`** — all business logic. Each service takes its dependencies via constructor
  injection and is registered scoped in `ServiceRegistration.AddErpServices`.
- **`LafErp.Web`** — thin MVC controllers + minimal-API endpoints, dev-auth, DI composition root.

No project-reference cycles. The layering is respected in practice, not just on paper.

---

## 2. Controllers delegate to services (no business logic in controllers)

Verified by reading every controller:

- **`HomeController`** holds only read queries: `Count()` aggregates for the dashboard and ordered
  list projections for each page. The single piece of "logic" is `CompanyId` (first company id) and
  stock-balance fan-out via `StockService.Balance`. No posting, no rule enforcement.
- **`CatalogController`** builds a `List<CatalogRow>` of `(EntityType, Count)` and returns the view.
- The `Login` POST writes the dev-auth cookies and redirects — no domain logic.

All posting, validation, workflow and RBAC enforcement lives in services
(`SalesService`, `WorkflowService`, `AccountingService`, `RbacService`, …). Controllers and API
endpoints are delegation-only. **This is correct and consistent across the codebase.**

---

## 3. API endpoint style

`ApiEndpoints.Map` registers minimal-API routes under `/api`. Three patterns worth noting:

- **Domain-error mapping.** A local `Guard(Func<IResult>)` wraps write endpoints and converts
  `DomainException` → `Results.BadRequest`. Reads don't need it.
- **Response shaping.** List endpoints `Select` into anonymous DTOs (e.g. `/api/customers` returns
  `{Id, Code, Name, Email, CreditLimit}`), so full entities and large/sensitive columns are not
  serialized.
- **Generated catalog endpoints use `[FromServices]` / `[FromBody]`.** Each accepted catalog entity
  gets a `GET /api/catalog/{plural}` and a `POST /api/catalog/{plural}` whose handler is
  `([Microsoft.AspNetCore.Mvc.FromServices] CatalogCrudService<T> svc,
  [Microsoft.AspNetCore.Mvc.FromBody] T e) => …`. The explicit `[FromServices]`/`[FromBody]`
  attributes are a **deliberate generator fix**: without them the minimal-API parameter binder
  cannot infer that the open-generic service comes from DI and the body is the entity, and the
  route fails to compile/bind. This is documented in
  `docs/reports/LOCALAIFACTORY_IMPROVEMENTS_FROM_SELF_GENERATED_ERP.md`.

---

## 4. Secrets & configuration

- **No hardcoded secrets.** No connection strings with credentials, API keys, or passwords are
  embedded in source. `Program.cs` reads the `Default` connection string from configuration and
  falls back to a local SQLite file when absent.
- Dev-auth identity is a cookie, not a secret store (this is an auth *gap*, not a leaked secret — see
  `SECURITY_REVIEW_REPORT.md`).

---

## 5. Generation provenance in-code

The generator emits a self-verifying test, `GenerationProvenanceTests`, which reflects over the
product assembly and asserts the LLM-proposed catalog types (`CustomerSegment`, `PaymentTerm`,
`TaxCode`) exist. The attribution manifest
(`benchmarks/results/laf-erp-v2-generation-attribution.json`) records each file's class
(`LAF_GENERATED` / `LOCAL_LLM_PROPOSAL_USED`) and the autonomy percentage.

---

## 6. Findings

| # | Severity | Area | Finding | Status |
|---|---|---|---|---|
| F1 | **High** | DI / generation | In `ServiceRegistration.cs` the catalog registration `services.AddScoped(typeof(CatalogCrudService<>));` is emitted **after** `return services;`, so it is **unreachable dead code**. The `__CATALOG_SERVICES__` marker sits after the return in the template. Consequence: a live `POST /api/catalog/*` cannot resolve `CatalogCrudService<T>` and would fail at runtime. Masked today because no test exercises the catalog POST (xUnit news the service directly; Playwright only GETs). | **Open** — generator/template fix needed (move the marker before `return`). Honestly disclosed. |
| F2 | Low | Tests | The catalog POST path has **no test coverage**, which is why F1 went unnoticed. | Open |
| F3 | Low | Artifacts | `playwright/playwright-report.json` is stale — its paths point at the V1 output dir (`generated-products/LAF-EnterpriseERP`) and it reports 12 passed. `test-results/.last-run.json` is `passed`. Regenerate against this tree. | Open |
| F4 | Info | Transactions | Services call `SaveChanges()` directly with no explicit transaction wrapping a multi-step submit (see `PRODUCTION_GRADE_REVIEW.md` §3). | By design (pilot) |
| F5 | Info | Persistence | Schema via `EnsureCreated()`; no EF migrations. | By design (pilot) |
| F6 | Info | Domain | `AccountingService.TrialBalance`/`AR`/`AP`/`GlTotals` deliberately pull a projection then aggregate **in memory** (`AsEnumerable()`) so decimal sums are exact on every provider. Intentional, documented in code. | Accepted |
| F7 | Info | EF | A `PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning` is explicitly *ignored* in `OnConfiguring`, with a comment explaining the soft-delete-filter-vs-document-reference rationale. Acknowledged rather than suppressed silently. | Accepted |

No correctness defects were found in the accounting, stock, or workflow engines; their behavior is
covered by 82 passing xUnit cases.

---

## 7. Style & consistency

- Naming is consistent (`*Service`, `*Entry`, `Post*`, `Move*`).
- XML-doc summaries on every service and the non-obvious domain types explain *intent and the rule
  being enforced*, not just the mechanics.
- `DomainException` is used uniformly for rule violations and is the single hook the API maps to 400.
- The only stylistic blemish is the F1 dead-code line and a minor formatting artifact around it
  (the appended registration on the same physical line as `return services;`), both originating from
  template marker placement.

---

## 8. Summary

The generated code is **clean, layered, and idiomatic**: controllers delegate, services own the
rules, no secrets are committed, and provenance is self-tested. One real generation defect (F1, the
unreachable catalog-service registration) is present and honestly recorded here rather than papered
over; it is fixable in the template, consistent with the generator's "fix the template, re-emit"
discipline. Everything else is either accepted-by-design pilot scope or a stale build artifact.
</content>
