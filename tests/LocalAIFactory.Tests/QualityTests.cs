using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Data.Quality;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalAIFactory.Tests;

public class QualityEvaluatorTests
{
    private readonly QualityEvaluator _e = new();
    private static readonly DateTime Now = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static QualityContext Ctx(
        PermanenceTier tier = PermanenceTier.Derived, ProvenanceMethod method = ProvenanceMethod.Deterministic,
        int corr = 0, int sources = 1, KnowledgeStatus status = KnowledgeStatus.NeedsReview,
        KnowledgeScope scope = KnowledgeScope.Project, double ageDays = 1,
        bool contradiction = false, bool failedOutcome = false)
        => new(tier, method, corr, sources, status, scope, Now.AddDays(-ageDays), Now, contradiction, failedOutcome);

    [Fact] public void Derived_single_source_is_provisional()
        => Assert.Equal(QualityBand.Provisional, _e.ComputeBand(Ctx()));

    [Fact] public void Derived_with_exact_duplicate_is_corroborated()
        => Assert.Equal(QualityBand.Corroborated, _e.ComputeBand(Ctx(corr: 1)));

    [Fact] public void Derived_with_multiple_distinct_sources_is_corroborated()
        => Assert.Equal(QualityBand.Corroborated, _e.ComputeBand(Ctx(sources: 2)));

    [Fact] public void Curated_floors_at_trusted()
        => Assert.Equal(QualityBand.Trusted, _e.ComputeBand(Ctx(tier: PermanenceTier.Curated)));

    [Fact] public void Approved_promotes_to_trusted()
        => Assert.Equal(QualityBand.Trusted, _e.ComputeBand(Ctx(status: KnowledgeStatus.Approved)));

    [Fact] public void Approved_regulatory_is_authoritative()
        => Assert.Equal(QualityBand.Authoritative,
            _e.ComputeBand(Ctx(status: KnowledgeStatus.Approved, scope: KnowledgeScope.Regulatory)));

    [Fact] public void Approved_standards_is_authoritative()
        => Assert.Equal(QualityBand.Authoritative,
            _e.ComputeBand(Ctx(status: KnowledgeStatus.Approved, scope: KnowledgeScope.Standards)));

    [Fact] public void Contradiction_floors_to_provisional_overriding_everything()
        => Assert.Equal(QualityBand.Provisional, _e.ComputeBand(
            Ctx(tier: PermanenceTier.Curated, status: KnowledgeStatus.Approved, scope: KnowledgeScope.Regulatory, contradiction: true)));

    [Fact] public void Failed_outcome_floors_to_provisional()
        => Assert.Equal(QualityBand.Provisional, _e.ComputeBand(Ctx(tier: PermanenceTier.Curated, failedOutcome: true)));

    [Fact] public void Stale_non_curated_is_demoted_one_step()
        => Assert.Equal(QualityBand.Provisional, _e.ComputeBand(Ctx(corr: 1, ageDays: 400))); // Corroborated -> Provisional

    [Fact] public void Stale_does_not_affect_curated()
        => Assert.Equal(QualityBand.Trusted, _e.ComputeBand(Ctx(tier: PermanenceTier.Curated, ageDays: 1000)));
}

public class QualityServiceTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static QualityService NewService(AppDbContext db) => new(db, new QualityEvaluator());

    [Fact]
    public async Task Recompute_persists_provisional_for_derived_item()
    {
        var db = NewDb();
        var item = new KnowledgeItem { Title = "T", Content = "x", Tier = PermanenceTier.Derived };
        db.KnowledgeItems.Add(item);
        await db.SaveChangesAsync();

        var band = await NewService(db).RecomputeAsync(item.Id);
        Assert.Equal(QualityBand.Provisional, band);
        Assert.Equal(QualityBand.Provisional, (await db.KnowledgeItems.FindAsync(item.Id))!.QualityBand);
    }

    [Fact]
    public async Task Recompute_with_exact_duplicate_raises_both_items_to_corroborated()
    {
        var db = NewDb();
        var a = new KnowledgeItem { Title = "A", Content = "same", Tier = PermanenceTier.Derived };
        var b = new KnowledgeItem { Title = "B", Content = "same", Tier = PermanenceTier.Derived };
        db.KnowledgeItems.AddRange(a, b);
        await db.SaveChangesAsync();
        db.KnowledgeDuplicates.Add(new KnowledgeDuplicate
        {
            KnowledgeItemId = b.Id, KnowledgeItemUid = b.Uid,
            DuplicateOfKnowledgeItemId = a.Id, DuplicateOfUid = a.Uid, MatchKind = DuplicateMatchKind.Exact
        });
        await db.SaveChangesAsync();

        var svc = NewService(db);
        Assert.Equal(QualityBand.Corroborated, await svc.RecomputeAsync(a.Id));
        Assert.Equal(QualityBand.Corroborated, await svc.RecomputeAsync(b.Id));
    }

    [Fact]
    public async Task Demote_moves_to_needsreview_and_floors_band()
    {
        var db = NewDb();
        var item = new KnowledgeItem
        {
            Title = "T", Content = "x", Tier = PermanenceTier.Curated,
            Status = KnowledgeStatus.Approved, IsApproved = true, QualityBand = QualityBand.Trusted
        };
        db.KnowledgeItems.Add(item);
        await db.SaveChangesAsync();

        await NewService(db).DemoteAsync(item.Id, DemotionReason.Contradiction);
        var updated = (await db.KnowledgeItems.FindAsync(item.Id))!;
        Assert.Equal(KnowledgeStatus.NeedsReview, updated.Status);
        Assert.False(updated.IsApproved);
        Assert.Equal(QualityBand.Provisional, updated.QualityBand);
    }

    [Fact]
    public async Task RecomputeAll_processes_every_item()
    {
        var db = NewDb();
        db.KnowledgeItems.AddRange(
            new KnowledgeItem { Title = "1", Content = "a", Tier = PermanenceTier.Curated },
            new KnowledgeItem { Title = "2", Content = "b", Tier = PermanenceTier.Derived });
        await db.SaveChangesAsync();

        var n = await NewService(db).RecomputeAllAsync(null);
        Assert.Equal(2, n);
        var bands = await db.KnowledgeItems.OrderBy(k => k.Id).Select(k => k.QualityBand).ToListAsync();
        Assert.Equal(QualityBand.Trusted, bands[0]);     // curated
        Assert.Equal(QualityBand.Provisional, bands[1]); // derived
    }
}
