// R2-ACC-INDUSTRIAL: synthetic ERP/CRM C# service layer (original, committed — NOT a vendor product clone).
// Each method names its SQL objects in a query string so the deterministic C#↔SQL bridge links service → table.
using Microsoft.EntityFrameworkCore;

namespace ErpCrm.Services
{
    public class CustomerService
    {
        private readonly DbContext _db;
        public CustomerService(DbContext db) { _db = db; }
        public void GetCustomer() => _db.Database.ExecuteSqlRaw("SELECT Id, Name, Segment FROM dbo.Customer WHERE Id = 1");
        public void ListContacts() => _db.Database.ExecuteSqlRaw("SELECT Id, Email FROM dbo.Contact WHERE CustomerId = 1");
    }

    public class OpportunityService
    {
        private readonly DbContext _db;
        public OpportunityService(DbContext db) { _db = db; }
        public void AdvanceStage() => _db.Database.ExecuteSqlRaw("UPDATE dbo.Opportunity SET Stage = 'Won' WHERE Id = 1");
        public void ConvertLead() => _db.Database.ExecuteSqlRaw("SELECT Id, Status FROM dbo.Lead WHERE CustomerId = 1");
    }

    public class SalesOrderService
    {
        private readonly DbContext _db;
        public SalesOrderService(DbContext db) { _db = db; }
        public void GetSalesOrders() => _db.Database.ExecuteSqlRaw("SELECT Id, CustomerId, Total FROM dbo.SalesOrder WHERE Status = 'Open'");
        public void CreateSalesOrder()
        {
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.SalesOrder (Id, CustomerId, Status, Total) VALUES (1, 1, 'Open', 0)");
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.SalesOrderLine (Id, SalesOrderId, ItemId, Qty) VALUES (1, 1, 1, 1)");
        }
    }

    public class InvoiceService
    {
        private readonly DbContext _db;
        public InvoiceService(DbContext db) { _db = db; }
        public void GenerateInvoice()
        {
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.Invoice (Id, SalesOrderId, Amount, Posted) VALUES (1, 1, 100, 0)");
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_PostInvoice @InvoiceId = 1");
        }
    }

    public class InventoryService
    {
        private readonly DbContext _db;
        public InventoryService(DbContext db) { _db = db; }
        public void RecordMovement() => _db.Database.ExecuteSqlRaw("INSERT INTO dbo.InventoryMovement (Id, ItemId, Delta, Reason) VALUES (1, 1, -1, 'Sale')");
    }

    public class ApprovalService
    {
        private readonly DbContext _db;
        public ApprovalService(DbContext db) { _db = db; }
        public void ApproveDiscount() => _db.Database.ExecuteSqlRaw("INSERT INTO dbo.DiscountApproval (Id, OpportunityId, ApproverRole, Approved) VALUES (1, 1, 'SalesManager', 1)");
    }

    public class ReportingService
    {
        private readonly DbContext _db;
        public ReportingService(DbContext db) { _db = db; }
        public void InventoryReport() => _db.Database.ExecuteSqlRaw("SELECT ItemId, SUM(Delta) AS Net FROM dbo.InventoryMovement GROUP BY ItemId");
    }
}
