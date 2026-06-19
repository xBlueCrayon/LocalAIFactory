using System.Text;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Ingestion.Profiling;

// Produces a structured, review-pending project profile. Works with or without a model
// (model-assisted overview when available, heuristic skeleton otherwise).
public sealed class ProjectProfileService : IProjectProfileService
{
    private readonly AppDbContext _db;
    private readonly IModelExecutionService _model;

    public ProjectProfileService(AppDbContext db, IModelExecutionService model)
    {
        _db = db; _model = model;
    }

    public async Task GenerateAsync(int projectId, int? ingestionJobId, CancellationToken ct = default)
    {
        var existing = await _db.ProjectProfiles.Where(p => p.ProjectId == projectId).ToListAsync(ct);
        if (existing.Count > 0) { _db.ProjectProfiles.RemoveRange(existing); await _db.SaveChangesAsync(ct); }

        var files = await _db.ImportedFiles.AsNoTracking()
            .Where(f => f.ProjectId == projectId && !f.Skipped)
            .Select(f => new { f.RelativePath, f.FileClass, f.SizeBytes })
            .ToListAsync(ct);

        var items = await _db.KnowledgeItems.AsNoTracking()
            .Where(k => k.ProjectId == projectId)
            .OrderByDescending(k => k.Content.Length)
            .Select(k => new { k.Title, k.SourceType, k.Content })
            .Take(40)
            .ToListAsync(ct);

        var digest = BuildDigest(items.Select(i => $"FILE: {i.Title}\n{Head(i.Content, 1200)}"), 6000);
        string overview;
        try
        {
            var sys = "Summarize this software project for an engineer new to it. Cover purpose, main components, "
                    + "data/SQL, external integrations, and how it appears to be deployed. Be concise and factual.";
            overview = string.IsNullOrWhiteSpace(digest) ? "" : await _model.CompleteSimpleAsync(TaskType.ProjectSummarization, sys, digest, ct);
        }
        catch { overview = ""; }
        if (string.IsNullOrWhiteSpace(overview)) overview = HeuristicOverview(files.Count, files);

        var profile = new ProjectProfile
        {
            ProjectId = projectId,
            Status = KnowledgeStatus.NeedsReview,
            Summary = overview,
            GeneratedByModelConfigurationId = null
        };
        _db.ProjectProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);

        var sections = new List<ProjectProfileSection>
        {
            Section(profile.Id, "overview", "Overview", overview, 0),
            Section(profile.Id, "architecture", "Architecture", Architecture(files), 1),
            Section(profile.Id, "key_files", "Key Files", KeyFiles(files), 2),
            Section(profile.Id, "data_sql", "Data & SQL", DataSql(files), 3),
            Section(profile.Id, "integrations", "Integrations", await IntegrationsAsync(projectId, ct), 4),
            Section(profile.Id, "deployment", "Deployment", await DeploymentAsync(projectId, ct), 5),
            Section(profile.Id, "risks", "Risks & Unknowns", "Review imported content and approve the items that are correct. Flag anything that looks outdated or environment-specific.", 6)
        };
        _db.ProjectProfileSections.AddRange(sections);
        await _db.SaveChangesAsync(ct);
    }

    private static ProjectProfileSection Section(int profileId, string key, string title, string content, int order) => new()
    {
        ProjectProfileId = profileId, SectionKey = key, Title = title, Content = content,
        Status = KnowledgeStatus.NeedsReview, OrderIndex = order
    };

    private static string HeuristicOverview(int fileCount, IEnumerable<dynamic> files)
    {
        var byClass = files.GroupBy(f => (FileClass)f.FileClass).ToDictionary(g => g.Key, g => g.Count());
        var sb = new StringBuilder();
        sb.AppendLine($"Imported {fileCount} text files.");
        foreach (var kv in byClass.OrderByDescending(k => k.Value))
            sb.AppendLine($"- {kv.Key}: {kv.Value}");
        sb.AppendLine();
        sb.AppendLine("This is a heuristic profile (no model summary was available). Approve and refine as needed.");
        return sb.ToString();
    }

    private static string Architecture(IEnumerable<dynamic> files)
    {
        var rels = files.Select(f => (string)(f.RelativePath ?? "")).ToList();
        var sb = new StringBuilder();
        var projFiles = rels.Where(r => r.EndsWith(".csproj") || r.EndsWith(".sln")).Take(20).ToList();
        if (projFiles.Count > 0) { sb.AppendLine("Project/solution files:"); foreach (var p in projFiles) sb.AppendLine("- " + p); }
        bool mvc = rels.Any(r => r.Contains("Controllers")) && rels.Any(r => r.Contains("Views"));
        if (mvc) sb.AppendLine("Detected an MVC-style layout (Controllers + Views).");
        if (rels.Any(r => r.Contains("Migrations"))) sb.AppendLine("Contains EF Core-style Migrations.");
        if (sb.Length == 0) sb.AppendLine("No obvious project structure detected.");
        return sb.ToString();
    }

    private static string KeyFiles(IEnumerable<dynamic> files)
    {
        var top = files.Where(f => (FileClass)f.FileClass == FileClass.SourceCode)
            .OrderByDescending(f => (long)f.SizeBytes).Take(15).ToList();
        if (top.Count == 0) return "No source files detected.";
        var sb = new StringBuilder("Largest source files:");
        sb.AppendLine();
        foreach (var f in top) sb.AppendLine($"- {f.RelativePath} ({f.SizeBytes} bytes)");
        return sb.ToString();
    }

    private static string DataSql(IEnumerable<dynamic> files)
    {
        var sql = files.Where(f => (FileClass)f.FileClass == FileClass.SqlScript).Select(f => (string)f.RelativePath).Take(40).ToList();
        if (sql.Count == 0) return "No SQL scripts detected.";
        var sb = new StringBuilder("SQL scripts:");
        sb.AppendLine();
        foreach (var s in sql) sb.AppendLine("- " + s);
        return sb.ToString();
    }

    private async Task<string> IntegrationsAsync(int projectId, CancellationToken ct)
    {
        var markers = new[] { "SFTP", "WinSCP", "HostFlag", "MCIB", "BDM", "Parascript", "ChequeXpert", "IIS", "Metabase", "ETAMS", "Mandate" };
        var hits = new List<string>();
        var contents = await _db.KnowledgeItems.AsNoTracking()
            .Where(k => k.ProjectId == projectId)
            .Select(k => k.Content).Take(200).ToListAsync(ct);
        foreach (var m in markers)
            if (contents.Any(c => c.Contains(m, StringComparison.OrdinalIgnoreCase))) hits.Add(m);
        return hits.Count == 0 ? "No known integration markers detected." : "Detected integration markers: " + string.Join(", ", hits) + ".";
    }

    private async Task<string> DeploymentAsync(int projectId, CancellationToken ct)
    {
        var note = await _db.KnowledgeItems.AsNoTracking()
            .Where(k => k.ProjectId == projectId && (k.SourceType == SourceType.DeploymentNote || k.SourceType == SourceType.Readme))
            .Select(k => k.Content).FirstOrDefaultAsync(ct);
        return string.IsNullOrWhiteSpace(note) ? "No deployment notes detected." : Head(note, 1200);
    }

    private static string BuildDigest(IEnumerable<string> parts, int maxChars)
    {
        var sb = new StringBuilder();
        foreach (var p in parts) { if (sb.Length >= maxChars) break; sb.AppendLine(p); sb.AppendLine("----"); }
        var s = sb.ToString();
        return s.Length > maxChars ? s.Substring(0, maxChars) : s;
    }

    private static string Head(string s, int n) => string.IsNullOrEmpty(s) ? "" : (s.Length <= n ? s : s.Substring(0, n));
}
