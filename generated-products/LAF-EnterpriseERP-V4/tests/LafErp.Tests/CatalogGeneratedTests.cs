using LafErp.Core;
using LafErp.Services;
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

}
