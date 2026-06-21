-- FINAL-ENTERPRISE-REASONING: synthetic ERP schema (ORIGINAL, committed).
-- Represents the PUBLIC, high-level ERP pattern family (SAP S/4HANA / Business One / Oracle E-Business /
-- NetSuite-style: order-to-cash, procure-to-pay, inventory valuation, GL posting). NOT a clone of any
-- vendor schema or documentation — only the generic finance/procurement/inventory pattern is modelled.
CREATE TABLE dbo.Supplier (
    Id INT NOT NULL PRIMARY KEY, Name NVARCHAR(200) NOT NULL, Active BIT NOT NULL);
GO
CREATE TABLE dbo.SalesOrder (
    Id INT NOT NULL PRIMARY KEY, CustomerId INT NOT NULL, Status NVARCHAR(40) NOT NULL, Total DECIMAL(18,2) NOT NULL);
GO
CREATE TABLE dbo.SalesOrderLine (
    Id INT NOT NULL PRIMARY KEY, SalesOrderId INT NOT NULL, ItemId INT NOT NULL, Qty INT NOT NULL, UnitPrice DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_SalesOrderLine_SalesOrder FOREIGN KEY (SalesOrderId) REFERENCES dbo.SalesOrder(Id));
GO
CREATE TABLE dbo.Invoice (
    Id INT NOT NULL PRIMARY KEY, SalesOrderId INT NOT NULL, Amount DECIMAL(18,2) NOT NULL, Posted BIT NOT NULL,
    CONSTRAINT FK_Invoice_SalesOrder FOREIGN KEY (SalesOrderId) REFERENCES dbo.SalesOrder(Id));
GO
CREATE TABLE dbo.PurchaseOrder (
    Id INT NOT NULL PRIMARY KEY, SupplierId INT NOT NULL, Status NVARCHAR(40) NOT NULL, Total DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_PurchaseOrder_Supplier FOREIGN KEY (SupplierId) REFERENCES dbo.Supplier(Id));
GO
CREATE TABLE dbo.PurchaseOrderApproval (
    Id INT NOT NULL PRIMARY KEY, PurchaseOrderId INT NOT NULL, ApproverRole NVARCHAR(40) NOT NULL,
    MakerUser NVARCHAR(100) NOT NULL, CheckerUser NVARCHAR(100) NULL, Approved BIT NOT NULL,
    CONSTRAINT FK_POApproval_PurchaseOrder FOREIGN KEY (PurchaseOrderId) REFERENCES dbo.PurchaseOrder(Id));
GO
CREATE TABLE dbo.GoodsReceipt (
    Id INT NOT NULL PRIMARY KEY, PurchaseOrderId INT NOT NULL, ReceivedQty INT NOT NULL, ReceivedAtUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_GoodsReceipt_PurchaseOrder FOREIGN KEY (PurchaseOrderId) REFERENCES dbo.PurchaseOrder(Id));
GO
CREATE TABLE dbo.InventoryItem (
    Id INT NOT NULL PRIMARY KEY, Sku NVARCHAR(60) NOT NULL, OnHand INT NOT NULL, StandardCost DECIMAL(18,4) NOT NULL);
GO
CREATE TABLE dbo.StockMovement (
    Id INT NOT NULL PRIMARY KEY, ItemId INT NOT NULL, Delta INT NOT NULL, Reason NVARCHAR(80) NULL, MovedAtUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_StockMovement_Item FOREIGN KEY (ItemId) REFERENCES dbo.InventoryItem(Id));
GO
CREATE TABLE dbo.InventoryValuation (
    Id INT NOT NULL PRIMARY KEY, ItemId INT NOT NULL, Method NVARCHAR(20) NOT NULL, UnitValue DECIMAL(18,4) NOT NULL, AsOfUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_InventoryValuation_Item FOREIGN KEY (ItemId) REFERENCES dbo.InventoryItem(Id));
GO
CREATE TABLE dbo.GlAccount (
    Id INT NOT NULL PRIMARY KEY, Code NVARCHAR(20) NOT NULL, Name NVARCHAR(120) NOT NULL, Type NVARCHAR(20) NOT NULL);
GO
CREATE TABLE dbo.GlJournal (
    Id INT NOT NULL PRIMARY KEY, GlAccountId INT NOT NULL, Debit DECIMAL(18,2) NOT NULL, Credit DECIMAL(18,2) NOT NULL,
    Source NVARCHAR(40) NOT NULL, PostedAtUtc DATETIME2 NOT NULL,
    CONSTRAINT FK_GlJournal_GlAccount FOREIGN KEY (GlAccountId) REFERENCES dbo.GlAccount(Id));
GO
CREATE PROCEDURE dbo.usp_PostToGl @GlAccountId INT, @Debit DECIMAL(18,2), @Credit DECIMAL(18,2), @Source NVARCHAR(40) AS
BEGIN
    INSERT INTO dbo.GlJournal (GlAccountId, Debit, Credit, Source, PostedAtUtc)
    VALUES (@GlAccountId, @Debit, @Credit, @Source, SYSUTCDATETIME());
END
GO
CREATE PROCEDURE dbo.usp_ApprovePurchaseOrder @PurchaseOrderApprovalId INT, @CheckerUser NVARCHAR(100) AS
BEGIN
    UPDATE dbo.PurchaseOrderApproval SET Approved = 1, CheckerUser = @CheckerUser
    WHERE Id = @PurchaseOrderApprovalId AND MakerUser <> @CheckerUser;
END
GO
