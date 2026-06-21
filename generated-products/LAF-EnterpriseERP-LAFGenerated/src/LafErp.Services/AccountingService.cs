using LafErp.Core;
using LafErp.Data;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Services;

public record GLRow(DateTime PostingDate, string Account, decimal Debit, decimal Credit, string VoucherType, string VoucherNo, string? Party);
public record TrialBalanceRow(string Account, decimal Debit, decimal Credit);
public record PartyOutstanding(PartyType PartyType, int PartyId, string Party, decimal Outstanding);

/// <summary>
/// General-ledger posting and financial reporting. Every posting writes balanced GL entries; the engine
/// refuses to persist an unbalanced voucher. Reports read only from the immutable GL.
/// </summary>
public class AccountingService
{
    private readonly ErpDbContext _db;
    private readonly StockService _stock;

    public AccountingService(ErpDbContext db, StockService stock)
    {
        _db = db;
        _stock = stock;
    }

    private int TaxAccountId(int companyId) =>
        _db.Accounts.Where(a => a.CompanyId == companyId && a.Name == "Tax Payable" && !a.IsGroup)
                    .Select(a => a.Id).FirstOrDefault();

    // ---------------- Posting ----------------

    public void PostJournalEntry(JournalEntry je)
    {
        if (je.Lines.Count == 0) throw new DomainException("Journal entry has no lines.");
        var dr = je.Lines.Sum(l => l.Debit);
        var cr = je.Lines.Sum(l => l.Credit);
        if (dr <= 0) throw new DomainException("Journal entry total must be positive.");
        if (Math.Round(dr, 4) != Math.Round(cr, 4))
            throw new DomainException($"Journal entry not balanced: debit {dr} != credit {cr}.");
        foreach (var l in je.Lines)
        {
            if (l.Debit < 0 || l.Credit < 0) throw new DomainException("Negative debit/credit not allowed.");
            if (l.Debit > 0 && l.Credit > 0) throw new DomainException("A line cannot be both debit and credit.");
            Post(je.CompanyId, l.AccountId, je.PostingDate, l.Debit, l.Credit, "JournalEntry", je.DocNo, l.PartyType, l.PartyId, l.CostCenterId);
        }
        je.TotalDebit = dr;
        je.TotalCredit = cr;
    }

    public void PostSalesInvoice(SalesInvoice si)
    {
        var customer = _db.Customers.IgnoreQueryFilters().First(c => c.Id == si.CustomerId);
        // Revenue side: Dr Debtors (grand total), Cr income per line, Cr tax payable.
        Post(si.CompanyId, customer.ReceivableAccountId, si.PostingDate, si.GrandTotal, 0, "SalesInvoice", si.DocNo, PartyType.Customer, si.CustomerId, null);
        foreach (var grp in si.Lines.GroupBy(l => _db.Items.IgnoreQueryFilters().First(i => i.Id == l.ItemId).IncomeAccountId))
            Post(si.CompanyId, grp.Key, si.PostingDate, 0, grp.Sum(l => l.Amount), "SalesInvoice", si.DocNo, null, null, null);
        if (si.TaxTotal > 0)
            Post(si.CompanyId, TaxAccountId(si.CompanyId), si.PostingDate, 0, si.TaxTotal, "SalesInvoice", si.DocNo, null, null, null);

        // Cost side + stock issue.
        if (si.UpdateStock && si.WarehouseId is int wh)
        {
            foreach (var l in si.Lines)
            {
                var item = _db.Items.IgnoreQueryFilters().First(i => i.Id == l.ItemId);
                if (!item.IsStockItem) continue;
                var warehouse = _db.Warehouses.First(w => w.Id == wh);
                var cost = _stock.ValuationRate(l.ItemId, wh) * l.Qty;
                _stock.MoveOut(si.CompanyId, l.ItemId, wh, l.Qty, "SalesInvoice", si.DocNo, si.PostingDate);
                if (cost > 0)
                {
                    Post(si.CompanyId, item.ExpenseAccountId, si.PostingDate, cost, 0, "SalesInvoice", si.DocNo, null, null, null); // COGS
                    Post(si.CompanyId, warehouse.StockAccountId, si.PostingDate, 0, cost, "SalesInvoice", si.DocNo, null, null, null); // relieve stock
                }
            }
        }
        si.OutstandingAmount = si.GrandTotal - si.PaidAmount;
    }

    public void PostPurchaseInvoice(PurchaseInvoice pi)
    {
        var supplier = _db.Suppliers.IgnoreQueryFilters().First(s => s.Id == pi.SupplierId);
        Post(pi.CompanyId, supplier.PayableAccountId, pi.PostingDate, 0, pi.GrandTotal, "PurchaseInvoice", pi.DocNo, PartyType.Supplier, pi.SupplierId, null);
        foreach (var l in pi.Lines)
        {
            var item = _db.Items.IgnoreQueryFilters().First(i => i.Id == l.ItemId);
            if (pi.UpdateStock && item.IsStockItem && pi.WarehouseId is int wh)
            {
                var warehouse = _db.Warehouses.First(w => w.Id == wh);
                Post(pi.CompanyId, warehouse.StockAccountId, pi.PostingDate, l.Amount, 0, "PurchaseInvoice", pi.DocNo, null, null, null);
                _stock.MoveIn(pi.CompanyId, l.ItemId, wh, l.Qty, l.Qty > 0 ? l.Amount / l.Qty : 0, "PurchaseInvoice", pi.DocNo, pi.PostingDate);
            }
            else
            {
                Post(pi.CompanyId, item.ExpenseAccountId, pi.PostingDate, l.Amount, 0, "PurchaseInvoice", pi.DocNo, null, null, null);
            }
        }
        if (pi.TaxTotal > 0)
            Post(pi.CompanyId, TaxAccountId(pi.CompanyId), pi.PostingDate, pi.TaxTotal, 0, "PurchaseInvoice", pi.DocNo, null, null, null);
        pi.OutstandingAmount = pi.GrandTotal - pi.PaidAmount;
    }

    public void PostPayment(PaymentEntry pe)
    {
        if (pe.Amount <= 0) throw new DomainException("Payment amount must be positive.");
        if (pe.PartyType == PartyType.Customer)
        {
            var customer = _db.Customers.IgnoreQueryFilters().First(c => c.Id == pe.PartyId);
            Post(pe.CompanyId, pe.BankAccountId, pe.PostingDate, pe.Amount, 0, "PaymentEntry", pe.DocNo, null, null, null);
            Post(pe.CompanyId, customer.ReceivableAccountId, pe.PostingDate, 0, pe.Amount, "PaymentEntry", pe.DocNo, PartyType.Customer, pe.PartyId, null);
        }
        else
        {
            var supplier = _db.Suppliers.IgnoreQueryFilters().First(s => s.Id == pe.PartyId);
            Post(pe.CompanyId, supplier.PayableAccountId, pe.PostingDate, pe.Amount, 0, "PaymentEntry", pe.DocNo, PartyType.Supplier, pe.PartyId, null);
            Post(pe.CompanyId, pe.BankAccountId, pe.PostingDate, 0, pe.Amount, "PaymentEntry", pe.DocNo, null, null, null);
        }
    }

    /// <summary>Write reversing GL (and stock) entries for a voucher being cancelled.</summary>
    public void ReverseVoucher(int companyId, string voucherType, string voucherNo)
    {
        var entries = _db.GLEntries
            .Where(g => g.VoucherType == voucherType && g.VoucherNo == voucherNo && !g.IsReversal)
            .ToList();
        foreach (var g in entries)
        {
            _db.GLEntries.Add(new GLEntry
            {
                CompanyId = g.CompanyId,
                AccountId = g.AccountId,
                PostingDate = g.PostingDate,
                Debit = g.Credit,
                Credit = g.Debit,
                VoucherType = voucherType,
                VoucherNo = voucherNo,
                PartyType = g.PartyType,
                PartyId = g.PartyId,
                CostCenterId = g.CostCenterId,
                IsReversal = true,
                Remarks = "Reversal on cancellation"
            });
        }
        _stock.ReverseVoucher(voucherType, voucherNo);
    }

    private void Post(int companyId, int accountId, DateTime date, decimal debit, decimal credit, string vt, string vn, PartyType? pt, int? pid, int? cc)
    {
        if (accountId == 0) throw new DomainException($"Missing account for {vt} {vn}.");
        _db.GLEntries.Add(new GLEntry
        {
            CompanyId = companyId,
            AccountId = accountId,
            PostingDate = date,
            Debit = debit,
            Credit = credit,
            VoucherType = vt,
            VoucherNo = vn,
            PartyType = pt,
            PartyId = pid,
            CostCenterId = cc
        });
    }

    // ---------------- Reports ----------------

    public List<GLRow> GeneralLedger(int companyId, DateTime from, DateTime to, int? accountId = null)
    {
        var q = _db.GLEntries.Where(g => g.CompanyId == companyId && g.PostingDate >= from && g.PostingDate <= to);
        if (accountId is int a) q = q.Where(g => g.AccountId == a);
        return q.OrderBy(g => g.PostingDate).ThenBy(g => g.Id)
            .Select(g => new GLRow(g.PostingDate, g.Account!.Name, g.Debit, g.Credit, g.VoucherType, g.VoucherNo,
                g.PartyType == null ? null : g.PartyType + ":" + g.PartyId))
            .ToList();
    }

    public List<TrialBalanceRow> TrialBalance(int companyId)
    {
        // Materialize then aggregate in memory so decimal sums are exact on every provider.
        return _db.GLEntries.Where(g => g.CompanyId == companyId)
            .Select(g => new { g.Account!.Name, g.Debit, g.Credit })
            .AsEnumerable()
            .GroupBy(g => g.Name)
            .Select(grp => new TrialBalanceRow(grp.Key, grp.Sum(x => x.Debit), grp.Sum(x => x.Credit)))
            .OrderBy(r => r.Account)
            .ToList();
    }

    public decimal AccountsReceivable(int companyId) =>
        _db.GLEntries.Where(g => g.CompanyId == companyId && g.PartyType == PartyType.Customer)
                     .Select(g => new { g.Debit, g.Credit }).AsEnumerable()
                     .Sum(g => g.Debit - g.Credit);

    public decimal AccountsPayable(int companyId) =>
        _db.GLEntries.Where(g => g.CompanyId == companyId && g.PartyType == PartyType.Supplier)
                     .Select(g => new { g.Debit, g.Credit }).AsEnumerable()
                     .Sum(g => g.Credit - g.Debit);

    /// <summary>Net GL must always balance globally; used by tests and the integrity check.</summary>
    public (decimal Debit, decimal Credit) GlTotals(int companyId)
    {
        var rows = _db.GLEntries.Where(g => g.CompanyId == companyId)
                                .Select(g => new { g.Debit, g.Credit }).AsEnumerable().ToList();
        return (rows.Sum(r => r.Debit), rows.Sum(r => r.Credit));
    }
}
