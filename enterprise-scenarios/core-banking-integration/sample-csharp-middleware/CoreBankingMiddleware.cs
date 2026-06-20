// R2-ACC-INDUSTRIAL: synthetic core-banking integration MIDDLEWARE (original, committed). It integrates with a
// core system; it is NOT a core-banking replacement. Each method names its SQL objects so the C#↔SQL bridge
// links integration code → core tables/procedures (posting, mandates, claims, settlement, reconciliation).
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Middleware
{
    public class PostingService
    {
        private readonly DbContext _db;
        public PostingService(DbContext db) { _db = db; }
        // Posts a transaction to the core system (idempotent via the proc's key check) — duplicate-posting control.
        public void PostTransaction() => _db.Database.ExecuteSqlRaw("EXEC dbo.usp_PostTransaction @AccountId = 1, @Amount = 100, @Idem = 'k1'");
        public void Reverse() => _db.Database.ExecuteSqlRaw("EXEC dbo.usp_ReverseTransaction @PostingId = 1");
        public void AccountBalance() => _db.Database.ExecuteSqlRaw("SELECT Balance FROM dbo.Account WHERE Id = 1");
    }

    public class MandateService
    {
        private readonly DbContext _db;
        public MandateService(DbContext db) { _db = db; }
        public void GenerateMandate() => _db.Database.ExecuteSqlRaw("INSERT INTO dbo.Mandate (Id, AccountId, Reference, Status) VALUES (1, 1, 'REF1', 'Active')");
        public void GenerateClaim() => _db.Database.ExecuteSqlRaw("INSERT INTO dbo.Claim (Id, MandateId, Amount) VALUES (1, 1, 100)");
    }

    public class ClaimResponseService
    {
        private readonly DbContext _db;
        public ClaimResponseService(DbContext db) { _db = db; }
        // Processes a claim response file; maps a rejection code and queues exceptions to suspense.
        public void ProcessResponse()
        {
            _db.Database.ExecuteSqlRaw("UPDATE dbo.Claim SET RejectionCode = 'R01' WHERE Id = 1");
            _db.Database.ExecuteSqlRaw("SELECT Code, Description FROM dbo.RejectionCode WHERE Code = 'R01'");
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.SuspenseQueue (Id, ClaimId, Reason) VALUES (1, 1, 'Rejected R01')");
        }
    }

    public class SettlementReconciliationService
    {
        private readonly DbContext _db;
        public SettlementReconciliationService(DbContext db) { _db = db; }
        public void Reconcile()
        {
            _db.Database.ExecuteSqlRaw("SELECT Id, FileName, Status FROM dbo.SettlementFile WHERE Status = 'Received'");
            _db.Database.ExecuteSqlRaw("SELECT AccountId, Amount FROM dbo.Posting WHERE Amount > 0");
        }
    }

    public class SftpFileProcessor
    {
        private readonly DbContext _db;
        public SftpFileProcessor(DbContext db) { _db = db; }
        // Archives a processed settlement file (idempotency + replay support).
        public void ArchiveFile() => _db.Database.ExecuteSqlRaw("INSERT INTO dbo.FileArchive (Id, FileName, ArchivedUtc, Sha256) VALUES (1, 'settle.txt', SYSUTCDATETIME(), '0')");
        public void RegisterIncoming() => _db.Database.ExecuteSqlRaw("INSERT INTO dbo.SettlementFile (Id, FileName, Status) VALUES (1, 'settle.txt', 'Received')");
    }
}
