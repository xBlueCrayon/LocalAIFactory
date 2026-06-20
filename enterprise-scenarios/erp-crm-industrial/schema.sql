-- R2-ACC-INDUSTRIAL: synthetic ERP/CRM schema (original, committed — NOT a clone of any vendor product).
-- Exercises ERP/CRM entity modelling + the C#↔SQL bridge over realistic master/transaction tables.
CREATE TABLE dbo.Customer (
    Id INT NOT NULL PRIMARY KEY, Name NVARCHAR(200) NOT NULL, Segment NVARCHAR(50) NULL);
GO
CREATE TABLE dbo.Contact (
    Id INT NOT NULL PRIMARY KEY, CustomerId INT NOT NULL, Email NVARCHAR(200) NULL,
    CONSTRAINT FK_Contact_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(Id));
GO
CREATE TABLE dbo.Lead (
    Id INT NOT NULL PRIMARY KEY, CustomerId INT NULL, Status NVARCHAR(40) NOT NULL);
GO
CREATE TABLE dbo.Opportunity (
    Id INT NOT NULL PRIMARY KEY, CustomerId INT NOT NULL, Stage NVARCHAR(40) NOT NULL, Amount DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_Opportunity_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(Id));
GO
CREATE TABLE dbo.PriceList (
    Id INT NOT NULL PRIMARY KEY, ItemId INT NOT NULL, UnitPrice DECIMAL(18,2) NOT NULL);
GO
CREATE TABLE dbo.SalesOrder (
    Id INT NOT NULL PRIMARY KEY, CustomerId INT NOT NULL, Status NVARCHAR(40) NOT NULL, Total DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_SalesOrder_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer(Id));
GO
CREATE TABLE dbo.SalesOrderLine (
    Id INT NOT NULL PRIMARY KEY, SalesOrderId INT NOT NULL, ItemId INT NOT NULL, Qty INT NOT NULL,
    CONSTRAINT FK_SalesOrderLine_SalesOrder FOREIGN KEY (SalesOrderId) REFERENCES dbo.SalesOrder(Id));
GO
CREATE TABLE dbo.Invoice (
    Id INT NOT NULL PRIMARY KEY, SalesOrderId INT NOT NULL, Amount DECIMAL(18,2) NOT NULL, Posted BIT NOT NULL,
    CONSTRAINT FK_Invoice_SalesOrder FOREIGN KEY (SalesOrderId) REFERENCES dbo.SalesOrder(Id));
GO
CREATE TABLE dbo.PurchaseOrder (
    Id INT NOT NULL PRIMARY KEY, SupplierId INT NOT NULL, Total DECIMAL(18,2) NOT NULL);
GO
CREATE TABLE dbo.InventoryItem (
    Id INT NOT NULL PRIMARY KEY, Sku NVARCHAR(60) NOT NULL, OnHand INT NOT NULL);
GO
CREATE TABLE dbo.InventoryMovement (
    Id INT NOT NULL PRIMARY KEY, ItemId INT NOT NULL, Delta INT NOT NULL, Reason NVARCHAR(80) NULL,
    CONSTRAINT FK_InventoryMovement_Item FOREIGN KEY (ItemId) REFERENCES dbo.InventoryItem(Id));
GO
CREATE TABLE dbo.DiscountApproval (
    Id INT NOT NULL PRIMARY KEY, OpportunityId INT NOT NULL, ApproverRole NVARCHAR(40) NOT NULL, Approved BIT NOT NULL);
GO
CREATE PROCEDURE dbo.usp_PostInvoice @InvoiceId INT AS
BEGIN
    UPDATE dbo.Invoice SET Posted = 1 WHERE Id = @InvoiceId;
END
GO
