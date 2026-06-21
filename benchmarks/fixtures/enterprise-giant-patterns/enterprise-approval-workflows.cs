// FINAL-ENTERPRISE-REASONING: synthetic approval-workflow service layer (ORIGINAL, committed — NOT a vendor clone).
// Each method names its SQL objects in a query string so the deterministic C#<->SQL bridge links service -> table/proc.
// Covers the maker/checker/approver control pattern shared across CRM discounts, ERP purchase orders, ITSM change
// requests, and core-banking payment release — the controls an operating manager / auditor must see enforced.
using Microsoft.EntityFrameworkCore;

namespace EnterprisePatterns.Services
{
    // CRM / Dynamics / Salesforce-style: high-value discount needs maker/checker approval before it is applied.
    public class DiscountApprovalService
    {
        private readonly DbContext _db;
        public DiscountApprovalService(DbContext db) { _db = db; }
        public void SubmitDiscount() => _db.Database.ExecuteSqlRaw(
            "INSERT INTO dbo.DiscountApproval (Id, OpportunityId, RequestedPct, ApproverRole, MakerUser, Approved) VALUES (1, 1, 25, 'SalesManager', 'maker1', 0)");
        public void ApproveDiscount()
        {
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_ApproveDiscount @DiscountApprovalId = 1, @CheckerUser = 'checker1'");
            _db.Database.ExecuteSqlRaw("SELECT Approved FROM dbo.DiscountApproval WHERE Id = 1");
        }
    }

    // ERP / SAP / Oracle-style: purchase order over a threshold requires approval before goods receipt.
    public class PurchaseApprovalService
    {
        private readonly DbContext _db;
        public PurchaseApprovalService(DbContext db) { _db = db; }
        public void SubmitPurchaseOrder()
        {
            _db.Database.ExecuteSqlRaw("UPDATE dbo.PurchaseOrder SET Status = 'PendingApproval' WHERE Id = 1");
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.PurchaseOrderApproval (Id, PurchaseOrderId, ApproverRole, MakerUser, Approved) VALUES (1, 1, 'ProcurementManager', 'maker2', 0)");
        }
        public void ApprovePurchaseOrder() => _db.Database.ExecuteSqlRaw(
            "EXEC dbo.usp_ApprovePurchaseOrder @PurchaseOrderApprovalId = 1, @CheckerUser = 'checker2'");
    }

    // ITSM / ServiceNow-style: a change request needs approval (CAB) before it can be scheduled.
    public class ChangeApprovalService
    {
        private readonly DbContext _db;
        public ChangeApprovalService(DbContext db) { _db = db; }
        public void SubmitChange()
        {
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.ChangeRequest (Id, Title, RiskLevel, State, RequestedBy) VALUES (1, 'Patch', 'High', 'Assess', 'maker3')");
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.ChangeApproval (Id, ChangeRequestId, ApproverRole, MakerUser, Approved) VALUES (1, 1, 'ChangeManager', 'maker3', 0)");
        }
        public void ApproveChange() => _db.Database.ExecuteSqlRaw(
            "EXEC dbo.usp_ApproveChangeRequest @ChangeApprovalId = 1, @CheckerUser = 'checker3'");
    }

    // Core-banking-style: maker submits a payment, checker/approver releases it — strict segregation of duties.
    public class PaymentApprovalService
    {
        private readonly DbContext _db;
        public PaymentApprovalService(DbContext db) { _db = db; }
        public void SubmitPayment()
        {
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_SubmitPayment @PaymentInstructionId = 1, @MakerUser = 'maker4'");
            _db.Database.ExecuteSqlRaw("SELECT State FROM dbo.PaymentInstruction WHERE Id = 1");
            _db.Database.ExecuteSqlRaw("SELECT Stage FROM dbo.MakerCheckerLog WHERE PaymentInstructionId = 1");
        }
        public void ReleasePayment()
        {
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_ReleasePayment @PaymentInstructionId = 1, @ApproverUser = 'approver4'");
            _db.Database.ExecuteSqlRaw("UPDATE dbo.MakerCheckerLog SET Stage = 'Released' WHERE PaymentInstructionId = 1");
        }
    }
}
