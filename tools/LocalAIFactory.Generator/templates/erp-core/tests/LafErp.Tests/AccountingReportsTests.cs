using LafErp.Core;
using LafErp.Services;
using Xunit;

namespace LafErp.Tests;

public class AccountingReportsTests
{
    [Fact]
    public void Profit_and_loss_reflects_revenue_minus_cogs()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 100, 60);          // cost 60 each
        h.Login("seller", "Sales User", "Accounts Manager");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 10, 100) }); // 1000 -> auto-approve
        h.Sales.Submit(si.Id);
        var pl = h.Accounting.ProfitAndLoss(h.Seed.CompanyId);
        Assert.Equal(1000m, pl.Income);   // Sales
        Assert.Equal(600m, pl.Expense);   // COGS 10 * 60
        Assert.Equal(400m, pl.NetProfit);
    }

    [Fact]
    public void Balance_sheet_balances_after_a_cycle()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 50, 60);
        h.Login("seller", "Sales User", "Accounts Manager");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 8, 100) });
        h.Sales.Submit(si.Id);
        var bs = h.Accounting.BalanceSheet(h.Seed.CompanyId);
        Assert.True(bs.Balanced, $"assets {bs.Assets} != liab+equity+retained");
    }
}
