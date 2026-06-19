using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

public class AgentTasksController : Controller
{
    private readonly AppDbContext _db;
    public AgentTasksController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await _db.AgentTasks.AsNoTracking().OrderByDescending(t => t.Id).Take(100).ToListAsync(ct));
}
