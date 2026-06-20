using LocalAIFactory.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocalAIFactory.Web.Controllers;

public class HomeController : Controller
{
    private readonly DashboardService _dashboard;

    public HomeController(DashboardService dashboard) => _dashboard = dashboard;

    // Dashboard. All metrics come from simple CountAsync() queries; health comes from a cached snapshot.
    // No GroupBy, no external service calls — see DashboardService for the structured-logging build steps.
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = await _dashboard.BuildAsync(ct);
        return View(vm);
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
