using LafErp.Core;
using LafErp.Data;
using LafErp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Web.Controllers;

public class HomeController : Controller
{
    private readonly ErpDbContext _db;
    private readonly AccountingService _acc;
    private readonly StockService _stock;

    public HomeController(ErpDbContext db, AccountingService acc, StockService stock)
    {
        _db = db; _acc = acc; _stock = stock;
    }

    private int CompanyId => _db.Companies.Select(c => c.Id).FirstOrDefault();

    public IActionResult Index()
    {
        var vm = new DashboardVm(
            Customers: _db.Customers.Count(),
            Suppliers: _db.Suppliers.Count(),
            Items: _db.Items.Count(),
            SalesInvoices: _db.SalesInvoices.Count(),
            PurchaseInvoices: _db.PurchaseInvoices.Count(),
            PendingApprovals: _db.WorkflowInstances.Count(w => w.CurrentState == "PendingApproval"),
            Receivable: _acc.AccountsReceivable(CompanyId),
            Payable: _acc.AccountsPayable(CompanyId),
            AuditEvents: _db.AuditEvents.Count());
        return View(vm);
    }

    public IActionResult Customers() =>
        View(_db.Customers.OrderBy(c => c.Code).ToList());

    public IActionResult Items() =>
        View(_db.Items.OrderBy(i => i.Code).ToList());

    public IActionResult SalesInvoices() =>
        View(_db.SalesInvoices.OrderByDescending(x => x.Id).ToList());

    public IActionResult GeneralLedger() =>
        View(_acc.GeneralLedger(CompanyId, DateTime.UtcNow.Date.AddYears(-1), DateTime.UtcNow.Date.AddDays(1)));

    public IActionResult StockBalance()
    {
        var combos = _db.StockLedgerEntries.Select(s => new { s.ItemId, s.WarehouseId }).Distinct().ToList();
        return View(combos.Select(c => _stock.Balance(c.ItemId, c.WarehouseId)).ToList());
    }

    public IActionResult AuditLog() =>
        View(_db.AuditEvents.OrderByDescending(x => x.Id).Take(200).ToList());

    public IActionResult WorkflowInbox() =>
        View(_db.WorkflowInstances.OrderByDescending(x => x.Id).Take(100).ToList());

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public IActionResult Login(string username, string roles)
    {
        Response.Cookies.Append("erp_user", username ?? "admin");
        Response.Cookies.Append("erp_roles", roles ?? "System Manager");
        return RedirectToAction(nameof(Index));
    }
}

public record DashboardVm(int Customers, int Suppliers, int Items, int SalesInvoices, int PurchaseInvoices,
    int PendingApprovals, decimal Receivable, decimal Payable, int AuditEvents);
