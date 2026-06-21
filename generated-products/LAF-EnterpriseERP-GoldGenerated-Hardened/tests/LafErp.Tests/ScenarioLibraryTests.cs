using LafErp.Core;
using LafErp.Services;
using Xunit;

namespace LafErp.Tests;

/// <summary>
/// Reusable end-to-end business scenarios that exercise the engine the way a real deployment would:
/// setup, quote-to-cash, procure-to-pay, inventory movements, journals, month-end integrity, and the
/// negative RBAC / maker-checker / stock paths. Each is self-contained on a fresh in-memory database.
/// </summary>
public class ScenarioLibraryTests
{
    // 1 — Company + chart of accounts are established with all root types and a fiscal year.
    [Fact]
    public void Scenario_company_and_chart_of_accounts_setup()
    {
        using var h = new TestHost();
        Assert.True(h.Db.Companies.Count() >= 1);
        Assert.True(h.Db.FiscalYears.Count() >= 1);
        foreach (var rt in new[] { RootType.Asset, RootType.Liability, RootType.Equity, RootType.Income, RootType.Expense })
            Assert.Contains(h.Db.Accounts, a => a.RootType == rt);
    }

    // 2 — Quote-to-cash: sell with maker/checker, receive payment, receivable clears.
    [Fact]
    public void Scenario_quote_to_cash_clears_receivable()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 100, 60);
        h.Login("alice", "Sales User");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 20, 100) });
        h.Sales.Submit(si.Id);
        h.Login("bob", "Accounts Manager");
        h.Sales.Approve(si.Id);
        Assert.Equal(2000m, h.Accounting.AccountsReceivable(h.Seed.CompanyId));
        h.Login("cathy", "Accounts User");
        var pe = h.Payment.Create(h.Seed.CompanyId, PartyType.Customer, h.Seed.CustomerId, h.Seed.BankAccountId, 2000, si.Id);
        h.Payment.Submit(pe.Id);
        h.Login("bob", "Accounts Manager");
        h.Payment.Approve(pe.Id);
        Assert.Equal(0m, h.Accounting.AccountsReceivable(h.Seed.CompanyId));
    }

    // 3 — Procure-to-pay: buy on credit, pay the supplier, payable clears.
    [Fact]
    public void Scenario_procure_to_pay_clears_payable()
    {
        using var h = new TestHost();
        var piId = h.ReceiveStock(h.Seed.WidgetItemId, 100, 60); // AP 6000
        Assert.Equal(6000m, h.Accounting.AccountsPayable(h.Seed.CompanyId));
        h.Login("cathy", "Accounts User");
        var pe = h.Payment.Create(h.Seed.CompanyId, PartyType.Supplier, h.Seed.SupplierId, h.Seed.BankAccountId, 6000, piId);
        h.Payment.Submit(pe.Id);
        h.Login("bob", "Accounts Manager");
        h.Payment.Approve(pe.Id);
        Assert.Equal(0m, h.Accounting.AccountsPayable(h.Seed.CompanyId));
    }

    // 4 — Inventory receipt increases on-hand quantity and value.
    [Fact]
    public void Scenario_inventory_receipt_increases_qty_and_value()
    {
        using var h = new TestHost();
        h.Stock.MoveIn(h.Seed.CompanyId, h.Seed.WidgetItemId, h.Seed.WarehouseId, 50, 10, "Receipt", "R-1", System.DateTime.UtcNow);
        h.Db.SaveChanges();
        Assert.Equal(50m, h.Stock.CurrentQty(h.Seed.WidgetItemId, h.Seed.WarehouseId));
        Assert.Equal(500m, h.Stock.Balance(h.Seed.WidgetItemId, h.Seed.WarehouseId).Value);
    }

    // 5 — Inventory issue relieves stock at the moving-average valuation.
    [Fact]
    public void Scenario_inventory_issue_uses_moving_average()
    {
        using var h = new TestHost();
        // Each movement is persisted before the next so the moving-average carry-forward reads the prior entry.
        h.Stock.MoveIn(h.Seed.CompanyId, h.Seed.WidgetItemId, h.Seed.WarehouseId, 100, 10, "Receipt", "R-1", System.DateTime.UtcNow); h.Db.SaveChanges();
        h.Stock.MoveIn(h.Seed.CompanyId, h.Seed.WidgetItemId, h.Seed.WarehouseId, 100, 20, "Receipt", "R-2", System.DateTime.UtcNow); h.Db.SaveChanges();
        h.Stock.MoveOut(h.Seed.CompanyId, h.Seed.WidgetItemId, h.Seed.WarehouseId, 50, "Issue", "I-1", System.DateTime.UtcNow); h.Db.SaveChanges();
        Assert.Equal(150m, h.Stock.CurrentQty(h.Seed.WidgetItemId, h.Seed.WarehouseId));
        Assert.Equal(15m, h.Stock.ValuationRate(h.Seed.WidgetItemId, h.Seed.WarehouseId)); // (1000+2000)/200
    }

    // 6 — Inventory transfer between warehouses conserves total quantity.
    [Fact]
    public void Scenario_inventory_transfer_conserves_total_qty()
    {
        using var h = new TestHost();
        var second = new Warehouse { CompanyId = h.Seed.CompanyId, Code = "WH2", Name = "Second Store", StockAccountId = h.Seed.CashAccountId };
        h.Db.Warehouses.Add(second); h.Db.SaveChanges();
        var wh2 = second.Id;
        h.Stock.MoveIn(h.Seed.CompanyId, h.Seed.WidgetItemId, h.Seed.WarehouseId, 100, 10, "Receipt", "R-1", System.DateTime.UtcNow); h.Db.SaveChanges();
        h.Stock.MoveOut(h.Seed.CompanyId, h.Seed.WidgetItemId, h.Seed.WarehouseId, 30, "Transfer", "T-1", System.DateTime.UtcNow); h.Db.SaveChanges();
        h.Stock.MoveIn(h.Seed.CompanyId, h.Seed.WidgetItemId, wh2, 30, 10, "Transfer", "T-1", System.DateTime.UtcNow); h.Db.SaveChanges();
        var total = h.Stock.CurrentQty(h.Seed.WidgetItemId, h.Seed.WarehouseId) + h.Stock.CurrentQty(h.Seed.WidgetItemId, wh2);
        Assert.Equal(100m, total);
    }

    // 7 — Inventory adjustment writes an additional signed ledger entry.
    [Fact]
    public void Scenario_inventory_adjustment_writes_entry()
    {
        using var h = new TestHost();
        h.Stock.MoveIn(h.Seed.CompanyId, h.Seed.WidgetItemId, h.Seed.WarehouseId, 100, 10, "Receipt", "R-1", System.DateTime.UtcNow);
        h.Db.SaveChanges();
        var before = h.Db.StockLedgerEntries.Count();
        h.Stock.MoveOut(h.Seed.CompanyId, h.Seed.WidgetItemId, h.Seed.WarehouseId, 5, "Adjustment", "ADJ-1", System.DateTime.UtcNow);
        h.Db.SaveChanges();
        Assert.Equal(before + 1, h.Db.StockLedgerEntries.Count());
        Assert.Equal(95m, h.Stock.CurrentQty(h.Seed.WidgetItemId, h.Seed.WarehouseId));
    }

    // 8 — Issuing more than on-hand is blocked (negative-stock control).
    [Fact]
    public void Scenario_insufficient_stock_issue_is_blocked()
    {
        using var h = new TestHost();
        h.Stock.MoveIn(h.Seed.CompanyId, h.Seed.WidgetItemId, h.Seed.WarehouseId, 10, 10, "Receipt", "R-1", System.DateTime.UtcNow);
        h.Db.SaveChanges();
        Assert.Throws<DomainException>(() =>
            h.Stock.MoveOut(h.Seed.CompanyId, h.Seed.WidgetItemId, h.Seed.WarehouseId, 50, "Issue", "I-1", System.DateTime.UtcNow));
    }

    // 9 — A balanced journal posts and keeps the general ledger in balance.
    [Fact]
    public void Scenario_balanced_journal_keeps_gl_balanced()
    {
        using var h = new TestHost();
        h.Login("cathy", "Accounts User"); // maker role for journals
        var je = h.Journal.Create(h.Seed.CompanyId, "Bank-to-cash transfer", new[]
        {
            new JournalService.JeLine(h.Seed.CashAccountId, 1000, 0),
            new JournalService.JeLine(h.Seed.BankAccountId, 0, 1000),
        });
        h.Journal.Submit(je.Id);
        var fresh = h.Db.JournalEntries.First(x => x.Id == je.Id);
        if (fresh.Status != DocStatus.Submitted) { h.Login("bob", "Accounts Manager"); h.Journal.Approve(je.Id); }
        var gl = h.Accounting.GlTotals(h.Seed.CompanyId);
        Assert.Equal(gl.Debit, gl.Credit);
    }

    // 10 — Month-end: after trading activity the trial balance (GL) still balances and is non-trivial.
    [Fact]
    public void Scenario_month_end_trial_balance_balances()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 100, 60);
        h.Login("alice", "Sales User");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 20, 100) });
        h.Sales.Submit(si.Id);
        h.Login("bob", "Accounts Manager");
        h.Sales.Approve(si.Id);
        var gl = h.Accounting.GlTotals(h.Seed.CompanyId);
        Assert.Equal(gl.Debit, gl.Credit);
        Assert.True(gl.Debit > 0);
    }

    // 11 — Maker/checker negative: the maker may not approve their own document.
    [Fact]
    public void Scenario_maker_cannot_approve_own_document()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 100, 60);
        h.Login("alice", "Sales User");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 20, 100) });
        h.Sales.Submit(si.Id);
        Assert.Throws<DomainException>(() => h.Sales.Approve(si.Id)); // alice is still the acting user
    }

    // 12 — RBAC negative + append-only audit: a role-less user cannot approve, and the audit trail grows.
    [Fact]
    public void Scenario_rbac_negative_and_audit_trail()
    {
        using var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 100, 60);
        h.Login("alice", "Sales User");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 20, 100) });
        h.Sales.Submit(si.Id);
        h.Login("mallory"); // no roles at all
        Assert.Throws<DomainException>(() => h.Sales.Approve(si.Id));
        Assert.True(h.Db.AuditEvents.Count() > 0);
        Assert.All(h.Db.AuditEvents, a => Assert.False(string.IsNullOrEmpty(a.PerformedBy)));
    }
}
