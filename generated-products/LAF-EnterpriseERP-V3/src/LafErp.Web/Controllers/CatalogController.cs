using LafErp.Core;
using LafErp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Web.Controllers;

public class CatalogController : Controller
{
    private readonly ErpDbContext _db;
    public CatalogController(ErpDbContext db) => _db = db;

    public IActionResult Index()
    {
        var rows = new List<CatalogRow>();
        rows.Add(new CatalogRow("BillOfMaterials", _db.Set<BillOfMaterials>().Count()));
        rows.Add(new CatalogRow("WorkOrder", _db.Set<WorkOrder>().Count()));
        rows.Add(new CatalogRow("QualityInspection", _db.Set<QualityInspection>().Count()));
        rows.Add(new CatalogRow("Employee", _db.Set<Employee>().Count()));
        rows.Add(new CatalogRow("SalaryComponent", _db.Set<SalaryComponent>().Count()));
        rows.Add(new CatalogRow("PosProfile", _db.Set<PosProfile>().Count()));
        rows.Add(new CatalogRow("WebProduct", _db.Set<WebProduct>().Count()));
        rows.Add(new CatalogRow("MaintenanceSchedule", _db.Set<MaintenanceSchedule>().Count()));
        rows.Add(new CatalogRow("CustomFieldDef", _db.Set<CustomFieldDef>().Count()));
        rows.Add(new CatalogRow("NotificationRule", _db.Set<NotificationRule>().Count()));
        rows.Add(new CatalogRow("CustomerSegment", _db.Set<CustomerSegment>().Count()));
        rows.Add(new CatalogRow("ProductCategory", _db.Set<ProductCategory>().Count()));
        rows.Add(new CatalogRow("EmployeeRole", _db.Set<EmployeeRole>().Count()));
        rows.Add(new CatalogRow("MarketingCampaign", _db.Set<MarketingCampaign>().Count()));
        rows.Add(new CatalogRow("VendorContract", _db.Set<VendorContract>().Count()));
        return View(rows);
    }
}

public record CatalogRow(string EntityType, int Count);