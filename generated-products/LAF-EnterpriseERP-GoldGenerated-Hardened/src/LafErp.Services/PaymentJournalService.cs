using LafErp.Core;
using LafErp.Data;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Services;

public class PaymentService
{
    private readonly ErpDbContext _db;
    private readonly NumberingService _num;
    private readonly WorkflowService _wf;
    private readonly AccountingService _acc;
    private readonly AuditService _audit;

    public PaymentService(ErpDbContext db, NumberingService num, WorkflowService wf, AccountingService acc, AuditService audit)
    { _db = db; _num = num; _wf = wf; _acc = acc; _audit = audit; }

    public PaymentEntry Create(int companyId, PartyType partyType, int partyId, int bankAccountId, decimal amount, int? againstInvoiceId = null, DateTime? postingDate = null)
    {
        if (amount <= 0) throw new DomainException("Payment amount must be positive.");
        var pe = new PaymentEntry
        {
            CompanyId = companyId, PartyType = partyType, PartyId = partyId, BankAccountId = bankAccountId,
            Amount = amount, AgainstInvoiceId = againstInvoiceId, PostingDate = postingDate ?? DateTime.UtcNow.Date, DocNo = _num.Next("PaymentEntry")
        };
        _db.PaymentEntries.Add(pe);
        _audit.Record("PaymentEntry", 0, "Create", pe.DocNo);
        _db.SaveChanges();
        return pe;
    }

    public void Submit(int id)
    {
        var pe = _db.PaymentEntries.First(x => x.Id == id);
        _wf.Submit(pe, "PaymentEntry", pe.Amount, () => { _acc.PostPayment(pe); ApplyAllocation(pe); });
        _db.SaveChanges();
    }

    public void Approve(int id)
    {
        var pe = _db.PaymentEntries.First(x => x.Id == id);
        _wf.Approve(pe, "PaymentEntry", () => { _acc.PostPayment(pe); ApplyAllocation(pe); });
        _db.SaveChanges();
    }

    public void Reject(int id, string reason)
    {
        var pe = _db.PaymentEntries.First(x => x.Id == id);
        _wf.Reject(pe, "PaymentEntry", reason);
        _db.SaveChanges();
    }

    private void ApplyAllocation(PaymentEntry pe)
    {
        if (pe.AgainstInvoiceId is not int invId) return;
        if (pe.PartyType == PartyType.Customer)
        {
            var si = _db.SalesInvoices.FirstOrDefault(x => x.Id == invId);
            if (si != null) { si.PaidAmount += pe.Amount; si.OutstandingAmount = si.GrandTotal - si.PaidAmount; }
        }
        else
        {
            var pi = _db.PurchaseInvoices.FirstOrDefault(x => x.Id == invId);
            if (pi != null) { pi.PaidAmount += pe.Amount; pi.OutstandingAmount = pi.GrandTotal - pi.PaidAmount; }
        }
    }
}

public class JournalService
{
    private readonly ErpDbContext _db;
    private readonly NumberingService _num;
    private readonly WorkflowService _wf;
    private readonly AccountingService _acc;
    private readonly AuditService _audit;

    public JournalService(ErpDbContext db, NumberingService num, WorkflowService wf, AccountingService acc, AuditService audit)
    { _db = db; _num = num; _wf = wf; _acc = acc; _audit = audit; }

    public record JeLine(int AccountId, decimal Debit, decimal Credit, PartyType? PartyType = null, int? PartyId = null);

    public JournalEntry Create(int companyId, string? narration, IEnumerable<JeLine> lines, DateTime? postingDate = null)
    {
        var je = new JournalEntry { CompanyId = companyId, Narration = narration, PostingDate = postingDate ?? DateTime.UtcNow.Date, DocNo = _num.Next("JournalEntry") };
        foreach (var l in lines)
            je.Lines.Add(new JournalEntryLine { AccountId = l.AccountId, Debit = l.Debit, Credit = l.Credit, PartyType = l.PartyType, PartyId = l.PartyId });
        je.TotalDebit = je.Lines.Sum(l => l.Debit);
        je.TotalCredit = je.Lines.Sum(l => l.Credit);
        _db.JournalEntries.Add(je);
        _audit.Record("JournalEntry", 0, "Create", je.DocNo);
        _db.SaveChanges();
        return je;
    }

    public void Submit(int id)
    {
        var je = _db.JournalEntries.Include(x => x.Lines).First(x => x.Id == id);
        _wf.Submit(je, "JournalEntry", je.TotalDebit, () => _acc.PostJournalEntry(je));
        _db.SaveChanges();
    }

    public void Approve(int id)
    {
        var je = _db.JournalEntries.Include(x => x.Lines).First(x => x.Id == id);
        _wf.Approve(je, "JournalEntry", () => _acc.PostJournalEntry(je));
        _db.SaveChanges();
    }

    public void Reject(int id, string reason)
    {
        var je = _db.JournalEntries.Include(x => x.Lines).First(x => x.Id == id);
        _wf.Reject(je, "JournalEntry", reason);
        _db.SaveChanges();
    }
}
