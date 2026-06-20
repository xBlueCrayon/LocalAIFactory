using System.Text.RegularExpressions;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Ingestion.Graph;

// Lightweight MSSQL-backed knowledge graph. Heuristic extraction (always) plus optional model-assisted
// triples. Everything is created as NeedsReview so it must be approved before it influences retrieval.
public sealed class KnowledgeGraphService : IKnowledgeGraphService
{
    private readonly AppDbContext _db;
    private readonly IModelExecutionService _model;

    public KnowledgeGraphService(AppDbContext db, IModelExecutionService model)
    {
        _db = db; _model = model;
    }

    private static readonly Regex SqlTable = new(
        @"\b(?:FROM|JOIN|UPDATE|INTO|TABLE)\s+\[?(?<t>[A-Za-z_][A-Za-z0-9_\.]*)\]?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex CsClass = new(
        @"\b(?:public|internal|sealed|abstract|partial)\s+class\s+(?<c>[A-Za-z_][A-Za-z0-9_]*)",
        RegexOptions.Compiled);

    private static readonly (string marker, EntityType type)[] DomainMarkers =
    {
        ("HostFlag", EntityType.Flag), ("SFTP", EntityType.ExternalSystem), ("WinSCP", EntityType.ExternalSystem),
        ("Mandate", EntityType.Workflow), ("MCIB", EntityType.ExternalSystem), ("BDM", EntityType.Module),
        ("Parascript", EntityType.ExternalSystem), ("ChequeXpert", EntityType.Module), ("Metabase", EntityType.ExternalSystem),
        ("IIS", EntityType.ExternalSystem), ("ETAMS", EntityType.Module)
    };

    public async Task ExtractAsync(int? projectId, int knowledgeItemId, string title, string content, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        var cache = new Dictionary<string, KnowledgeEntity>(StringComparer.OrdinalIgnoreCase);

        var fileEntity = await GetOrCreateEntityAsync(projectId, ShortName(title), EntityType.File, knowledgeItemId, cache, ct);
        int budget = 15;

        // SQL tables
        foreach (Match m in SqlTable.Matches(content))
        {
            if (budget-- <= 0) break;
            var table = m.Groups["t"].Value.Trim();
            if (table.Length is < 2 or > 80) continue;
            var ent = await GetOrCreateEntityAsync(projectId, table, EntityType.Table, knowledgeItemId, cache, ct);
            await AddRelationshipAsync(projectId, fileEntity.Id, ent.Id, RelationType.Reads, "References table in SQL.", knowledgeItemId, ct);
        }

        // C# classes
        foreach (Match m in CsClass.Matches(content))
        {
            if (budget-- <= 0) break;
            var cls = m.Groups["c"].Value.Trim();
            var ent = await GetOrCreateEntityAsync(projectId, cls, EntityType.Module, knowledgeItemId, cache, ct);
            await AddRelationshipAsync(projectId, fileEntity.Id, ent.Id, RelationType.DependsOn, "Declares type.", knowledgeItemId, ct);
        }

        // Domain markers
        foreach (var (marker, type) in DomainMarkers)
        {
            if (budget-- <= 0) break;
            if (content.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                var ent = await GetOrCreateEntityAsync(projectId, marker, type, knowledgeItemId, cache, ct);
                await AddRelationshipAsync(projectId, fileEntity.Id, ent.Id, RelationType.IntegratesWith, "Mentions domain concept.", knowledgeItemId, ct);
            }
        }

        await _db.SaveChangesAsync(ct);

        // Optional model-assisted triples (single bounded call) when a model is available.
        if (content.Length <= 8000)
        {
            var sys = "From the text, list up to 8 relationships as 'EntityA | relation | EntityB', one per line. "
                    + "Use short relation verbs (uses, calls, reads, writes, depends_on, integrates_with). If none, return nothing.";
            string answer;
            try { answer = await _model.CompleteSimpleAsync(TaskType.ArchitectureAnalysis, sys, Head(content, 6000), ct); }
            catch { answer = ""; }

            if (!string.IsNullOrWhiteSpace(answer))
            {
                int added = 0;
                foreach (var raw in answer.Split('\n'))
                {
                    if (added >= 8) break;
                    var parts = raw.Split('|');
                    if (parts.Length != 3) continue;
                    var from = parts[0].Trim(); var rel = parts[1].Trim(); var to = parts[2].Trim();
                    if (from.Length is < 2 or > 80 || to.Length is < 2 or > 80) continue;
                    var fe = await GetOrCreateEntityAsync(projectId, from, EntityType.Other, knowledgeItemId, cache, ct);
                    var te = await GetOrCreateEntityAsync(projectId, to, EntityType.Other, knowledgeItemId, cache, ct);
                    await AddRelationshipAsync(projectId, fe.Id, te.Id, MapRelation(rel), "Model-suggested relationship.", knowledgeItemId, ct);
                    added++;
                }
                if (added > 0) await _db.SaveChangesAsync(ct);
            }
        }
    }

    public async Task<List<RagContextItem>> NeighborsAsync(int? projectId, string query, int max, CancellationToken ct = default)
    {
        var q = (query ?? "").ToLowerInvariant();
        var entities = await _db.KnowledgeEntities.AsNoTracking()
            .Where(e => e.ProjectId == projectId || e.ProjectId == null)
            .Take(500).ToListAsync(ct);
        var matched = entities.Where(e => !string.IsNullOrWhiteSpace(e.Name)
            && (q.Contains(e.Name.ToLowerInvariant()) || e.Name.ToLowerInvariant().Contains(q)))
            .Take(max).ToList();
        if (matched.Count == 0) return new List<RagContextItem>();

        var ids = matched.Select(e => e.Id).ToHashSet();
        var rels = await _db.KnowledgeRelationships.AsNoTracking()
            .Where(r => ids.Contains(r.FromEntityId) || ids.Contains(r.ToEntityId)).Take(40).ToListAsync(ct);

        var lines = rels.Select(r => $"- #{r.FromEntityId} {r.RelationType} #{r.ToEntityId}").ToList();
        return new List<RagContextItem>
        {
            new() { Kind = "Graph", Source = "graph", Title = "Knowledge-graph neighbors",
                    Content = string.Join("\n", lines), IsApproved = false, Score = 0.5 }
        };
    }

    private async Task<KnowledgeEntity> GetOrCreateEntityAsync(
        int? projectId, string name, EntityType type, int sourceItemId,
        Dictionary<string, KnowledgeEntity> cache, CancellationToken ct)
    {
        if (cache.TryGetValue(name, out var cached)) return cached;

        var existing = await _db.KnowledgeEntities.FirstOrDefaultAsync(
            e => e.ProjectId == projectId && e.Name == name, ct);
        if (existing is not null) { cache[name] = existing; return existing; }

        var ent = new KnowledgeEntity
        {
            ProjectId = projectId,
            Name = name,
            EntityType = type,
            Status = KnowledgeStatus.NeedsReview,
            SourceKnowledgeItemId = sourceItemId,
            Tier = PermanenceTier.Derived
        };
        _db.KnowledgeEntities.Add(ent);
        await _db.SaveChangesAsync(ct);
        cache[name] = ent;
        return ent;
    }

    private async Task AddRelationshipAsync(int? projectId, int fromId, int toId, RelationType rel, string desc, int sourceItemId, CancellationToken ct)
    {
        if (fromId == toId) return;
        var exists = await _db.KnowledgeRelationships.AnyAsync(
            r => r.ProjectId == projectId && r.FromEntityId == fromId && r.ToEntityId == toId && r.RelationType == rel, ct);
        if (exists) return;

        _db.KnowledgeRelationships.Add(new KnowledgeRelationship
        {
            ProjectId = projectId,
            FromEntityId = fromId,
            ToEntityId = toId,
            RelationType = rel,
            Description = desc,
            Status = KnowledgeStatus.NeedsReview,
            Confidence = 0.5,
            SourceKnowledgeItemId = sourceItemId,
            Tier = PermanenceTier.Derived
        });
    }

    private static RelationType MapRelation(string rel) => rel.ToLowerInvariant() switch
    {
        "calls" => RelationType.Calls,
        "reads" => RelationType.Reads,
        "writes" => RelationType.Writes,
        "depends_on" or "depends" => RelationType.DependsOn,
        "deploys_to" or "deploys" => RelationType.DeploysTo,
        "integrates_with" or "integrates" => RelationType.IntegratesWith,
        "belongs_to" or "belongs" => RelationType.BelongsTo,
        _ => RelationType.Uses
    };

    private static string ShortName(string path)
    {
        var name = Path.GetFileName(path);
        return string.IsNullOrWhiteSpace(name) ? path : name;
    }

    private static string Head(string s, int n) => string.IsNullOrEmpty(s) ? "" : (s.Length <= n ? s : s.Substring(0, n));
}
