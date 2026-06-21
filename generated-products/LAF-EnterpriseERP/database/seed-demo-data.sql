-- LAF Enterprise ERP - illustrative demo seed (T-SQL)
-- NOTE: The AUTHORITATIVE, idempotent seed is LafErp.Services/DataSeeder.cs, which the app runs
-- automatically on startup (EnsureCreated + DataSeeder.Seed + DemoData.Post). This file documents the
-- shape of the seed for reviewers; it is not the runtime path. Account ids below are illustrative.

-- Currency / Company / Fiscal Year
INSERT INTO Currencies (Code, Name, Symbol, CreatedUtc, CreatedBy, IsDeleted) VALUES ('USD','US Dollar','$', SYSUTCDATETIME(),'system',0);
INSERT INTO Companies (Name, Abbreviation, DefaultCurrencyId, CreatedUtc, CreatedBy, IsDeleted) VALUES ('LAF Demo Corp','LAF',1, SYSUTCDATETIME(),'system',0);
INSERT INTO FiscalYears (Name, StartDate, EndDate, IsClosed, CreatedUtc, CreatedBy, IsDeleted) VALUES ('2026','2026-01-01','2026-12-31',0, SYSUTCDATETIME(),'system',0);

-- Chart of accounts (group + leaf). RootType: 0=Asset 1=Liability 2=Equity 3=Income 4=Expense
-- Groups
INSERT INTO Accounts (CompanyId, Code, Name, RootType, IsGroup, CreatedUtc, CreatedBy, IsDeleted) VALUES
 (1,'1000','Assets',0,1, SYSUTCDATETIME(),'system',0),
 (1,'2000','Liabilities',1,1, SYSUTCDATETIME(),'system',0),
 (1,'3000','Equity',2,1, SYSUTCDATETIME(),'system',0),
 (1,'4000','Income',3,1, SYSUTCDATETIME(),'system',0),
 (1,'5000','Expenses',4,1, SYSUTCDATETIME(),'system',0);
-- Leaf accounts (Debtors/Creditors are control accounts carrying party)
INSERT INTO Accounts (CompanyId, Code, Name, RootType, IsGroup, PartyTypeRequired, CreatedUtc, CreatedBy, IsDeleted) VALUES
 (1,'1100','Cash',0,0,NULL, SYSUTCDATETIME(),'system',0),
 (1,'1110','Bank',0,0,NULL, SYSUTCDATETIME(),'system',0),
 (1,'1200','Debtors',0,0,0, SYSUTCDATETIME(),'system',0),
 (1,'1300','Stock In Hand',0,0,NULL, SYSUTCDATETIME(),'system',0),
 (1,'2100','Creditors',1,0,1, SYSUTCDATETIME(),'system',0),
 (1,'2200','Tax Payable',1,0,NULL, SYSUTCDATETIME(),'system',0),
 (1,'4100','Sales',3,0,NULL, SYSUTCDATETIME(),'system',0),
 (1,'5100','Cost of Goods Sold',4,0,NULL, SYSUTCDATETIME(),'system',0);

-- Roles
INSERT INTO AppRoles (Name, CreatedUtc, CreatedBy, IsDeleted) VALUES
 ('System Manager',SYSUTCDATETIME(),'system',0),('Accounts User',SYSUTCDATETIME(),'system',0),
 ('Accounts Manager',SYSUTCDATETIME(),'system',0),('Stock User',SYSUTCDATETIME(),'system',0),
 ('Sales User',SYSUTCDATETIME(),'system',0),('Purchase User',SYSUTCDATETIME(),'system',0);

-- Approval workflows (amount above threshold needs a separate Accounts Manager; maker != checker)
INSERT INTO WorkflowDefinitions (DocType, Name, SubmitRole, ApproverRole, ApprovalThreshold, MakerCannotApprove, CreatedUtc, CreatedBy, IsDeleted) VALUES
 ('SalesInvoice','SalesInvoice Approval','Sales User','Accounts Manager',1000,1,SYSUTCDATETIME(),'system',0),
 ('PurchaseInvoice','PurchaseInvoice Approval','Purchase User','Accounts Manager',1000,1,SYSUTCDATETIME(),'system',0),
 ('PaymentEntry','PaymentEntry Approval','Accounts User','Accounts Manager',500,1,SYSUTCDATETIME(),'system',0),
 ('JournalEntry','JournalEntry Approval','Accounts User','Accounts Manager',0,1,SYSUTCDATETIME(),'system',0);

-- Demo transactions (sales/purchase invoices, GL & stock postings) are produced programmatically by
-- DemoData.Post so that double-entry GL and the stock ledger are generated through the real services,
-- not hand-inserted. See LafErp.Services/DemoData.cs.
