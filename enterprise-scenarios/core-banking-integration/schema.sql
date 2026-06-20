-- R2-ACC-INDUSTRIAL: synthetic core-banking integration schema (original, committed — NOT a core-banking
-- product). Models the integration/middleware surface (BDM-style direct-debit/settlement), not a ledger engine.
CREATE TABLE dbo.Account (
    Id INT NOT NULL PRIMARY KEY, Number NVARCHAR(34) NOT NULL, Balance DECIMAL(18,2) NOT NULL);
GO
CREATE TABLE dbo.AccountHold (
    Id INT NOT NULL PRIMARY KEY, AccountId INT NOT NULL, Amount DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_Hold_Account FOREIGN KEY (AccountId) REFERENCES dbo.Account(Id));
GO
CREATE TABLE dbo.Mandate (
    Id INT NOT NULL PRIMARY KEY, AccountId INT NOT NULL, Reference NVARCHAR(40) NOT NULL, Status NVARCHAR(20) NOT NULL,
    CONSTRAINT FK_Mandate_Account FOREIGN KEY (AccountId) REFERENCES dbo.Account(Id));
GO
CREATE TABLE dbo.Claim (
    Id INT NOT NULL PRIMARY KEY, MandateId INT NOT NULL, Amount DECIMAL(18,2) NOT NULL, RejectionCode NVARCHAR(10) NULL,
    CONSTRAINT FK_Claim_Mandate FOREIGN KEY (MandateId) REFERENCES dbo.Mandate(Id));
GO
CREATE TABLE dbo.RejectionCode (
    Code NVARCHAR(10) NOT NULL PRIMARY KEY, Description NVARCHAR(200) NOT NULL);
GO
CREATE TABLE dbo.SettlementFile (
    Id INT NOT NULL PRIMARY KEY, FileName NVARCHAR(260) NOT NULL, Status NVARCHAR(20) NOT NULL, Sha256 CHAR(64) NULL);
GO
CREATE TABLE dbo.Posting (
    Id INT NOT NULL PRIMARY KEY, AccountId INT NOT NULL, Amount DECIMAL(18,2) NOT NULL, IdempotencyKey NVARCHAR(64) NOT NULL,
    CONSTRAINT FK_Posting_Account FOREIGN KEY (AccountId) REFERENCES dbo.Account(Id),
    CONSTRAINT UQ_Posting_Idem UNIQUE (IdempotencyKey));
GO
CREATE TABLE dbo.SuspenseQueue (
    Id INT NOT NULL PRIMARY KEY, ClaimId INT NOT NULL, Reason NVARCHAR(200) NULL);
GO
CREATE TABLE dbo.GLEntry (
    Id INT NOT NULL PRIMARY KEY, PostingId INT NOT NULL, Account NVARCHAR(20) NOT NULL, Debit DECIMAL(18,2) NULL, Credit DECIMAL(18,2) NULL);
GO
CREATE TABLE dbo.FileArchive (
    Id INT NOT NULL PRIMARY KEY, FileName NVARCHAR(260) NOT NULL, ArchivedUtc DATETIME2 NOT NULL, Sha256 CHAR(64) NOT NULL);
GO
CREATE PROCEDURE dbo.usp_PostTransaction @AccountId INT, @Amount DECIMAL(18,2), @Idem NVARCHAR(64) AS
BEGIN
    -- idempotent insert: a duplicate idempotency key is ignored (duplicate-posting control)
    IF NOT EXISTS (SELECT 1 FROM dbo.Posting WHERE IdempotencyKey = @Idem)
        INSERT INTO dbo.Posting (AccountId, Amount, IdempotencyKey) VALUES (@AccountId, @Amount, @Idem);
END
GO
CREATE PROCEDURE dbo.usp_ReverseTransaction @PostingId INT AS
BEGIN
    INSERT INTO dbo.GLEntry (PostingId, Account, Debit, Credit) SELECT Id, 'REVERSAL', Amount, NULL FROM dbo.Posting WHERE Id = @PostingId;
END
GO
