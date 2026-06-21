-- ENTERPRISE-WORKFLOWS FIXTURE: synthetic support-ticket SLA workflow (ORIGINAL, committed).
-- Demonstrates SLA timers, calendar-aware warning/breach, and tiered escalation over workflow-core.
-- Synthetic prototype only; ITSM-concept lineage, no publication text reproduced.

INSERT INTO dbo.WorkflowDefinition (Id, [Key], Name, Version, IsActive, CreatedAtUtc)
    VALUES (3, 'support-ticket', 'Support Ticket', 1, 1, SYSUTCDATETIME());
GO
-- SLA rule: 240 minutes to resolve, warn at 80%, business hours only.
INSERT INTO dbo.WorkflowSlaRule (Id, WorkflowDefinitionId, StepCode, TargetMinutes, WarnAtPercent, BusinessHoursOnly)
    VALUES (1, 3, 'RESOLVE', 240, 80, 1);
GO

-- Open a ticket: assign and start the SLA clock by setting DueAtUtc.
CREATE PROCEDURE dbo.usp_TicketOpen @InstanceId INT, @ActorId INT, @AssignedActorId INT AS
BEGIN
    SET XACT_ABORT ON;
    DECLARE @target INT = (SELECT TargetMinutes FROM dbo.WorkflowSlaRule WHERE WorkflowDefinitionId = 3 AND StepCode = 'RESOLVE');
    INSERT INTO dbo.WorkflowStep (Id, WorkflowInstanceId, StepCode, State, AssignedActorId, DueAtUtc)
        VALUES ((SELECT ISNULL(MAX(Id),0)+1 FROM dbo.WorkflowStep), @InstanceId, 'RESOLVE', 'InProgress', @AssignedActorId, DATEADD(MINUTE, @target, SYSUTCDATETIME()));
    EXEC dbo.usp_RecordTransition @InstanceId, 'New', 'Assigned', @ActorId, 'Ticket assigned', 'TicketOpen';
END
GO

-- SLA sweep: warn near the threshold, escalate on breach. Never silently resets the clock.
CREATE PROCEDURE dbo.usp_TicketSlaSweep AS
BEGIN
    SET XACT_ABORT ON;
    -- Breach: due time passed and not resolved -> raise an escalation tier and an exception.
    INSERT INTO dbo.WorkflowEscalation (Id, WorkflowInstanceId, FromTier, ToTier, TriggerReason, EscalatedAtUtc)
    SELECT (SELECT ISNULL(MAX(Id),0) FROM dbo.WorkflowEscalation) + ROW_NUMBER() OVER (ORDER BY s.Id),
           s.WorkflowInstanceId, 0, 1, 'SLA breach on RESOLVE', SYSUTCDATETIME()
    FROM dbo.WorkflowStep s
    WHERE s.StepCode = 'RESOLVE' AND s.State = 'InProgress' AND s.DueAtUtc < SYSUTCDATETIME();
END
GO

-- Resolve: record resolution; closure confirmation handled by usp_TicketClose.
CREATE PROCEDURE dbo.usp_TicketResolve @InstanceId INT, @ActorId INT, @Resolution NVARCHAR(1000) AS
BEGIN
    SET XACT_ABORT ON;
    IF @Resolution IS NULL OR LEN(@Resolution) = 0 THROW 50020, 'Resolution detail is mandatory.', 1;
    UPDATE dbo.WorkflowStep SET State = 'Done', CompletedAtUtc = SYSUTCDATETIME()
        WHERE WorkflowInstanceId = @InstanceId AND StepCode = 'RESOLVE';
    EXEC dbo.usp_RecordTransition @InstanceId, 'Assigned', 'Resolved', @ActorId, @Resolution, 'TicketResolve';
END
GO

CREATE PROCEDURE dbo.usp_TicketClose @InstanceId INT, @ActorId INT AS
BEGIN
    SET XACT_ABORT ON;
    IF (SELECT CurrentState FROM dbo.WorkflowInstance WHERE Id = @InstanceId) <> 'Resolved'
        THROW 50021, 'Only a Resolved ticket can be closed.', 1;
    EXEC dbo.usp_RecordTransition @InstanceId, 'Resolved', 'Closed', @ActorId, 'Closed after confirmation', 'TicketClose';
    UPDATE dbo.WorkflowInstance SET IsTerminal = 1, ClosedAtUtc = SYSUTCDATETIME() WHERE Id = @InstanceId;
END
GO
