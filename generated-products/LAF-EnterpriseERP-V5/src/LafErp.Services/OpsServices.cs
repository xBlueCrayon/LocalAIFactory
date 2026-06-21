using LafErp.Core;
using LafErp.Data;

namespace LafErp.Services;

/// <summary>CRM: leads and opportunities, with lead→customer conversion.</summary>
public class CrmService
{
    private readonly ErpDbContext _db;
    private readonly AuditService _audit;
    public CrmService(ErpDbContext db, AuditService audit) { _db = db; _audit = audit; }

    public Lead CreateLead(string name, string? company, string? email)
    {
        var lead = new Lead { Name = name, Company = company, Email = email };
        _db.Leads.Add(lead);
        _audit.Record("Lead", 0, "Create", name);
        _db.SaveChanges();
        return lead;
    }

    public Customer ConvertLead(int leadId, int receivableAccountId, string code)
    {
        var lead = _db.Leads.First(l => l.Id == leadId);
        if (lead.Status == LeadStatus.Converted) throw new DomainException("Lead already converted.");
        var customer = new Customer { Code = code, Name = lead.Name, Email = lead.Email, ReceivableAccountId = receivableAccountId };
        _db.Customers.Add(customer);
        _db.SaveChanges();
        lead.Status = LeadStatus.Converted;
        lead.ConvertedCustomerId = customer.Id;
        _audit.Record("Lead", lead.Id, "Convert", $"-> Customer {code}");
        _db.SaveChanges();
        return customer;
    }

    public Opportunity CreateOpportunity(int? customerId, string title, decimal value)
    {
        var opp = new Opportunity { CustomerId = customerId, Title = title, EstimatedValue = value };
        _db.Opportunities.Add(opp);
        _db.SaveChanges();
        return opp;
    }
}

public class ProjectService
{
    private readonly ErpDbContext _db;
    private readonly AuditService _audit;
    public ProjectService(ErpDbContext db, AuditService audit) { _db = db; _audit = audit; }

    public Project Create(string name, int? customerId = null)
    {
        var p = new Project { Name = name, CustomerId = customerId };
        _db.Projects.Add(p);
        _audit.Record("Project", 0, "Create", name);
        _db.SaveChanges();
        return p;
    }

    public ProjectTask AddTask(int projectId, string subject, bool requiresApproval = false)
    {
        var t = new ProjectTask { ProjectId = projectId, Subject = subject, RequiresApproval = requiresApproval };
        _db.ProjectTasks.Add(t);
        _db.SaveChanges();
        return t;
    }

    public void CompleteTask(int taskId, string approver)
    {
        var t = _db.ProjectTasks.First(x => x.Id == taskId);
        if (t.RequiresApproval && !t.IsApproved)
            throw new DomainException("Task requires approval before completion.");
        t.Status = LafErp.Core.TaskStatus.Completed;
        _audit.Record("ProjectTask", t.Id, "Complete", null);
        _db.SaveChanges();
    }

    public void ApproveTask(int taskId)
    {
        var t = _db.ProjectTasks.First(x => x.Id == taskId);
        t.IsApproved = true;
        _audit.Record("ProjectTask", t.Id, "Approve", null);
        _db.SaveChanges();
    }
}

public class SupportService
{
    private readonly ErpDbContext _db;
    private readonly AuditService _audit;
    public SupportService(ErpDbContext db, AuditService audit) { _db = db; _audit = audit; }

    public SupportTicket Create(string subject, TicketPriority priority = TicketPriority.Medium, int? customerId = null)
    {
        var t = new SupportTicket { Subject = subject, Priority = priority, CustomerId = customerId };
        _db.SupportTickets.Add(t);
        _audit.Record("SupportTicket", 0, "Create", subject);
        _db.SaveChanges();
        return t;
    }

    public void Escalate(int ticketId, string assignTo)
    {
        var t = _db.SupportTickets.First(x => x.Id == ticketId);
        t.Status = TicketStatus.Escalated;
        t.AssignedTo = assignTo;
        t.EscalatedUtc = DateTime.UtcNow;
        _audit.Record("SupportTicket", t.Id, "Escalate", $"-> {assignTo}");
        _db.SaveChanges();
    }

    public void Resolve(int ticketId, string resolution)
    {
        var t = _db.SupportTickets.First(x => x.Id == ticketId);
        if (string.IsNullOrWhiteSpace(resolution)) throw new DomainException("Resolution text is required.");
        t.Status = TicketStatus.Resolved;
        t.Resolution = resolution;
        _audit.Record("SupportTicket", t.Id, "Resolve", null);
        _db.SaveChanges();
    }
}

public class AssetService
{
    private readonly ErpDbContext _db;
    private readonly AuditService _audit;
    public AssetService(ErpDbContext db, AuditService audit) { _db = db; _audit = audit; }

    public Asset Create(int companyId, string name, decimal value)
    {
        var a = new Asset { CompanyId = companyId, Name = name, PurchaseValue = value, PurchaseDate = DateTime.UtcNow.Date, Status = AssetStatus.InUse };
        _db.Assets.Add(a);
        _audit.Record("Asset", 0, "Create", name);
        _db.SaveChanges();
        return a;
    }

    public void ScheduleMaintenance(int assetId, DateTime when)
    {
        var a = _db.Assets.First(x => x.Id == assetId);
        a.NextMaintenanceDate = when;
        a.Status = AssetStatus.UnderMaintenance;
        _audit.Record("Asset", a.Id, "ScheduleMaintenance", when.ToString("yyyy-MM-dd"));
        _db.SaveChanges();
    }
}
