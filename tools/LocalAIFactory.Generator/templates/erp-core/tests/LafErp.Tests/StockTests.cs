using LafErp.Core;
using LafErp.Services;
using Xunit;

namespace LafErp.Tests;

public class StockTests
{
    [Fact]
    public void Receipt_increases_on_hand_quantity()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 40, 60);
        Assert.Equal(40m, h.Stock.CurrentQty(h.Seed.WidgetItemId, h.Seed.WarehouseId));
    }

    [Fact]
    public void Sale_decreases_on_hand_quantity()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 40, 60);
        h.Login("seller", "Sales User", "Accounts Manager");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 15, 100) }); // 1500 > 1000 -> needs a separate approver
        h.Sales.Submit(si.Id);
        h.Login("approver", "Accounts Manager");
        h.Sales.Approve(si.Id);
        Assert.Equal(25m, h.Stock.CurrentQty(h.Seed.WidgetItemId, h.Seed.WarehouseId));
    }

    [Fact]
    public void Outward_movement_cannot_drive_stock_negative()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 5, 60);
        h.Login("seller", "Sales User", "Accounts Manager");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 10, 100) });
        var ex = Assert.Throws<DomainException>(() => h.Sales.Submit(si.Id));
        Assert.Contains("Insufficient stock", ex.Message);
    }

    [Fact]
    public void Moving_average_valuation_is_tracked()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 10, 50);  // value 500
        h.ReceiveStock(h.Seed.WidgetItemId, 10, 70);  // value 700 -> avg (1200/20)=60
        Assert.Equal(60m, h.Stock.ValuationRate(h.Seed.WidgetItemId, h.Seed.WarehouseId));
    }

    [Fact]
    public void Cancelling_a_submitted_invoice_restores_stock()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 40, 60);
        h.Login("seller", "Sales User", "Accounts Manager");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 15, 100) }); // 1500 > 1000 -> needs a separate approver
        h.Sales.Submit(si.Id);
        h.Login("approver", "Accounts Manager");
        h.Sales.Approve(si.Id);
        Assert.Equal(25m, h.Stock.CurrentQty(h.Seed.WidgetItemId, h.Seed.WarehouseId));
        h.Sales.Cancel(si.Id);
        Assert.Equal(40m, h.Stock.CurrentQty(h.Seed.WidgetItemId, h.Seed.WarehouseId));
    }

    [Fact]
    public void Stock_balance_reports_quantity_and_value()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.GadgetItemId, 8, 150);
        var bal = h.Stock.Balance(h.Seed.GadgetItemId, h.Seed.WarehouseId);
        Assert.Equal(8m, bal.Qty);
        Assert.Equal(1200m, bal.Value);
        Assert.Equal(150m, bal.ValuationRate);
    }
}
