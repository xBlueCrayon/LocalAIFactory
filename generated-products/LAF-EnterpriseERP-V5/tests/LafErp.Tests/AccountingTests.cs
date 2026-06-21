using LafErp.Core;
using LafErp.Services;
using Xunit;

namespace LafErp.Tests;

public class AccountingTests
{
    [Fact]
    public void JournalEntry_must_balance_or_is_rejected()
    {
        using var h = new TestHost("acc", "Accounts User", "Accounts Manager");
        var je = h.Journal.Create(h.Seed.CompanyId, "unbalanced", new[]
        {
            new JournalService.JeLine(h.Seed.CashAccountId, 100, 0),
            new JournalService.JeLine(h.Seed.SalesAccountId, 0, 90)
        });
        h.Login("alice", "Accounts User");
        h.Journal.Submit(je.Id);
        h.Login("bob", "Accounts Manager"); // separate checker so the balance check (not maker/checker) fires
        var ex = Assert.Throws<DomainException>(() => h.Journal.Approve(je.Id));
        Assert.Contains("not balanced", ex.Message);
    }

    [Fact]
    public void Balanced_journal_entry_posts_gl()
    {
        using var h = new TestHost("acc", "Accounts User", "Accounts Manager");
        var je = h.Journal.Create(h.Seed.CompanyId, "capital", new[]
        {
            new JournalService.JeLine(h.Seed.CashAccountId, 5000, 0),
            new JournalService.JeLine(h.Seed.SalesAccountId, 0, 5000)
        });
        h.Journal.Submit(je.Id);
        h.Login("mgr", "Accounts Manager");
        h.Journal.Approve(je.Id);
        var totals = h.Accounting.GlTotals(h.Seed.CompanyId);
        Assert.Equal(totals.Debit, totals.Credit);
        Assert.Equal(5000m, totals.Debit);
    }

    [Fact]
    public void Sales_invoice_posts_balanced_gl_with_tax()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 100, 60);
        h.Login("seller", "Sales User", "Accounts Manager");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 10, 100, 10) });
        Assert.Equal(1000m, si.NetTotal);
        Assert.Equal(100m, si.TaxTotal);
        Assert.Equal(1100m, si.GrandTotal);
        h.Sales.Submit(si.Id);
        h.Login("approver", "Accounts Manager");
        h.Sales.Approve(si.Id);
        var t = h.Accounting.GlTotals(h.Seed.CompanyId);
        Assert.Equal(t.Debit, t.Credit);
    }

    [Fact]
    public void Sales_invoice_increases_accounts_receivable()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 100, 60);
        h.Login("seller", "Sales User", "Accounts Manager");
        var before = h.Accounting.AccountsReceivable(h.Seed.CompanyId);
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 5, 100) }); // 500 <= 1000 threshold -> auto-approves
        h.Sales.Submit(si.Id);
        var after = h.Accounting.AccountsReceivable(h.Seed.CompanyId);
        Assert.Equal(500m, after - before);
    }

    [Fact]
    public void Sales_invoice_posts_cogs_against_stock()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 100, 60);   // cost 60 each
        h.Login("seller", "Sales User", "Accounts Manager");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 10, 100) }); // 1000 == threshold -> auto-approves
        h.Sales.Submit(si.Id);
        var gl = h.Accounting.GeneralLedger(h.Seed.CompanyId, DateTime.UtcNow.Date.AddDays(-1), DateTime.UtcNow.Date.AddDays(1));
        var cogs = gl.Where(r => r.Account == "Cost of Goods Sold").Sum(r => r.Debit);
        Assert.Equal(600m, cogs); // 10 * 60
    }

    [Fact]
    public void Payment_reduces_outstanding_receivable()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 100, 60);
        h.Login("seller", "Sales User", "Accounts Manager");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 10, 100) }); // 1000 -> auto-approves on submit
        h.Sales.Submit(si.Id);

        h.Login("cashier", "Accounts User");
        var pe = h.Payment.Create(h.Seed.CompanyId, PartyType.Customer, h.Seed.CustomerId, h.Seed.BankAccountId, 1000, si.Id);
        h.Payment.Submit(pe.Id);
        h.Login("approver", "Accounts Manager");
        h.Payment.Approve(pe.Id);

        var ar = h.Accounting.AccountsReceivable(h.Seed.CompanyId);
        Assert.Equal(0m, ar);
        var inv = h.Db.SalesInvoices.First(x => x.Id == si.Id);
        Assert.Equal(0m, inv.OutstandingAmount);
    }

    [Fact]
    public void Global_gl_always_balances_after_full_cycle()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 50, 60);     // purchase
        h.Login("seller", "Sales User", "Accounts Manager");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 20, 100, 10) });
        h.Sales.Submit(si.Id);
        h.Login("approver", "Accounts Manager");
        h.Sales.Approve(si.Id);
        var t = h.Accounting.GlTotals(h.Seed.CompanyId);
        Assert.Equal(t.Debit, t.Credit);
        Assert.True(t.Debit > 0);
    }

    [Fact]
    public void Trial_balance_is_balanced()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.GadgetItemId, 30, 150);
        h.Login("seller", "Sales User", "Accounts Manager");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.GadgetItemId, 10, 250) });
        h.Sales.Submit(si.Id);
        h.Login("approver", "Accounts Manager");
        h.Sales.Approve(si.Id);
        var tb = h.Accounting.TrialBalance(h.Seed.CompanyId);
        Assert.Equal(tb.Sum(r => r.Debit), tb.Sum(r => r.Credit));
    }

    [Fact]
    public void Purchase_invoice_increases_payable()
    {
        using var h = new TestHost("buyer", "Purchase User", "Accounts Manager");
        var pi = h.Purchase.CreateInvoice(h.Seed.CompanyId, h.Seed.SupplierId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 100, 60) });
        h.Purchase.Submit(pi.Id);
        h.Login("approver", "Accounts Manager");
        h.Purchase.Approve(pi.Id);
        Assert.Equal(6000m, h.Accounting.AccountsPayable(h.Seed.CompanyId));
    }
}
