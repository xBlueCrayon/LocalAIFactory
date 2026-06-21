using LafErp.Core;
using LafErp.Services;
using Xunit;

namespace LafErp.Tests;

public class ImmutabilityTests
{
    [Fact]
    public void Submitted_invoice_cannot_be_edited()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 100, 60);
        h.Login("seller", "Sales User", "Accounts Manager");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 5, 100) });
        var lineId = si.Lines.First().Id;
        h.Sales.Submit(si.Id);
        var ex = Assert.Throws<DomainException>(() => h.Sales.EditRate(si.Id, lineId, 200));
        Assert.Contains("immutable", ex.Message);
    }

    [Fact]
    public void Draft_invoice_can_be_edited()
    {
        using var h = new TestHost();
        h.Login("seller", "Sales User");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 5, 100) });
        h.Sales.EditRate(si.Id, si.Lines.First().Id, 120);
        Assert.Equal(600m, h.Db.SalesInvoices.First(x => x.Id == si.Id).NetTotal);
    }

    [Fact]
    public void Cannot_submit_twice()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 100, 60);
        h.Login("seller", "Sales User", "Accounts Manager");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 5, 100) });
        h.Sales.Submit(si.Id);
        Assert.Throws<DomainException>(() => h.Sales.Submit(si.Id));
    }

    [Fact]
    public void Cancel_reverses_gl_to_zero_net()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 100, 60);
        h.Login("seller", "Sales User", "Accounts Manager");
        var arStart = h.Accounting.AccountsReceivable(h.Seed.CompanyId);
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 5, 100) });
        h.Sales.Submit(si.Id);
        h.Sales.Cancel(si.Id);
        Assert.Equal(arStart, h.Accounting.AccountsReceivable(h.Seed.CompanyId));
        var t = h.Accounting.GlTotals(h.Seed.CompanyId);
        Assert.Equal(t.Debit, t.Credit);
    }
}

public class ValidationTests
{
    [Fact]
    public void Invoice_with_no_lines_is_rejected()
    {
        using var h = new TestHost();
        h.Login("seller", "Sales User");
        Assert.Throws<DomainException>(() =>
            h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId, Array.Empty<InvoiceLineInput>()));
    }

    [Fact]
    public void Negative_quantity_is_rejected()
    {
        using var h = new TestHost();
        h.Login("seller", "Sales User");
        Assert.Throws<DomainException>(() =>
            h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
                new[] { new InvoiceLineInput(h.Seed.WidgetItemId, -1, 100) }));
    }

    [Fact]
    public void Unknown_customer_is_rejected()
    {
        using var h = new TestHost();
        h.Login("seller", "Sales User");
        Assert.Throws<DomainException>(() =>
            h.Sales.CreateInvoice(h.Seed.CompanyId, 99999, h.Seed.WarehouseId,
                new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 1, 100) }));
    }

    [Fact]
    public void Payment_must_be_positive()
    {
        using var h = new TestHost("acc", "Accounts User");
        Assert.Throws<DomainException>(() =>
            h.Payment.Create(h.Seed.CompanyId, PartyType.Customer, h.Seed.CustomerId, h.Seed.BankAccountId, 0));
    }

    [Theory]
    [InlineData("SalesInvoice", "submit", "Sales User", true)]
    [InlineData("SalesInvoice", "approve", "Sales User", false)]
    [InlineData("SalesInvoice", "approve", "Accounts Manager", true)]
    [InlineData("PurchaseInvoice", "create", "Purchase User", true)]
    [InlineData("PurchaseInvoice", "create", "Sales User", false)]
    [InlineData("JournalEntry", "submit", "Accounts User", true)]
    public void Rbac_matrix_enforced(string doc, string action, string role, bool allowed)
    {
        using var h = new TestHost("u", role);
        Assert.Equal(allowed, h.Rbac.Can(doc, action));
    }
}
