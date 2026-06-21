namespace LafErp.Core;

public class Currency : EntityBase
{
    public string Code { get; set; } = string.Empty;   // ISO 4217, e.g. USD
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
}

public class Company : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public int DefaultCurrencyId { get; set; }
    public Currency? DefaultCurrency { get; set; }
    public string? TaxId { get; set; }
}

public class FiscalYear : EntityBase
{
    public string Name { get; set; } = string.Empty;   // e.g. "2026"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; }
}

/// <summary>Chart-of-accounts node. Group accounts have children; leaf accounts are postable.</summary>
public class Account : EntityBase
{
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public RootType RootType { get; set; }
    public bool IsGroup { get; set; }
    public int? ParentAccountId { get; set; }
    public Account? ParentAccount { get; set; }
    /// <summary>If set, GL entries on this account must carry a party of this type (debtors/creditors control).</summary>
    public PartyType? PartyTypeRequired { get; set; }
}

public class CostCenter : EntityBase
{
    public int CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsGroup { get; set; }
    public int? ParentCostCenterId { get; set; }
}

/// <summary>Document numbering series, clean-room equivalent of a naming series (PREFIX-#####).</summary>
public class NumberingSeries : EntityBase
{
    public string DocType { get; set; } = string.Empty; // e.g. "SalesInvoice"
    public string Prefix { get; set; } = string.Empty;  // e.g. "SINV-"
    public int NextNumber { get; set; } = 1;
    public int Padding { get; set; } = 5;
}

public class TaxTemplate : EntityBase
{
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal RatePercent { get; set; }
    public int TaxAccountId { get; set; }
}
