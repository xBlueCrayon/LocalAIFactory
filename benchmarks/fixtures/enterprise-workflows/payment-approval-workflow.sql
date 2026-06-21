-- ENTERPRISE-WORKFLOWS FIXTURE: synthetic payment-authorization workflow (ORIGINAL, committed).
-- Demonstrates the CORRECT order of controls: validate -> SCREEN -> authorize by limit -> settle.
-- Screening happens BEFORE authorization; a screening hit holds the payment for compliance.
-- Synthetic prototype only; codes/limits are illustrative.

INSERT INTO dbo.WorkflowDefinition (Id, [Key], Name, Version, IsActive, CreatedAtUtc)
    VALUES (2, 'payment-authorization', 'Payment Authorization', 1, 1, SYSUTCDATETIME());
GO
INSERT INTO dbo.WorkflowPolicy (Id, WorkflowDefinitionId, PolicyKey, PolicyValue, EffectiveFromUtc) VALUES
    (2, 2, 'Tier1LimitAmount', '50000', SYSUTCDATETIME()),
    (3, 2, 'Tier2LimitAmount', '500000', SYSUTCDATETIME());
GO

-- Step 1: initiate with a duplicate/idempotency guard.
CREATE PROCEDURE dbo.usp_PaymentInitiate @InstanceId INT, @ActorId INT, @IdempotencyKey NVARCHAR(80) AS
BEGIN
    SET XACT_ABORT ON;
    IF EXISTS (SELECT 1 FROM dbo.WorkflowComment c JOIN dbo.WorkflowInstance i ON i.Id = c.WorkflowInstanceId
               WHERE c.Body = CONCAT('idem:', @IdempotencyKey) AND i.WorkflowDefinitionId = 2 AND i.Id <> @InstanceId)
        THROW 50010, 'Duplicate payment (idempotency key already used).', 1;
    INSERT INTO dbo.WorkflowComment (Id, WorkflowInstanceId, AuthorActorId, Body, CreatedAtUtc)
        VALUES ((SELECT ISNULL(MAX(Id),0)+1 FROM dbo.WorkflowComment), @InstanceId, @ActorId, CONCAT('idem:', @IdempotencyKey), SYSUTCDATETIME());
    EXEC dbo.usp_RecordTransition @InstanceId, 'Initiated', 'PendingScreening', @ActorId, 'Initiated', 'PaymentInitiate';
END
GO

-- Step 2: sanctions/AML screening BEFORE any authorization. A hit holds the payment.
CREATE PROCEDURE dbo.usp_PaymentScreen @InstanceId INT, @ActorId INT, @ScreeningResult NVARCHAR(20) AS
BEGIN
    SET XACT_ABORT ON;
    INSERT INTO dbo.WorkflowRiskSignal (Id, WorkflowInstanceId, SignalType, Band, RaisedAtUtc)
        VALUES ((SELECT ISNULL(MAX(Id),0)+1 FROM dbo.WorkflowRiskSignal), @InstanceId, 'SanctionsScreening', @ScreeningResult, SYSUTCDATETIME());
    IF @ScreeningResult = 'Hit'
    BEGIN
        EXEC dbo.usp_RecordTransition @InstanceId, 'PendingScreening', 'ComplianceHold', @ActorId, 'Screening hit; held for compliance', 'PaymentScreen';
        INSERT INTO dbo.WorkflowException (Id, WorkflowInstanceId, ExceptionType, Severity, State, Details, RaisedAtUtc)
            VALUES ((SELECT ISNULL(MAX(Id),0)+1 FROM dbo.WorkflowException), @InstanceId, 'ScreeningHit', 'High', 'Open', 'Potential sanctions match', SYSUTCDATETIME());
    END
    ELSE
        EXEC dbo.usp_RecordTransition @InstanceId, 'PendingScreening', 'PendingAuthorization', @ActorId, 'Screening clear', 'PaymentScreen';
END
GO

-- Step 3: limit-based authorization. The authorizer must hold sufficient limit for the amount.
CREATE PROCEDURE dbo.usp_PaymentAuthorize @InstanceId INT, @ActorId INT, @Amount DECIMAL(18,2), @ActorLimit DECIMAL(18,2) AS
BEGIN
    SET XACT_ABORT ON;
    IF @ActorLimit < @Amount THROW 50011, 'Authorizer limit is below the payment amount.', 1;
    EXEC dbo.usp_RecordTransition @InstanceId, 'PendingAuthorization', 'Authorized', @ActorId, 'Authorized within limit', 'PaymentAuthorize';
END
GO

-- Step 4: settlement is the terminal success state; only an Authorized instance may settle.
CREATE PROCEDURE dbo.usp_PaymentSettle @InstanceId INT, @ActorId INT AS
BEGIN
    SET XACT_ABORT ON;
    IF (SELECT CurrentState FROM dbo.WorkflowInstance WHERE Id = @InstanceId) <> 'Authorized'
        THROW 50012, 'Only an Authorized payment can be settled.', 1;
    EXEC dbo.usp_RecordTransition @InstanceId, 'Authorized', 'Settled', @ActorId, 'Settled', 'PaymentSettle';
    UPDATE dbo.WorkflowInstance SET IsTerminal = 1, ClosedAtUtc = SYSUTCDATETIME() WHERE Id = @InstanceId;
END
GO
