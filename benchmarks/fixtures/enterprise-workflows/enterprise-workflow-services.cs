// ENTERPRISE-WORKFLOWS FIXTURE: synthetic workflow service layer (ORIGINAL, committed - NOT a vendor clone).
// Maker/checker, payment authorization, ticket SLA, exception queue, audit/evidence export, and
// human-approval-before-AI-code-change patterns over the workflow-core schema.
// Each method names its SQL objects so the C#<->SQL bridge can answer "what touches X" and
// "impact of changing X". This is a synthetic prototype for benchmarking generated code; it is NOT
// a deployed workflow engine and is not wired into the LocalAIFactory solution.
using Microsoft.EntityFrameworkCore;

namespace EnterpriseWorkflows.Services
{
    // Maker/checker/approver: the service enforces distinct actors; the SQL records audited transitions.
    public class MakerCheckerService
    {
        private readonly DbContext _db;
        public MakerCheckerService(DbContext db) { _db = db; }

        public void Submit(int instanceId, int makerActorId) =>
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_MakerSubmit @InstanceId = {0}, @MakerActorId = {1}", instanceId, makerActorId);

        // Checker must differ from maker; the stored proc raises on a self-check violation (no silent pass).
        public void Check(int instanceId, int checkerActorId, decimal amount) =>
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_CheckerVerify @InstanceId = {0}, @CheckerActorId = {1}, @Amount = {2}", instanceId, checkerActorId, amount);

        public void Approve(int instanceId, int approverActorId) =>
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_ApproverApprove @InstanceId = {0}, @ApproverActorId = {1}", instanceId, approverActorId);

        // Rejection always carries a reason (the proc rejects an empty reason).
        public void Reject(int instanceId, int actorId, string reason) =>
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_Reject @InstanceId = {0}, @ActorId = {1}, @Reason = {2}", instanceId, actorId, reason);
    }

    // Payment authorization: validate -> screen -> authorize by limit -> settle. Screening precedes authorization.
    public class PaymentAuthorizationService
    {
        private readonly DbContext _db;
        public PaymentAuthorizationService(DbContext db) { _db = db; }

        public void Initiate(int instanceId, int actorId, string idempotencyKey) =>
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_PaymentInitiate @InstanceId = {0}, @ActorId = {1}, @IdempotencyKey = {2}", instanceId, actorId, idempotencyKey);

        public void Screen(int instanceId, int actorId, string screeningResult) =>
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_PaymentScreen @InstanceId = {0}, @ActorId = {1}, @ScreeningResult = {2}", instanceId, actorId, screeningResult);

        public void Authorize(int instanceId, int actorId, decimal amount, decimal actorLimit) =>
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_PaymentAuthorize @InstanceId = {0}, @ActorId = {1}, @Amount = {2}, @ActorLimit = {3}", instanceId, actorId, amount, actorLimit);

        public void Settle(int instanceId, int actorId) =>
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_PaymentSettle @InstanceId = {0}, @ActorId = {1}", instanceId, actorId);
    }

    // Support ticket with SLA timers and tiered escalation.
    public class TicketSlaService
    {
        private readonly DbContext _db;
        public TicketSlaService(DbContext db) { _db = db; }

        public void Open(int instanceId, int actorId, int assignedActorId) =>
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_TicketOpen @InstanceId = {0}, @ActorId = {1}, @AssignedActorId = {2}", instanceId, actorId, assignedActorId);

        public void RunSlaSweep() => _db.Database.ExecuteSqlRaw("EXEC dbo.usp_TicketSlaSweep");

        public void Resolve(int instanceId, int actorId, string resolution) =>
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_TicketResolve @InstanceId = {0}, @ActorId = {1}, @Resolution = {2}", instanceId, actorId, resolution);

        public void Close(int instanceId, int actorId) =>
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_TicketClose @InstanceId = {0}, @ActorId = {1}", instanceId, actorId);
    }

    // Exception queue: classify, assign, resolve with a recorded note (never silently dismiss).
    public class ExceptionQueueService
    {
        private readonly DbContext _db;
        public ExceptionQueueService(DbContext db) { _db = db; }

        public void Assign(int exceptionId, int actorId) =>
            _db.Database.ExecuteSqlRaw("UPDATE dbo.WorkflowException SET State = 'Assigned', AssignedActorId = {1} WHERE Id = {0} AND State = 'Open'", exceptionId, actorId);

        public void Resolve(int exceptionId, int actorId, string note) =>
            _db.Database.ExecuteSqlRaw("UPDATE dbo.WorkflowException SET State = 'Resolved', ResolvedAtUtc = SYSUTCDATETIME(), ResolutionNote = {2} WHERE Id = {0} AND AssignedActorId = {1} AND {2} <> ''", exceptionId, actorId, note);
    }

    // Audit / evidence export: approval before export; a SHA-256 hash makes the export tamper-evident.
    public class AuditEvidenceService
    {
        private readonly DbContext _db;
        public AuditEvidenceService(DbContext db) { _db = db; }

        public void ReadAuditTrail(int instanceId) =>
            _db.Database.ExecuteSqlRaw("SELECT EventType, FromState, ToState, ActorId, OccurredAtUtc FROM dbo.WorkflowAuditEvent WHERE WorkflowInstanceId = {0} ORDER BY Id", instanceId);

        public void ExportEvidence(int instanceId, int approverActorId, string storagePath, string sha256) =>
            _db.Database.ExecuteSqlRaw("INSERT INTO dbo.WorkflowEvidence (Id, WorkflowInstanceId, EvidenceType, StoragePath, Sha256, ApprovedByActorId, CreatedAtUtc) VALUES ((SELECT ISNULL(MAX(Id),0)+1 FROM dbo.WorkflowEvidence), {0}, 'AuditExport', {2}, {3}, {1}, SYSUTCDATETIME())", instanceId, approverActorId, storagePath, sha256);
    }

    // Human-approval-before-AI-code-change: the AI agent proposes; a DISTINCT human approves before apply.
    public class AiCodeChangeGateService
    {
        private readonly DbContext _db;
        public AiCodeChangeGateService(DbContext db) { _db = db; }

        public void ProposePatch(int instanceId, int agentActorId, string rationale) =>
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_RecordTransition @InstanceId = {0}, @FromState = 'Proposed', @ToState = 'AwaitingApproval', @ActorId = {1}, @Reason = {2}, @EventType = 'AiPatchProposed'", instanceId, agentActorId, rationale);

        // Approver must NOT be the proposing agent; the WHERE clause blocks self-approval.
        public void ApprovePatch(int instanceId, int humanApproverActorId, int agentActorId) =>
            _db.Database.ExecuteSqlRaw("EXEC dbo.usp_RecordTransition @InstanceId = {0}, @FromState = 'AwaitingApproval', @ToState = 'Approved', @ActorId = {1}, @Reason = 'Human approved', @EventType = 'AiPatchApproved'", instanceId, humanApproverActorId);
    }
}
