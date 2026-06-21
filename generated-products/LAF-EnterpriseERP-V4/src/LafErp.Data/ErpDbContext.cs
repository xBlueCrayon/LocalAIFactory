using LafErp.Core;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Data;

public class ErpDbContext : DbContext
{
    public ErpDbContext(DbContextOptions<ErpDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // The soft-delete query filters on master data (Customer/Supplier/Item) are intentional; documents
        // reference them by id and never soft-delete a referenced master in normal flows. Acknowledge the
        // EF interaction warning explicitly rather than leaving it as build noise.
        options.ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
    }

    // Setup
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<NumberingSeries> NumberingSeries => Set<NumberingSeries>();
    public DbSet<TaxTemplate> TaxTemplates => Set<TaxTemplate>();

    // Masters
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ItemGroup> ItemGroups => Set<ItemGroup>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    // Transactions
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceLine> SalesInvoiceLines => Set<SalesInvoiceLine>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceLine> PurchaseInvoiceLines => Set<PurchaseInvoiceLine>();
    public DbSet<PaymentEntry> PaymentEntries => Set<PaymentEntry>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();

    // Ledgers
    public DbSet<GLEntry> GLEntries => Set<GLEntry>();
    public DbSet<StockLedgerEntry> StockLedgerEntries => Set<StockLedgerEntry>();

    // Ops
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<Asset> Assets => Set<Asset>();

    // Platform
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowApproval> WorkflowApprovals => Set<WorkflowApproval>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<AppRole> AppRoles => Set<AppRole>();
    public DbSet<AppUserRole> AppUserRoles => Set<AppUserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<ReportDefinition> ReportDefinitions => Set<ReportDefinition>();
        public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<DeliveryNote> DeliveryNotes => Set<DeliveryNote>();
    public DbSet<PurchaseReceipt> PurchaseReceipts => Set<PurchaseReceipt>();
    public DbSet<MaterialRequest> MaterialRequests => Set<MaterialRequest>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<BillOfMaterials> BillOfMaterialses => Set<BillOfMaterials>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<QualityInspection> QualityInspections => Set<QualityInspection>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<SalaryComponent> SalaryComponents => Set<SalaryComponent>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<PosProfile> PosProfiles => Set<PosProfile>();
    public DbSet<WebProduct> WebProducts => Set<WebProduct>();
    public DbSet<MaintenanceSchedule> MaintenanceSchedules => Set<MaintenanceSchedule>();
    public DbSet<CustomFieldDef> CustomFieldDefs => Set<CustomFieldDef>();
    public DbSet<NotificationRule> NotificationRules => Set<NotificationRule>();
    public DbSet<CustomerSegment> CustomerSegments => Set<CustomerSegment>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<EmployeeRole> EmployeeRoles => Set<EmployeeRole>();
    public DbSet<MarketingCampaign> MarketingCampaigns => Set<MarketingCampaign>();
    public DbSet<VendorContract> VendorContracts => Set<VendorContract>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Money / quantity precision for relational providers.
        foreach (var prop in b.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            prop.SetPrecision(18);
            prop.SetScale(4);
        }

        // Global soft-delete filter on master/setup data (documents keep history).
        b.Entity<Customer>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Supplier>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<Item>().HasQueryFilter(x => !x.IsDeleted);

        // Unique business keys.
        b.Entity<Currency>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Customer>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Supplier>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Item>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Warehouse>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Account>().HasIndex(x => new { x.CompanyId, x.Code }).IsUnique();
        b.Entity<NumberingSeries>().HasIndex(x => x.DocType).IsUnique();
        b.Entity<AppUser>().HasIndex(x => x.Username).IsUnique();
        b.Entity<AppRole>().HasIndex(x => x.Name).IsUnique();
        b.Entity<RolePermission>().HasIndex(x => new { x.RoleName, x.DocType }).IsUnique();

        // Document numbers unique within their type.
        b.Entity<SalesInvoice>().HasIndex(x => x.DocNo).IsUnique();
        b.Entity<PurchaseInvoice>().HasIndex(x => x.DocNo).IsUnique();
        b.Entity<SalesOrder>().HasIndex(x => x.DocNo).IsUnique();
        b.Entity<PurchaseOrder>().HasIndex(x => x.DocNo).IsUnique();
        b.Entity<PaymentEntry>().HasIndex(x => x.DocNo).IsUnique();
        b.Entity<JournalEntry>().HasIndex(x => x.DocNo).IsUnique();

        // Ledger query indexes.
        b.Entity<GLEntry>().HasIndex(x => new { x.AccountId, x.PostingDate });
        b.Entity<GLEntry>().HasIndex(x => new { x.PartyType, x.PartyId });
        b.Entity<GLEntry>().HasIndex(x => new { x.VoucherType, x.VoucherNo });
        b.Entity<StockLedgerEntry>().HasIndex(x => new { x.ItemId, x.WarehouseId, x.Id });

        // Relationships (no cascade into ledgers).
        b.Entity<SalesOrder>().HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.SalesOrderId);
        b.Entity<SalesInvoice>().HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.SalesInvoiceId);
        b.Entity<PurchaseOrder>().HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.PurchaseOrderId);
        b.Entity<PurchaseInvoice>().HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.PurchaseInvoiceId);
        b.Entity<JournalEntry>().HasMany(x => x.Lines).WithOne().HasForeignKey(l => l.JournalEntryId);
        b.Entity<WorkflowDefinition>().HasMany(x => x.Transitions).WithOne().HasForeignKey(t => t.WorkflowDefinitionId);
        b.Entity<WorkflowInstance>().HasMany(x => x.Approvals).WithOne().HasForeignKey(a => a.WorkflowInstanceId);
        b.Entity<Project>().HasMany(x => x.Tasks).WithOne().HasForeignKey(t => t.ProjectId);
        b.Entity<AppUser>().HasMany(x => x.Roles).WithOne().HasForeignKey(r => r.AppUserId);

        // Self-referencing account hierarchy.
        b.Entity<Account>().HasOne(x => x.ParentAccount).WithMany().HasForeignKey(x => x.ParentAccountId);

        // Concurrency token only on SQL Server (rowversion); ignored on Sqlite test provider.
        if (Database.IsSqlServer())
        {
            foreach (var t in new[] { typeof(SalesInvoice), typeof(PurchaseInvoice), typeof(SalesOrder),
                                      typeof(PurchaseOrder), typeof(PaymentEntry), typeof(JournalEntry) })
            {
                b.Entity(t).Property<byte[]>(nameof(DocumentBase.RowVersion)).IsRowVersion();
            }
        }
        else
        {
            foreach (var t in new[] { typeof(SalesInvoice), typeof(PurchaseInvoice), typeof(SalesOrder),
                                      typeof(PurchaseOrder), typeof(PaymentEntry), typeof(JournalEntry) })
            {
                b.Entity(t).Ignore(nameof(DocumentBase.RowVersion));
            }
        }
    }

    /// <summary>Stamp audit timestamps automatically.</summary>
    public override int SaveChanges()
    {
        StampTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var e in ChangeTracker.Entries<EntityBase>())
        {
            if (e.State == EntityState.Added && e.Entity.CreatedUtc == default)
                e.Entity.CreatedUtc = now;
            if (e.State == EntityState.Modified)
                e.Entity.UpdatedUtc = now;
        }
    }
}
