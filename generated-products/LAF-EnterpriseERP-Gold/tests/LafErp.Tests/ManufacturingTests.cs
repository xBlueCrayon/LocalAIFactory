using LafErp.Core;
using LafErp.Services;
using Xunit;

namespace LafErp.Tests;

/// <summary>
/// Real manufacturing depth: BOM-driven production with material issue, quality gating, finished-goods
/// receipt and moving-average production costing. Produces 1 Gadget (finished) from 2 Widgets (component).
/// </summary>
public class ManufacturingTests
{
    private static (TestHost h, int bomId) SetUp(decimal widgetQtyOnHand = 100m, decimal widgetRate = 60m)
    {
        var h = new TestHost();
        h.ReceiveStock(h.Seed.WidgetItemId, widgetQtyOnHand, widgetRate);
        h.Login("planner", "System Manager");
        var bom = h.Manufacturing.CreateBom("Gadget from 2 Widgets", h.Seed.GadgetItemId, 1,
            new[] { (h.Seed.WidgetItemId, 2m) });
        return (h, bom.Id);
    }

    [Fact]
    public void Bom_requires_at_least_one_component()
    {
        using var h = new TestHost();
        Assert.Throws<DomainException>(() =>
            h.Manufacturing.CreateBom("Empty", h.Seed.GadgetItemId, 1, System.Array.Empty<(int, decimal)>()));
    }

    [Fact]
    public void Create_order_from_bom_is_draft_with_docno()
    {
        var (h, bomId) = SetUp();
        using (h)
        {
            var order = h.Manufacturing.CreateOrder(h.Seed.CompanyId, h.Seed.WarehouseId, bomId, 10);
            Assert.Equal(ProductionStatus.Draft, order.Status);
            Assert.False(string.IsNullOrEmpty(order.DocNo));
        }
    }

    [Fact]
    public void Issue_materials_reduces_component_stock()
    {
        var (h, bomId) = SetUp();
        using (h)
        {
            var order = h.Manufacturing.CreateOrder(h.Seed.CompanyId, h.Seed.WarehouseId, bomId, 10); // needs 20 widgets
            h.Manufacturing.IssueMaterials(order.Id);
            Assert.Equal(80m, h.Stock.CurrentQty(h.Seed.WidgetItemId, h.Seed.WarehouseId));
        }
    }

    [Fact]
    public void Insufficient_materials_blocks_issue()
    {
        var (h, bomId) = SetUp(widgetQtyOnHand: 10m); // only 10 widgets, need 20
        using (h)
        {
            var order = h.Manufacturing.CreateOrder(h.Seed.CompanyId, h.Seed.WarehouseId, bomId, 10);
            Assert.Throws<DomainException>(() => h.Manufacturing.IssueMaterials(order.Id));
        }
    }

    [Fact]
    public void Quality_failure_blocks_completion()
    {
        var (h, bomId) = SetUp();
        using (h)
        {
            var order = h.Manufacturing.CreateOrder(h.Seed.CompanyId, h.Seed.WarehouseId, bomId, 10);
            h.Manufacturing.IssueMaterials(order.Id);
            h.Manufacturing.InspectQuality(order.Id, passed: false);
            Assert.Throws<DomainException>(() => h.Manufacturing.Complete(order.Id));
        }
    }

    [Fact]
    public void Quality_pass_allows_completion_and_receives_finished_goods()
    {
        var (h, bomId) = SetUp();
        using (h)
        {
            var order = h.Manufacturing.CreateOrder(h.Seed.CompanyId, h.Seed.WarehouseId, bomId, 10);
            h.Manufacturing.IssueMaterials(order.Id);
            h.Manufacturing.InspectQuality(order.Id, passed: true);
            h.Manufacturing.Complete(order.Id);
            Assert.Equal(ProductionStatus.Completed, h.Manufacturing.Get(order.Id).Status);
            Assert.Equal(10m, h.Stock.CurrentQty(h.Seed.GadgetItemId, h.Seed.WarehouseId));
        }
    }

    [Fact]
    public void Production_cost_is_computed_from_issued_materials()
    {
        var (h, bomId) = SetUp(widgetRate: 60m);
        using (h)
        {
            var order = h.Manufacturing.CreateOrder(h.Seed.CompanyId, h.Seed.WarehouseId, bomId, 10); // 20 widgets @60 = 1200
            h.Manufacturing.IssueMaterials(order.Id);
            h.Manufacturing.InspectQuality(order.Id, true);
            h.Manufacturing.Complete(order.Id);
            var done = h.Manufacturing.Get(order.Id);
            Assert.Equal(1200m, done.MaterialCost);
            Assert.Equal(120m, done.UnitCost); // 1200 / 10
        }
    }

    [Fact]
    public void Completed_order_is_immutable()
    {
        var (h, bomId) = SetUp();
        using (h)
        {
            var order = h.Manufacturing.CreateOrder(h.Seed.CompanyId, h.Seed.WarehouseId, bomId, 10);
            h.Manufacturing.IssueMaterials(order.Id);
            h.Manufacturing.InspectQuality(order.Id, true);
            h.Manufacturing.Complete(order.Id);
            Assert.Throws<DomainException>(() => h.Manufacturing.Complete(order.Id));
            Assert.Throws<DomainException>(() => h.Manufacturing.IssueMaterials(order.Id));
        }
    }

    [Fact]
    public void Cannot_complete_before_quality_pass()
    {
        var (h, bomId) = SetUp();
        using (h)
        {
            var order = h.Manufacturing.CreateOrder(h.Seed.CompanyId, h.Seed.WarehouseId, bomId, 10);
            h.Manufacturing.IssueMaterials(order.Id);
            Assert.Throws<DomainException>(() => h.Manufacturing.Complete(order.Id)); // no quality pass yet
        }
    }

    [Fact]
    public void Manufacturing_lifecycle_is_audited()
    {
        var (h, bomId) = SetUp();
        using (h)
        {
            var order = h.Manufacturing.CreateOrder(h.Seed.CompanyId, h.Seed.WarehouseId, bomId, 10);
            h.Manufacturing.IssueMaterials(order.Id);
            h.Manufacturing.InspectQuality(order.Id, true);
            h.Manufacturing.Complete(order.Id);
            foreach (var action in new[] { "Create", "IssueMaterials", "QualityPassed", "Complete" })
                Assert.Contains(h.Db.AuditEvents, a => a.EntityType == "ProductionOrder" && a.Action == action);
        }
    }
}
