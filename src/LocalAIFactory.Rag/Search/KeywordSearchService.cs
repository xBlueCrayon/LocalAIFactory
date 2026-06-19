using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Rag.Search;

// MSSQL keyword fallback used whenever vector search is unavailable. Approved knowledge ranks first.
public sealed class KeywordSearchService
{
    private readonly AppDbContext _db;
    public KeywordSearchService(AppDbContext db) => _db = db;

    public async Task<List<RagContextItem>> SearchAsync(int? projectId, string query, int topK, CancellationToken ct = default)
    {
        var terms = Tokenize(query);
        var candidates = await _db.KnowledgeChunks
            .AsNoTracking()
            .Where(c => !c.KnowledgeItem!.IsDeprecated
                        && (projectId == null || c.KnowledgeItem.ProjectId == projectId || c.KnowledgeItem.ProjectId == null))
            .OrderByDescending(c => c.KnowledgeItem!.IsApproved)
            .ThenByDescending(c => c.KnowledgeItem!.UpdatedUtc)
            .Take(500)
            .Select(c => new
            {
                c.Content,
                c.KnowledgeItem!.Title,
                c.KnowledgeItem.IsApproved
            })
            .ToListAsync(ct);

        IEnumerable<(string Content, string Title, bool IsApproved, int Hits)> scored;
        if (terms.Count == 0)
        {
            scored = candidates.Select(c => (c.Content, c.Title, c.IsApproved, 0));
        }
        else
        {
            scored = candidates
                .Select(c =>
                {
                    var hits = terms.Count(t => c.Content.Contains(t, StringComparison.OrdinalIgnoreCase)
                                                || c.Title.Contains(t, StringComparison.OrdinalIgnoreCase));
                    return (c.Content, c.Title, c.IsApproved, hits);
                })
                .Where(x => x.hits > 0);
        }

        return scored
            .OrderByDescending(x => x.IsApproved)
            .ThenByDescending(x => x.Hits)
            .Take(topK)
            .Select(x => new RagContextItem
            {
                Kind = "Knowledge",
                Source = "keyword",
                Title = x.Title,
                Content = x.Content,
                IsApproved = x.IsApproved,
                Score = x.IsApproved ? 0.6 : 0.4
            })
            .ToList();
    }

    private static List<string> Tokenize(string query)
        => (query ?? "")
            .Split(new[] { ' ', '\t', '\n', '\r', ',', '.', ';', ':', '(', ')', '[', ']', '{', '}', '"', '\'', '/', '\\', '?', '!', '<', '>' },
                   StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .Select(t => t.ToLowerInvariant())
            .Distinct()
            .Take(12)
            .ToList();
}
