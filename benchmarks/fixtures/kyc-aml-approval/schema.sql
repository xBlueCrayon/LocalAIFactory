-- R2-ACC-INDUSTRIAL: synthetic KYC/AML transaction-approval schema (original, committed — NOT a vendor product
-- and NOT a regulatory artifact). Models an onboarding (KYC) -> screening (AML) -> maker/checker approval ->
-- transaction-approval surface so the C#<->SQL bridge can answer "what touches X", "what runs proc Y", and
-- impact-of-change questions. This is a structural fixture for graph proofs, not a compliance control set.
CREATE TABLE dbo.Customer (
    Id INT NOT NULL PRIMARY KEY, LegalName NVARCHAR(200) NOT NULL, CustomerType NVARCHAR(20) NOT NULL, Status NVARCHAR(20) NOT NULL);
GO
CREATE TABLE dbo.CustomerRiskRating (
    Id INT NOT NULL PRIMARY KEY, CustomerId INT NOT NULL, RiskBand NVARCHAR(20) NOT NULL, Score INT NOT NULL, RatedUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_RiskRating_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(Id));
GO
CREATE TABLE dbo.IdentityDocument (
    Id INT NOT NULL PRIMARY KEY, CustomerId INT NOT NULL, DocType NVARCHAR(40) NOT NULL, DocReference NVARCHAR(80) NOT NULL, Verified BIT NOT NULL,
    CONSTRAINT FK_IdentityDoc_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(Id),
    CONSTRAINT UQ_IdentityDoc_Ref UNIQUE (DocType, DocReference));
GO
CREATE TABLE dbo.ScreeningResult (
    Id INT NOT NULL PRIMARY KEY, CustomerId INT NOT NULL, ListType NVARCHAR(30) NOT NULL, MatchStatus NVARCHAR(20) NOT NULL, ScreenedUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_Screening_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(Id));
GO
CREATE TABLE dbo.OnboardingCase (
    Id INT NOT NULL PRIMARY KEY, CustomerId INT NOT NULL, Stage NVARCHAR(30) NOT NULL, Outcome NVARCHAR(20) NULL,
    CONSTRAINT FK_OnboardingCase_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(Id));
GO
CREATE TABLE dbo.OnboardingApproval (
    Id INT NOT NULL PRIMARY KEY, OnboardingCaseId INT NOT NULL, MakerUser NVARCHAR(80) NOT NULL, CheckerUser NVARCHAR(80) NULL, Decision NVARCHAR(20) NOT NULL,
    CONSTRAINT FK_OnboardingApproval_Case FOREIGN KEY (OnboardingCaseId) REFERENCES dbo.OnboardingCase(Id),
    CONSTRAINT UQ_OnboardingApproval_Case UNIQUE (OnboardingCaseId, MakerUser));
GO
CREATE TABLE dbo.ApprovalLimit (
    Id INT NOT NULL PRIMARY KEY, RiskBand NVARCHAR(20) NOT NULL, ApproverRole NVARCHAR(40) NOT NULL, MaxAmount DECIMAL(18,2) NOT NULL,
    CONSTRAINT UQ_ApprovalLimit_Band_Role UNIQUE (RiskBand, ApproverRole));
GO
CREATE TABLE dbo.TransactionRequest (
    Id INT NOT NULL PRIMARY KEY, CustomerId INT NOT NULL, Amount DECIMAL(18,2) NOT NULL, Currency NVARCHAR(3) NOT NULL,
    Status NVARCHAR(20) NOT NULL, IdempotencyKey NVARCHAR(64) NOT NULL,
    CONSTRAINT FK_TxnRequest_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(Id),
    CONSTRAINT UQ_TxnRequest_Idem UNIQUE (IdempotencyKey));
GO
CREATE TABLE dbo.TransactionApproval (
    Id INT NOT NULL PRIMARY KEY, TransactionRequestId INT NOT NULL, MakerUser NVARCHAR(80) NOT NULL, CheckerUser NVARCHAR(80) NULL,
    Decision NVARCHAR(20) NOT NULL, DecidedUtc DATETIME2 NULL,
    CONSTRAINT FK_TxnApproval_Request FOREIGN KEY (TransactionRequestId) REFERENCES dbo.TransactionRequest(Id),
    CONSTRAINT UQ_TxnApproval_Request_Maker UNIQUE (TransactionRequestId, MakerUser));
GO
CREATE TABLE dbo.AmlAlert (
    Id INT NOT NULL PRIMARY KEY, TransactionRequestId INT NOT NULL, AlertType NVARCHAR(40) NOT NULL, Disposition NVARCHAR(20) NULL,
    CONSTRAINT FK_AmlAlert_Request FOREIGN KEY (TransactionRequestId) REFERENCES dbo.TransactionRequest(Id));
GO
-- Submit a transaction for approval (idempotent via the request's key check — duplicate-submit control). The
-- maker is recorded so the checker step can enforce maker<>checker segregation.
CREATE PROCEDURE dbo.usp_SubmitTransactionForApproval @CustomerId INT, @Amount DECIMAL(18,2), @Currency NVARCHAR(3), @MakerUser NVARCHAR(80), @Idem NVARCHAR(64) AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.TransactionRequest WHERE IdempotencyKey = @Idem)
    BEGIN
        INSERT INTO dbo.TransactionRequest (CustomerId, Amount, Currency, Status, IdempotencyKey)
            VALUES (@CustomerId, @Amount, @Currency, 'PendingApproval', @Idem);
        INSERT INTO dbo.TransactionApproval (TransactionRequestId, MakerUser, Decision)
            SELECT Id, @MakerUser, 'Submitted' FROM dbo.TransactionRequest WHERE IdempotencyKey = @Idem;
    END
END
GO
-- Checker decision. Maker/checker is enforced: the checker may not be the recorded maker, and the decision is
-- only applied to a request still pending. Approval flips the request to Approved; otherwise it is Rejected.
CREATE PROCEDURE dbo.usp_ApproveTransaction @TransactionRequestId INT, @CheckerUser NVARCHAR(80), @Approve BIT AS
BEGIN
    IF EXISTS (
        SELECT 1 FROM dbo.TransactionApproval
        WHERE TransactionRequestId = @TransactionRequestId AND MakerUser <> @CheckerUser AND Decision = 'Submitted')
    BEGIN
        UPDATE dbo.TransactionApproval
            SET CheckerUser = @CheckerUser, Decision = CASE WHEN @Approve = 1 THEN 'Approved' ELSE 'Rejected' END, DecidedUtc = SYSUTCDATETIME()
            WHERE TransactionRequestId = @TransactionRequestId;
        UPDATE dbo.TransactionRequest
            SET Status = CASE WHEN @Approve = 1 THEN 'Approved' ELSE 'Rejected' END
            WHERE Id = @TransactionRequestId;
    END
END
GO
