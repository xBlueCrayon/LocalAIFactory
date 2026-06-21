using LafErp.Core;
using LafErp.Data;
using LafErp.Services;
using Microsoft.AspNetCore.Mvc;

namespace LafErp.Web.Controllers;

// Generated controller: a catalog overview + a generic reflection-driven CREATE form for every generated
// module (the create/edit UI capability V4 lacked). Records are persisted via the DbContext with an audit event.
public class CatalogController : Controller
{
    private static readonly HashSet<string> Skip = new() { "Id", "CreatedUtc", "UpdatedUtc", "CreatedBy", "UpdatedBy", "IsDeleted", "RowVersion" };
    private readonly ErpDbContext _db;
    private readonly AuditService _audit;
    public CatalogController(ErpDbContext db, AuditService audit) { _db = db; _audit = audit; }

    public IActionResult Index()
    {
        var rows = new List<CatalogRow>();
        rows.Add(new CatalogRow("Quotation", _db.Set<Quotation>().Count()));
        rows.Add(new CatalogRow("DeliveryNote", _db.Set<DeliveryNote>().Count()));
        rows.Add(new CatalogRow("CreditNote", _db.Set<CreditNote>().Count()));
        rows.Add(new CatalogRow("PurchaseReceipt", _db.Set<PurchaseReceipt>().Count()));
        rows.Add(new CatalogRow("MaterialRequest", _db.Set<MaterialRequest>().Count()));
        rows.Add(new CatalogRow("DebitNote", _db.Set<DebitNote>().Count()));
        rows.Add(new CatalogRow("StockTransfer", _db.Set<StockTransfer>().Count()));
        rows.Add(new CatalogRow("StockReconciliation", _db.Set<StockReconciliation>().Count()));
        rows.Add(new CatalogRow("PriceList", _db.Set<PriceList>().Count()));
        rows.Add(new CatalogRow("BillOfMaterials", _db.Set<BillOfMaterials>().Count()));
        rows.Add(new CatalogRow("WorkOrder", _db.Set<WorkOrder>().Count()));
        rows.Add(new CatalogRow("JobCard", _db.Set<JobCard>().Count()));
        rows.Add(new CatalogRow("QualityInspection", _db.Set<QualityInspection>().Count()));
        rows.Add(new CatalogRow("Employee", _db.Set<Employee>().Count()));
        rows.Add(new CatalogRow("AttendanceRecord", _db.Set<AttendanceRecord>().Count()));
        rows.Add(new CatalogRow("LeaveApplication", _db.Set<LeaveApplication>().Count()));
        rows.Add(new CatalogRow("SalaryComponent", _db.Set<SalaryComponent>().Count()));
        rows.Add(new CatalogRow("Timesheet", _db.Set<Timesheet>().Count()));
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

    [HttpGet]
    public IActionResult Create(string entity)
    {
        var t = Resolve(entity);
        if (t == null) return NotFound();
        ViewBag.Entity = entity;
        return View("Create", Fields(t));
    }

    [HttpPost]
    public IActionResult Create(string entity, IFormCollection form)
    {
        var t = Resolve(entity);
        if (t == null) return NotFound();
        var obj = Activator.CreateInstance(t)!;
        foreach (var p in t.GetProperties().Where(Editable))
        {
            if (!form.TryGetValue(p.Name, out var v) || string.IsNullOrWhiteSpace(v)) continue;
            try { var pt = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType; p.SetValue(obj, Convert.ChangeType(v.ToString(), pt)); } catch { }
        }
        var nameProp = t.GetProperty("Name");
        if (nameProp != null && string.IsNullOrWhiteSpace(nameProp.GetValue(obj) as string))
        { ViewBag.Entity = entity; ViewBag.Error = "Name is required."; return View("Create", Fields(t)); }
        _db.Add(obj);
        _audit.Record(entity, 0, "Create", "via UI form");
        _db.SaveChanges();
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult List(string entity)
    {
        var t = Resolve(entity);
        if (t == null) return NotFound();
        ViewBag.Entity = entity;
        var set = (System.Collections.IEnumerable)_db.GetType().GetMethod("Set", Type.EmptyTypes)!
            .MakeGenericMethod(t).Invoke(_db, null)!;
        var rows = set.Cast<EntityBase>().Where(x => !x.IsDeleted).OrderByDescending(x => x.Id).Take(500).ToList();
        return View("List", rows);
    }

    [HttpGet]
    public IActionResult Edit(string entity, int id)
    {
        var t = Resolve(entity);
        if (t == null) return NotFound();
        var row = _db.Find(t, id) as EntityBase;
        if (row == null) return NotFound();
        ViewBag.Entity = entity; ViewBag.Id = id;
        return View("Edit", Fields(t).Select(f =>
            new CatalogFieldValue(f.Name, f.Type, t.GetProperty(f.Name)!.GetValue(row)?.ToString() ?? "")).ToList());
    }

    [HttpPost]
    public IActionResult Edit(string entity, int id, IFormCollection form)
    {
        var t = Resolve(entity);
        if (t == null) return NotFound();
        var row = _db.Find(t, id) as EntityBase;
        if (row == null) return NotFound();
        foreach (var p in t.GetProperties().Where(Editable))
        {
            if (!form.TryGetValue(p.Name, out var v)) continue;
            try
            {
                var underlying = Nullable.GetUnderlyingType(p.PropertyType);
                if (string.IsNullOrWhiteSpace(v))
                {
                    if (p.PropertyType == typeof(string)) p.SetValue(row, "");
                    else if (underlying != null) p.SetValue(row, null); // nullable value type -> null
                    // non-nullable value type with no input: leave the existing value unchanged
                }
                else
                {
                    p.SetValue(row, Convert.ChangeType(v.ToString(), underlying ?? p.PropertyType));
                }
            }
            catch { }
        }
        var nameProp = t.GetProperty("Name");
        if (nameProp != null && string.IsNullOrWhiteSpace(nameProp.GetValue(row) as string))
        {
            ViewBag.Entity = entity; ViewBag.Id = id; ViewBag.Error = "Name is required.";
            return View("Edit", Fields(t).Select(f => new CatalogFieldValue(f.Name, f.Type, form[f.Name].ToString())).ToList());
        }
        _audit.Record(entity, id, "Update", "via UI form");
        _db.SaveChanges();
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Deactivate(string entity, int id)
    {
        var t = Resolve(entity);
        if (t == null) return NotFound();
        if (_db.Find(t, id) is EntityBase row)
        {
            row.IsDeleted = true;
            _audit.Record(entity, id, "Deactivate", "via UI");
            _db.SaveChanges();
        }
        return RedirectToAction("Index");
    }

    private static Type? Resolve(string entity) => typeof(EntityBase).Assembly.GetType("LafErp.Core." + entity);
    private static bool Editable(System.Reflection.PropertyInfo p) =>
        p.CanWrite && !Skip.Contains(p.Name) &&
        (p.PropertyType == typeof(string) || p.PropertyType == typeof(int) || p.PropertyType == typeof(int?) ||
         p.PropertyType == typeof(decimal) || p.PropertyType == typeof(decimal?) || p.PropertyType == typeof(bool) ||
         p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?));
    private static List<CatalogField> Fields(Type t) =>
        t.GetProperties().Where(Editable).Select(p => new CatalogField(p.Name, p.PropertyType.Name)).ToList();
}

public record CatalogRow(string EntityType, int Count);
public record CatalogField(string Name, string Type);
public record CatalogFieldValue(string Name, string Type, string Value);