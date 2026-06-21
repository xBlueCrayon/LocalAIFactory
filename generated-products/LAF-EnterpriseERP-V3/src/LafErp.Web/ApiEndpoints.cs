using LafErp.Core;
using LafErp.Data;
using LafErp.Services;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Web;

/// <summary>REST-style JSON endpoints for the core entities, reports, workflows and audit.</summary>
public static class ApiEndpoints
{
    public static void Map(WebApplication app)
    {
        var api = app.MapGroup("/api");

        // Domain-rule violations map to 400; everything else bubbles to 500.
        Task<IResult> Guard(Func<IResult> f)
        {
            try { return Task.FromResult(f()); }
            catch (DomainException ex) { return Task.FromResult(Results.BadRequest(new { error = ex.Message })); }
        }

        api.MapGet("/customers", (ErpDbContext db) =>
            Results.Ok(db.Customers.OrderBy(c => c.Code).Select(c => new { c.Id, c.Code, c.Name, c.Email, c.CreditLimit }).ToList()));

        api.MapGet("/suppliers", (ErpDbContext db) =>
            Results.Ok(db.Suppliers.OrderBy(s => s.Code).Select(s => new { s.Id, s.Code, s.Name, s.Email }).ToList()));

        api.MapGet("/items", (ErpDbContext db) =>
            Results.Ok(db.Items.OrderBy(i => i.Code).Select(i => new { i.Id, i.Code, i.Name, i.StandardRate, i.IsStockItem }).ToList()));

        api.MapGet("/warehouses", (ErpDbContext db) =>
            Results.Ok(db.Warehouses.Select(w => new { w.Id, w.Code, w.Name }).ToList()));

        api.MapGet("/sales-invoices", (ErpDbContext db) =>
            Results.Ok(db.SalesInvoices.OrderByDescending(x => x.Id)
                .Select(x => new { x.Id, x.DocNo, x.CustomerId, x.GrandTotal, x.OutstandingAmount, Status = x.Status.ToString() }).ToList()));

        api.MapPost("/sales-invoices", (ErpDbContext db, SalesService sales, RbacService rbac, CreateInvoiceDto dto) => Guard(() =>
        {
            rbac.Demand("SalesInvoice", "create"); // RBAC create gate (submit/approve roles enforced by the workflow)
            var si = sales.CreateInvoice(dto.CompanyId, dto.CustomerId, dto.WarehouseId,
                dto.Lines.Select(l => new InvoiceLineInput(l.ItemId, l.Qty, l.Rate, l.TaxRatePercent)));
            return Results.Created($"/api/sales-invoices/{si.Id}", new { si.Id, si.DocNo, si.GrandTotal });
        }));

        api.MapPost("/sales-invoices/{id:int}/submit", (int id, SalesService sales) => Guard(() => { sales.Submit(id); return Results.Ok(new { id, action = "submitted" }); }));
        api.MapPost("/sales-invoices/{id:int}/approve", (int id, SalesService sales) => Guard(() => { sales.Approve(id); return Results.Ok(new { id, action = "approved" }); }));
        api.MapPost("/sales-invoices/{id:int}/cancel", (int id, SalesService sales) => Guard(() => { sales.Cancel(id); return Results.Ok(new { id, action = "cancelled" }); }));

        api.MapGet("/purchase-invoices", (ErpDbContext db) =>
            Results.Ok(db.PurchaseInvoices.OrderByDescending(x => x.Id)
                .Select(x => new { x.Id, x.DocNo, x.SupplierId, x.GrandTotal, Status = x.Status.ToString() }).ToList()));

        api.MapGet("/payments", (ErpDbContext db) =>
            Results.Ok(db.PaymentEntries.OrderByDescending(x => x.Id)
                .Select(x => new { x.Id, x.DocNo, PartyType = x.PartyType.ToString(), x.PartyId, x.Amount, Status = x.Status.ToString() }).ToList()));

        api.MapGet("/journal-entries", (ErpDbContext db) =>
            Results.Ok(db.JournalEntries.OrderByDescending(x => x.Id)
                .Select(x => new { x.Id, x.DocNo, x.TotalDebit, x.TotalCredit, Status = x.Status.ToString() }).ToList()));

        api.MapGet("/stock-ledger", (ErpDbContext db) =>
            Results.Ok(db.StockLedgerEntries.OrderByDescending(x => x.Id).Take(200)
                .Select(x => new { x.Id, x.ItemId, x.WarehouseId, x.QtyChange, x.QtyAfter, x.ValuationRate, x.VoucherType, x.VoucherNo }).ToList()));

        api.MapGet("/reports/general-ledger", (ErpDbContext db, AccountingService acc, int companyId) =>
            Results.Ok(acc.GeneralLedger(companyId, DateTime.UtcNow.Date.AddYears(-1), DateTime.UtcNow.Date.AddDays(1))));

        api.MapGet("/reports/trial-balance", (AccountingService acc, int companyId) => Results.Ok(acc.TrialBalance(companyId)));
        api.MapGet("/reports/profit-and-loss", (AccountingService acc, int companyId) => { var p = acc.ProfitAndLoss(companyId); return Results.Ok(new { income = p.Income, expense = p.Expense, netProfit = p.NetProfit }); });
        api.MapGet("/reports/balance-sheet", (AccountingService acc, int companyId) => { var b = acc.BalanceSheet(companyId); return Results.Ok(new { assets = b.Assets, liabilities = b.Liabilities, equity = b.Equity, balanced = b.Balanced }); });

        api.MapGet("/reports/stock-balance", (ErpDbContext db, StockService stock) =>
        {
            var combos = db.StockLedgerEntries.Select(s => new { s.ItemId, s.WarehouseId }).Distinct().ToList();
            return Results.Ok(combos.Select(c => stock.Balance(c.ItemId, c.WarehouseId)).ToList());
        });

        api.MapGet("/reports/ar-ap", (AccountingService acc, int companyId) =>
            Results.Ok(new { receivable = acc.AccountsReceivable(companyId), payable = acc.AccountsPayable(companyId) }));

        api.MapGet("/workflows", (ErpDbContext db) =>
            Results.Ok(db.WorkflowInstances.OrderByDescending(x => x.Id).Take(100)
                .Select(x => new { x.Id, x.DocType, x.DocumentId, x.CurrentState, x.SubmittedBy, x.Amount }).ToList()));

        api.MapGet("/audit", (ErpDbContext db) =>
            Results.Ok(db.AuditEvents.OrderByDescending(x => x.Id).Take(200)
                .Select(x => new { x.Id, x.EntityType, x.EntityId, x.Action, x.PerformedBy, x.EventUtc, x.Details }).ToList()));

                api.MapGet("/catalog/billofmaterialses", (ErpDbContext db) => Results.Ok(db.Set<BillOfMaterials>().OrderByDescending(x => x.Id).ToList()));
        api.MapPost("/catalog/billofmaterialses", ([Microsoft.AspNetCore.Mvc.FromServices] CatalogCrudService<BillOfMaterials> svc, [Microsoft.AspNetCore.Mvc.FromBody] BillOfMaterials e) => { try { return Results.Created("/api/catalog/billofmaterialses", svc.Create(e)); } catch (LafErp.Core.DomainException ex) { return Results.BadRequest(new { error = ex.Message }); } });
        api.MapGet("/catalog/workorders", (ErpDbContext db) => Results.Ok(db.Set<WorkOrder>().OrderByDescending(x => x.Id).ToList()));
        api.MapPost("/catalog/workorders", ([Microsoft.AspNetCore.Mvc.FromServices] CatalogCrudService<WorkOrder> svc, [Microsoft.AspNetCore.Mvc.FromBody] WorkOrder e) => { try { return Results.Created("/api/catalog/workorders", svc.Create(e)); } catch (LafErp.Core.DomainException ex) { return Results.BadRequest(new { error = ex.Message }); } });
        api.MapGet("/catalog/qualityinspections", (ErpDbContext db) => Results.Ok(db.Set<QualityInspection>().OrderByDescending(x => x.Id).ToList()));
        api.MapPost("/catalog/qualityinspections", ([Microsoft.AspNetCore.Mvc.FromServices] CatalogCrudService<QualityInspection> svc, [Microsoft.AspNetCore.Mvc.FromBody] QualityInspection e) => { try { return Results.Created("/api/catalog/qualityinspections", svc.Create(e)); } catch (LafErp.Core.DomainException ex) { return Results.BadRequest(new { error = ex.Message }); } });
        api.MapGet("/catalog/employees", (ErpDbContext db) => Results.Ok(db.Set<Employee>().OrderByDescending(x => x.Id).ToList()));
        api.MapPost("/catalog/employees", ([Microsoft.AspNetCore.Mvc.FromServices] CatalogCrudService<Employee> svc, [Microsoft.AspNetCore.Mvc.FromBody] Employee e) => { try { return Results.Created("/api/catalog/employees", svc.Create(e)); } catch (LafErp.Core.DomainException ex) { return Results.BadRequest(new { error = ex.Message }); } });
        api.MapGet("/catalog/salarycomponents", (ErpDbContext db) => Results.Ok(db.Set<SalaryComponent>().OrderByDescending(x => x.Id).ToList()));
        api.MapPost("/catalog/salarycomponents", ([Microsoft.AspNetCore.Mvc.FromServices] CatalogCrudService<SalaryComponent> svc, [Microsoft.AspNetCore.Mvc.FromBody] SalaryComponent e) => { try { return Results.Created("/api/catalog/salarycomponents", svc.Create(e)); } catch (LafErp.Core.DomainException ex) { return Results.BadRequest(new { error = ex.Message }); } });
        api.MapGet("/catalog/posprofiles", (ErpDbContext db) => Results.Ok(db.Set<PosProfile>().OrderByDescending(x => x.Id).ToList()));
        api.MapPost("/catalog/posprofiles", ([Microsoft.AspNetCore.Mvc.FromServices] CatalogCrudService<PosProfile> svc, [Microsoft.AspNetCore.Mvc.FromBody] PosProfile e) => { try { return Results.Created("/api/catalog/posprofiles", svc.Create(e)); } catch (LafErp.Core.DomainException ex) { return Results.BadRequest(new { error = ex.Message }); } });
        api.MapGet("/catalog/webproducts", (ErpDbContext db) => Results.Ok(db.Set<WebProduct>().OrderByDescending(x => x.Id).ToList()));
        api.MapPost("/catalog/webproducts", ([Microsoft.AspNetCore.Mvc.FromServices] CatalogCrudService<WebProduct> svc, [Microsoft.AspNetCore.Mvc.FromBody] WebProduct e) => { try { return Results.Created("/api/catalog/webproducts", svc.Create(e)); } catch (LafErp.Core.DomainException ex) { return Results.BadRequest(new { error = ex.Message }); } });
        api.MapGet("/catalog/maintenanceschedules", (ErpDbContext db) => Results.Ok(db.Set<MaintenanceSchedule>().OrderByDescending(x => x.Id).ToList()));
        api.MapPost("/catalog/maintenanceschedules", ([Microsoft.AspNetCore.Mvc.FromServices] CatalogCrudService<MaintenanceSchedule> svc, [Microsoft.AspNetCore.Mvc.FromBody] MaintenanceSchedule e) => { try { return Results.Created("/api/catalog/maintenanceschedules", svc.Create(e)); } catch (LafErp.Core.DomainException ex) { return Results.BadRequest(new { error = ex.Message }); } });
        api.MapGet("/catalog/customfielddefs", (ErpDbContext db) => Results.Ok(db.Set<CustomFieldDef>().OrderByDescending(x => x.Id).ToList()));
        api.MapPost("/catalog/customfielddefs", ([Microsoft.AspNetCore.Mvc.FromServices] CatalogCrudService<CustomFieldDef> svc, [Microsoft.AspNetCore.Mvc.FromBody] CustomFieldDef e) => { try { return Results.Created("/api/catalog/customfielddefs", svc.Create(e)); } catch (LafErp.Core.DomainException ex) { return Results.BadRequest(new { error = ex.Message }); } });
        api.MapGet("/catalog/notificationrules", (ErpDbContext db) => Results.Ok(db.Set<NotificationRule>().OrderByDescending(x => x.Id).ToList()));
        api.MapPost("/catalog/notificationrules", ([Microsoft.AspNetCore.Mvc.FromServices] CatalogCrudService<NotificationRule> svc, [Microsoft.AspNetCore.Mvc.FromBody] NotificationRule e) => { try { return Results.Created("/api/catalog/notificationrules", svc.Create(e)); } catch (LafErp.Core.DomainException ex) { return Results.BadRequest(new { error = ex.Message }); } });
        api.MapGet("/catalog/customersegments", (ErpDbContext db) => Results.Ok(db.Set<CustomerSegment>().OrderByDescending(x => x.Id).ToList()));
        api.MapPost("/catalog/customersegments", ([Microsoft.AspNetCore.Mvc.FromServices] CatalogCrudService<CustomerSegment> svc, [Microsoft.AspNetCore.Mvc.FromBody] CustomerSegment e) => { try { return Results.Created("/api/catalog/customersegments", svc.Create(e)); } catch (LafErp.Core.DomainException ex) { return Results.BadRequest(new { error = ex.Message }); } });
        api.MapGet("/catalog/productcategories", (ErpDbContext db) => Results.Ok(db.Set<ProductCategory>().OrderByDescending(x => x.Id).ToList()));
        api.MapPost("/catalog/productcategories", ([Microsoft.AspNetCore.Mvc.FromServices] CatalogCrudService<ProductCategory> svc, [Microsoft.AspNetCore.Mvc.FromBody] ProductCategory e) => { try { return Results.Created("/api/catalog/productcategories", svc.Create(e)); } catch (LafErp.Core.DomainException ex) { return Results.BadRequest(new { error = ex.Message }); } });
        api.MapGet("/catalog/employeeroles", (ErpDbContext db) => Results.Ok(db.Set<EmployeeRole>().OrderByDescending(x => x.Id).ToList()));
        api.MapPost("/catalog/employeeroles", ([Microsoft.AspNetCore.Mvc.FromServices] CatalogCrudService<EmployeeRole> svc, [Microsoft.AspNetCore.Mvc.FromBody] EmployeeRole e) => { try { return Results.Created("/api/catalog/employeeroles", svc.Create(e)); } catch (LafErp.Core.DomainException ex) { return Results.BadRequest(new { error = ex.Message }); } });
        api.MapGet("/catalog/marketingcampaigns", (ErpDbContext db) => Results.Ok(db.Set<MarketingCampaign>().OrderByDescending(x => x.Id).ToList()));
        api.MapPost("/catalog/marketingcampaigns", ([Microsoft.AspNetCore.Mvc.FromServices] CatalogCrudService<MarketingCampaign> svc, [Microsoft.AspNetCore.Mvc.FromBody] MarketingCampaign e) => { try { return Results.Created("/api/catalog/marketingcampaigns", svc.Create(e)); } catch (LafErp.Core.DomainException ex) { return Results.BadRequest(new { error = ex.Message }); } });
        api.MapGet("/catalog/vendorcontracts", (ErpDbContext db) => Results.Ok(db.Set<VendorContract>().OrderByDescending(x => x.Id).ToList()));
        api.MapPost("/catalog/vendorcontracts", ([Microsoft.AspNetCore.Mvc.FromServices] CatalogCrudService<VendorContract> svc, [Microsoft.AspNetCore.Mvc.FromBody] VendorContract e) => { try { return Results.Created("/api/catalog/vendorcontracts", svc.Create(e)); } catch (LafErp.Core.DomainException ex) { return Results.BadRequest(new { error = ex.Message }); } });
        api.MapGet("/health"
        , () => Results.Ok(new { status = "ok", product = "LAF Enterprise ERP V3" }));
    }
}

public record CreateInvoiceLineDto(int ItemId, decimal Qty, decimal Rate, decimal TaxRatePercent = 0);
public record CreateInvoiceDto(int CompanyId, int CustomerId, int? WarehouseId, List<CreateInvoiceLineDto> Lines);
