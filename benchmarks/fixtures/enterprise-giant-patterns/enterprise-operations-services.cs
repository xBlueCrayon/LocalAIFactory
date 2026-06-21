// FINAL-ENTERPRISE-REASONING: synthetic operations service layer (ORIGINAL, committed — NOT a vendor clone).
// ITSM incident lifecycle + SLA evaluation, core-banking reconciliation, and ERP GL posting — the operational
// surface an operations manager monitors. Each method names its SQL objects for the C#<->SQL bridge.
using Microsoft.EntityFrameworkCore;

namespace EnterprisePatterns.Services
{
    // ITSM / ServiceNow-style incident lifecycle with an explicit audit trail on resolution.
    public class IncidentService
    {
        private readonly DbContext _db;
        public IncidentService(DbContext db) { _db = db; }
        public void LogIncident() => _db.Database.ExecuteSqlRaw(
            "INSERT INTO dbo.Incident (Id, Title, Priority, State, OpenedAtUtc) VALUES (1, 'Outage', 'P1', 'New', SYSUTCDATETIME())");
        public void ResolveIncident()
        {
            _db.Database.ExecuteSqlRaw("UPDATE dbo.Incident SET State = 'Resolved', ResolvedAtUtc = SYSUTCDATETIME() WHERE Id = 1");
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.IncidentAudit (Id, IncidentId, Action, ActorUser, AtUtc) VALUES (1, 1, 'Resolved', 'ops1', SYSUTCDATETIME())");
        }
    }

    public class SlaService
    {
        private readonly DbContext _db;
        public SlaService(DbContext db) { _db = db; }
        public void EvaluateSla()
        {
            _db.Database.ExecuteSqlRaw("SELECT Id, OpenedAtUtc, ResolvedAtUtc FROM dbo.Incident WHERE State <> 'Resolved'");
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.SlaBreach (Id, IncidentId, SlaName, BreachedAtUtc) VALUES (1, 1, 'P1-Resolve-4h', SYSUTCDATETIME())");
        }
    }

    // Core-banking-style settlement reconciliation over released payments.
    public class ReconciliationService
    {
        private readonly DbContext _db;
        public ReconciliationService(DbContext db) { _db = db; }
        public void Reconcile()
        {
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.Settlement (Id, PaymentInstructionId, SettledAmount, SettledAtUtc, Reconciled) VALUES (1, 1, 100, SYSUTCDATETIME(), 1)");
            _db.Database.ExecuteSqlRaw("SELECT State FROM dbo.PaymentInstruction WHERE Id = 1");
        }
    }

    // ERP GL posting — the financial control point that downstream finance reports depend on.
    public class GlService
    {
        private readonly DbContext _db;
        public GlService(DbContext db) { _db = db; }
        public void PostJournal()
        {
            _db.Database.ExecuteSqlRaw("SELECT Id, Code, Type FROM dbo.GlAccount WHERE Code = '4000'");
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_PostToGl @GlAccountId = 1, @Debit = 0, @Credit = 100, @Source = 'Revenue'");
        }
    }
}
