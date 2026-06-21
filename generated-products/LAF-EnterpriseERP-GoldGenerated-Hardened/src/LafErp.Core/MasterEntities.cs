namespace LafErp.Core;

public class Customer : EntityBase
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public decimal CreditLimit { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Receivable control account for this customer's company.</summary>
    public int ReceivableAccountId { get; set; }
}

public class Supplier : EntityBase
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Payable control account for this supplier's company.</summary>
    public int PayableAccountId { get; set; }
}

public class ItemGroup : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public int? ParentItemGroupId { get; set; }
    public bool IsGroup { get; set; }
}

public class Item : EntityBase
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ItemGroupId { get; set; }
    public ItemGroup? ItemGroup { get; set; }
    public bool IsStockItem { get; set; } = true;
    public string Uom { get; set; } = "Nos";
    public decimal StandardRate { get; set; }
    public decimal StandardBuyRate { get; set; }
    /// <summary>Income account used when this item is sold.</summary>
    public int IncomeAccountId { get; set; }
    /// <summary>Expense account used when this item is consumed/purchased (non-stock) or COGS.</summary>
    public int ExpenseAccountId { get; set; }
    public int? DefaultTaxTemplateId { get; set; }
}

public class Warehouse : EntityBase
{
    public int CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsGroup { get; set; }
    public int? ParentWarehouseId { get; set; }
    /// <summary>Stock (asset) account used when posting stock movements for this warehouse.</summary>
    public int StockAccountId { get; set; }
}
