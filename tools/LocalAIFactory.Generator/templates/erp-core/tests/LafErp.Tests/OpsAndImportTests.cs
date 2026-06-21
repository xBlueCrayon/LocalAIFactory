using LafErp.Core;
using LafErp.Services;
using Xunit;

namespace LafErp.Tests;

public class OpsTests
{
    [Fact]
    public void Lead_converts_to_customer()
    {
        using var h = new TestHost();
        var lead = h.Crm.CreateLead("Initech", "Initech LLC", "buy@initech.test");
        var cust = h.Crm.ConvertLead(lead.Id, h.Seed.DebtorsAccountId, "CUST-NEW");
        Assert.Equal(LeadStatus.Converted, h.Db.Leads.First(l => l.Id == lead.Id).Status);
        Assert.Equal("Initech", cust.Name);
    }

    [Fact]
    public void Lead_cannot_convert_twice()
    {
        using var h = new TestHost();
        var lead = h.Crm.CreateLead("Initech", null, null);
        h.Crm.ConvertLead(lead.Id, h.Seed.DebtorsAccountId, "CUST-A");
        Assert.Throws<DomainException>(() => h.Crm.ConvertLead(lead.Id, h.Seed.DebtorsAccountId, "CUST-B"));
    }

    [Fact]
    public void Task_requiring_approval_cannot_complete_until_approved()
    {
        using var h = new TestHost();
        var p = h.Projects.Create("Implementation");
        var t = h.Projects.AddTask(p.Id, "Go live", requiresApproval: true);
        Assert.Throws<DomainException>(() => h.Projects.CompleteTask(t.Id, "alice"));
        h.Projects.ApproveTask(t.Id);
        h.Projects.CompleteTask(t.Id, "alice");
        Assert.Equal(LafErp.Core.TaskStatus.Completed, h.Db.ProjectTasks.First(x => x.Id == t.Id).Status);
    }

    [Fact]
    public void Support_ticket_escalates_and_resolves()
    {
        using var h = new TestHost();
        var t = h.Support.Create("Cannot login", TicketPriority.High);
        h.Support.Escalate(t.Id, "tier2");
        Assert.Equal(TicketStatus.Escalated, h.Db.SupportTickets.First(x => x.Id == t.Id).Status);
        h.Support.Resolve(t.Id, "Reset credentials");
        Assert.Equal(TicketStatus.Resolved, h.Db.SupportTickets.First(x => x.Id == t.Id).Status);
    }

    [Fact]
    public void Support_resolution_requires_text()
    {
        using var h = new TestHost();
        var t = h.Support.Create("Issue");
        Assert.Throws<DomainException>(() => h.Support.Resolve(t.Id, ""));
    }

    [Fact]
    public void Asset_can_be_scheduled_for_maintenance()
    {
        using var h = new TestHost();
        var a = h.Assets.Create(h.Seed.CompanyId, "Forklift", 25000);
        h.Assets.ScheduleMaintenance(a.Id, DateTime.UtcNow.Date.AddDays(30));
        Assert.Equal(AssetStatus.UnderMaintenance, h.Db.Assets.First(x => x.Id == a.Id).Status);
    }
}

public class AuditAndImportTests
{
    [Fact]
    public void Create_records_audit_event()
    {
        using var h = new TestHost();
        h.Login("seller", "Sales User");
        h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 1, 100) });
        Assert.Contains(h.Db.AuditEvents, a => a.EntityType == "SalesInvoice" && a.Action == "Create" && a.PerformedBy == "seller");
    }

    [Fact]
    public void Csv_import_reports_good_and_bad_rows()
    {
        using var h = new TestHost();
        var csv = "Code,Name,Email,ReceivableAccountId\n" +
                  $"IMP-1,Imported One,a@x.test,{h.Seed.DebtorsAccountId}\n" +
                  $"IMP-2,Imported Two,b@x.test,{h.Seed.DebtorsAccountId}\n" +
                  ",Missing Code,c@x.test,1\n";
        var batch = h.Import.ImportCustomers("customers.csv", csv);
        Assert.Equal(3, batch.TotalRows);
        Assert.Equal(2, batch.ImportedRows);
        Assert.Equal(1, batch.FailedRows);
        Assert.NotNull(batch.Errors);
    }

    [Fact]
    public void Csv_import_rejects_duplicate_codes()
    {
        using var h = new TestHost();
        var csv = "Code,Name,Email,ReceivableAccountId\n" +
                  $"CUST-0001,Dup,d@x.test,{h.Seed.DebtorsAccountId}\n"; // CUST-0001 already seeded
        var batch = h.Import.ImportCustomers("dup.csv", csv);
        Assert.Equal(0, batch.ImportedRows);
        Assert.Equal(1, batch.FailedRows);
    }

    [Fact]
    public void Export_customers_round_trips_header()
    {
        using var h = new TestHost();
        var csv = h.Import.ExportCustomersCsv();
        Assert.StartsWith("Code,Name,Email,ReceivableAccountId", csv);
        Assert.Contains("CUST-0001", csv);
    }

    [Fact]
    public void Seed_is_internally_consistent()
    {
        using var h = new TestHost();
        Assert.True(h.Seed.CompanyId > 0);
        Assert.Equal(6, h.Db.AppRoles.Count());
        Assert.True(h.Db.Accounts.Count() >= 10);
        Assert.Equal(4, h.Db.WorkflowDefinitions.Count());
    }
}
