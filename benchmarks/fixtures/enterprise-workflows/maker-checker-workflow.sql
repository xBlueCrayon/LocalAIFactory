-- ENTERPRISE-WORKFLOWS FIXTURE: synthetic maker/checker/approver workflow (ORIGINAL, committed).
-- Demonstrates the CORRECT segregation-of-duties pattern over the workflow-core schema:
-- the checker must differ from the maker; the approver gate only applies above a policy threshold;
-- every step records an immutable audit event. Synthetic prototype only.

-- Seed the definition, its roles and the value threshold policy.
INSERT INTO dbo.WorkflowDefinition (Id, [Key], Name, Version, IsActive, CreatedAtUtc)
    VALUES (1, 'maker-checker-approver', 'Maker / Checker / Approver', 1, 1, SYSUTCDATETIME());
GO
INSERT INTO dbo.WorkflowRole (Id, WorkflowDefinitionId, Code, Name) VALUES
    (1, 1, 'MAKER', 'Maker'), (2, 1, 'CHECKER', 'Checker'), (3, 1, 'APPROVER', 'Approver');
GO
INSERT INTO dbo.WorkflowPolicy (Id, WorkflowDefinitionId, PolicyKey, PolicyValue, EffectiveFromUtc)
    VALUES (1, 1, 'ApproverThresholdAmount', '100000', SYSUTCDATETIME());
GO

-- Make: maker creates the instance in Draft and submits it for checking.
CREATE PROCEDURE dbo.usp_MakerSubmit @InstanceId INT, @MakerActorId INT AS
BEGIN
    SET XACT_ABORT ON;
    EXEC dbo.usp_RecordTransition @InstanceId, 'Draft', 'PendingCheck', @MakerActorId, 'Submitted by maker', 'MakerSubmit';
END
GO

-- Check: the checker MUST differ from the maker (segregation of duties).
-- If the instance amount is at/over the policy threshold, route to PendingApproval, else Approved.
CREATE PROCEDURE dbo.usp_CheckerVerify @InstanceId INT, @CheckerActorId INT, @Amount DECIMAL(18,2) AS
BEGIN
    SET XACT_ABORT ON;
    DECLARE @MakerActorId INT = (
        SELECT TOP 1 TriggeredByActorId FROM dbo.WorkflowTransition
        WHERE WorkflowInstanceId = @InstanceId AND ToState = 'PendingCheck' ORDER BY Id);
    IF @MakerActorId = @CheckerActorId
    BEGIN
        -- Self-check is rejected at the control boundary; nothing is committed.
        THROW 50001, 'Segregation of duties violation: checker must differ from maker.', 1;
    END
    DECLARE @threshold DECIMAL(18,2) = (
        SELECT TRY_CAST(PolicyValue AS DECIMAL(18,2)) FROM dbo.WorkflowPolicy
        WHERE WorkflowDefinitionId = 1 AND PolicyKey = 'ApproverThresholdAmount');
    IF @Amount >= @threshold
        EXEC dbo.usp_RecordTransition @InstanceId, 'PendingCheck', 'PendingApproval', @CheckerActorId, 'Checked; above threshold', 'CheckerVerify';
    ELSE
        EXEC dbo.usp_RecordTransition @InstanceId, 'PendingCheck', 'Approved', @CheckerActorId, 'Checked and approved (below threshold)', 'CheckerVerify';
END
GO

-- Approve: independent approver finalises high-value items; approver must differ from maker and checker.
CREATE PROCEDURE dbo.usp_ApproverApprove @InstanceId INT, @ApproverActorId INT AS
BEGIN
    SET XACT_ABORT ON;
    IF EXISTS (SELECT 1 FROM dbo.WorkflowTransition
               WHERE WorkflowInstanceId = @InstanceId AND TriggeredByActorId = @ApproverActorId
                 AND ToState IN ('PendingCheck','PendingApproval'))
        THROW 50002, 'Approver must differ from maker and checker.', 1;
    EXEC dbo.usp_RecordTransition @InstanceId, 'PendingApproval', 'Approved', @ApproverActorId, 'Approved', 'ApproverApprove';
    UPDATE dbo.WorkflowInstance SET IsTerminal = 1, ClosedAtUtc = SYSUTCDATETIME() WHERE Id = @InstanceId;
END
GO

-- Reject: a rejection ALWAYS records a reason; the instance becomes terminal.
CREATE PROCEDURE dbo.usp_Reject @InstanceId INT, @ActorId INT, @Reason NVARCHAR(400) AS
BEGIN
    SET XACT_ABORT ON;
    IF @Reason IS NULL OR LEN(@Reason) = 0 THROW 50003, 'Rejection reason is mandatory.', 1;
    DECLARE @from NVARCHAR(60) = (SELECT CurrentState FROM dbo.WorkflowInstance WHERE Id = @InstanceId);
    EXEC dbo.usp_RecordTransition @InstanceId, @from, 'Rejected', @ActorId, @Reason, 'Reject';
    UPDATE dbo.WorkflowInstance SET IsTerminal = 1, ClosedAtUtc = SYSUTCDATETIME() WHERE Id = @InstanceId;
END
GO
