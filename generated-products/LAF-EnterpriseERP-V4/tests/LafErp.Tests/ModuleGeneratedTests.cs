using LafErp.Core;
using LafErp.Services;
using Xunit;

namespace LafErp.Tests;

public class ModuleGeneratedTests
{
    [Fact]
    public void Quotation_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<Quotation>(h.Db, h.Audit);
        svc.Create(new Quotation { Name = "Demo Quotation" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo Quotation");
    }

    [Fact]
    public void Quotation_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<Quotation>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new Quotation { Name = "" }));
    }

    [Fact]
    public void DeliveryNote_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<DeliveryNote>(h.Db, h.Audit);
        svc.Create(new DeliveryNote { Name = "Demo DeliveryNote" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo DeliveryNote");
    }

    [Fact]
    public void DeliveryNote_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<DeliveryNote>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new DeliveryNote { Name = "" }));
    }

    [Fact]
    public void PurchaseReceipt_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<PurchaseReceipt>(h.Db, h.Audit);
        svc.Create(new PurchaseReceipt { Name = "Demo PurchaseReceipt" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo PurchaseReceipt");
    }

    [Fact]
    public void PurchaseReceipt_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<PurchaseReceipt>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new PurchaseReceipt { Name = "" }));
    }

    [Fact]
    public void MaterialRequest_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<MaterialRequest>(h.Db, h.Audit);
        svc.Create(new MaterialRequest { Name = "Demo MaterialRequest" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo MaterialRequest");
    }

    [Fact]
    public void MaterialRequest_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<MaterialRequest>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new MaterialRequest { Name = "" }));
    }

    [Fact]
    public void StockTransfer_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<StockTransfer>(h.Db, h.Audit);
        svc.Create(new StockTransfer { Name = "Demo StockTransfer" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo StockTransfer");
    }

    [Fact]
    public void StockTransfer_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<StockTransfer>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new StockTransfer { Name = "" }));
    }

    [Fact]
    public void BillOfMaterials_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<BillOfMaterials>(h.Db, h.Audit);
        svc.Create(new BillOfMaterials { Name = "Demo BillOfMaterials" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo BillOfMaterials");
    }

    [Fact]
    public void BillOfMaterials_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<BillOfMaterials>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new BillOfMaterials { Name = "" }));
    }

    [Fact]
    public void WorkOrder_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<WorkOrder>(h.Db, h.Audit);
        svc.Create(new WorkOrder { Name = "Demo WorkOrder" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo WorkOrder");
    }

    [Fact]
    public void WorkOrder_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<WorkOrder>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new WorkOrder { Name = "" }));
    }

    [Fact]
    public void QualityInspection_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<QualityInspection>(h.Db, h.Audit);
        svc.Create(new QualityInspection { Name = "Demo QualityInspection" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo QualityInspection");
    }

    [Fact]
    public void QualityInspection_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<QualityInspection>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new QualityInspection { Name = "" }));
    }

    [Fact]
    public void Employee_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<Employee>(h.Db, h.Audit);
        svc.Create(new Employee { Name = "Demo Employee" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo Employee");
    }

    [Fact]
    public void Employee_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<Employee>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new Employee { Name = "" }));
    }

    [Fact]
    public void AttendanceRecord_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<AttendanceRecord>(h.Db, h.Audit);
        svc.Create(new AttendanceRecord { Name = "Demo AttendanceRecord" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo AttendanceRecord");
    }

    [Fact]
    public void AttendanceRecord_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<AttendanceRecord>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new AttendanceRecord { Name = "" }));
    }

    [Fact]
    public void SalaryComponent_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<SalaryComponent>(h.Db, h.Audit);
        svc.Create(new SalaryComponent { Name = "Demo SalaryComponent" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo SalaryComponent");
    }

    [Fact]
    public void SalaryComponent_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<SalaryComponent>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new SalaryComponent { Name = "" }));
    }

    [Fact]
    public void Timesheet_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<Timesheet>(h.Db, h.Audit);
        svc.Create(new Timesheet { Name = "Demo Timesheet" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo Timesheet");
    }

    [Fact]
    public void Timesheet_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<Timesheet>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new Timesheet { Name = "" }));
    }

    [Fact]
    public void PosProfile_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<PosProfile>(h.Db, h.Audit);
        svc.Create(new PosProfile { Name = "Demo PosProfile" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo PosProfile");
    }

    [Fact]
    public void PosProfile_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<PosProfile>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new PosProfile { Name = "" }));
    }

    [Fact]
    public void WebProduct_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<WebProduct>(h.Db, h.Audit);
        svc.Create(new WebProduct { Name = "Demo WebProduct" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo WebProduct");
    }

    [Fact]
    public void WebProduct_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<WebProduct>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new WebProduct { Name = "" }));
    }

    [Fact]
    public void MaintenanceSchedule_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<MaintenanceSchedule>(h.Db, h.Audit);
        svc.Create(new MaintenanceSchedule { Name = "Demo MaintenanceSchedule" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo MaintenanceSchedule");
    }

    [Fact]
    public void MaintenanceSchedule_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<MaintenanceSchedule>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new MaintenanceSchedule { Name = "" }));
    }

    [Fact]
    public void CustomFieldDef_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<CustomFieldDef>(h.Db, h.Audit);
        svc.Create(new CustomFieldDef { Name = "Demo CustomFieldDef" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo CustomFieldDef");
    }

    [Fact]
    public void CustomFieldDef_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<CustomFieldDef>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new CustomFieldDef { Name = "" }));
    }

    [Fact]
    public void NotificationRule_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<NotificationRule>(h.Db, h.Audit);
        svc.Create(new NotificationRule { Name = "Demo NotificationRule" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo NotificationRule");
    }

    [Fact]
    public void NotificationRule_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<NotificationRule>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new NotificationRule { Name = "" }));
    }

}
