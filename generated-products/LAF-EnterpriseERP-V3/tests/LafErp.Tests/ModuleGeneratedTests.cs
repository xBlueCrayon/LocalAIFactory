using LafErp.Core;
using LafErp.Services;
using Xunit;

namespace LafErp.Tests;

public class ModuleGeneratedTests
{
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
