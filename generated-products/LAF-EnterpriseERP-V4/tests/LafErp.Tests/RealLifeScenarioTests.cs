using LafErp.Core;
using LafErp.Services;
using Xunit;

namespace LafErp.Tests;

/// <summary>
/// End-to-end trading-company scenario: buy stock, sell to a customer with maker/checker approval,
/// receive payment, and verify AP, AR, GL balance, stock, audit, and workflow history.
/// </summary>
public class RealLifeScenarioTests
{
    [Fact]
    public void Trading_company_buy_sell_pay_cycle()
    {
        using var h = new TestHost();

        // --- Purchase: receive 100 widgets @ 60 (AP 6000, stock 100) ---
        h.ReceiveStock(h.Seed.WidgetItemId, 100, 60);
        Assert.Equal(100m, h.Stock.CurrentQty(h.Seed.WidgetItemId, h.Seed.WarehouseId));
        Assert.Equal(6000m, h.Accounting.AccountsPayable(h.Seed.CompanyId));

        // --- Sell 20 widgets @ 100 = 2000 (> 1000 threshold -> needs a separate checker) ---
        h.Login("alice", "Sales User");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 20, 100) });
        h.Sales.Submit(si.Id);
        Assert.Equal(DocStatus.Draft, h.Db.SalesInvoices.First(x => x.Id == si.Id).Status); // pending, not posted

        // maker cannot approve own
        Assert.Throws<DomainException>(() => h.Sales.Approve(si.Id));

        // separate checker approves -> posts GL + stock issue
        h.Login("bob", "Accounts Manager");
        h.Sales.Approve(si.Id);
        Assert.Equal(DocStatus.Submitted, h.Db.SalesInvoices.First(x => x.Id == si.Id).Status);
        Assert.Equal(80m, h.Stock.CurrentQty(h.Seed.WidgetItemId, h.Seed.WarehouseId));
        Assert.Equal(2000m, h.Accounting.AccountsReceivable(h.Seed.CompanyId));

        // --- Receive payment of 2000 (> 500 -> needs a separate approver) ---
        h.Login("cathy", "Accounts User");
        var pe = h.Payment.Create(h.Seed.CompanyId, PartyType.Customer, h.Seed.CustomerId, h.Seed.BankAccountId, 2000, si.Id);
        h.Payment.Submit(pe.Id);
        h.Login("bob", "Accounts Manager");
        h.Payment.Approve(pe.Id);
        Assert.Equal(0m, h.Accounting.AccountsReceivable(h.Seed.CompanyId));

        // --- Integrity: GL balances globally; audit + workflow history present ---
        var gl = h.Accounting.GlTotals(h.Seed.CompanyId);
        Assert.Equal(gl.Debit, gl.Credit);
        Assert.True(gl.Debit > 0);
        Assert.Contains(h.Db.AuditEvents, a => a.EntityType == "SalesInvoice" && a.Action == "Approve");
        Assert.Contains(h.Db.WorkflowInstances, w => w.DocType == "SalesInvoice");
        Assert.True(h.Db.AuditEvents.Count() >= 5);
    }
}
