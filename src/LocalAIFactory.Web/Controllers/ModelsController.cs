using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

public class ModelsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IApiKeyProtector _protector;
    private readonly IModelRouter _router;

    public ModelsController(AppDbContext db, IApiKeyProtector protector, IModelRouter router)
    {
        _db = db; _protector = protector; _router = router;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await _db.ModelConfigurations.OrderByDescending(m => m.IsDefault).ThenBy(m => m.Name).ToListAsync(ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, ModelProvider provider, string modelName, string baseUrl,
        string? apiKey, string? embeddingModel, double temperature, int maxTokens, int contextWindowHint, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(modelName) || string.IsNullOrWhiteSpace(baseUrl))
        {
            TempData["Error"] = "Name, model name and base URL are required.";
            return RedirectToAction(nameof(Index));
        }
        _db.ModelConfigurations.Add(new ModelConfiguration
        {
            Name = name, Provider = provider, ModelName = modelName, BaseUrl = baseUrl,
            ApiKeyEncrypted = string.IsNullOrWhiteSpace(apiKey) ? null : _protector.Protect(apiKey),
            EmbeddingModel = embeddingModel,
            Temperature = temperature, MaxTokens = maxTokens <= 0 ? 2048 : maxTokens,
            ContextWindowHint = contextWindowHint <= 0 ? 8192 : contextWindowHint,
            IsEnabled = true
        });
        await _db.SaveChangesAsync(ct);
        TempData["Message"] = "Model added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string name, ModelProvider provider, string modelName, string baseUrl,
        string? apiKey, string? embeddingModel, double temperature, int maxTokens, int contextWindowHint, bool isEnabled, CancellationToken ct)
    {
        var m = await _db.ModelConfigurations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) { TempData["Error"] = "Model not found."; return RedirectToAction(nameof(Index)); }
        m.Name = name; m.Provider = provider; m.ModelName = modelName; m.BaseUrl = baseUrl;
        m.EmbeddingModel = embeddingModel; m.Temperature = temperature;
        m.MaxTokens = maxTokens <= 0 ? 2048 : maxTokens; m.ContextWindowHint = contextWindowHint <= 0 ? 8192 : contextWindowHint;
        m.IsEnabled = isEnabled; m.UpdatedUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(apiKey)) m.ApiKeyEncrypted = _protector.Protect(apiKey);
        await _db.SaveChangesAsync(ct);
        TempData["Message"] = "Model updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TestConnection(int id, CancellationToken ct)
    {
        var m = await _db.ModelConfigurations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) { TempData["Error"] = "Model not found."; return RedirectToAction(nameof(Index)); }
        try
        {
            var clone = new ModelConfiguration
            {
                Provider = m.Provider, ModelName = m.ModelName, BaseUrl = m.BaseUrl,
                ApiKeyEncrypted = _protector.Unprotect(m.ApiKeyEncrypted)
            };
            var provider = _router.Resolve(m.Provider);
            var result = await provider.TestAsync(clone, ct);
            if (result.Success) TempData["Message"] = $"{m.Name}: {result.Message}";
            else TempData["Error"] = $"{m.Name}: {result.Message}";
        }
        catch (Exception ex) { TempData["Error"] = $"{m.Name}: {ex.Message}"; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefault(int id, CancellationToken ct)
    {
        var all = await _db.ModelConfigurations.ToListAsync(ct);
        foreach (var m in all) m.IsDefault = m.Id == id;
        await _db.SaveChangesAsync(ct);
        TempData["Message"] = "Default model updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var m = await _db.ModelConfigurations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (m is null) return RedirectToAction(nameof(Index));
        try
        {
            _db.ModelConfigurations.Remove(m);
            await _db.SaveChangesAsync(ct);
            TempData["Message"] = "Model deleted.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Could not delete (it may be referenced by a task profile or chat). " + ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
