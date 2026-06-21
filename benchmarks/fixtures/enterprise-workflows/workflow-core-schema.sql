-- ENTERPRISE-WORKFLOWS FIXTURE: synthetic workflow-core schema (ORIGINAL, committed).
-- Represents the generic, widely-known enterprise-workflow engine pattern family
-- (definition -> instance -> step -> transition, with roles/actors, approvals, comments,
-- attachments, audit events, exceptions, SLA rules, notifications, escalations, policies,
-- risk signals and evidence). This is NOT a clone of any vendor BPM/workflow product,
-- schema, UI, or documentation - only the generic entity pattern is modelled.
-- It is a synthetic prototype for benchmarking code generation, NOT a deployed engine.

-- A workflow blueprint: the named process with a version.
CREATE TABLE dbo.WorkflowDefinition (
    Id INT NOT NULL PRIMARY KEY, [Key] NVARCHAR(80) NOT NULL, Name NVARCHAR(200) NOT NULL,
    Version INT NOT NULL, IsActive BIT NOT NULL, CreatedAtUtc DATETIME2 NOT NULL,
    CONSTRAINT UQ_WorkflowDefinition UNIQUE ([Key], Version));
GO
-- The roles a definition recognises (e.g. Maker, Checker, Approver).
CREATE TABLE dbo.WorkflowRole (
    Id INT NOT NULL PRIMARY KEY, WorkflowDefinitionId INT NOT NULL, Code NVARCHAR(60) NOT NULL,
    Name NVARCHAR(120) NOT NULL,
    CONSTRAINT FK_WorkflowRole_Definition FOREIGN KEY (WorkflowDefinitionId) REFERENCES dbo.WorkflowDefinition(Id));
GO
-- An actor (user/service) that can be bound to a role.
CREATE TABLE dbo.WorkflowActor (
    Id INT NOT NULL PRIMARY KEY, UserId NVARCHAR(100) NOT NULL, DisplayName NVARCHAR(160) NULL,
    IsService BIT NOT NULL, CONSTRAINT UQ_WorkflowActor UNIQUE (UserId));
GO
-- A running case of a definition over one business entity.
CREATE TABLE dbo.WorkflowInstance (
    Id INT NOT NULL PRIMARY KEY, WorkflowDefinitionId INT NOT NULL, BusinessEntityType NVARCHAR(80) NOT NULL,
    BusinessEntityId NVARCHAR(80) NOT NULL, CurrentState NVARCHAR(60) NOT NULL, IsTerminal BIT NOT NULL,
    CorrelationId UNIQUEIDENTIFIER NOT NULL, RowVersion ROWVERSION NOT NULL,
    StartedByUserId NVARCHAR(100) NOT NULL, StartedAtUtc DATETIME2 NOT NULL, ClosedAtUtc DATETIME2 NULL,
    CONSTRAINT FK_WorkflowInstance_Definition FOREIGN KEY (WorkflowDefinitionId) REFERENCES dbo.WorkflowDefinition(Id));
GO
-- A unit of work within an instance (a stage/task).
CREATE TABLE dbo.WorkflowStep (
    Id INT NOT NULL PRIMARY KEY, WorkflowInstanceId INT NOT NULL, StepCode NVARCHAR(60) NOT NULL,
    State NVARCHAR(40) NOT NULL, AssignedRoleId INT NULL, AssignedActorId INT NULL,
    DueAtUtc DATETIME2 NULL, CompletedAtUtc DATETIME2 NULL,
    CONSTRAINT FK_WorkflowStep_Instance FOREIGN KEY (WorkflowInstanceId) REFERENCES dbo.WorkflowInstance(Id),
    CONSTRAINT FK_WorkflowStep_Role FOREIGN KEY (AssignedRoleId) REFERENCES dbo.WorkflowRole(Id),
    CONSTRAINT FK_WorkflowStep_Actor FOREIGN KEY (AssignedActorId) REFERENCES dbo.WorkflowActor(Id));
GO
-- An allowed (and recorded) state transition for an instance.
CREATE TABLE dbo.WorkflowTransition (
    Id INT NOT NULL PRIMARY KEY, WorkflowInstanceId INT NOT NULL, FromState NVARCHAR(60) NULL,
    ToState NVARCHAR(60) NOT NULL, TriggeredByActorId INT NOT NULL, Reason NVARCHAR(400) NULL,
    OccurredAtUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_WorkflowTransition_Instance FOREIGN KEY (WorkflowInstanceId) REFERENCES dbo.WorkflowInstance(Id),
    CONSTRAINT FK_WorkflowTransition_Actor FOREIGN KEY (TriggeredByActorId) REFERENCES dbo.WorkflowActor(Id));
GO
-- An approval decision against a step (enforces distinct maker/checker via service logic).
CREATE TABLE dbo.WorkflowApproval (
    Id INT NOT NULL PRIMARY KEY, WorkflowStepId INT NOT NULL, ApproverActorId INT NOT NULL,
    Decision NVARCHAR(20) NOT NULL, RejectionReason NVARCHAR(400) NULL, Tier INT NOT NULL,
    DecidedAtUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_WorkflowApproval_Step FOREIGN KEY (WorkflowStepId) REFERENCES dbo.WorkflowStep(Id),
    CONSTRAINT FK_WorkflowApproval_Actor FOREIGN KEY (ApproverActorId) REFERENCES dbo.WorkflowActor(Id),
    CONSTRAINT CK_WorkflowApproval_Decision CHECK (Decision IN ('Approved','Rejected','Returned')));
GO
CREATE TABLE dbo.WorkflowComment (
    Id INT NOT NULL PRIMARY KEY, WorkflowInstanceId INT NOT NULL, AuthorActorId INT NOT NULL,
    Body NVARCHAR(2000) NOT NULL, CreatedAtUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_WorkflowComment_Instance FOREIGN KEY (WorkflowInstanceId) REFERENCES dbo.WorkflowInstance(Id),
    CONSTRAINT FK_WorkflowComment_Actor FOREIGN KEY (AuthorActorId) REFERENCES dbo.WorkflowActor(Id));
GO
-- Bounded attachment metadata (size/type validated in the service layer; integrity via checksum).
CREATE TABLE dbo.WorkflowAttachment (
    Id INT NOT NULL PRIMARY KEY, WorkflowInstanceId INT NOT NULL, FileName NVARCHAR(260) NOT NULL,
    ContentType NVARCHAR(120) NOT NULL, SizeBytes BIGINT NOT NULL, Sha256 CHAR(64) NOT NULL,
    UploadedByActorId INT NOT NULL, UploadedAtUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_WorkflowAttachment_Instance FOREIGN KEY (WorkflowInstanceId) REFERENCES dbo.WorkflowInstance(Id),
    CONSTRAINT CK_WorkflowAttachment_Size CHECK (SizeBytes > 0 AND SizeBytes <= 26214400));
GO
-- Immutable audit event: one row per meaningful action; never updated or deleted.
CREATE TABLE dbo.WorkflowAuditEvent (
    Id BIGINT NOT NULL PRIMARY KEY, WorkflowInstanceId INT NOT NULL, ActorId INT NOT NULL,
    EventType NVARCHAR(80) NOT NULL, FromState NVARCHAR(60) NULL, ToState NVARCHAR(60) NULL,
    Reason NVARCHAR(400) NULL, CorrelationId UNIQUEIDENTIFIER NOT NULL, OccurredAtUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_WorkflowAuditEvent_Instance FOREIGN KEY (WorkflowInstanceId) REFERENCES dbo.WorkflowInstance(Id),
    CONSTRAINT FK_WorkflowAuditEvent_Actor FOREIGN KEY (ActorId) REFERENCES dbo.WorkflowActor(Id));
GO
CREATE TABLE dbo.WorkflowException (
    Id INT NOT NULL PRIMARY KEY, WorkflowInstanceId INT NOT NULL, ExceptionType NVARCHAR(80) NOT NULL,
    Severity NVARCHAR(20) NOT NULL, State NVARCHAR(40) NOT NULL, AssignedActorId INT NULL,
    Details NVARCHAR(2000) NULL, RaisedAtUtc DATETIME2 NOT NULL, ResolvedAtUtc DATETIME2 NULL,
    ResolutionNote NVARCHAR(1000) NULL,
    CONSTRAINT FK_WorkflowException_Instance FOREIGN KEY (WorkflowInstanceId) REFERENCES dbo.WorkflowInstance(Id),
    CONSTRAINT FK_WorkflowException_Actor FOREIGN KEY (AssignedActorId) REFERENCES dbo.WorkflowActor(Id),
    CONSTRAINT CK_WorkflowException_Severity CHECK (Severity IN ('Low','Medium','High','Critical')));
GO
-- SLA targets per step code, with calendar awareness flag.
CREATE TABLE dbo.WorkflowSlaRule (
    Id INT NOT NULL PRIMARY KEY, WorkflowDefinitionId INT NOT NULL, StepCode NVARCHAR(60) NOT NULL,
    TargetMinutes INT NOT NULL, WarnAtPercent INT NOT NULL, BusinessHoursOnly BIT NOT NULL,
    CONSTRAINT FK_WorkflowSlaRule_Definition FOREIGN KEY (WorkflowDefinitionId) REFERENCES dbo.WorkflowDefinition(Id));
GO
CREATE TABLE dbo.WorkflowNotification (
    Id INT NOT NULL PRIMARY KEY, WorkflowInstanceId INT NOT NULL, Channel NVARCHAR(30) NOT NULL,
    Recipient NVARCHAR(200) NOT NULL, Template NVARCHAR(80) NOT NULL, State NVARCHAR(20) NOT NULL,
    QueuedAtUtc DATETIME2 NOT NULL, SentAtUtc DATETIME2 NULL,
    CONSTRAINT FK_WorkflowNotification_Instance FOREIGN KEY (WorkflowInstanceId) REFERENCES dbo.WorkflowInstance(Id));
GO
-- Escalation rows record tier jumps when an SLA threshold is crossed.
CREATE TABLE dbo.WorkflowEscalation (
    Id INT NOT NULL PRIMARY KEY, WorkflowInstanceId INT NOT NULL, FromTier INT NOT NULL, ToTier INT NOT NULL,
    TriggerReason NVARCHAR(200) NOT NULL, EscalatedAtUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_WorkflowEscalation_Instance FOREIGN KEY (WorkflowInstanceId) REFERENCES dbo.WorkflowInstance(Id));
GO
-- Policy drives thresholds/limits without hardcoding them in code.
CREATE TABLE dbo.WorkflowPolicy (
    Id INT NOT NULL PRIMARY KEY, WorkflowDefinitionId INT NOT NULL, PolicyKey NVARCHAR(80) NOT NULL,
    PolicyValue NVARCHAR(200) NOT NULL, EffectiveFromUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_WorkflowPolicy_Definition FOREIGN KEY (WorkflowDefinitionId) REFERENCES dbo.WorkflowDefinition(Id),
    CONSTRAINT UQ_WorkflowPolicy UNIQUE (WorkflowDefinitionId, PolicyKey, EffectiveFromUtc));
GO
-- Risk signals (e.g. screening hit, high amount) attached to an instance, advisory not conclusive.
CREATE TABLE dbo.WorkflowRiskSignal (
    Id INT NOT NULL PRIMARY KEY, WorkflowInstanceId INT NOT NULL, SignalType NVARCHAR(60) NOT NULL,
    Score DECIMAL(5,2) NULL, Band NVARCHAR(20) NULL, RaisedAtUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_WorkflowRiskSignal_Instance FOREIGN KEY (WorkflowInstanceId) REFERENCES dbo.WorkflowInstance(Id));
GO
-- Tamper-evident evidence export records (hash retained for integrity).
CREATE TABLE dbo.WorkflowEvidence (
    Id INT NOT NULL PRIMARY KEY, WorkflowInstanceId INT NOT NULL, EvidenceType NVARCHAR(60) NOT NULL,
    StoragePath NVARCHAR(400) NOT NULL, Sha256 CHAR(64) NOT NULL, ApprovedByActorId INT NULL,
    CreatedAtUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_WorkflowEvidence_Instance FOREIGN KEY (WorkflowInstanceId) REFERENCES dbo.WorkflowInstance(Id),
    CONSTRAINT FK_WorkflowEvidence_Actor FOREIGN KEY (ApprovedByActorId) REFERENCES dbo.WorkflowActor(Id));
GO

-- Core service helper: record a transition AND its immutable audit event atomically.
-- Distinct-actor / threshold checks are enforced by the calling service before this runs.
CREATE PROCEDURE dbo.usp_RecordTransition
    @InstanceId INT, @FromState NVARCHAR(60), @ToState NVARCHAR(60),
    @ActorId INT, @Reason NVARCHAR(400), @EventType NVARCHAR(80) AS
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRAN;
        DECLARE @cid UNIQUEIDENTIFIER = (SELECT CorrelationId FROM dbo.WorkflowInstance WHERE Id = @InstanceId);
        INSERT INTO dbo.WorkflowTransition (WorkflowInstanceId, FromState, ToState, TriggeredByActorId, Reason, OccurredAtUtc)
            VALUES (@InstanceId, @FromState, @ToState, @ActorId, @Reason, SYSUTCDATETIME());
        UPDATE dbo.WorkflowInstance SET CurrentState = @ToState WHERE Id = @InstanceId;
        INSERT INTO dbo.WorkflowAuditEvent (WorkflowInstanceId, ActorId, EventType, FromState, ToState, Reason, CorrelationId, OccurredAtUtc)
            VALUES (@InstanceId, @ActorId, @EventType, @FromState, @ToState, @Reason, @cid, SYSUTCDATETIME());
    COMMIT TRAN;
END
GO
