using LafErp.Core;
using LafErp.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LafErp.Tests;

public class CatalogGeneratedTests
{
    [Fact]
    public void CustomerSegment_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<CustomerSegment>(h.Db, h.Audit);
        svc.Create(new CustomerSegment { Name = "Demo CustomerSegment" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo CustomerSegment");
    }

    [Fact]
    public void CustomerSegment_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<CustomerSegment>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new CustomerSegment { Name = "" }));
    }

    [Fact]
    public void CustomerSegment_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<CustomerSegment>(h.Db, h.Audit);
        var e = svc.Create(new CustomerSegment { Name = "Before CustomerSegment" });
        e.Name = "After CustomerSegment";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After CustomerSegment");
    }

    [Fact]
    public void CustomerSegment_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<CustomerSegment>(h.Db, h.Audit);
        var e = svc.Create(new CustomerSegment { Name = "Temp CustomerSegment" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<CustomerSegment>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
    }

    [Fact]
    public void ProductCategory_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<ProductCategory>(h.Db, h.Audit);
        svc.Create(new ProductCategory { Name = "Demo ProductCategory" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo ProductCategory");
    }

    [Fact]
    public void ProductCategory_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<ProductCategory>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new ProductCategory { Name = "" }));
    }

    [Fact]
    public void ProductCategory_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<ProductCategory>(h.Db, h.Audit);
        var e = svc.Create(new ProductCategory { Name = "Before ProductCategory" });
        e.Name = "After ProductCategory";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After ProductCategory");
    }

    [Fact]
    public void ProductCategory_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<ProductCategory>(h.Db, h.Audit);
        var e = svc.Create(new ProductCategory { Name = "Temp ProductCategory" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<ProductCategory>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
    }

    [Fact]
    public void EmployeeRole_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<EmployeeRole>(h.Db, h.Audit);
        svc.Create(new EmployeeRole { Name = "Demo EmployeeRole" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo EmployeeRole");
    }

    [Fact]
    public void EmployeeRole_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<EmployeeRole>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new EmployeeRole { Name = "" }));
    }

    [Fact]
    public void EmployeeRole_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<EmployeeRole>(h.Db, h.Audit);
        var e = svc.Create(new EmployeeRole { Name = "Before EmployeeRole" });
        e.Name = "After EmployeeRole";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After EmployeeRole");
    }

    [Fact]
    public void EmployeeRole_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<EmployeeRole>(h.Db, h.Audit);
        var e = svc.Create(new EmployeeRole { Name = "Temp EmployeeRole" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<EmployeeRole>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
    }

    [Fact]
    public void MarketingCampaign_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<MarketingCampaign>(h.Db, h.Audit);
        svc.Create(new MarketingCampaign { Name = "Demo MarketingCampaign" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo MarketingCampaign");
    }

    [Fact]
    public void MarketingCampaign_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<MarketingCampaign>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new MarketingCampaign { Name = "" }));
    }

    [Fact]
    public void MarketingCampaign_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<MarketingCampaign>(h.Db, h.Audit);
        var e = svc.Create(new MarketingCampaign { Name = "Before MarketingCampaign" });
        e.Name = "After MarketingCampaign";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After MarketingCampaign");
    }

    [Fact]
    public void MarketingCampaign_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<MarketingCampaign>(h.Db, h.Audit);
        var e = svc.Create(new MarketingCampaign { Name = "Temp MarketingCampaign" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<MarketingCampaign>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
    }

    [Fact]
    public void VendorContract_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<VendorContract>(h.Db, h.Audit);
        svc.Create(new VendorContract { Name = "Demo VendorContract" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo VendorContract");
    }

    [Fact]
    public void VendorContract_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<VendorContract>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new VendorContract { Name = "" }));
    }

    [Fact]
    public void VendorContract_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<VendorContract>(h.Db, h.Audit);
        var e = svc.Create(new VendorContract { Name = "Before VendorContract" });
        e.Name = "After VendorContract";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After VendorContract");
    }

    [Fact]
    public void VendorContract_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<VendorContract>(h.Db, h.Audit);
        var e = svc.Create(new VendorContract { Name = "Temp VendorContract" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<VendorContract>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
    }

}
