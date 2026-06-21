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
    public void PaymentTerm_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<PaymentTerm>(h.Db, h.Audit);
        svc.Create(new PaymentTerm { Name = "Demo PaymentTerm" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo PaymentTerm");
    }

    [Fact]
    public void PaymentTerm_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<PaymentTerm>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new PaymentTerm { Name = "" }));
    }

    [Fact]
    public void TaxCode_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<TaxCode>(h.Db, h.Audit);
        svc.Create(new TaxCode { Name = "Demo TaxCode" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo TaxCode");
    }

    [Fact]
    public void TaxCode_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<TaxCode>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new TaxCode { Name = "" }));
    }

}
