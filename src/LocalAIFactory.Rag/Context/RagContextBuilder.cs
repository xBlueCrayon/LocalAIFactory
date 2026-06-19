using System.Text;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Core.Options;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LocalAIFactory.Rag.Context;

// Assembles the prompt context. Approved project memory is injected FIRST, then graph facts,
// then general knowledge search. Everything is trimmed to a character budget.
public sealed class RagContextBuilder : IRagContextBuilder
{
    private readonly AppDbContext _db;
    private readonly IKnowledgeSearchService _search;
    private readonly RagOptions _rag;

    public RagContextBuilder(AppDbContext db, IKnowledgeSearchService search, IOptions<RagOptions> rag)
    {
        _db = db; _search = search; _rag = rag.Value;
    }

    public async Task<RetrievedContext> BuildAsync(
        int? projectId, string query, bool useProjectMemory, bool useKnowledgeBase, bool useKnowledgeGraph,
        CancellationToken ct = default)
    {
        var items = new List<RagContextItem>();

        if (useProjectMemory)
        {
            var rules = await _db.BusinessRules.AsNoTracking()
                .Where(r => r.IsApproved && (r.ProjectId == projectId || r.ProjectId == null))
                .OrderByDescending(r => r.ProjectId == projectId)
                .ThenByDescending(r => r.UpdatedUtc)
                .Take(6)
                .ToListAsync(ct);
            foreach (var r in rules)
                items.Add(new RagContextItem
                {
                    Kind = "BusinessRule", Source = "memory", Title = r.Title, Content = r.Content,
                    IsApproved = true, Score = r.ProjectId == projectId ? 1.0 : 0.9
                });

            var code = await _db.ApprovedCodeSnippets.AsNoTracking()
                .Where(s => s.IsReusable && (s.ProjectId == projectId || s.ProjectId == null))
                .OrderByDescending(s => s.ProjectId == projectId)
                .ThenByDescending(s => s.UpdatedUtc)
                .Take(4)
                .ToListAsync(ct);
            foreach (var s in code)
                items.Add(new RagContextItem
                {
                    Kind = "ApprovedCode", Source = "memory",
                    Title = $"{s.Title} ({s.Language})",
                    Content = (string.IsNullOrWhiteSpace(s.Explanation) ? "" : s.Explanation + "\n") + "```" + s.Language + "\n" + s.Content + "\n```",
                    IsApproved = true, Score = 0.85
                });
        }

        if (useKnowledgeGraph)
        {
            var graph = await BuildGraphFactsAsync(projectId, query, ct);
            if (graph is not null) items.Add(graph);
        }

        if (useKnowledgeBase)
        {
            var found = await _search.SearchAsync(projectId, query, _rag.TopK, ct);
            items.AddRange(found);
        }

        var ordered = items
            .OrderByDescending(SortRank)
            .ThenByDescending(i => i.Score)
            .ToList();

        return new RetrievedContext { Items = Trim(ordered, _rag.MaxContextChars) };
    }

    private static int SortRank(RagContextItem i) => i.Kind switch
    {
        "BusinessRule" => 5,
        "ApprovedCode" => 4,
        "Graph" => 3,
        "Knowledge" => i.IsApproved ? 2 : 1,
        _ => 0
    };

    private async Task<RagContextItem?> BuildGraphFactsAsync(int? projectId, string query, CancellationToken ct)
    {
        var q = (query ?? "").ToLowerInvariant();
        var entities = await _db.KnowledgeEntities.AsNoTracking()
            .Where(e => e.Status == KnowledgeStatus.Approved && (e.ProjectId == projectId || e.ProjectId == null))
            .Take(400)
            .ToListAsync(ct);

        var matched = entities
            .Where(e => !string.IsNullOrWhiteSpace(e.Name) && (q.Contains(e.Name.ToLowerInvariant()) || e.Name.ToLowerInvariant().Contains(q)))
            .Take(8)
            .ToList();
        if (matched.Count == 0) return null;

        var ids = matched.Select(e => e.Id).ToHashSet();
        var rels = await _db.KnowledgeRelationships.AsNoTracking()
            .Where(r => r.Status == KnowledgeStatus.Approved && (ids.Contains(r.FromEntityId) || ids.Contains(r.ToEntityId)))
            .Take(40)
            .ToListAsync(ct);
        if (rels.Count == 0) return null;

        var nameById = await _db.KnowledgeEntities.AsNoTracking()
            .Where(e => rels.Select(r => r.FromEntityId).Concat(rels.Select(r => r.ToEntityId)).Distinct().Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Name, ct);

        var sb = new StringBuilder();
        foreach (var r in rels)
        {
            var from = nameById.TryGetValue(r.FromEntityId, out var fn) ? fn : ("#" + r.FromEntityId);
            var to = nameById.TryGetValue(r.ToEntityId, out var tn) ? tn : ("#" + r.ToEntityId);
            sb.AppendLine($"- {from} {Rel(r.RelationType)} {to}{(string.IsNullOrWhiteSpace(r.Description) ? "" : " (" + r.Description + ")")}");
        }

        return new RagContextItem
        {
            Kind = "Graph", Source = "graph", Title = "Related knowledge-graph facts",
            Content = sb.ToString(), IsApproved = true, Score = 0.8
        };
    }

    private static string Rel(RelationType t) => t switch
    {
        RelationType.BelongsTo => "belongs_to",
        RelationType.Uses => "uses",
        RelationType.Calls => "calls",
        RelationType.Reads => "reads",
        RelationType.Writes => "writes",
        RelationType.DependsOn => "depends_on",
        RelationType.DeploysTo => "deploys_to",
        RelationType.IntegratesWith => "integrates_with",
        _ => "related_to"
    };

    private static List<RagContextItem> Trim(List<RagContextItem> items, int maxChars)
    {
        var result = new List<RagContextItem>();
        int total = 0;
        foreach (var i in items)
        {
            var len = (i.Content?.Length ?? 0) + (i.Title?.Length ?? 0) + 16;
            if (total + len > maxChars && result.Count > 0) break;
            result.Add(i);
            total += len;
        }
        return result;
    }
}
