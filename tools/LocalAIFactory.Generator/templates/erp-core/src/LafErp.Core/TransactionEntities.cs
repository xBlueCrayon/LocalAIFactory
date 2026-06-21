namespace LafErp.Core;

// ---------------- Selling ----------------

public class SalesOrder : DocumentBase
{
    public int CompanyId { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public decimal NetTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public List<SalesOrderLine> Lines { get; set; } = new();
}

public class SalesOrderLine : EntityBase
{
    public int SalesOrderId { get; set; }
    public int ItemId { get; set; }
    public Item? Item { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}

public class SalesInvoice : DocumentBase
{
    public int CompanyId { get; set; }
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? SalesOrderId { get; set; }
    public int? WarehouseId { get; set; }
    public bool UpdateStock { get; set; } = true;
    public decimal NetTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public List<SalesInvoiceLine> Lines { get; set; } = new();
}

public class SalesInvoiceLine : EntityBase
{
    public int SalesInvoiceId { get; set; }
    public int ItemId { get; set; }
    public Item? Item { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxRatePercent { get; set; }
}

// ---------------- Buying ----------------

public class PurchaseOrder : DocumentBase
{
    public int CompanyId { get; set; }
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public decimal NetTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public List<PurchaseOrderLine> Lines { get; set; } = new();
}

public class PurchaseOrderLine : EntityBase
{
    public int PurchaseOrderId { get; set; }
    public int ItemId { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}

public class PurchaseInvoice : DocumentBase
{
    public int CompanyId { get; set; }
    public int SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public int? PurchaseOrderId { get; set; }
    public int? WarehouseId { get; set; }
    public bool UpdateStock { get; set; } = true;
    public decimal NetTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public List<PurchaseInvoiceLine> Lines { get; set; } = new();
}

public class PurchaseInvoiceLine : EntityBase
{
    public int PurchaseInvoiceId { get; set; }
    public int ItemId { get; set; }
    public decimal Qty { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxRatePercent { get; set; }
}

// ---------------- Payments & Journals ----------------

public class PaymentEntry : DocumentBase
{
    public int CompanyId { get; set; }
    public PartyType PartyType { get; set; }
    public int PartyId { get; set; }
    /// <summary>Cash/bank account that moves.</summary>
    public int BankAccountId { get; set; }
    /// <summary>Amount received (from customer) or paid (to supplier).</summary>
    public decimal Amount { get; set; }
    /// <summary>Optional invoice this payment is allocated against.</summary>
    public int? AgainstInvoiceId { get; set; }
    public string? AgainstVoucherType { get; set; }
}

public class JournalEntry : DocumentBase
{
    public int CompanyId { get; set; }
    public string? Narration { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public List<JournalEntryLine> Lines { get; set; } = new();
}

public class JournalEntryLine : EntityBase
{
    public int JournalEntryId { get; set; }
    public int AccountId { get; set; }
    public Account? Account { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public PartyType? PartyType { get; set; }
    public int? PartyId { get; set; }
    public int? CostCenterId { get; set; }
}
