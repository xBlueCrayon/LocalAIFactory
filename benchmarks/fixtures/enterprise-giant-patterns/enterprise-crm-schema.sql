-- FINAL-ENTERPRISE-REASONING: synthetic CRM schema (ORIGINAL, committed).
-- Represents the PUBLIC, high-level CRM pattern family (Dynamics 365 / Dataverse / Salesforce-style:
-- customer, contact, account, lead, opportunity, stage history, discount approval). This is NOT a clone of
-- any vendor's schema, UI, or documentation — only the generic, widely-known entity pattern is modelled.
CREATE TABLE dbo.Customer (
    Id INT NOT NULL PRIMARY KEY, Name NVARCHAR(200) NOT NULL, Segment NVARCHAR(50) NULL, Status NVARCHAR(40) NOT NULL);
GO
CREATE TABLE dbo.Account (
    Id INT NOT NULL PRIMARY KEY, CustomerId INT NOT NULL, AccountManager NVARCHAR(100) NULL,
    CONSTRAINT FK_Account_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(Id));
GO
CREATE TABLE dbo.Contact (
    Id INT NOT NULL PRIMARY KEY, CustomerId INT NOT NULL, Email NVARCHAR(200) NULL, IsPrimary BIT NOT NULL,
    CONSTRAINT FK_Contact_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(Id));
GO
CREATE TABLE dbo.Lead (
    Id INT NOT NULL PRIMARY KEY, CustomerId INT NULL, Source NVARCHAR(60) NULL, Status NVARCHAR(40) NOT NULL);
GO
CREATE TABLE dbo.Opportunity (
    Id INT NOT NULL PRIMARY KEY, CustomerId INT NOT NULL, Stage NVARCHAR(40) NOT NULL, Amount DECIMAL(18,2) NOT NULL,
    DiscountPct DECIMAL(5,2) NOT NULL,
    CONSTRAINT FK_Opportunity_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(Id));
GO
CREATE TABLE dbo.OpportunityStageHistory (
    Id INT NOT NULL PRIMARY KEY, OpportunityId INT NOT NULL, FromStage NVARCHAR(40) NULL, ToStage NVARCHAR(40) NOT NULL,
    ChangedBy NVARCHAR(100) NOT NULL, ChangedAtUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_StageHistory_Opportunity FOREIGN KEY (OpportunityId) REFERENCES dbo.Opportunity(Id));
GO
CREATE TABLE dbo.DiscountApproval (
    Id INT NOT NULL PRIMARY KEY, OpportunityId INT NOT NULL, RequestedPct DECIMAL(5,2) NOT NULL,
    ApproverRole NVARCHAR(40) NOT NULL, MakerUser NVARCHAR(100) NOT NULL, CheckerUser NVARCHAR(100) NULL,
    Approved BIT NOT NULL,
    CONSTRAINT FK_DiscountApproval_Opportunity FOREIGN KEY (OpportunityId) REFERENCES dbo.Opportunity(Id));
GO
CREATE PROCEDURE dbo.usp_ApproveDiscount @DiscountApprovalId INT, @CheckerUser NVARCHAR(100) AS
BEGIN
    -- Maker/checker control: the checker must differ from the maker (segregation of duties).
    UPDATE dbo.DiscountApproval SET Approved = 1, CheckerUser = @CheckerUser
    WHERE Id = @DiscountApprovalId AND MakerUser <> @CheckerUser;
END
GO
