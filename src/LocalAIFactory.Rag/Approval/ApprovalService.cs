using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Rag.Approval;

// Approval is the learning loop: flip status, audit, and re-index so the item is retrieved first.
public sealed class ApprovalService : IApprovalService
{
    private readonly AppDbContext _db;
    private readonly IKnowledgeIndexer _indexer;
    private readonly IAuditService _audit;

    public ApprovalService(AppDbContext db, IKnowledgeIndexer indexer, IAuditService audit)
    {
        _db = db; _indexer = indexer; _audit = audit;
    }

    public async Task ApproveKnowledgeItemAsync(int knowledgeItemId, CancellationToken ct = default)
    {
        var item = await _db.KnowledgeItems.FirstOrDefaultAsync(k => k.Id == knowledgeItemId, ct);
        if (item is null) return;
        item.Status = KnowledgeStatus.Approved;
        item.IsApproved = true;
        item.IsDeprecated = false;
        item.Tier = PermanenceTier.Curated; // approval is curation: the item is now human-anchored.
        item.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("ApproveKnowledgeItem", nameof(KnowledgeItem), item.Id.ToString(), item.Title, ct);
        await _indexer.IndexKnowledgeItemAsync(item.Id, ct);
    }

    public async Task DeprecateKnowledgeItemAsync(int knowledgeItemId, CancellationToken ct = default)
    {
        var item = await _db.KnowledgeItems.FirstOrDefaultAsync(k => k.Id == knowledgeItemId, ct);
        if (item is null) return;
        item.Status = KnowledgeStatus.Deprecated;
        item.IsApproved = false;
        item.IsDeprecated = true;
        item.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("DeprecateKnowledgeItem", nameof(KnowledgeItem), item.Id.ToString(), item.Title, ct);
        try { await _indexer.RemoveKnowledgeItemAsync(item.Id, ct); } catch { /* keyword search unaffected */ }
    }

    public async Task DeleteKnowledgeItemAsync(int knowledgeItemId, CancellationToken ct = default)
    {
        var item = await _db.KnowledgeItems.FirstOrDefaultAsync(k => k.Id == knowledgeItemId, ct);
        if (item is null) return;
        try { await _indexer.RemoveKnowledgeItemAsync(item.Id, ct); } catch { }
        var title = item.Title;
        _db.KnowledgeItems.Remove(item); // KnowledgeChunks / tags cascade
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("DeleteKnowledgeItem", nameof(KnowledgeItem), knowledgeItemId.ToString(), title, ct);
    }

    public async Task ApproveBusinessRuleAsync(int businessRuleId, CancellationToken ct = default)
    {
        var rule = await _db.BusinessRules.FirstOrDefaultAsync(r => r.Id == businessRuleId, ct);
        if (rule is null) return;
        rule.Status = BusinessRuleStatus.Approved;
        rule.IsApproved = true;
        rule.Tier = PermanenceTier.Curated;
        rule.ApprovedUtc = DateTime.UtcNow;
        rule.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("ApproveBusinessRule", nameof(BusinessRule), rule.Id.ToString(), rule.Title, ct);
    }

    public async Task ApproveCodeSnippetAsync(int approvedCodeSnippetId, CancellationToken ct = default)
    {
        var snip = await _db.ApprovedCodeSnippets.FirstOrDefaultAsync(s => s.Id == approvedCodeSnippetId, ct);
        if (snip is null) return;
        snip.IsReusable = true;
        snip.Tier = PermanenceTier.Curated;
        snip.ApprovedUtc = DateTime.UtcNow;
        snip.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("ApproveCodeSnippet", nameof(ApprovedCodeSnippet), snip.Id.ToString(), snip.Title, ct);
    }

    public async Task<int> PromoteCodeBlockAsync(int extractedCodeBlockId, string title, CancellationToken ct = default)
    {
        var block = await _db.ExtractedCodeBlocks.FirstOrDefaultAsync(b => b.Id == extractedCodeBlockId, ct);
        if (block is null) return 0;

        var snippet = new ApprovedCodeSnippet
        {
            ProjectId = block.ProjectId,
            Title = string.IsNullOrWhiteSpace(title) ? "Promoted code block" : title,
            Language = string.IsNullOrWhiteSpace(block.Language) ? "text" : block.Language!,
            Content = block.Content ?? "",
            Explanation = "Promoted from an extracted code block.",
            SourceReference = "ExtractedCodeBlock #" + block.Id,
            IsReusable = true,
            Tier = PermanenceTier.Curated,
            ApprovedUtc = DateTime.UtcNow
        };
        _db.ApprovedCodeSnippets.Add(snippet);
        block.Status = KnowledgeStatus.Approved;
        await _db.SaveChangesAsync(ct);

        block.PromotedToApprovedCodeSnippetId = snippet.Id;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("PromoteCodeBlock", nameof(ApprovedCodeSnippet), snippet.Id.ToString(), snippet.Title, ct);
        return snippet.Id;
    }

    public async Task ApproveProjectProfileSectionAsync(int sectionId, CancellationToken ct = default)
    {
        var section = await _db.ProjectProfileSections.FirstOrDefaultAsync(s => s.Id == sectionId, ct);
        if (section is null) return;
        section.Status = KnowledgeStatus.Approved;
        section.Tier = PermanenceTier.Curated;
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync("ApproveProjectProfileSection", nameof(ProjectProfileSection), section.Id.ToString(), section.Title, ct);
    }

    // ---------------- Phase 1.1 bulk operations ----------------

    public async Task<int> BulkApproveAsync(string kind, IEnumerable<int> ids, CancellationToken ct = default)
    {
        var list = Normalize(ids);
        if (list.Count == 0) return 0;
        int n = 0;
        switch (kind)
        {
            case "knowledge":
                foreach (var id in list) { await ApproveKnowledgeItemAsync(id, ct); n++; }
                break;
            case "rule":
                foreach (var id in list) { await ApproveBusinessRuleAsync(id, ct); n++; }
                break;
            case "code":
                n = await BulkPromoteCodeBlocksAsync(list, ct);
                break;
            case "entity":
                n = await SetEntityStatusAsync(list, KnowledgeStatus.Approved, "BulkApproveEntities", ct);
                break;
            case "relationship":
                n = await SetRelationshipStatusAsync(list, KnowledgeStatus.Approved, "BulkApproveRelationships", ct);
                break;
            case "section":
                n = await SetSectionStatusAsync(list, KnowledgeStatus.Approved, "BulkApproveSections", ct);
                break;
        }
        return n;
    }

    public async Task<int> BulkDeprecateAsync(string kind, IEnumerable<int> ids, CancellationToken ct = default)
    {
        var list = Normalize(ids);
        if (list.Count == 0) return 0;
        int n = 0;
        switch (kind)
        {
            case "knowledge":
                foreach (var id in list) { await DeprecateKnowledgeItemAsync(id, ct); n++; }
                break;
            case "rule":
                var rules = await _db.BusinessRules.Where(r => list.Contains(r.Id)).ToListAsync(ct);
                foreach (var r in rules) { r.Status = BusinessRuleStatus.Deprecated; r.IsApproved = false; r.UpdatedUtc = DateTime.UtcNow; }
                await _db.SaveChangesAsync(ct); n = rules.Count;
                await _audit.LogAsync("BulkDeprecateRules", nameof(BusinessRule), string.Join(",", list), $"{n} rule(s)", ct);
                break;
            case "code":
                var blocks = await _db.ExtractedCodeBlocks.Where(b => list.Contains(b.Id)).ToListAsync(ct);
                foreach (var b in blocks) b.Status = KnowledgeStatus.Deprecated;
                await _db.SaveChangesAsync(ct); n = blocks.Count;
                await _audit.LogAsync("BulkDeprecateCode", nameof(ExtractedCodeBlock), string.Join(",", list), $"{n} candidate(s)", ct);
                break;
            case "entity":
                n = await SetEntityStatusAsync(list, KnowledgeStatus.Deprecated, "BulkDeprecateEntities", ct);
                break;
            case "relationship":
                n = await SetRelationshipStatusAsync(list, KnowledgeStatus.Deprecated, "BulkDeprecateRelationships", ct);
                break;
            case "section":
                n = await SetSectionStatusAsync(list, KnowledgeStatus.Deprecated, "BulkDeprecateSections", ct);
                break;
        }
        return n;
    }

    public async Task<int> BulkDeleteAsync(string kind, IEnumerable<int> ids, CancellationToken ct = default)
    {
        var list = Normalize(ids);
        if (list.Count == 0) return 0;
        int n = 0;
        switch (kind)
        {
            case "knowledge":
                foreach (var id in list) { await DeleteKnowledgeItemAsync(id, ct); n++; }
                break;
            case "rule":
                var rules = await _db.BusinessRules.Where(r => list.Contains(r.Id)).ToListAsync(ct);
                _db.BusinessRules.RemoveRange(rules); await _db.SaveChangesAsync(ct); n = rules.Count;
                await _audit.LogAsync("BulkDeleteRules", nameof(BusinessRule), string.Join(",", list), $"{n} rule(s)", ct);
                break;
            case "code":
                var blocks = await _db.ExtractedCodeBlocks.Where(b => list.Contains(b.Id)).ToListAsync(ct);
                _db.ExtractedCodeBlocks.RemoveRange(blocks); await _db.SaveChangesAsync(ct); n = blocks.Count;
                await _audit.LogAsync("BulkDeleteCode", nameof(ExtractedCodeBlock), string.Join(",", list), $"{n} candidate(s)", ct);
                break;
            case "entity":
                // remove relationships referencing these entities first (FK is Restrict)
                var rels = await _db.KnowledgeRelationships.Where(r => list.Contains(r.FromEntityId) || list.Contains(r.ToEntityId)).ToListAsync(ct);
                _db.KnowledgeRelationships.RemoveRange(rels);
                var ents = await _db.KnowledgeEntities.Where(e => list.Contains(e.Id)).ToListAsync(ct);
                _db.KnowledgeEntities.RemoveRange(ents); await _db.SaveChangesAsync(ct); n = ents.Count;
                await _audit.LogAsync("BulkDeleteEntities", nameof(KnowledgeEntity), string.Join(",", list), $"{n} entity(ies)", ct);
                break;
            case "relationship":
                var r2 = await _db.KnowledgeRelationships.Where(r => list.Contains(r.Id)).ToListAsync(ct);
                _db.KnowledgeRelationships.RemoveRange(r2); await _db.SaveChangesAsync(ct); n = r2.Count;
                await _audit.LogAsync("BulkDeleteRelationships", nameof(KnowledgeRelationship), string.Join(",", list), $"{n} edge(s)", ct);
                break;
            case "section":
                var secs = await _db.ProjectProfileSections.Where(s => list.Contains(s.Id)).ToListAsync(ct);
                _db.ProjectProfileSections.RemoveRange(secs); await _db.SaveChangesAsync(ct); n = secs.Count;
                await _audit.LogAsync("BulkDeleteSections", nameof(ProjectProfileSection), string.Join(",", list), $"{n} section(s)", ct);
                break;
        }
        return n;
    }

    public async Task<int> BulkPromoteCodeBlocksAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var list = Normalize(ids);
        int n = 0;
        foreach (var id in list)
        {
            var newId = await PromoteCodeBlockAsync(id, "", ct);
            if (newId > 0) n++;
        }
        return n;
    }

    private async Task<int> SetEntityStatusAsync(List<int> ids, KnowledgeStatus status, string action, CancellationToken ct)
    {
        var items = await _db.KnowledgeEntities.Where(e => ids.Contains(e.Id)).ToListAsync(ct);
        foreach (var e in items) { e.Status = status; if (status == KnowledgeStatus.Approved) e.Tier = PermanenceTier.Curated; }
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(action, nameof(KnowledgeEntity), string.Join(",", ids), $"{items.Count} entity(ies)", ct);
        return items.Count;
    }

    private async Task<int> SetRelationshipStatusAsync(List<int> ids, KnowledgeStatus status, string action, CancellationToken ct)
    {
        var items = await _db.KnowledgeRelationships.Where(r => ids.Contains(r.Id)).ToListAsync(ct);
        foreach (var r in items) { r.Status = status; if (status == KnowledgeStatus.Approved) r.Tier = PermanenceTier.Curated; }
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(action, nameof(KnowledgeRelationship), string.Join(",", ids), $"{items.Count} edge(s)", ct);
        return items.Count;
    }

    private async Task<int> SetSectionStatusAsync(List<int> ids, KnowledgeStatus status, string action, CancellationToken ct)
    {
        var items = await _db.ProjectProfileSections.Where(s => ids.Contains(s.Id)).ToListAsync(ct);
        foreach (var s in items) { s.Status = status; if (status == KnowledgeStatus.Approved) s.Tier = PermanenceTier.Curated; }
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(action, nameof(ProjectProfileSection), string.Join(",", ids), $"{items.Count} section(s)", ct);
        return items.Count;
    }

    private static List<int> Normalize(IEnumerable<int> ids) =>
        ids?.Where(i => i > 0).Distinct().ToList() ?? new List<int>();
}
