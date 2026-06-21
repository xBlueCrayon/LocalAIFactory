-- FINAL-ENTERPRISE-REASONING: synthetic ITSM schema (ORIGINAL, committed).
-- Represents the PUBLIC, high-level IT-service-management pattern family (ServiceNow-style incident /
-- change / SLA). NOT a clone of ServiceNow's schema, tables, UI, or documentation — only the generic,
-- widely-taught ITSM lifecycle (incident -> resolution -> audit; change -> approval; SLA breach) is modelled.
CREATE TABLE dbo.Incident (
    Id INT NOT NULL PRIMARY KEY, Title NVARCHAR(200) NOT NULL, Priority NVARCHAR(20) NOT NULL,
    State NVARCHAR(30) NOT NULL, AssignedTo NVARCHAR(100) NULL, OpenedAtUtc DATETIME2 NOT NULL, ResolvedAtUtc DATETIME2 NULL);
GO
CREATE TABLE dbo.IncidentAudit (
    Id INT NOT NULL PRIMARY KEY, IncidentId INT NOT NULL, Action NVARCHAR(60) NOT NULL, ActorUser NVARCHAR(100) NOT NULL,
    AtUtc DATETIME2 NOT NULL, Notes NVARCHAR(400) NULL,
    CONSTRAINT FK_IncidentAudit_Incident FOREIGN KEY (IncidentId) REFERENCES dbo.Incident(Id));
GO
CREATE TABLE dbo.ChangeRequest (
    Id INT NOT NULL PRIMARY KEY, Title NVARCHAR(200) NOT NULL, RiskLevel NVARCHAR(20) NOT NULL,
    State NVARCHAR(30) NOT NULL, RequestedBy NVARCHAR(100) NOT NULL, PlannedStartUtc DATETIME2 NULL);
GO
CREATE TABLE dbo.ChangeApproval (
    Id INT NOT NULL PRIMARY KEY, ChangeRequestId INT NOT NULL, ApproverRole NVARCHAR(40) NOT NULL,
    MakerUser NVARCHAR(100) NOT NULL, CheckerUser NVARCHAR(100) NULL, Approved BIT NOT NULL,
    CONSTRAINT FK_ChangeApproval_ChangeRequest FOREIGN KEY (ChangeRequestId) REFERENCES dbo.ChangeRequest(Id));
GO
CREATE TABLE dbo.SlaBreach (
    Id INT NOT NULL PRIMARY KEY, IncidentId INT NOT NULL, SlaName NVARCHAR(60) NOT NULL, BreachedAtUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_SlaBreach_Incident FOREIGN KEY (IncidentId) REFERENCES dbo.Incident(Id));
GO
CREATE PROCEDURE dbo.usp_ApproveChangeRequest @ChangeApprovalId INT, @CheckerUser NVARCHAR(100) AS
BEGIN
    -- Change approval requires a checker distinct from the maker before the change can proceed.
    UPDATE dbo.ChangeApproval SET Approved = 1, CheckerUser = @CheckerUser
    WHERE Id = @ChangeApprovalId AND MakerUser <> @CheckerUser;
END
GO
