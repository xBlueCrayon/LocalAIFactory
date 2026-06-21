using LafErp.Core;
using LafErp.Services;
using Xunit;

namespace LafErp.Tests;

public class WorkflowTests
{
    private static JournalEntry NewJe(TestHost h) => h.Journal.Create(h.Seed.CompanyId, "test", new[]
    {
        new JournalService.JeLine(h.Seed.CashAccountId, 200, 0),
        new JournalService.JeLine(h.Seed.SalesAccountId, 0, 200)
    });

    [Fact]
    public void Maker_cannot_approve_own_document()
    {
        using var h = new TestHost("bob", "Accounts User", "Accounts Manager");
        var je = NewJe(h);
        h.Journal.Submit(je.Id); // threshold 0 -> pending approval
        // bob is also an approver, but is the maker -> must be blocked
        var ex = Assert.Throws<DomainException>(() => h.Journal.Approve(je.Id));
        Assert.Contains("Maker/checker", ex.Message);
    }

    [Fact]
    public void Separate_checker_can_approve()
    {
        using var h = new TestHost("alice", "Accounts User");
        var je = NewJe(h);
        h.Journal.Submit(je.Id);
        h.Login("bob", "Accounts Manager");
        h.Journal.Approve(je.Id);
        Assert.Equal(DocStatus.Submitted, h.Db.JournalEntries.First(x => x.Id == je.Id).Status);
    }

    [Fact]
    public void Amount_within_threshold_auto_approves_on_submit()
    {
        using var h = new TestHost("seller", "Sales User");
        h.ReceiveStock(h.Seed.WidgetItemId, 100, 60);
        h.Login("seller", "Sales User");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 5, 100) }); // 500 <= 1000 threshold
        h.Sales.Submit(si.Id);
        Assert.Equal(DocStatus.Submitted, h.Db.SalesInvoices.First(x => x.Id == si.Id).Status);
    }

    [Fact]
    public void Amount_over_threshold_requires_separate_approver()
    {
        using var h = new TestHost("seller", "Sales User");
        h.ReceiveStock(h.Seed.WidgetItemId, 100, 60);
        h.Login("seller", "Sales User");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 50, 100) }); // 5000 > 1000
        h.Sales.Submit(si.Id);
        Assert.Equal(DocStatus.Draft, h.Db.SalesInvoices.First(x => x.Id == si.Id).Status); // not yet posted
        h.Login("approver", "Accounts Manager");
        h.Sales.Approve(si.Id);
        Assert.Equal(DocStatus.Submitted, h.Db.SalesInvoices.First(x => x.Id == si.Id).Status);
    }

    [Fact]
    public void Reject_requires_a_reason()
    {
        using var h = new TestHost("alice", "Accounts User");
        var je = NewJe(h);
        h.Journal.Submit(je.Id);
        h.Login("bob", "Accounts Manager");
        Assert.Throws<DomainException>(() => h.Journal.Reject(je.Id, "  "));
    }

    [Fact]
    public void Reject_returns_document_to_draft_and_records_reason()
    {
        using var h = new TestHost("alice", "Accounts User");
        var je = NewJe(h);
        h.Journal.Submit(je.Id);
        h.Login("bob", "Accounts Manager");
        h.Journal.Reject(je.Id, "wrong cost center");
        Assert.Equal(DocStatus.Draft, h.Db.JournalEntries.First(x => x.Id == je.Id).Status);
        Assert.Contains(h.Db.WorkflowApprovals, a => a.Action == WorkflowAction.Reject && a.Reason == "wrong cost center");
    }

    [Fact]
    public void Approver_without_role_is_blocked()
    {
        using var h = new TestHost("alice", "Accounts User");
        var je = NewJe(h);
        h.Journal.Submit(je.Id);
        h.Login("mallory", "Sales User"); // not an approver
        Assert.Throws<DomainException>(() => h.Journal.Approve(je.Id));
    }

    [Fact]
    public void Submitter_without_submit_role_is_blocked()
    {
        using var h = new TestHost("nobody", "Stock User"); // no submit role for JournalEntry
        var je = NewJe(h);
        Assert.Throws<DomainException>(() => h.Journal.Submit(je.Id));
    }

    [Fact]
    public void Every_transition_records_an_audit_event()
    {
        using var h = new TestHost("alice", "Accounts User");
        var je = NewJe(h);
        h.Journal.Submit(je.Id);
        h.Login("bob", "Accounts Manager");
        h.Journal.Approve(je.Id);
        Assert.Contains(h.Db.AuditEvents, a => a.EntityType == "JournalEntry" && a.Action == "Submit");
        Assert.Contains(h.Db.AuditEvents, a => a.EntityType == "JournalEntry" && a.Action == "Approve");
    }
}
