using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Data.Scope;

// KE-005: deterministic scope precedence + applicability resolution. Pure EF/MSSQL — no vectors, no model.
public sealed class ScopeResolver : IScopeResolver
{
    private readonly AppDbContext _db;
    public ScopeResolver(AppDbContext db) => _db = db;

    public int Rank(KnowledgeScope scope) => scope switch
    {
        KnowledgeScope.Regulatory => 5,
        KnowledgeScope.Standards => 4,
        KnowledgeScope.Project => 3,
        KnowledgeScope.Global => 2,
        KnowledgeScope.Team => 1,
        _ => 0
    };

    public AuthorityLevel AuthorityForScope(KnowledgeScope scope) => scope switch
    {
        KnowledgeScope.Regulatory => AuthorityLevel.Binding,
        KnowledgeScope.Standards => AuthorityLevel.High,
        KnowledgeScope.Team => AuthorityLevel.Low,
        _ => AuthorityLevel.Normal
    };

    public IReadOnlyList<PrecedenceItem> OrderByPrecedence(IEnumerable<PrecedenceItem> items) =>
        items.OrderByDescending(i => Rank(i.Scope))
             .ThenByDescending(i => i.ProjectId.HasValue) // project-scoped (more specific) before global
             .ThenByDescending(i => i.UpdatedUtc)
             .ThenByDescending(i => i.Id)                 // stable tie-break -> fully deterministic
             .ToList();

    public async Task<IReadOnlyList<int>> ResolveApplicableConstraintItemIdsAsync(int projectId, CancellationToken ct = default)
    {
        var constraintIds = await _db.KnowledgeItems
            .Where(k => k.Scope == KnowledgeScope.Standards || k.Scope == KnowledgeScope.Regulatory)
            .Select(k => k.Id)
            .ToListAsync(ct);
        if (constraintIds.Count == 0) return Array.Empty<int>();

        var links = await _db.ScopeApplicabilities
            .Where(a => constraintIds.Contains(a.ConstraintKnowledgeItemId))
            .Select(a => new { a.ConstraintKnowledgeItemId, a.TargetKind, a.TargetId })
            .ToListAsync(ct);

        var hasAnyLink = links.Select(l => l.ConstraintKnowledgeItemId).ToHashSet();
        var linkedToProject = links
            .Where(l => l.TargetKind == ScopeTargetKind.Project && l.TargetId == projectId)
            .Select(l => l.ConstraintKnowledgeItemId).ToHashSet();

        // Applies if it has no links (global constraint) OR is explicitly linked to this project.
        return constraintIds.Where(id => !hasAnyLink.Contains(id) || linkedToProject.Contains(id)).ToList();
    }
}
