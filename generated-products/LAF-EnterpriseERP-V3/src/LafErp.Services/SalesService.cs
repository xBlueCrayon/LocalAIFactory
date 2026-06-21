using LafErp.Core;
using LafErp.Data;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Services;

public record InvoiceLineInput(int ItemId, decimal Qty, decimal Rate, decimal TaxRatePercent = 0);

public class SalesService
{
    private readonly ErpDbContext _db;
    private readonly NumberingService _num;
    private readonly WorkflowService _wf;
    private readonly AccountingService _acc;
    private readonly AuditService _audit;

    public SalesService(ErpDbContext db, NumberingService num, WorkflowService wf, AccountingService acc, AuditService audit)
    {
        _db = db; _num = num; _wf = wf; _acc = acc; _audit = audit;
    }

    public SalesOrder CreateOrder(int companyId, int customerId, IEnumerable<InvoiceLineInput> lines, DateTime? postingDate = null)
    {
        var so = new SalesOrder { CompanyId = companyId, CustomerId = customerId, PostingDate = postingDate ?? DateTime.UtcNow.Date, DocNo = _num.Next("SalesOrder") };
        foreach (var l in lines)
            so.Lines.Add(new SalesOrderLine { ItemId = l.ItemId, Qty = l.Qty, Rate = l.Rate, Amount = l.Qty * l.Rate });
        Recalc(so);
        _db.SalesOrders.Add(so);
        _audit.Record("SalesOrder", 0, "Create", so.DocNo);
        _db.SaveChanges();
        return so;
    }

    public SalesInvoice CreateInvoice(int companyId, int customerId, int? warehouseId, IEnumerable<InvoiceLineInput> lines, DateTime? postingDate = null, int? salesOrderId = null)
    {
        if (!_db.Customers.Any(c => c.Id == customerId)) throw new DomainException("Unknown customer.");
        var si = new SalesInvoice
        {
            CompanyId = companyId, CustomerId = customerId, WarehouseId = warehouseId,
            SalesOrderId = salesOrderId, PostingDate = postingDate ?? DateTime.UtcNow.Date, DocNo = _num.Next("SalesInvoice")
        };
        foreach (var l in lines)
        {
            if (l.Qty <= 0) throw new DomainException("Line quantity must be positive.");
            if (l.Rate < 0) throw new DomainException("Line rate cannot be negative.");
            si.Lines.Add(new SalesInvoiceLine { ItemId = l.ItemId, Qty = l.Qty, Rate = l.Rate, Amount = l.Qty * l.Rate, TaxRatePercent = l.TaxRatePercent });
        }
        if (si.Lines.Count == 0) throw new DomainException("Invoice has no lines.");
        Recalc(si);
        si.OutstandingAmount = si.GrandTotal;
        _db.SalesInvoices.Add(si);
        _audit.Record("SalesInvoice", 0, "Create", si.DocNo);
        _db.SaveChanges();
        return si;
    }

    public void Submit(int invoiceId)
    {
        var si = Load(invoiceId);
        _wf.Submit(si, "SalesInvoice", si.GrandTotal, () => _acc.PostSalesInvoice(si));
        _db.SaveChanges();
    }

    public void Approve(int invoiceId)
    {
        var si = Load(invoiceId);
        _wf.Approve(si, "SalesInvoice", () => _acc.PostSalesInvoice(si));
        _db.SaveChanges();
    }

    public void Reject(int invoiceId, string reason)
    {
        var si = Load(invoiceId);
        _wf.Reject(si, "SalesInvoice", reason);
        _db.SaveChanges();
    }

    public void Cancel(int invoiceId)
    {
        var si = Load(invoiceId);
        if (si.Status != DocStatus.Submitted) throw new DomainException("Only a submitted invoice can be cancelled.");
        _acc.ReverseVoucher(si.CompanyId, "SalesInvoice", si.DocNo);
        si.Status = DocStatus.Cancelled;
        _audit.Record("SalesInvoice", si.Id, "Cancel", "reversed GL + stock");
        _db.SaveChanges();
    }

    /// <summary>Editing a posted invoice is forbidden — corrections go through cancel/reissue.</summary>
    public void EditRate(int invoiceId, int lineId, decimal newRate)
    {
        var si = Load(invoiceId);
        if (si.Status != DocStatus.Draft) throw new DomainException("A submitted invoice is immutable; cancel and reissue instead.");
        var line = si.Lines.First(l => l.Id == lineId);
        line.Rate = newRate; line.Amount = line.Qty * newRate;
        Recalc(si);
        _db.SaveChanges();
    }

    private SalesInvoice Load(int id) =>
        _db.SalesInvoices.Include(x => x.Lines).FirstOrDefault(x => x.Id == id)
        ?? throw new DomainException($"Sales invoice #{id} not found.");

    private static void Recalc(SalesInvoice si)
    {
        si.NetTotal = si.Lines.Sum(l => l.Amount);
        si.TaxTotal = si.Lines.Sum(l => Math.Round(l.Amount * l.TaxRatePercent / 100m, 4));
        si.GrandTotal = si.NetTotal + si.TaxTotal;
    }

    private static void Recalc(SalesOrder so)
    {
        so.NetTotal = so.Lines.Sum(l => l.Amount);
        so.GrandTotal = so.NetTotal + so.TaxTotal;
    }
}
