// R2-ACC-INDUSTRIAL: synthetic KYC/AML transaction-approval C# service layer (original, committed — NOT a
// vendor product clone and NOT a compliance control implementation). Each method names its SQL objects in a
// query/EXEC string so the deterministic C#<->SQL bridge links service -> table/procedure. This exists to prove
// the structural graph (find / dependents / dependencies / impact), not to assert any regulatory guarantee.
using Microsoft.EntityFrameworkCore;

namespace KycAml.Services
{
    // Opens and progresses an onboarding (KYC) case and records the captured identity documents.
    public class OnboardingService
    {
        private readonly DbContext _db;
        public OnboardingService(DbContext db) { _db = db; }
        public void OpenCase()
        {
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.Customer (Id, LegalName, CustomerType, Status) VALUES (1, 'Acme Ltd', 'Corporate', 'Onboarding')");
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.OnboardingCase (Id, CustomerId, Stage, Outcome) VALUES (1, 1, 'IdentityCheck', NULL)");
        }
        public void RecordIdentityDocument() => _db.Database.ExecuteSqlRaw("INSERT INTO dbo.IdentityDocument (Id, CustomerId, DocType, DocReference, Verified) VALUES (1, 1, 'Passport', 'P12345', 0)");
        // Maker/checker onboarding decision (segregation recorded on the row).
        public void RecordApproval() => _db.Database.ExecuteSqlRaw("INSERT INTO dbo.OnboardingApproval (Id, OnboardingCaseId, MakerUser, CheckerUser, Decision) VALUES (1, 1, 'maker.a', 'checker.b', 'Approved')");
    }

    // Runs sanctions/PEP/adverse-media screening and records the hit/no-hit outcome against the customer.
    public class ScreeningService
    {
        private readonly DbContext _db;
        public ScreeningService(DbContext db) { _db = db; }
        public void ScreenCustomer() => _db.Database.ExecuteSqlRaw("INSERT INTO dbo.ScreeningResult (Id, CustomerId, ListType, MatchStatus, ScreenedUtc) VALUES (1, 1, 'Sanctions', 'NoMatch', SYSUTCDATETIME())");
        public void GetScreeningHistory() => _db.Database.ExecuteSqlRaw("SELECT Id, ListType, MatchStatus FROM dbo.ScreeningResult WHERE CustomerId = 1");
    }

    // Assigns/reads the customer risk band that drives the per-band approval limits.
    public class RiskRatingService
    {
        private readonly DbContext _db;
        public RiskRatingService(DbContext db) { _db = db; }
        public void RateCustomer() => _db.Database.ExecuteSqlRaw("INSERT INTO dbo.CustomerRiskRating (Id, CustomerId, RiskBand, Score, RatedUtc) VALUES (1, 1, 'Medium', 55, SYSUTCDATETIME())");
        public void GetApprovalLimit() => _db.Database.ExecuteSqlRaw("SELECT ApproverRole, MaxAmount FROM dbo.ApprovalLimit WHERE RiskBand = 'Medium'");
    }

    // Submits a transaction for maker/checker approval and applies the checker decision via the procedures.
    public class TransactionApprovalService
    {
        private readonly DbContext _db;
        public TransactionApprovalService(DbContext db) { _db = db; }
        // Idempotent submit (proc enforces the duplicate-key check); records the maker.
        public void SubmitForApproval() => _db.Database.ExecuteSqlRaw("EXEC dbo.usp_SubmitTransactionForApproval @CustomerId = 1, @Amount = 1000, @Currency = 'GBP', @MakerUser = 'maker.a', @Idem = 'tx-1'");
        // Checker decision (proc enforces maker<>checker segregation).
        public void ApproveTransaction() => _db.Database.ExecuteSqlRaw("EXEC dbo.usp_ApproveTransaction @TransactionRequestId = 1, @CheckerUser = 'checker.b', @Approve = 1");
        public void ListPendingApprovals() => _db.Database.ExecuteSqlRaw("SELECT Id, Amount, Currency FROM dbo.TransactionRequest WHERE Status = 'PendingApproval'");
    }

    // Raises and dispositions AML alerts attached to a transaction request.
    public class AmlAlertService
    {
        private readonly DbContext _db;
        public AmlAlertService(DbContext db) { _db = db; }
        public void RaiseAlert() => _db.Database.ExecuteSqlRaw("INSERT INTO dbo.AmlAlert (Id, TransactionRequestId, AlertType, Disposition) VALUES (1, 1, 'StructuringPattern', NULL)");
        public void DispositionAlert() => _db.Database.ExecuteSqlRaw("UPDATE dbo.AmlAlert SET Disposition = 'Cleared' WHERE Id = 1");
    }
}
