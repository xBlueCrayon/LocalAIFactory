-- R2-ACC-CAP3: synthetic Python↔SQL bridge fixture (original, committed — not third-party source).
CREATE TABLE dbo.Invoices (
    Id        INT           NOT NULL PRIMARY KEY,
    Amount    DECIMAL(18,2) NOT NULL
);
GO

CREATE PROCEDURE dbo.usp_PostInvoice
    @Id INT
AS
BEGIN
    SELECT Id, Amount FROM dbo.Invoices WHERE Id = @Id;
END
GO
