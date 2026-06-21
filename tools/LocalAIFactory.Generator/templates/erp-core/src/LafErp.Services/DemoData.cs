using LafErp.Core;
using LafErp.Data;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Services;

/// <summary>
/// Posts a few small (under-threshold, so single-user auto-approve) demo transactions so the dashboard,
/// ledgers, stock balance, workflow inbox and audit log show live data after first startup. Idempotent.
/// </summary>
public static class DemoData
{
    public static void Post(ErpDbContext db, SeedResult seed, PurchaseService purchase, SalesService sales, PaymentService payment)
    {
        if (db.PurchaseInvoices.Any()) return; // already posted

        var widgetBuy = purchase.CreateInvoice(seed.CompanyId, seed.SupplierId, seed.WarehouseId,
            new[] { new InvoiceLineInput(seed.WidgetItemId, 10, 60) });   // 600 < 1000 -> auto
        purchase.Submit(widgetBuy.Id);

        var gadgetBuy = purchase.CreateInvoice(seed.CompanyId, seed.SupplierId, seed.WarehouseId,
            new[] { new InvoiceLineInput(seed.GadgetItemId, 4, 150) });    // 600 < 1000 -> auto
        purchase.Submit(gadgetBuy.Id);

        var sale = sales.CreateInvoice(seed.CompanyId, seed.CustomerId, seed.WarehouseId,
            new[] { new InvoiceLineInput(seed.WidgetItemId, 5, 100, 10) }); // 500 net < 1000 -> auto
        sales.Submit(sale.Id);

        var pay = payment.Create(seed.CompanyId, PartyType.Customer, seed.CustomerId, seed.BankAccountId, 200, sale.Id); // 200 < 500 -> auto
        payment.Submit(pay.Id);
    }
}
