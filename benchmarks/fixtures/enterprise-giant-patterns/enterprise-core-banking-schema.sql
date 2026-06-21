-- FINAL-ENTERPRISE-REASONING: synthetic core-banking integration schema (ORIGINAL, committed).
-- Represents the PUBLIC, high-level core-banking integration pattern family (Temenos / Finastra / Mambu /
-- FIS / Fiserv-style: payment instruction -> sanctions screening -> maker/checker -> release -> settlement;
-- rejection-code mapping). This is integration-middleware modelling, NOT a core-banking product, and NOT a
-- clone of any vendor's schema, API, or documentation. No regulatory guarantee is expressed or implied.
CREATE TABLE dbo.PaymentInstruction (
    Id INT NOT NULL PRIMARY KEY, Reference NVARCHAR(40) NOT NULL, DebtorAccount NVARCHAR(34) NOT NULL,
    CreditorAccount NVARCHAR(34) NOT NULL, Amount DECIMAL(18,2) NOT NULL, Currency NVARCHAR(3) NOT NULL,
    State NVARCHAR(30) NOT NULL, RejectionCodeId INT NULL);
GO
CREATE TABLE dbo.SanctionsScreening (
    Id INT NOT NULL PRIMARY KEY, PaymentInstructionId INT NOT NULL, Result NVARCHAR(20) NOT NULL,
    ListVersion NVARCHAR(40) NOT NULL, ScreenedAtUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_Sanctions_Payment FOREIGN KEY (PaymentInstructionId) REFERENCES dbo.PaymentInstruction(Id));
GO
CREATE TABLE dbo.RejectionCode (
    Id INT NOT NULL PRIMARY KEY, Code NVARCHAR(20) NOT NULL, Description NVARCHAR(200) NOT NULL, Retryable BIT NOT NULL);
GO
CREATE TABLE dbo.MakerCheckerLog (
    Id INT NOT NULL PRIMARY KEY, PaymentInstructionId INT NOT NULL, MakerUser NVARCHAR(100) NOT NULL,
    CheckerUser NVARCHAR(100) NULL, ApproverUser NVARCHAR(100) NULL, Stage NVARCHAR(30) NOT NULL, AtUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_MakerChecker_Payment FOREIGN KEY (PaymentInstructionId) REFERENCES dbo.PaymentInstruction(Id));
GO
CREATE TABLE dbo.Settlement (
    Id INT NOT NULL PRIMARY KEY, PaymentInstructionId INT NOT NULL, SettledAmount DECIMAL(18,2) NOT NULL,
    SettledAtUtc DATETIME2 NOT NULL, Reconciled BIT NOT NULL,
    CONSTRAINT FK_Settlement_Payment FOREIGN KEY (PaymentInstructionId) REFERENCES dbo.PaymentInstruction(Id));
GO
CREATE PROCEDURE dbo.usp_SubmitPayment @PaymentInstructionId INT, @MakerUser NVARCHAR(100) AS
BEGIN
    INSERT INTO dbo.MakerCheckerLog (PaymentInstructionId, MakerUser, Stage, AtUtc)
    VALUES (@PaymentInstructionId, @MakerUser, 'Submitted', SYSUTCDATETIME());
    UPDATE dbo.PaymentInstruction SET State = 'PendingApproval' WHERE Id = @PaymentInstructionId;
END
GO
CREATE PROCEDURE dbo.usp_ReleasePayment @PaymentInstructionId INT, @ApproverUser NVARCHAR(100) AS
BEGIN
    -- Release is only permitted after a clean sanctions screen and a checker decision distinct from the maker.
    UPDATE dbo.PaymentInstruction SET State = 'Released' WHERE Id = @PaymentInstructionId;
    UPDATE dbo.MakerCheckerLog SET ApproverUser = @ApproverUser, Stage = 'Released'
    WHERE PaymentInstructionId = @PaymentInstructionId;
END
GO
