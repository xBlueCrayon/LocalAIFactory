CREATE TABLE [AppRoles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(450) NOT NULL,
    [Description] nvarchar(max) NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_AppRoles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AppUsers] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(450) NOT NULL,
    [FullName] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_AppUsers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Assets] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [CompanyId] int NOT NULL,
    [ItemId] int NULL,
    [PurchaseValue] decimal(18,4) NOT NULL,
    [PurchaseDate] datetime2 NULL,
    [Status] int NOT NULL,
    [Location] nvarchar(max) NULL,
    [NextMaintenanceDate] datetime2 NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Assets] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AuditEvents] (
    [Id] int NOT NULL IDENTITY,
    [EntityType] nvarchar(max) NOT NULL,
    [EntityId] int NOT NULL,
    [Action] nvarchar(max) NOT NULL,
    [PerformedBy] nvarchar(max) NOT NULL,
    [Details] nvarchar(max) NULL,
    [EventUtc] datetime2 NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_AuditEvents] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CostCenters] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [Code] nvarchar(max) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [IsGroup] bit NOT NULL,
    [ParentCostCenterId] int NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_CostCenters] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Currencies] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Symbol] nvarchar(max) NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Currencies] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Customers] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NULL,
    [Phone] nvarchar(max) NULL,
    [CreditLimit] decimal(18,4) NOT NULL,
    [IsActive] bit NOT NULL,
    [ReceivableAccountId] int NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [FiscalYears] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [IsClosed] bit NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_FiscalYears] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ImportBatches] (
    [Id] int NOT NULL IDENTITY,
    [DocType] nvarchar(max) NOT NULL,
    [FileName] nvarchar(max) NOT NULL,
    [TotalRows] int NOT NULL,
    [ImportedRows] int NOT NULL,
    [FailedRows] int NOT NULL,
    [Errors] nvarchar(max) NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_ImportBatches] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ItemGroups] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [ParentItemGroupId] int NULL,
    [IsGroup] bit NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_ItemGroups] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [JournalEntries] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [Narration] nvarchar(max) NULL,
    [TotalDebit] decimal(18,4) NOT NULL,
    [TotalCredit] decimal(18,4) NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [DocNo] nvarchar(450) NOT NULL,
    [Status] int NOT NULL,
    [PostingDate] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_JournalEntries] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Leads] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Company] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [Phone] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [Source] nvarchar(max) NULL,
    [ConvertedCustomerId] int NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Leads] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [NumberingSeries] (
    [Id] int NOT NULL IDENTITY,
    [DocType] nvarchar(450) NOT NULL,
    [Prefix] nvarchar(max) NOT NULL,
    [NextNumber] int NOT NULL,
    [Padding] int NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_NumberingSeries] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Opportunities] (
    [Id] int NOT NULL IDENTITY,
    [LeadId] int NULL,
    [CustomerId] int NULL,
    [Title] nvarchar(max) NOT NULL,
    [Stage] int NOT NULL,
    [EstimatedValue] decimal(18,4) NOT NULL,
    [ProbabilityPercent] int NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Opportunities] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PaymentEntries] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [PartyType] int NOT NULL,
    [PartyId] int NOT NULL,
    [BankAccountId] int NOT NULL,
    [Amount] decimal(18,4) NOT NULL,
    [AgainstInvoiceId] int NULL,
    [AgainstVoucherType] nvarchar(max) NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [DocNo] nvarchar(450) NOT NULL,
    [Status] int NOT NULL,
    [PostingDate] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_PaymentEntries] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Projects] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [CustomerId] int NULL,
    [Status] int NOT NULL,
    [StartDate] datetime2 NULL,
    [EndDate] datetime2 NULL,
    [PercentComplete] decimal(18,4) NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Projects] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ReportDefinitions] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Module] nvarchar(max) NOT NULL,
    [ReportType] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_ReportDefinitions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [RolePermissions] (
    [Id] int NOT NULL IDENTITY,
    [RoleName] nvarchar(450) NOT NULL,
    [DocType] nvarchar(450) NOT NULL,
    [CanRead] bit NOT NULL,
    [CanCreate] bit NOT NULL,
    [CanWrite] bit NOT NULL,
    [CanSubmit] bit NOT NULL,
    [CanApprove] bit NOT NULL,
    [CanCancel] bit NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Suppliers] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NULL,
    [Phone] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [PayableAccountId] int NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SupportTickets] (
    [Id] int NOT NULL IDENTITY,
    [Subject] nvarchar(max) NOT NULL,
    [CustomerId] int NULL,
    [Status] int NOT NULL,
    [Priority] int NOT NULL,
    [AssignedTo] nvarchar(max) NULL,
    [EscalatedUtc] datetime2 NULL,
    [Resolution] nvarchar(max) NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_SupportTickets] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [TaxTemplates] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [RatePercent] decimal(18,4) NOT NULL,
    [TaxAccountId] int NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_TaxTemplates] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Warehouses] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [Code] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [IsGroup] bit NOT NULL,
    [ParentWarehouseId] int NULL,
    [StockAccountId] int NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [WorkflowDefinitions] (
    [Id] int NOT NULL IDENTITY,
    [DocType] nvarchar(max) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [MakerCannotApprove] bit NOT NULL,
    [ApprovalThreshold] decimal(18,4) NOT NULL,
    [SubmitRole] nvarchar(max) NOT NULL,
    [ApproverRole] nvarchar(max) NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_WorkflowDefinitions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [WorkflowInstances] (
    [Id] int NOT NULL IDENTITY,
    [WorkflowDefinitionId] int NOT NULL,
    [DocType] nvarchar(max) NOT NULL,
    [DocumentId] int NOT NULL,
    [CurrentState] nvarchar(max) NOT NULL,
    [SubmittedBy] nvarchar(max) NOT NULL,
    [Amount] decimal(18,4) NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_WorkflowInstances] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [AppUserRoles] (
    [Id] int NOT NULL IDENTITY,
    [AppUserId] int NOT NULL,
    [AppRoleId] int NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_AppUserRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AppUserRoles_AppRoles_AppRoleId] FOREIGN KEY ([AppRoleId]) REFERENCES [AppRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AppUserRoles_AppUsers_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [AppUsers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Companies] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Abbreviation] nvarchar(max) NOT NULL,
    [DefaultCurrencyId] int NOT NULL,
    [TaxId] nvarchar(max) NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Companies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Companies_Currencies_DefaultCurrencyId] FOREIGN KEY ([DefaultCurrencyId]) REFERENCES [Currencies] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SalesInvoices] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [CustomerId] int NOT NULL,
    [SalesOrderId] int NULL,
    [WarehouseId] int NULL,
    [UpdateStock] bit NOT NULL,
    [NetTotal] decimal(18,4) NOT NULL,
    [TaxTotal] decimal(18,4) NOT NULL,
    [GrandTotal] decimal(18,4) NOT NULL,
    [PaidAmount] decimal(18,4) NOT NULL,
    [OutstandingAmount] decimal(18,4) NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [DocNo] nvarchar(450) NOT NULL,
    [Status] int NOT NULL,
    [PostingDate] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_SalesInvoices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SalesInvoices_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SalesOrders] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [CustomerId] int NOT NULL,
    [DeliveryDate] datetime2 NULL,
    [NetTotal] decimal(18,4) NOT NULL,
    [TaxTotal] decimal(18,4) NOT NULL,
    [GrandTotal] decimal(18,4) NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [DocNo] nvarchar(450) NOT NULL,
    [Status] int NOT NULL,
    [PostingDate] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_SalesOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SalesOrders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Items] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [ItemGroupId] int NOT NULL,
    [IsStockItem] bit NOT NULL,
    [Uom] nvarchar(max) NOT NULL,
    [StandardRate] decimal(18,4) NOT NULL,
    [StandardBuyRate] decimal(18,4) NOT NULL,
    [IncomeAccountId] int NOT NULL,
    [ExpenseAccountId] int NOT NULL,
    [DefaultTaxTemplateId] int NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Items] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Items_ItemGroups_ItemGroupId] FOREIGN KEY ([ItemGroupId]) REFERENCES [ItemGroups] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [ProjectTasks] (
    [Id] int NOT NULL IDENTITY,
    [ProjectId] int NOT NULL,
    [Subject] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [AssignedTo] nvarchar(max) NULL,
    [DueDate] datetime2 NULL,
    [RequiresApproval] bit NOT NULL,
    [IsApproved] bit NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_ProjectTasks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ProjectTasks_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PurchaseInvoices] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [SupplierId] int NOT NULL,
    [PurchaseOrderId] int NULL,
    [WarehouseId] int NULL,
    [UpdateStock] bit NOT NULL,
    [NetTotal] decimal(18,4) NOT NULL,
    [TaxTotal] decimal(18,4) NOT NULL,
    [GrandTotal] decimal(18,4) NOT NULL,
    [PaidAmount] decimal(18,4) NOT NULL,
    [OutstandingAmount] decimal(18,4) NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [DocNo] nvarchar(450) NOT NULL,
    [Status] int NOT NULL,
    [PostingDate] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_PurchaseInvoices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseInvoices_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PurchaseOrders] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [SupplierId] int NOT NULL,
    [NetTotal] decimal(18,4) NOT NULL,
    [TaxTotal] decimal(18,4) NOT NULL,
    [GrandTotal] decimal(18,4) NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    [DocNo] nvarchar(450) NOT NULL,
    [Status] int NOT NULL,
    [PostingDate] datetime2 NOT NULL,
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseOrders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [WorkflowTransitions] (
    [Id] int NOT NULL IDENTITY,
    [WorkflowDefinitionId] int NOT NULL,
    [FromState] nvarchar(max) NOT NULL,
    [ToState] nvarchar(max) NOT NULL,
    [Action] int NOT NULL,
    [AllowedRole] nvarchar(max) NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_WorkflowTransitions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkflowTransitions_WorkflowDefinitions_WorkflowDefinitionId] FOREIGN KEY ([WorkflowDefinitionId]) REFERENCES [WorkflowDefinitions] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [WorkflowApprovals] (
    [Id] int NOT NULL IDENTITY,
    [WorkflowInstanceId] int NOT NULL,
    [Action] int NOT NULL,
    [ActedBy] nvarchar(max) NOT NULL,
    [FromState] nvarchar(max) NOT NULL,
    [ToState] nvarchar(max) NOT NULL,
    [Reason] nvarchar(max) NULL,
    [ActedUtc] datetime2 NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_WorkflowApprovals] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WorkflowApprovals_WorkflowInstances_WorkflowInstanceId] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [WorkflowInstances] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Accounts] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [Code] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [RootType] int NOT NULL,
    [IsGroup] bit NOT NULL,
    [ParentAccountId] int NULL,
    [PartyTypeRequired] int NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Accounts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Accounts_Accounts_ParentAccountId] FOREIGN KEY ([ParentAccountId]) REFERENCES [Accounts] ([Id]),
    CONSTRAINT [FK_Accounts_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SalesInvoiceLines] (
    [Id] int NOT NULL IDENTITY,
    [SalesInvoiceId] int NOT NULL,
    [ItemId] int NOT NULL,
    [Qty] decimal(18,4) NOT NULL,
    [Rate] decimal(18,4) NOT NULL,
    [Amount] decimal(18,4) NOT NULL,
    [TaxRatePercent] decimal(18,4) NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_SalesInvoiceLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SalesInvoiceLines_Items_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Items] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SalesInvoiceLines_SalesInvoices_SalesInvoiceId] FOREIGN KEY ([SalesInvoiceId]) REFERENCES [SalesInvoices] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SalesOrderLines] (
    [Id] int NOT NULL IDENTITY,
    [SalesOrderId] int NOT NULL,
    [ItemId] int NOT NULL,
    [Qty] decimal(18,4) NOT NULL,
    [Rate] decimal(18,4) NOT NULL,
    [Amount] decimal(18,4) NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_SalesOrderLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SalesOrderLines_Items_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Items] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SalesOrderLines_SalesOrders_SalesOrderId] FOREIGN KEY ([SalesOrderId]) REFERENCES [SalesOrders] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [StockLedgerEntries] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [ItemId] int NOT NULL,
    [WarehouseId] int NOT NULL,
    [PostingDate] datetime2 NOT NULL,
    [QtyChange] decimal(18,4) NOT NULL,
    [ValuationRate] decimal(18,4) NOT NULL,
    [QtyAfter] decimal(18,4) NOT NULL,
    [ValueAfter] decimal(18,4) NOT NULL,
    [VoucherType] nvarchar(max) NOT NULL,
    [VoucherNo] nvarchar(max) NOT NULL,
    [IsReversal] bit NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_StockLedgerEntries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockLedgerEntries_Items_ItemId] FOREIGN KEY ([ItemId]) REFERENCES [Items] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StockLedgerEntries_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PurchaseInvoiceLines] (
    [Id] int NOT NULL IDENTITY,
    [PurchaseInvoiceId] int NOT NULL,
    [ItemId] int NOT NULL,
    [Qty] decimal(18,4) NOT NULL,
    [Rate] decimal(18,4) NOT NULL,
    [Amount] decimal(18,4) NOT NULL,
    [TaxRatePercent] decimal(18,4) NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_PurchaseInvoiceLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseInvoiceLines_PurchaseInvoices_PurchaseInvoiceId] FOREIGN KEY ([PurchaseInvoiceId]) REFERENCES [PurchaseInvoices] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PurchaseOrderLines] (
    [Id] int NOT NULL IDENTITY,
    [PurchaseOrderId] int NOT NULL,
    [ItemId] int NOT NULL,
    [Qty] decimal(18,4) NOT NULL,
    [Rate] decimal(18,4) NOT NULL,
    [Amount] decimal(18,4) NOT NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_PurchaseOrderLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [GLEntries] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [AccountId] int NOT NULL,
    [PostingDate] datetime2 NOT NULL,
    [Debit] decimal(18,4) NOT NULL,
    [Credit] decimal(18,4) NOT NULL,
    [VoucherType] nvarchar(450) NOT NULL,
    [VoucherNo] nvarchar(450) NOT NULL,
    [PartyType] int NULL,
    [PartyId] int NULL,
    [CostCenterId] int NULL,
    [IsReversal] bit NOT NULL,
    [Remarks] nvarchar(max) NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_GLEntries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GLEntries_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [JournalEntryLines] (
    [Id] int NOT NULL IDENTITY,
    [JournalEntryId] int NOT NULL,
    [AccountId] int NOT NULL,
    [Debit] decimal(18,4) NOT NULL,
    [Credit] decimal(18,4) NOT NULL,
    [PartyType] int NULL,
    [PartyId] int NULL,
    [CostCenterId] int NULL,
    [CreatedUtc] datetime2 NOT NULL,
    [UpdatedUtc] datetime2 NULL,
    [CreatedBy] nvarchar(max) NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_JournalEntryLines] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_JournalEntryLines_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_JournalEntryLines_JournalEntries_JournalEntryId] FOREIGN KEY ([JournalEntryId]) REFERENCES [JournalEntries] ([Id]) ON DELETE CASCADE
);
GO


CREATE UNIQUE INDEX [IX_Accounts_CompanyId_Code] ON [Accounts] ([CompanyId], [Code]);
GO


CREATE INDEX [IX_Accounts_ParentAccountId] ON [Accounts] ([ParentAccountId]);
GO


CREATE UNIQUE INDEX [IX_AppRoles_Name] ON [AppRoles] ([Name]);
GO


CREATE INDEX [IX_AppUserRoles_AppRoleId] ON [AppUserRoles] ([AppRoleId]);
GO


CREATE INDEX [IX_AppUserRoles_AppUserId] ON [AppUserRoles] ([AppUserId]);
GO


CREATE UNIQUE INDEX [IX_AppUsers_Username] ON [AppUsers] ([Username]);
GO


CREATE INDEX [IX_Companies_DefaultCurrencyId] ON [Companies] ([DefaultCurrencyId]);
GO


CREATE UNIQUE INDEX [IX_Currencies_Code] ON [Currencies] ([Code]);
GO


CREATE UNIQUE INDEX [IX_Customers_Code] ON [Customers] ([Code]);
GO


CREATE INDEX [IX_GLEntries_AccountId_PostingDate] ON [GLEntries] ([AccountId], [PostingDate]);
GO


CREATE INDEX [IX_GLEntries_PartyType_PartyId] ON [GLEntries] ([PartyType], [PartyId]);
GO


CREATE INDEX [IX_GLEntries_VoucherType_VoucherNo] ON [GLEntries] ([VoucherType], [VoucherNo]);
GO


CREATE UNIQUE INDEX [IX_Items_Code] ON [Items] ([Code]);
GO


CREATE INDEX [IX_Items_ItemGroupId] ON [Items] ([ItemGroupId]);
GO


CREATE UNIQUE INDEX [IX_JournalEntries_DocNo] ON [JournalEntries] ([DocNo]);
GO


CREATE INDEX [IX_JournalEntryLines_AccountId] ON [JournalEntryLines] ([AccountId]);
GO


CREATE INDEX [IX_JournalEntryLines_JournalEntryId] ON [JournalEntryLines] ([JournalEntryId]);
GO


CREATE UNIQUE INDEX [IX_NumberingSeries_DocType] ON [NumberingSeries] ([DocType]);
GO


CREATE UNIQUE INDEX [IX_PaymentEntries_DocNo] ON [PaymentEntries] ([DocNo]);
GO


CREATE INDEX [IX_ProjectTasks_ProjectId] ON [ProjectTasks] ([ProjectId]);
GO


CREATE INDEX [IX_PurchaseInvoiceLines_PurchaseInvoiceId] ON [PurchaseInvoiceLines] ([PurchaseInvoiceId]);
GO


CREATE UNIQUE INDEX [IX_PurchaseInvoices_DocNo] ON [PurchaseInvoices] ([DocNo]);
GO


CREATE INDEX [IX_PurchaseInvoices_SupplierId] ON [PurchaseInvoices] ([SupplierId]);
GO


CREATE INDEX [IX_PurchaseOrderLines_PurchaseOrderId] ON [PurchaseOrderLines] ([PurchaseOrderId]);
GO


CREATE UNIQUE INDEX [IX_PurchaseOrders_DocNo] ON [PurchaseOrders] ([DocNo]);
GO


CREATE INDEX [IX_PurchaseOrders_SupplierId] ON [PurchaseOrders] ([SupplierId]);
GO


CREATE UNIQUE INDEX [IX_RolePermissions_RoleName_DocType] ON [RolePermissions] ([RoleName], [DocType]);
GO


CREATE INDEX [IX_SalesInvoiceLines_ItemId] ON [SalesInvoiceLines] ([ItemId]);
GO


CREATE INDEX [IX_SalesInvoiceLines_SalesInvoiceId] ON [SalesInvoiceLines] ([SalesInvoiceId]);
GO


CREATE INDEX [IX_SalesInvoices_CustomerId] ON [SalesInvoices] ([CustomerId]);
GO


CREATE UNIQUE INDEX [IX_SalesInvoices_DocNo] ON [SalesInvoices] ([DocNo]);
GO


CREATE INDEX [IX_SalesOrderLines_ItemId] ON [SalesOrderLines] ([ItemId]);
GO


CREATE INDEX [IX_SalesOrderLines_SalesOrderId] ON [SalesOrderLines] ([SalesOrderId]);
GO


CREATE INDEX [IX_SalesOrders_CustomerId] ON [SalesOrders] ([CustomerId]);
GO


CREATE UNIQUE INDEX [IX_SalesOrders_DocNo] ON [SalesOrders] ([DocNo]);
GO


CREATE INDEX [IX_StockLedgerEntries_ItemId_WarehouseId_Id] ON [StockLedgerEntries] ([ItemId], [WarehouseId], [Id]);
GO


CREATE INDEX [IX_StockLedgerEntries_WarehouseId] ON [StockLedgerEntries] ([WarehouseId]);
GO


CREATE UNIQUE INDEX [IX_Suppliers_Code] ON [Suppliers] ([Code]);
GO


CREATE UNIQUE INDEX [IX_Warehouses_Code] ON [Warehouses] ([Code]);
GO


CREATE INDEX [IX_WorkflowApprovals_WorkflowInstanceId] ON [WorkflowApprovals] ([WorkflowInstanceId]);
GO


CREATE INDEX [IX_WorkflowTransitions_WorkflowDefinitionId] ON [WorkflowTransitions] ([WorkflowDefinitionId]);
GO


