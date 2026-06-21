using LafErp.Core;
using LafErp.Services;
using Microsoft.EntityFrameworkCore;
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
    public void Quotation_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<Quotation>(h.Db, h.Audit);
        var e = svc.Create(new Quotation { Name = "Before Quotation" });
        e.Name = "After Quotation";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After Quotation");
    }

    [Fact]
    public void Quotation_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<Quotation>(h.Db, h.Audit);
        var e = svc.Create(new Quotation { Name = "Temp Quotation" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<Quotation>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
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
    public void DeliveryNote_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<DeliveryNote>(h.Db, h.Audit);
        var e = svc.Create(new DeliveryNote { Name = "Before DeliveryNote" });
        e.Name = "After DeliveryNote";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After DeliveryNote");
    }

    [Fact]
    public void DeliveryNote_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<DeliveryNote>(h.Db, h.Audit);
        var e = svc.Create(new DeliveryNote { Name = "Temp DeliveryNote" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<DeliveryNote>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
    }

    [Fact]
    public void CreditNote_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<CreditNote>(h.Db, h.Audit);
        svc.Create(new CreditNote { Name = "Demo CreditNote" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo CreditNote");
    }

    [Fact]
    public void CreditNote_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<CreditNote>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new CreditNote { Name = "" }));
    }

    [Fact]
    public void CreditNote_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<CreditNote>(h.Db, h.Audit);
        var e = svc.Create(new CreditNote { Name = "Before CreditNote" });
        e.Name = "After CreditNote";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After CreditNote");
    }

    [Fact]
    public void CreditNote_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<CreditNote>(h.Db, h.Audit);
        var e = svc.Create(new CreditNote { Name = "Temp CreditNote" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<CreditNote>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
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
    public void PurchaseReceipt_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<PurchaseReceipt>(h.Db, h.Audit);
        var e = svc.Create(new PurchaseReceipt { Name = "Before PurchaseReceipt" });
        e.Name = "After PurchaseReceipt";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After PurchaseReceipt");
    }

    [Fact]
    public void PurchaseReceipt_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<PurchaseReceipt>(h.Db, h.Audit);
        var e = svc.Create(new PurchaseReceipt { Name = "Temp PurchaseReceipt" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<PurchaseReceipt>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
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
    public void MaterialRequest_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<MaterialRequest>(h.Db, h.Audit);
        var e = svc.Create(new MaterialRequest { Name = "Before MaterialRequest" });
        e.Name = "After MaterialRequest";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After MaterialRequest");
    }

    [Fact]
    public void MaterialRequest_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<MaterialRequest>(h.Db, h.Audit);
        var e = svc.Create(new MaterialRequest { Name = "Temp MaterialRequest" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<MaterialRequest>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
    }

    [Fact]
    public void DebitNote_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<DebitNote>(h.Db, h.Audit);
        svc.Create(new DebitNote { Name = "Demo DebitNote" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo DebitNote");
    }

    [Fact]
    public void DebitNote_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<DebitNote>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new DebitNote { Name = "" }));
    }

    [Fact]
    public void DebitNote_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<DebitNote>(h.Db, h.Audit);
        var e = svc.Create(new DebitNote { Name = "Before DebitNote" });
        e.Name = "After DebitNote";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After DebitNote");
    }

    [Fact]
    public void DebitNote_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<DebitNote>(h.Db, h.Audit);
        var e = svc.Create(new DebitNote { Name = "Temp DebitNote" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<DebitNote>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
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
    public void StockTransfer_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<StockTransfer>(h.Db, h.Audit);
        var e = svc.Create(new StockTransfer { Name = "Before StockTransfer" });
        e.Name = "After StockTransfer";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After StockTransfer");
    }

    [Fact]
    public void StockTransfer_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<StockTransfer>(h.Db, h.Audit);
        var e = svc.Create(new StockTransfer { Name = "Temp StockTransfer" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<StockTransfer>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
    }

    [Fact]
    public void StockReconciliation_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<StockReconciliation>(h.Db, h.Audit);
        svc.Create(new StockReconciliation { Name = "Demo StockReconciliation" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo StockReconciliation");
    }

    [Fact]
    public void StockReconciliation_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<StockReconciliation>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new StockReconciliation { Name = "" }));
    }

    [Fact]
    public void StockReconciliation_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<StockReconciliation>(h.Db, h.Audit);
        var e = svc.Create(new StockReconciliation { Name = "Before StockReconciliation" });
        e.Name = "After StockReconciliation";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After StockReconciliation");
    }

    [Fact]
    public void StockReconciliation_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<StockReconciliation>(h.Db, h.Audit);
        var e = svc.Create(new StockReconciliation { Name = "Temp StockReconciliation" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<StockReconciliation>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
    }

    [Fact]
    public void PriceList_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<PriceList>(h.Db, h.Audit);
        svc.Create(new PriceList { Name = "Demo PriceList" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo PriceList");
    }

    [Fact]
    public void PriceList_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<PriceList>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new PriceList { Name = "" }));
    }

    [Fact]
    public void PriceList_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<PriceList>(h.Db, h.Audit);
        var e = svc.Create(new PriceList { Name = "Before PriceList" });
        e.Name = "After PriceList";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After PriceList");
    }

    [Fact]
    public void PriceList_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<PriceList>(h.Db, h.Audit);
        var e = svc.Create(new PriceList { Name = "Temp PriceList" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<PriceList>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
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
    public void BillOfMaterials_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<BillOfMaterials>(h.Db, h.Audit);
        var e = svc.Create(new BillOfMaterials { Name = "Before BillOfMaterials" });
        e.Name = "After BillOfMaterials";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After BillOfMaterials");
    }

    [Fact]
    public void BillOfMaterials_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<BillOfMaterials>(h.Db, h.Audit);
        var e = svc.Create(new BillOfMaterials { Name = "Temp BillOfMaterials" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<BillOfMaterials>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
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
    public void WorkOrder_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<WorkOrder>(h.Db, h.Audit);
        var e = svc.Create(new WorkOrder { Name = "Before WorkOrder" });
        e.Name = "After WorkOrder";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After WorkOrder");
    }

    [Fact]
    public void WorkOrder_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<WorkOrder>(h.Db, h.Audit);
        var e = svc.Create(new WorkOrder { Name = "Temp WorkOrder" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<WorkOrder>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
    }

    [Fact]
    public void JobCard_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<JobCard>(h.Db, h.Audit);
        svc.Create(new JobCard { Name = "Demo JobCard" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo JobCard");
    }

    [Fact]
    public void JobCard_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<JobCard>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new JobCard { Name = "" }));
    }

    [Fact]
    public void JobCard_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<JobCard>(h.Db, h.Audit);
        var e = svc.Create(new JobCard { Name = "Before JobCard" });
        e.Name = "After JobCard";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After JobCard");
    }

    [Fact]
    public void JobCard_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<JobCard>(h.Db, h.Audit);
        var e = svc.Create(new JobCard { Name = "Temp JobCard" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<JobCard>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
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
    public void QualityInspection_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<QualityInspection>(h.Db, h.Audit);
        var e = svc.Create(new QualityInspection { Name = "Before QualityInspection" });
        e.Name = "After QualityInspection";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After QualityInspection");
    }

    [Fact]
    public void QualityInspection_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<QualityInspection>(h.Db, h.Audit);
        var e = svc.Create(new QualityInspection { Name = "Temp QualityInspection" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<QualityInspection>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
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
    public void Employee_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<Employee>(h.Db, h.Audit);
        var e = svc.Create(new Employee { Name = "Before Employee" });
        e.Name = "After Employee";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After Employee");
    }

    [Fact]
    public void Employee_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<Employee>(h.Db, h.Audit);
        var e = svc.Create(new Employee { Name = "Temp Employee" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<Employee>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
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
    public void AttendanceRecord_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<AttendanceRecord>(h.Db, h.Audit);
        var e = svc.Create(new AttendanceRecord { Name = "Before AttendanceRecord" });
        e.Name = "After AttendanceRecord";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After AttendanceRecord");
    }

    [Fact]
    public void AttendanceRecord_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<AttendanceRecord>(h.Db, h.Audit);
        var e = svc.Create(new AttendanceRecord { Name = "Temp AttendanceRecord" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<AttendanceRecord>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
    }

    [Fact]
    public void LeaveApplication_create_and_list()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<LeaveApplication>(h.Db, h.Audit);
        svc.Create(new LeaveApplication { Name = "Demo LeaveApplication" });
        Assert.Equal(1, svc.Count());
        Assert.Contains(svc.List(), x => x.Name == "Demo LeaveApplication");
    }

    [Fact]
    public void LeaveApplication_requires_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<LeaveApplication>(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.Create(new LeaveApplication { Name = "" }));
    }

    [Fact]
    public void LeaveApplication_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<LeaveApplication>(h.Db, h.Audit);
        var e = svc.Create(new LeaveApplication { Name = "Before LeaveApplication" });
        e.Name = "After LeaveApplication";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After LeaveApplication");
    }

    [Fact]
    public void LeaveApplication_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<LeaveApplication>(h.Db, h.Audit);
        var e = svc.Create(new LeaveApplication { Name = "Temp LeaveApplication" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<LeaveApplication>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
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
    public void SalaryComponent_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<SalaryComponent>(h.Db, h.Audit);
        var e = svc.Create(new SalaryComponent { Name = "Before SalaryComponent" });
        e.Name = "After SalaryComponent";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After SalaryComponent");
    }

    [Fact]
    public void SalaryComponent_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<SalaryComponent>(h.Db, h.Audit);
        var e = svc.Create(new SalaryComponent { Name = "Temp SalaryComponent" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<SalaryComponent>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
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
    public void Timesheet_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<Timesheet>(h.Db, h.Audit);
        var e = svc.Create(new Timesheet { Name = "Before Timesheet" });
        e.Name = "After Timesheet";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After Timesheet");
    }

    [Fact]
    public void Timesheet_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<Timesheet>(h.Db, h.Audit);
        var e = svc.Create(new Timesheet { Name = "Temp Timesheet" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<Timesheet>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
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
    public void PosProfile_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<PosProfile>(h.Db, h.Audit);
        var e = svc.Create(new PosProfile { Name = "Before PosProfile" });
        e.Name = "After PosProfile";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After PosProfile");
    }

    [Fact]
    public void PosProfile_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<PosProfile>(h.Db, h.Audit);
        var e = svc.Create(new PosProfile { Name = "Temp PosProfile" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<PosProfile>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
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
    public void WebProduct_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<WebProduct>(h.Db, h.Audit);
        var e = svc.Create(new WebProduct { Name = "Before WebProduct" });
        e.Name = "After WebProduct";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After WebProduct");
    }

    [Fact]
    public void WebProduct_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<WebProduct>(h.Db, h.Audit);
        var e = svc.Create(new WebProduct { Name = "Temp WebProduct" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<WebProduct>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
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
    public void MaintenanceSchedule_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<MaintenanceSchedule>(h.Db, h.Audit);
        var e = svc.Create(new MaintenanceSchedule { Name = "Before MaintenanceSchedule" });
        e.Name = "After MaintenanceSchedule";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After MaintenanceSchedule");
    }

    [Fact]
    public void MaintenanceSchedule_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<MaintenanceSchedule>(h.Db, h.Audit);
        var e = svc.Create(new MaintenanceSchedule { Name = "Temp MaintenanceSchedule" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<MaintenanceSchedule>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
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
    public void CustomFieldDef_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<CustomFieldDef>(h.Db, h.Audit);
        var e = svc.Create(new CustomFieldDef { Name = "Before CustomFieldDef" });
        e.Name = "After CustomFieldDef";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After CustomFieldDef");
    }

    [Fact]
    public void CustomFieldDef_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<CustomFieldDef>(h.Db, h.Audit);
        var e = svc.Create(new CustomFieldDef { Name = "Temp CustomFieldDef" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<CustomFieldDef>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
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

    [Fact]
    public void NotificationRule_edit_updates_name()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<NotificationRule>(h.Db, h.Audit);
        var e = svc.Create(new NotificationRule { Name = "Before NotificationRule" });
        e.Name = "After NotificationRule";
        svc.Update(e);
        Assert.Contains(svc.List(), x => x.Name == "After NotificationRule");
    }

    [Fact]
    public void NotificationRule_deactivate_soft_deletes()
    {
        using var h = new TestHost();
        var svc = new CatalogCrudService<NotificationRule>(h.Db, h.Audit);
        var e = svc.Create(new NotificationRule { Name = "Temp NotificationRule" });
        svc.Deactivate(e.Id);
        Assert.DoesNotContain(svc.List(), x => x.Id == e.Id);
        Assert.True(h.Db.Set<NotificationRule>().IgnoreQueryFilters().First(x => x.Id == e.Id).IsDeleted);
    }

}
