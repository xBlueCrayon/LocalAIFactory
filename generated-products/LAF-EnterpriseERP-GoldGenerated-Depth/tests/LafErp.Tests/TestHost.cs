using LafErp.Core;
using LafErp.Data;
using LafErp.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Tests;

/// <summary>
/// Builds an isolated in-memory SQLite ERP database (real relational provider, real schema via
/// EnsureCreated), seeds the demo company, and wires the full service graph around a single mutable
/// current-user so tests can switch between maker and checker identities.
/// </summary>
public sealed class TestHost : IDisposable
{
    private readonly SqliteConnection _conn;
    public ErpDbContext Db { get; }
    public CurrentUser User { get; }
    public SeedResult Seed { get; }

    public AuditService Audit { get; }
    public NumberingService Numbering { get; }
    public WorkflowService Workflow { get; }
    public StockService Stock { get; }
    public AccountingService Accounting { get; }
    public SalesService Sales { get; }
    public PurchaseService Purchase { get; }
    public PaymentService Payment { get; }
    public JournalService Journal { get; }
    public RbacService Rbac { get; }
    public ImportService Import { get; }
    public CrmService Crm { get; }
    public ProjectService Projects { get; }
    public SupportService Support { get; }
    public AssetService Assets { get; }
    public ManufacturingService Manufacturing { get; }
    public ReportsService Reports { get; }

    public TestHost(string user = "admin", params string[] roles)
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var opts = new DbContextOptionsBuilder<ErpDbContext>().UseSqlite(_conn).Options;
        Db = new ErpDbContext(opts);
        Db.Database.EnsureCreated();
        Seed = DataSeeder.Seed(Db);

        User = new CurrentUser(user, roles.Length == 0 ? new[] { "System Manager" } : roles);
        Audit = new AuditService(Db, User);
        Numbering = new NumberingService(Db);
        Workflow = new WorkflowService(Db, User, Audit);
        Stock = new StockService(Db);
        Accounting = new AccountingService(Db, Stock);
        Sales = new SalesService(Db, Numbering, Workflow, Accounting, Audit);
        Purchase = new PurchaseService(Db, Numbering, Workflow, Accounting, Audit);
        Payment = new PaymentService(Db, Numbering, Workflow, Accounting, Audit);
        Journal = new JournalService(Db, Numbering, Workflow, Accounting, Audit);
        Rbac = new RbacService(Db, User);
        Import = new ImportService(Db, Audit);
        Crm = new CrmService(Db, Audit);
        Projects = new ProjectService(Db, Audit);
        Support = new SupportService(Db, Audit);
        Assets = new AssetService(Db, Audit);
        Manufacturing = new ManufacturingService(Db, Stock, Numbering, Audit);
        Reports = new ReportsService(Db, Stock);
    }

    /// <summary>Switch the acting identity (maker → checker) without rebuilding the graph.</summary>
    public TestHost Login(string username, params string[] roles)
    {
        User.Username = username;
        User.RoleList = roles.ToList();
        return this;
    }

    /// <summary>Receive stock so outward sales have valuation to relieve. Returns the purchase invoice id.</summary>
    public int ReceiveStock(int itemId, decimal qty, decimal rate)
    {
        Login("buyer", "Purchase User", "Accounts Manager");
        var pi = Purchase.CreateInvoice(Seed.CompanyId, Seed.SupplierId, Seed.WarehouseId,
            new[] { new InvoiceLineInput(itemId, qty, rate) });
        Purchase.Submit(pi.Id); // amount may exceed threshold -> may need approve
        var fresh = Db.PurchaseInvoices.First(x => x.Id == pi.Id);
        if (fresh.Status != DocStatus.Submitted)
        {
            Login("approver", "Accounts Manager");
            Purchase.Approve(pi.Id);
        }
        return pi.Id;
    }

    public void Dispose()
    {
        Db.Dispose();
        _conn.Dispose();
    }
}
