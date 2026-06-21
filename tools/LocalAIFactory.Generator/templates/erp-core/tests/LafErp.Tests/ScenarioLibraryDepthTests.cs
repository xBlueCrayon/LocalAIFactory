using System.Linq;
using LafErp.Core;
using LafErp.Services;
using Xunit;

namespace LafErp.Tests;

/// <summary>
/// Depth scenarios composing manufacturing, reporting, and trading flows end-to-end. Each asserts a distinct
/// business outcome on a fresh in-memory database.
/// </summary>
public class ScenarioLibraryDepthTests
{
    private static (TestHost h, int bomId) MfgSetup(decimal widgets = 100m, decimal rate = 60m)
    {
        var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, widgets, rate);
        h.Login("planner", "System Manager");
        var bom = h.Manufacturing.CreateBom("Gadget BOM", h.Seed.GadgetItemId, 1, new[] { (h.Seed.WidgetItemId, 2m) });
        return (h, bom.Id);
    }

    // 14
    [Fact] public void Scenario_make_to_stock_full_cycle()
    {
        var (h, bom) = MfgSetup(); using (h)
        {
            var o = h.Manufacturing.CreateOrder(h.Seed.CompanyId, h.Seed.WarehouseId, bom, 10);
            h.Manufacturing.IssueMaterials(o.Id); h.Manufacturing.InspectQuality(o.Id, true); h.Manufacturing.Complete(o.Id);
            Assert.Equal(10m, h.Stock.CurrentQty(h.Seed.GadgetItemId, h.Seed.WarehouseId));
        }
    }

    // 15
    [Fact] public void Scenario_quality_fail_then_rework_passes()
    {
        var (h, bom) = MfgSetup(); using (h)
        {
            var o = h.Manufacturing.CreateOrder(h.Seed.CompanyId, h.Seed.WarehouseId, bom, 5);
            h.Manufacturing.IssueMaterials(o.Id);
            h.Manufacturing.InspectQuality(o.Id, false);
            Assert.Throws<DomainException>(() => h.Manufacturing.Complete(o.Id));
            h.Manufacturing.InspectQuality(o.Id, true);
            h.Manufacturing.Complete(o.Id);
            Assert.Equal(ProductionStatus.Completed, h.Manufacturing.Get(o.Id).Status);
        }
    }

    // 16
    [Fact] public void Scenario_production_cost_flows_to_finished_good_valuation()
    {
        var (h, bom) = MfgSetup(rate: 50m); using (h)
        {
            var o = h.Manufacturing.CreateOrder(h.Seed.CompanyId, h.Seed.WarehouseId, bom, 10); // 20 widgets @50 = 1000 -> unit 100
            h.Manufacturing.IssueMaterials(o.Id); h.Manufacturing.InspectQuality(o.Id, true); h.Manufacturing.Complete(o.Id);
            Assert.Equal(100m, h.Stock.ValuationRate(h.Seed.GadgetItemId, h.Seed.WarehouseId));
        }
    }

    // 17
    [Fact] public void Scenario_procure_produce_sell_finished_good()
    {
        var (h, bom) = MfgSetup(widgets: 200m); using (h)
        {
            var o = h.Manufacturing.CreateOrder(h.Seed.CompanyId, h.Seed.WarehouseId, bom, 20);
            h.Manufacturing.IssueMaterials(o.Id); h.Manufacturing.InspectQuality(o.Id, true); h.Manufacturing.Complete(o.Id);
            h.Login("alice", "Sales User");
            var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
                new[] { new InvoiceLineInput(h.Seed.GadgetItemId, 5, 200) });
            h.Sales.Submit(si.Id);
            if (h.Db.SalesInvoices.First(x => x.Id == si.Id).Status != DocStatus.Submitted)
            { h.Login("bob", "Accounts Manager"); h.Sales.Approve(si.Id); }
            Assert.Equal(15m, h.Stock.CurrentQty(h.Seed.GadgetItemId, h.Seed.WarehouseId)); // 20 made - 5 sold
            Assert.True(h.Accounting.AccountsReceivable(h.Seed.CompanyId) > 0);
        }
    }

    private static TestHost Sold()
    {
        var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, 100, 60);
        h.Login("alice", "Sales User");
        var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
            new[] { new InvoiceLineInput(h.Seed.WidgetItemId, 20, 100) });
        h.Sales.Submit(si.Id);
        h.Login("bob", "Accounts Manager"); h.Sales.Approve(si.Id);
        return h;
    }

    // 18
    [Fact] public void Scenario_sales_reporting_is_consistent()
    {
        using var h = Sold();
        var reg = h.Reports.SalesRegister(h.Seed.CompanyId);
        Assert.Single(reg);
        Assert.Equal(reg.Sum(r => r.GrandTotal), h.Reports.SalesSummaryByCustomer(h.Seed.CompanyId).Sum(r => r.Total));
    }

    // 19
    [Fact] public void Scenario_receivables_aging_is_current_for_new_invoice()
    {
        using var h = Sold();
        var aging = h.Reports.ReceivablesAging(h.Seed.CompanyId);
        Assert.Equal(aging.Sum(a => a.Total), aging.Sum(a => a.Bucket0_30));
    }

    // 20
    [Fact] public void Scenario_tax_summary_reconciles_with_sales()
    {
        using var h = Sold();
        var tax = h.Reports.TaxSummaryReport(h.Seed.CompanyId);
        Assert.Equal(h.Reports.SalesRegister(h.Seed.CompanyId).Sum(r => r.TaxTotal), tax.OutputTax);
    }

    // 21
    [Fact] public void Scenario_stock_valuation_reconciles_after_sale()
    {
        using var h = Sold();
        Assert.Contains(h.Reports.StockValuation(h.Seed.CompanyId), r => r.ItemId == h.Seed.WidgetItemId && r.Qty == 80m);
    }

    // 22
    [Fact] public void Scenario_reorder_alert_for_low_stock()
    {
        using var h = Sold(); // 80 widgets remain
        Assert.Contains(h.Reports.ReorderReport(h.Seed.CompanyId, 90m), r => r.ItemId == h.Seed.WidgetItemId);
    }

    // 23
    [Fact] public void Scenario_work_order_summary_tracks_status()
    {
        var (h, bom) = MfgSetup(); using (h)
        {
            var o = h.Manufacturing.CreateOrder(h.Seed.CompanyId, h.Seed.WarehouseId, bom, 5);
            Assert.Contains(h.Reports.WorkOrderSummary(h.Seed.CompanyId), s => s.Status == ProductionStatus.Draft);
            h.Manufacturing.IssueMaterials(o.Id); h.Manufacturing.InspectQuality(o.Id, true); h.Manufacturing.Complete(o.Id);
            Assert.Contains(h.Reports.WorkOrderSummary(h.Seed.CompanyId), s => s.Status == ProductionStatus.Completed);
        }
    }

    // 24
    [Fact] public void Scenario_purchase_reporting_reflects_procurement()
    {
        using var h = Sold();
        Assert.NotEmpty(h.Reports.PurchaseRegister(h.Seed.CompanyId));
        Assert.Contains(h.Reports.PurchaseSummaryBySupplier(h.Seed.CompanyId), r => r.PartyId == h.Seed.SupplierId);
    }

    // 25
    [Fact] public void Scenario_combined_business_day_balances_and_reports()
    {
        var (h, bom) = MfgSetup(widgets: 200m); using (h)
        {
            // produce
            var o = h.Manufacturing.CreateOrder(h.Seed.CompanyId, h.Seed.WarehouseId, bom, 10);
            h.Manufacturing.IssueMaterials(o.Id); h.Manufacturing.InspectQuality(o.Id, true); h.Manufacturing.Complete(o.Id);
            // sell finished + raw
            h.Login("alice", "Sales User");
            var si = h.Sales.CreateInvoice(h.Seed.CompanyId, h.Seed.CustomerId, h.Seed.WarehouseId,
                new[] { new InvoiceLineInput(h.Seed.GadgetItemId, 4, 200) });
            h.Sales.Submit(si.Id);
            if (h.Db.SalesInvoices.First(x => x.Id == si.Id).Status != DocStatus.Submitted)
            { h.Login("bob", "Accounts Manager"); h.Sales.Approve(si.Id); }
            // integrity
            var gl = h.Accounting.GlTotals(h.Seed.CompanyId);
            Assert.Equal(gl.Debit, gl.Credit);
            Assert.NotEmpty(h.Reports.SalesRegister(h.Seed.CompanyId));
            Assert.Contains(h.Reports.WorkOrderSummary(h.Seed.CompanyId), s => s.Status == ProductionStatus.Completed);
        }
    }
}
