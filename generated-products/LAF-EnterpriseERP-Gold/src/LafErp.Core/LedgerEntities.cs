namespace LafErp.Core;

/// <summary>
/// Immutable general-ledger posting. Every financial document submission writes balanced GL entries
/// (sum of debit == sum of credit). GL entries are never edited; a cancellation writes reversing entries.
/// </summary>
public class GLEntry : EntityBase
{
    public int CompanyId { get; set; }
    public int AccountId { get; set; }
    public Account? Account { get; set; }
    public DateTime PostingDate { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string VoucherType { get; set; } = string.Empty; // e.g. "SalesInvoice"
    public string VoucherNo { get; set; } = string.Empty;
    public PartyType? PartyType { get; set; }
    public int? PartyId { get; set; }
    public int? CostCenterId { get; set; }
    public bool IsReversal { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>
/// Immutable stock ledger entry. Quantity is signed by direction; running balance and moving-average
/// valuation are derived from the ordered sequence of entries per item+warehouse.
/// </summary>
public class StockLedgerEntry : EntityBase
{
    public int CompanyId { get; set; }
    public int ItemId { get; set; }
    public Item? Item { get; set; }
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public DateTime PostingDate { get; set; }
    /// <summary>Signed quantity change (+in / -out).</summary>
    public decimal QtyChange { get; set; }
    public decimal ValuationRate { get; set; }
    /// <summary>Running quantity balance after this entry (item+warehouse).</summary>
    public decimal QtyAfter { get; set; }
    /// <summary>Running valuation balance (stock value) after this entry.</summary>
    public decimal ValueAfter { get; set; }
    public string VoucherType { get; set; } = string.Empty;
    public string VoucherNo { get; set; } = string.Empty;
    public bool IsReversal { get; set; }
}
