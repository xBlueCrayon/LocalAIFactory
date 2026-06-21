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

        api.MapGet("/health", () => Results.Ok(new { status = "ok", product = "LAF Enterprise ERP" }));
    }
}

public record CreateInvoiceLineDto(int ItemId, decimal Qty, decimal Rate, decimal TaxRatePercent = 0);
public record CreateInvoiceDto(int CompanyId, int CustomerId, int? WarehouseId, List<CreateInvoiceLineDto> Lines);
