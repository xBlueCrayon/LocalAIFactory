using System.Linq;
using LafErp.Core;
using LafErp.Services;
using Xunit;

namespace LafErp.Tests;

/// <summary>ERP-grade reporting depth: registers, party summaries, aging, tax, stock valuation, work-order summary.</summary>
public class ReportsDepthTests
{
    // A submitted sale + the seeded purchase, so registers/aging/tax/stock reports have real data.
    private static TestHost WithActivity()
    {
        var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 100, 60); // creates a submitted PurchaseInvoice + stock
        h.Login("alice", "Sales User");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 20, 100) });
        h.Sales.Submit(si.Id);
        h.Login("bob", "Accounts Manager");
        h.Sales.Approve(si.Id);
        return h;
    }

    [Fact] public void Sales_register_lists_submitted_invoices()
    { using var h = WithActivity(); Assert.Single(h.Reports.SalesRegister(h.Seed.CompanyId)); }

    [Fact] public void Purchase_register_lists_submitted_invoices()
    { using var h = WithActivity(); Assert.NotEmpty(h.Reports.PurchaseRegister(h.Seed.CompanyId)); }

    [Fact] public void Sales_summary_by_customer_totals_match_register()
    {
        using var h = WithActivity();
        var reg = h.Reports.SalesRegister(h.Seed.CompanyId).Sum(r => r.GrandTotal);
        var sum = h.Reports.SalesSummaryByCustomer(h.Seed.CompanyId).Sum(r => r.Total);
        Assert.Equal(reg, sum);
    }

    [Fact] public void Purchase_summary_by_supplier_has_the_seeded_supplier()
    {
        using var h = WithActivity();
        Assert.Contains(h.Reports.PurchaseSummaryBySupplier(h.Seed.CompanyId), r => r.PartyId == h.Seed.SupplierId);
    }

    [Fact] public void Outstanding_sales_invoices_appear_before_payment()
    { using var h = WithActivity(); Assert.NotEmpty(h.Reports.OutstandingSalesInvoices(h.Seed.CompanyId)); }

    [Fact] public void Receivables_aging_total_equals_outstanding_and_is_recent()
    {
        using var h = WithActivity();
        var aging = h.Reports.ReceivablesAging(h.Seed.CompanyId);
        Assert.NotEmpty(aging);
        var total = aging.Sum(a => a.Total);
        Assert.Equal(total, aging.Sum(a => a.Bucket0_30)); // freshly posted -> all in the 0-30 bucket
        Assert.True(total > 0);
    }

    [Fact] public void Tax_summary_output_equals_sum_of_sales_tax()
    {
        using var h = WithActivity();
        var tax = h.Reports.TaxSummaryReport(h.Seed.CompanyId);
        var expected = h.Reports.SalesRegister(h.Seed.CompanyId).Sum(r => r.TaxTotal);
        Assert.Equal(expected, tax.OutputTax);
        Assert.Equal(tax.OutputTax - tax.InputTax, tax.NetTax);
    }

    [Fact] public void Stock_valuation_reports_remaining_widgets()
    {
        using var h = WithActivity();
        var val = h.Reports.StockValuation(h.Seed.CompanyId);
        Assert.Contains(val, r => r.ItemId == h.Seed.WidgetItemId && r.Qty == 80m); // 100 received - 20 sold
    }

    [Fact] public void Reorder_report_flags_items_at_or_below_threshold()
    {
        using var h = WithActivity();
        Assert.Contains(h.Reports.ReorderReport(h.Seed.CompanyId, 100m), r => r.ItemId == h.Seed.WidgetItemId);
        Assert.DoesNotContain(h.Reports.ReorderReport(h.Seed.CompanyId, 10m), r => r.ItemId == h.Seed.WidgetItemId);
    }

    [Fact] public void Work_order_summary_groups_by_status()
    {
        using var h = WithActivity();
        h.Login("planner", "System Manager");
        var bom = h.Manufacturing.CreateBom("B", h.Seed.GadgetItemId, 1, new[] { (h.Seed.WidgetItemId, 2m) });
        h.Manufacturing.CreateOrder(h.Seed.CompanyId, h.Seed.WarehouseId, bom.Id, 5);
        var summary = h.Reports.WorkOrderSummary(h.Seed.CompanyId);
        Assert.Contains(summary, s => s.Status == ProductionStatus.Draft && s.Count == 1 && s.TotalQty == 5m);
    }

    [Fact] public void Reports_are_company_scoped()
    {
        using var h = WithActivity();
        Assert.Empty(h.Reports.SalesRegister(companyId: 9999));
        Assert.Empty(h.Reports.StockValuation(companyId: 9999));
    }
}
