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
        rows.Add(new CatalogRow("CustomerSegment", _db.Set<CustomerSegment>().Count()));
        rows.Add(new CatalogRow("PaymentTerm", _db.Set<PaymentTerm>().Count()));
        rows.Add(new CatalogRow("TaxCode", _db.Set<TaxCode>().Count()));
        return View(rows);
    }
}

public record CatalogRow(string EntityType, int Count);