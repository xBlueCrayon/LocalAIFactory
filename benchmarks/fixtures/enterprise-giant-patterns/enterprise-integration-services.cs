// FINAL-ENTERPRISE-REASONING: synthetic integration service layer (ORIGINAL, committed — NOT a vendor clone).
// CRM 360, ERP order-to-cash / procure-to-pay, and core-banking screening/rejection integration patterns.
// Each method names its SQL objects so the C#<->SQL bridge can answer "what touches X" and "impact of changing X".
using Microsoft.EntityFrameworkCore;

namespace EnterprisePatterns.Services
{
    // CRM / Dynamics-style customer 360: one read assembles the customer across contact, account, opportunity.
    public class CustomerService
    {
        private readonly DbContext _db;
        public CustomerService(DbContext db) { _db = db; }
        public void GetCustomer360()
        {
            _db.Database.ExecuteSqlRaw("SELECT Id, Name, Segment, Status FROM dbo.Customer WHERE Id = 1");
            _db.Database.ExecuteSqlRaw("SELECT Id, Email, IsPrimary FROM dbo.Contact WHERE CustomerId = 1");
            _db.Database.ExecuteSqlRaw("SELECT Id, AccountManager FROM dbo.Account WHERE CustomerId = 1");
            _db.Database.ExecuteSqlRaw("SELECT Id, Stage, Amount FROM dbo.Opportunity WHERE CustomerId = 1");
        }
        public void UpdateContact() => _db.Database.ExecuteSqlRaw("UPDATE dbo.Contact SET Email = 'x@y.z' WHERE Id = 1");
    }

    public class OpportunityService
    {
        private readonly DbContext _db;
        public OpportunityService(DbContext db) { _db = db; }
        public void AdvanceStage()
        {
            _db.Database.ExecuteSqlRaw("UPDATE dbo.Opportunity SET Stage = 'Won' WHERE Id = 1");
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.OpportunityStageHistory (Id, OpportunityId, ToStage, ChangedBy, ChangedAtUtc) VALUES (1, 1, 'Won', 'rep1', SYSUTCDATETIME())");
        }
    }

    // ERP order-to-cash: create order -> invoice -> post to GL.
    public class OrderToCashService
    {
        private readonly DbContext _db;
        public OrderToCashService(DbContext db) { _db = db; }
        public void CreateSalesOrder()
        {
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.SalesOrder (Id, CustomerId, Status, Total) VALUES (1, 1, 'Open', 0)");
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.SalesOrderLine (Id, SalesOrderId, ItemId, Qty, UnitPrice) VALUES (1, 1, 1, 1, 10)");
        }
        public void GenerateInvoice()
        {
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.Invoice (Id, SalesOrderId, Amount, Posted) VALUES (1, 1, 100, 0)");
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_PostToGl @GlAccountId = 1, @Debit = 100, @Credit = 0, @Source = 'AR'");
        }
    }

    // ERP procure-to-pay: receive goods -> stock movement -> inventory revaluation.
    public class ProcureToPayService
    {
        private readonly DbContext _db;
        public ProcureToPayService(DbContext db) { _db = db; }
        public void ReceiveGoods()
        {
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.GoodsReceipt (Id, PurchaseOrderId, ReceivedQty, ReceivedAtUtc) VALUES (1, 1, 5, SYSUTCDATETIME())");
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.StockMovement (Id, ItemId, Delta, Reason, MovedAtUtc) VALUES (1, 1, 5, 'Receipt', SYSUTCDATETIME())");
        }
        public void RevalueInventory()
        {
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.InventoryValuation (Id, ItemId, Method, UnitValue, AsOfUtc) VALUES (1, 1, 'Standard', 10, SYSUTCDATETIME())");
            _db.Database.ExecuteSqlRaw("SELECT SUM(Delta) AS Net FROM dbo.StockMovement WHERE ItemId = 1");
        }
    }

    // Core-banking integration: sanctions screening and rejection-code mapping over the payment instruction.
    public class SanctionsScreeningService
    {
        private readonly DbContext _db;
        public SanctionsScreeningService(DbContext db) { _db = db; }
        public void ScreenPayment()
        {
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.SanctionsScreening (Id, PaymentInstructionId, Result, ListVersion, ScreenedAtUtc) VALUES (1, 1, 'Clear', 'v1', SYSUTCDATETIME())");
            _db.Database.ExecuteSqlRaw("UPDATE dbo.PaymentInstruction SET State = 'Screened' WHERE Id = 1");
        }
    }

    public class RejectionService
    {
        private readonly DbContext _db;
        public RejectionService(DbContext db) { _db = db; }
        public void ProcessRejection()
        {
            _db.Database.ExecuteSqlRaw("SELECT Id, Code, Retryable FROM dbo.RejectionCode WHERE Code = 'AC04'");
            _db.Database.ExecuteSqlRaw("UPDATE dbo.PaymentInstruction SET State = 'Rejected', RejectionCodeId = 1 WHERE Id = 1");
        }
    }
}
