using LafErp.Core;
using LafErp.Data;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Services;

public record RegisterRow(string DocNo, int PartyId, decimal NetTotal, decimal TaxTotal, decimal GrandTotal, DocStatus Status, DateTime PostingDate);
public record PartySummaryRow(int PartyId, int Count, decimal Total);
public record AgingRow(int PartyId, decimal Bucket0_30, decimal Bucket31_60, decimal Bucket61_90, decimal Bucket90Plus, decimal Total);
public record StockValuationRow(int ItemId, int WarehouseId, decimal Qty, decimal Value);
public record WorkOrderSummaryRow(ProductionStatus Status, int Count, decimal TotalQty);
public record TaxSummary(decimal OutputTax, decimal InputTax, decimal NetTax);

/// <summary>
/// ERP-grade reporting depth built on the authoritative ledgers. All queries are company-scoped and use
/// straightforward EF aggregation (no group-by-constant). Submitted documents only, so reports reconcile to the GL.
/// </summary>
public class ReportsService
{
    private readonly ErpDbContext _db;
    private readonly StockService _stock;
    public ReportsService(ErpDbContext db, StockService stock) { _db = db; _stock = stock; }

    public List<RegisterRow> SalesRegister(int companyId) =>
        _db.SalesInvoices.Where(x => x.CompanyId == companyId && x.Status == DocStatus.Submitted)
           .Select(x => new RegisterRow(x.DocNo, x.CustomerId, x.NetTotal, x.TaxTotal, x.GrandTotal, x.Status, x.PostingDate))
           .ToList();

    public List<RegisterRow> PurchaseRegister(int companyId) =>
        _db.PurchaseInvoices.Where(x => x.CompanyId == companyId && x.Status == DocStatus.Submitted)
           .Select(x => new RegisterRow(x.DocNo, x.SupplierId, x.NetTotal, x.TaxTotal, x.GrandTotal, x.Status, x.PostingDate))
           .ToList();

    public List<PartySummaryRow> SalesSummaryByCustomer(int companyId) =>
        _db.SalesInvoices.Where(x => x.CompanyId == companyId && x.Status == DocStatus.Submitted)
           .GroupBy(x => x.CustomerId)
           .Select(g => new PartySummaryRow(g.Key, g.Count(), g.Sum(x => x.GrandTotal)))
           .ToList();

    public List<PartySummaryRow> PurchaseSummaryBySupplier(int companyId) =>
        _db.PurchaseInvoices.Where(x => x.CompanyId == companyId && x.Status == DocStatus.Submitted)
           .GroupBy(x => x.SupplierId)
           .Select(g => new PartySummaryRow(g.Key, g.Count(), g.Sum(x => x.GrandTotal)))
           .ToList();

    public List<RegisterRow> OutstandingSalesInvoices(int companyId) =>
        _db.SalesInvoices.Where(x => x.CompanyId == companyId && x.Status == DocStatus.Submitted && x.OutstandingAmount > 0)
           .Select(x => new RegisterRow(x.DocNo, x.CustomerId, x.NetTotal, x.TaxTotal, x.OutstandingAmount, x.Status, x.PostingDate))
           .ToList();

    public List<RegisterRow> OutstandingPurchaseInvoices(int companyId) =>
        _db.PurchaseInvoices.Where(x => x.CompanyId == companyId && x.Status == DocStatus.Submitted && x.OutstandingAmount > 0)
           .Select(x => new RegisterRow(x.DocNo, x.SupplierId, x.NetTotal, x.TaxTotal, x.OutstandingAmount, x.Status, x.PostingDate))
           .ToList();

    /// <summary>Receivables aging bucketed by invoice age (asOf defaults to now).</summary>
    public List<AgingRow> ReceivablesAging(int companyId, DateTime? asOf = null)
    {
        var now = asOf ?? DateTime.UtcNow;
        var rows = _db.SalesInvoices
            .Where(x => x.CompanyId == companyId && x.Status == DocStatus.Submitted && x.OutstandingAmount > 0)
            .Select(x => new { x.CustomerId, x.OutstandingAmount, x.PostingDate }).ToList();
        return rows.GroupBy(r => r.CustomerId).Select(g =>
        {
            decimal b0 = 0, b1 = 0, b2 = 0, b3 = 0;
            foreach (var r in g)
            {
                var days = (now - r.PostingDate).TotalDays;
                if (days <= 30) b0 += r.OutstandingAmount;
                else if (days <= 60) b1 += r.OutstandingAmount;
                else if (days <= 90) b2 += r.OutstandingAmount;
                else b3 += r.OutstandingAmount;
            }
            return new AgingRow(g.Key, b0, b1, b2, b3, b0 + b1 + b2 + b3);
        }).ToList();
    }

    public TaxSummary TaxSummaryReport(int companyId)
    {
        var output = _db.SalesInvoices.Where(x => x.CompanyId == companyId && x.Status == DocStatus.Submitted).Sum(x => (decimal?)x.TaxTotal) ?? 0m;
        var input = _db.PurchaseInvoices.Where(x => x.CompanyId == companyId && x.Status == DocStatus.Submitted).Sum(x => (decimal?)x.TaxTotal) ?? 0m;
        return new TaxSummary(output, input, output - input);
    }

    public List<StockValuationRow> StockValuation(int companyId)
    {
        var combos = _db.StockLedgerEntries.Where(s => s.CompanyId == companyId)
            .Select(s => new { s.ItemId, s.WarehouseId }).Distinct().ToList();
        return combos.Select(c => { var b = _stock.Balance(c.ItemId, c.WarehouseId); return new StockValuationRow(c.ItemId, c.WarehouseId, b.Qty, b.Value); })
                     .Where(r => r.Qty != 0 || r.Value != 0).ToList();
    }

    /// <summary>Items whose on-hand quantity (any warehouse) is at or below a reorder threshold.</summary>
    public List<StockValuationRow> ReorderReport(int companyId, decimal threshold)
    {
        return StockValuation(companyId).Where(r => r.Qty <= threshold).ToList();
    }

    public List<WorkOrderSummaryRow> WorkOrderSummary(int companyId) =>
        _db.ProductionOrders.Where(o => o.CompanyId == companyId)
           .GroupBy(o => o.Status)
           .Select(g => new WorkOrderSummaryRow(g.Key, g.Count(), g.Sum(o => o.Quantity)))
           .ToList();
}
