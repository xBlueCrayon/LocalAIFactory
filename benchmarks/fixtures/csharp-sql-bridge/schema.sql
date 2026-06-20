-- R2-ACC-CAP2: synthetic C#↔SQL bridge fixture (original, committed — not third-party source).
CREATE TABLE dbo.Customers (
    Id          INT          NOT NULL PRIMARY KEY,
    Name        NVARCHAR(200) NOT NULL
);
GO

CREATE TABLE dbo.Orders (
    Id          INT          NOT NULL PRIMARY KEY,
    CustomerId  INT          NOT NULL,
    Total       DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id)
);
GO

CREATE PROCEDURE dbo.usp_GetCustomerOrders
    @CustomerId INT
AS
BEGIN
    SELECT o.Id, o.Total
    FROM dbo.Orders o
    WHERE o.CustomerId = @CustomerId;
END
GO
