-- FINAL-ENTERPRISE-REASONING: synthetic reporting/dashboard schema (ORIGINAL, committed).
-- Represents the PUBLIC, high-level reporting pattern family (Power BI / Tableau-style report definitions
-- and dashboard widgets, plus an operations daily snapshot an operating manager would monitor). NOT a clone
-- of any vendor's product, schema, or documentation — only the generic report-definition / dashboard pattern.
CREATE TABLE dbo.ReportDefinition (
    Id INT NOT NULL PRIMARY KEY, Name NVARCHAR(120) NOT NULL, Domain NVARCHAR(40) NOT NULL, OwnerRole NVARCHAR(40) NOT NULL);
GO
CREATE TABLE dbo.DashboardWidget (
    Id INT NOT NULL PRIMARY KEY, ReportDefinitionId INT NOT NULL, WidgetType NVARCHAR(40) NOT NULL, Position INT NOT NULL,
    CONSTRAINT FK_Widget_ReportDefinition FOREIGN KEY (ReportDefinitionId) REFERENCES dbo.ReportDefinition(Id));
GO
CREATE TABLE dbo.OperationsDailySnapshot (
    Id INT NOT NULL PRIMARY KEY, SnapshotDate DATE NOT NULL, OpenIncidents INT NOT NULL, BreachedSlas INT NOT NULL,
    PaymentsPendingApproval INT NOT NULL, PaymentsReleased INT NOT NULL, RejectedPayments INT NOT NULL);
GO
