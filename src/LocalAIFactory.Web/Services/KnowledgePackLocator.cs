namespace LocalAIFactory.Web.Services;

// R2-ACC-B1: resolves the on-disk Professional Base Knowledge Pack directory across dev and deployed layouts.
// In a published app the pack is copied next to the binaries (AppContext.BaseDirectory); under `dotnet run`
// the content root is the project dir, so we also walk up to the repo root that holds knowledge-packs/.
public static class KnowledgePackLocator
{
    public const string BaseV1 = "professional-base-v1";

    public static string? FindBaseV1(Microsoft.Extensions.Configuration.IConfiguration config, string contentRoot)
    {
        var candidates = new List<string>();
        var configured = config["KnowledgePacks:BaseV1Path"];
        if (!string.IsNullOrWhiteSpace(configured)) candidates.Add(configured);
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "knowledge-packs", BaseV1)); // published output
        candidates.Add(Path.Combine(contentRoot, "knowledge-packs", BaseV1));

        var dir = new DirectoryInfo(contentRoot);
        for (int i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
            candidates.Add(Path.Combine(dir.FullName, "knowledge-packs", BaseV1));

        foreach (var c in candidates)
            if (File.Exists(Path.Combine(c, "manifest.json"))) return c;
        return null;
    }

    // R2-ACC-FINAL: locate ALL shipped knowledge-pack directories (each containing a manifest.json) so the
    // included knowledge base — not just the base pack — is seeded on startup. Resolves the knowledge-packs root
    // across dev (repo root) and published (next to binaries) layouts; returns base pack first, then the rest.
    public static IReadOnlyList<string> FindAllPacks(Microsoft.Extensions.Configuration.IConfiguration config, string contentRoot)
    {
        var roots = new List<string>();
        var configuredRoot = config["KnowledgePacks:RootPath"];
        if (!string.IsNullOrWhiteSpace(configuredRoot)) roots.Add(configuredRoot);
        roots.Add(Path.Combine(AppContext.BaseDirectory, "knowledge-packs"));
        roots.Add(Path.Combine(contentRoot, "knowledge-packs"));
        var dir = new DirectoryInfo(contentRoot);
        for (int i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
            roots.Add(Path.Combine(dir.FullName, "knowledge-packs"));

        var packsRoot = roots.FirstOrDefault(Directory.Exists);
        if (packsRoot is null) return Array.Empty<string>();

        var packs = Directory.EnumerateDirectories(packsRoot)
            .Where(d => File.Exists(Path.Combine(d, "manifest.json")))
            // base pack first (deterministic order; the rest alphabetical).
            .OrderByDescending(d => Path.GetFileName(d) == BaseV1)
            .ThenBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase)
            .ToList();
        return packs;
    }
}
