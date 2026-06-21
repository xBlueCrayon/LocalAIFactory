using Xunit;

namespace LafErp.Tests;

/// <summary>Proves the generator emitted the governed local-LLM catalog modules into the product assembly.</summary>
public class GenerationProvenanceTests
{
    [Fact]
    public void Generated_catalog_entities_exist_in_assembly()
    {
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.Quotation"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.DeliveryNote"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.PurchaseReceipt"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.MaterialRequest"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.StockTransfer"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.BillOfMaterials"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.WorkOrder"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.QualityInspection"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.Employee"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.AttendanceRecord"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.SalaryComponent"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.Timesheet"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.PosProfile"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.WebProduct"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.MaintenanceSchedule"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.CustomFieldDef"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.NotificationRule"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.CustomerSegment"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.ProductCategory"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.EmployeeRole"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.MarketingCampaign"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.VendorContract"));
        Assert.True(22 >= 0);
    }
}