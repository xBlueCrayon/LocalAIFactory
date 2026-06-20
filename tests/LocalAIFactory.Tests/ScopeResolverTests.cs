using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Data.Scope;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalAIFactory.Tests;

public class ScopeResolverTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public void Rank_orders_regulatory_highest_through_team_lowest()
    {
        var r = new ScopeResolver(NewDb());
        Assert.True(r.Rank(KnowledgeScope.Regulatory) > r.Rank(KnowledgeScope.Standards));
        Assert.True(r.Rank(KnowledgeScope.Standards) > r.Rank(KnowledgeScope.Project));
        Assert.True(r.Rank(KnowledgeScope.Project) > r.Rank(KnowledgeScope.Global));
        Assert.True(r.Rank(KnowledgeScope.Global) > r.Rank(KnowledgeScope.Team));
        Assert.True(r.Rank(KnowledgeScope.Team) > r.Rank(KnowledgeScope.Unspecified));
    }

    [Fact]
    public void AuthorityForScope_maps_constraint_scopes_to_binding_and_high()
    {
        var r = new ScopeResolver(NewDb());
        Assert.Equal(AuthorityLevel.Binding, r.AuthorityForScope(KnowledgeScope.Regulatory));
        Assert.Equal(AuthorityLevel.High, r.AuthorityForScope(KnowledgeScope.Standards));
        Assert.Equal(AuthorityLevel.Normal, r.AuthorityForScope(KnowledgeScope.Project));
        Assert.Equal(AuthorityLevel.Normal, r.AuthorityForScope(KnowledgeScope.Global));
        Assert.Equal(AuthorityLevel.Low, r.AuthorityForScope(KnowledgeScope.Team));
    }

    [Fact]
    public void OrderByPrecedence_is_deterministic_scope_then_specificity_then_recency()
    {
        var r = new ScopeResolver(NewDb());
        var t = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var items = new[]
        {
            new PrecedenceItem(1, KnowledgeScope.Global, null, t),
            new PrecedenceItem(2, KnowledgeScope.Regulatory, null, t),
            new PrecedenceItem(3, KnowledgeScope.Project, 7, t),
            new PrecedenceItem(4, KnowledgeScope.Standards, null, t),
        };
        var ordered = r.OrderByPrecedence(items).Select(i => i.Id).ToList();
        Assert.Equal(new[] { 2, 4, 3, 1 }, ordered); // Regulatory > Standards > Project > Global
    }

    [Fact]
    public async Task ResolveApplicableConstraints_global_applies_everywhere_linked_applies_only_to_target()
    {
        var db = NewDb();
        var global = new KnowledgeItem { Title = "G", Scope = KnowledgeScope.Standards };      // no links => global
        var linked = new KnowledgeItem { Title = "P", Scope = KnowledgeScope.Regulatory };     // linked to project 100
        db.KnowledgeItems.AddRange(global, linked);
        await db.SaveChangesAsync();
        db.ScopeApplicabilities.Add(new ScopeApplicability
        {
            ConstraintKnowledgeItemId = linked.Id, ConstraintUid = linked.Uid,
            TargetKind = ScopeTargetKind.Project, TargetId = 100
        });
        await db.SaveChangesAsync();

        var r = new ScopeResolver(db);

        var for100 = await r.ResolveApplicableConstraintItemIdsAsync(100);
        Assert.Contains(global.Id, for100);
        Assert.Contains(linked.Id, for100);

        var for200 = await r.ResolveApplicableConstraintItemIdsAsync(200);
        Assert.Contains(global.Id, for200);        // global applies everywhere
        Assert.DoesNotContain(linked.Id, for200);  // linked-only does not apply to a different project
    }
}
