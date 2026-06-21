using LafErp.Core;
using LafErp.Data;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Services;

public class PurchaseService
{
    private readonly ErpDbContext _db;
    private readonly NumberingService _num;
    private readonly WorkflowService _wf;
    private readonly AccountingService _acc;
    private readonly AuditService _audit;

    public PurchaseService(ErpDbContext db, NumberingService num, WorkflowService wf, AccountingService acc, AuditService audit)
    {
        _db = db; _num = num; _wf = wf; _acc = acc; _audit = audit;
    }

    public PurchaseOrder CreateOrder(int companyId, int supplierId, IEnumerable<InvoiceLineInput> lines, DateTime? postingDate = null)
    {
        var po = new PurchaseOrder { CompanyId = companyId, SupplierId = supplierId, PostingDate = postingDate ?? DateTime.UtcNow.Date, DocNo = _num.Next("PurchaseOrder") };
        foreach (var l in lines)
            po.Lines.Add(new PurchaseOrderLine { ItemId = l.ItemId, Qty = l.Qty, Rate = l.Rate, Amount = l.Qty * l.Rate });
        po.NetTotal = po.Lines.Sum(l => l.Amount);
        po.GrandTotal = po.NetTotal;
        _db.PurchaseOrders.Add(po);
        _audit.Record("PurchaseOrder", 0, "Create", po.DocNo);
        _db.SaveChanges();
        return po;
    }

    public PurchaseInvoice CreateInvoice(int companyId, int supplierId, int? warehouseId, IEnumerable<InvoiceLineInput> lines, DateTime? postingDate = null, int? purchaseOrderId = null)
    {
        if (!_db.Suppliers.Any(s => s.Id == supplierId)) throw new DomainException("Unknown supplier.");
        var pi = new PurchaseInvoice
        {
            CompanyId = companyId, SupplierId = supplierId, WarehouseId = warehouseId,
            PurchaseOrderId = purchaseOrderId, PostingDate = postingDate ?? DateTime.UtcNow.Date, DocNo = _num.Next("PurchaseInvoice")
        };
        foreach (var l in lines)
        {
            if (l.Qty <= 0) throw new DomainException("Line quantity must be positive.");
            pi.Lines.Add(new PurchaseInvoiceLine { ItemId = l.ItemId, Qty = l.Qty, Rate = l.Rate, Amount = l.Qty * l.Rate, TaxRatePercent = l.TaxRatePercent });
        }
        if (pi.Lines.Count == 0) throw new DomainException("Invoice has no lines.");
        Recalc(pi);
        pi.OutstandingAmount = pi.GrandTotal;
        _db.PurchaseInvoices.Add(pi);
        _audit.Record("PurchaseInvoice", 0, "Create", pi.DocNo);
        _db.SaveChanges();
        return pi;
    }

    public void Submit(int invoiceId)
    {
        var pi = Load(invoiceId);
        _wf.Submit(pi, "PurchaseInvoice", pi.GrandTotal, () => _acc.PostPurchaseInvoice(pi));
        _db.SaveChanges();
    }

    public void Approve(int invoiceId)
    {
        var pi = Load(invoiceId);
        _wf.Approve(pi, "PurchaseInvoice", () => _acc.PostPurchaseInvoice(pi));
        _db.SaveChanges();
    }

    public void Reject(int invoiceId, string reason)
    {
        var pi = Load(invoiceId);
        _wf.Reject(pi, "PurchaseInvoice", reason);
        _db.SaveChanges();
    }

    public void Cancel(int invoiceId)
    {
        var pi = Load(invoiceId);
        if (pi.Status != DocStatus.Submitted) throw new DomainException("Only a submitted invoice can be cancelled.");
        _acc.ReverseVoucher(pi.CompanyId, "PurchaseInvoice", pi.DocNo);
        pi.Status = DocStatus.Cancelled;
        _audit.Record("PurchaseInvoice", pi.Id, "Cancel", "reversed GL + stock");
        _db.SaveChanges();
    }

    private PurchaseInvoice Load(int id) =>
        _db.PurchaseInvoices.Include(x => x.Lines).FirstOrDefault(x => x.Id == id)
        ?? throw new DomainException($"Purchase invoice #{id} not found.");

    private static void Recalc(PurchaseInvoice pi)
    {
        pi.NetTotal = pi.Lines.Sum(l => l.Amount);
        pi.TaxTotal = pi.Lines.Sum(l => Math.Round(l.Amount * l.TaxRatePercent / 100m, 4));
        pi.GrandTotal = pi.NetTotal + pi.TaxTotal;
    }
}
