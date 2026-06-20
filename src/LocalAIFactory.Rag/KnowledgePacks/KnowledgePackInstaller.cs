using System.Text;
using System.Text.Json;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Core.Options;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LocalAIFactory.Rag.KnowledgePacks;

// R2-ACC-B1: installs a portable Professional Base Knowledge Pack into MSSQL. Design rules:
//  - Validate the ENTIRE pack in memory before any DB write — a malformed pack never partially corrupts.
//  - Idempotent: each item carries a stable Uid; re-installing the same content is a no-op (no duplicates).
//  - Never silently overwrite a user-edited baseline item — raise a ProposedRevision via the permanence guard.
//  - Baseline items are ordinary KnowledgeItems stamped with KnowledgePackId + Tier=Curated, so they flow
//    through existing search, approval, versioning, provenance and (future) Knowledge Pack export unchanged.
public sealed class KnowledgePackInstaller : IKnowledgePackInstaller
{
    private readonly AppDbContext _db;
    private readonly IContentHasher _hasher;
    private readonly IInstanceContext _instance;
    private readonly IPermanenceGuard _guard;
    private readonly IChunkingService _chunking;
    private readonly IKnowledgeIndexer _indexer;
    private readonly RagOptions _rag;
    private readonly ILogger<KnowledgePackInstaller> _log;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public KnowledgePackInstaller(AppDbContext db, IContentHasher hasher, IInstanceContext instance,
        IPermanenceGuard guard, IChunkingService chunking, IKnowledgeIndexer indexer,
        IOptions<RagOptions> rag, ILogger<KnowledgePackInstaller> log)
    {
        _db = db; _hasher = hasher; _instance = instance; _guard = guard;
        _chunking = chunking; _indexer = indexer; _rag = rag.Value; _log = log;
    }

    public async Task<KnowledgePackInstallResult> InstallAsync(string packDirectory, string actor, CancellationToken ct = default)
    {
        // ---- 1. Load + validate the whole pack in memory (no DB writes yet) ----
        var (manifest, items, sourceTitles, manifestHash, errors) = await LoadAndValidateAsync(packDirectory, ct);
        if (errors.Count > 0)
        {
            _log.LogWarning("Knowledge pack install rejected ({Count} validation error(s)): {First}", errors.Count, errors[0]);
            return KnowledgePackInstallResult.Failed(errors);
        }
        var packUid = Guid.Parse(manifest!.PackUid!);

        // ---- 2. Fast idempotency path: an already-installed pack with the same content hash is current ----
        var existingPack = await _db.KnowledgePacks.FirstOrDefaultAsync(p => p.Uid == packUid, ct);
        if (existingPack is { Status: KnowledgePackStatus.Installed } && existingPack.SourceManifestHash == manifestHash)
        {
            return new KnowledgePackInstallResult(true, packUid, existingPack.Name, existingPack.Version,
                items.Count, 0, 0, items.Count, 0, AlreadyCurrent: true, Array.Empty<string>());
        }

        var relational = _db.Database.IsRelational();
        var tx = relational ? await _db.Database.BeginTransactionAsync(ct) : null;
        try
        {
            // ---- 3. Upsert the pack anchor (need its Id before stamping items) ----
            var pack = existingPack ?? new KnowledgePack { Uid = packUid };
            pack.Name = manifest.Name ?? "Knowledge Pack";
            pack.Version = manifest.Version ?? "1.0.0";
            pack.Description = Trim(manifest.Description, 2000);
            pack.License = Trim(manifest.License, 1000);
            pack.ItemCount = items.Count;
            pack.SourceManifestHash = manifestHash;
            pack.InstalledUtc = DateTime.UtcNow;
            pack.Status = KnowledgePackStatus.Installed;
            if (existingPack is null) _db.KnowledgePacks.Add(pack);
            await _db.SaveChangesAsync(ct);

            var instanceId = await _instance.GetInstanceIdAsync(ct);
            var generalDomainId = await _db.KnowledgeDomains
                .Where(d => d.Code == "GENERAL").Select(d => (int?)d.Id).FirstOrDefaultAsync(ct);
            var tagCache = new Dictionary<string, Tag>(StringComparer.OrdinalIgnoreCase);

            int created = 0, updated = 0, unchanged = 0, proposed = 0;
            var toIndex = new List<int>();

            foreach (var dto in items)
            {
                ct.ThrowIfCancellationRequested();
                var uid = Guid.Parse(dto.Uid!);
                var content = Render(dto, sourceTitles);
                var incomingHash = _hasher.Compute(content);
                var existing = await _db.KnowledgeItems.FirstOrDefaultAsync(k => k.Uid == uid, ct);

                if (existing is null)
                {
                    var ki = new KnowledgeItem
                    {
                        Uid = uid,
                        ProjectId = null,                       // baseline = global, not project-scoped
                        KnowledgePackId = pack.Id,
                        Title = Trim(dto.Title, 400)!,
                        Content = content,
                        Summary = Trim(dto.Description, 2000),
                        SourceType = ParseSourceType(dto.SourceType),
                        KnowledgeType = ParseEnum(dto.KnowledgeType, KnowledgeType.Other),
                        Scope = ParseEnum(dto.Scope, KnowledgeScope.Global),
                        KnowledgeDomainId = generalDomainId,
                        Confidence = dto.Confidence ?? 0.8,
                        Tier = PermanenceTier.Curated,          // protected from silent automated overwrite
                        Status = ParseStatus(dto.ReviewStatus),
                        ContentHash = incomingHash,
                        LastReviewedUtc = ParseDate(dto.LastReviewedUtc),
                        EffectiveUtc = DateTime.UtcNow
                    };
                    ki.IsApproved = ki.Status == KnowledgeStatus.Approved;
                    _db.KnowledgeItems.Add(ki);
                    await _db.SaveChangesAsync(ct);

                    await WriteVersionAsync(ki, version: 1, previousUid: null, reason: $"Installed from {pack.Name} v{pack.Version}", ct);
                    await WriteProvenanceAsync(ki, $"Installed from Knowledge Pack {pack.Name} v{pack.Version}", actor, instanceId, packUid, ct);
                    await SyncTagsAsync(ki, dto, tagCache, ct);
                    toIndex.Add(ki.Id);
                    created++;
                }
                else if (existing.ContentHash == incomingHash)
                {
                    // Content identical to what we ship: idempotent no-op, but keep the pack link + review date fresh.
                    existing.KnowledgePackId ??= pack.Id;
                    existing.LastReviewedUtc = ParseDate(dto.LastReviewedUtc) ?? existing.LastReviewedUtc;
                    await _db.SaveChangesAsync(ct);
                    unchanged++;
                }
                else
                {
                    // Content differs. Did a human touch this baseline item? A human edit/approve always writes a
                    // Human provenance event — that is our reliable "do not overwrite" signal.
                    var humanTouched = await _db.ProvenanceEvents
                        .AnyAsync(p => p.KnowledgeItemId == existing.Id && p.Method == ProvenanceMethod.Human, ct);
                    if (humanTouched)
                    {
                        await _guard.ProposeRevisionAsync("KnowledgeItem", existing.Id, existing.Id,
                            Trim(dto.Title, 400), content,
                            $"Knowledge Pack {pack.Name} v{pack.Version} update (user-edited baseline preserved)",
                            RevisionSource.Extraction, ct);
                        proposed++;
                    }
                    else
                    {
                        // Unedited baseline whose shipped content changed in a newer pack version: safe in-place
                        // update + a new version snapshot (hash-guarded) + provenance.
                        var prevUid = await _db.KnowledgeVersions
                            .Where(v => v.KnowledgeItemId == existing.Id)
                            .OrderByDescending(v => v.VersionNumber).Select(v => (Guid?)v.Uid).FirstOrDefaultAsync(ct);
                        existing.Title = Trim(dto.Title, 400)!;
                        existing.Content = content;
                        existing.Summary = Trim(dto.Description, 2000);
                        existing.Confidence = dto.Confidence ?? existing.Confidence;
                        existing.KnowledgePackId = pack.Id;
                        existing.Tier = PermanenceTier.Curated;
                        existing.ContentHash = incomingHash;
                        existing.VersionNumber += 1;
                        existing.LastReviewedUtc = ParseDate(dto.LastReviewedUtc) ?? existing.LastReviewedUtc;
                        existing.UpdatedUtc = DateTime.UtcNow;
                        await _db.SaveChangesAsync(ct);
                        await WriteVersionAsync(existing, existing.VersionNumber, prevUid, $"Updated by {pack.Name} v{pack.Version}", ct);
                        await WriteProvenanceAsync(existing, $"Updated from Knowledge Pack {pack.Name} v{pack.Version}", actor, instanceId, packUid, ct);
                        toIndex.Add(existing.Id);
                        updated++;
                    }
                }
            }

            // Older versions of the same pack uid are superseded (kept for audit, not deleted).
            if (tx is not null) await tx.CommitAsync(ct);

            // ---- 4. Best-effort chunk + index OUTSIDE the transaction (retrieval is regenerable) ----
            await ChunkAndIndexAsync(toIndex, ct);

            _log.LogInformation("Knowledge pack {Name} v{Version} installed: {Created} created, {Updated} updated, {Unchanged} unchanged, {Proposed} proposed.",
                pack.Name, pack.Version, created, updated, unchanged, proposed);
            return new KnowledgePackInstallResult(true, packUid, pack.Name, pack.Version,
                items.Count, created, updated, unchanged, proposed, AlreadyCurrent: false, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            if (tx is not null) { try { await tx.RollbackAsync(ct); } catch { /* best effort */ } }
            _log.LogError(ex, "Knowledge pack install failed; rolled back.");
            return KnowledgePackInstallResult.Failed(new[] { "Install failed and was rolled back: " + ex.Message });
        }
        finally
        {
            if (tx is not null) await tx.DisposeAsync();
        }
    }

    // ---------------- load + validate ----------------

    private async Task<(PackManifestDto? manifest, List<PackItemDto> items, Dictionary<string, string> sourceTitles, string hash, List<string> errors)>
        LoadAndValidateAsync(string packDirectory, CancellationToken ct)
    {
        var errors = new List<string>();
        var items = new List<PackItemDto>();
        var sourceTitles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(packDirectory))
            return (null, items, sourceTitles, "", new List<string> { $"Pack directory not found: {packDirectory}" });

        var manifestPath = Path.Combine(packDirectory, "manifest.json");
        if (!File.Exists(manifestPath))
            return (null, items, sourceTitles, "", new List<string> { "manifest.json not found in pack directory." });

        PackManifestDto? manifest;
        string manifestText;
        try
        {
            manifestText = await File.ReadAllTextAsync(manifestPath, ct);
            manifest = JsonSerializer.Deserialize<PackManifestDto>(manifestText, Json);
        }
        catch (Exception ex) { return (null, items, sourceTitles, "", new List<string> { "manifest.json is not valid JSON: " + ex.Message }); }

        if (manifest is null) return (null, items, sourceTitles, "", new List<string> { "manifest.json deserialized to null." });
        if (string.IsNullOrWhiteSpace(manifest.PackUid) || !Guid.TryParse(manifest.PackUid, out _))
            errors.Add("manifest.packUid is missing or not a valid GUID.");
        if (string.IsNullOrWhiteSpace(manifest.Name)) errors.Add("manifest.name is required.");
        if (string.IsNullOrWhiteSpace(manifest.Version)) errors.Add("manifest.version is required.");
        if (manifest.Files is null || manifest.Files.Count == 0) errors.Add("manifest.files is empty.");

        // Hash covers manifest + the source registry + every category file so a re-install detects any change.
        var hashSb = new StringBuilder(manifestText);

        // R2-ACC-B2 (v1.2): source registry. Validated for required metadata + unique sourceUids; items may
        // then reference these sources. Optional — a pack without a registry behaves exactly as v1.
        var registeredSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(manifest.SourceRegistry))
        {
            var regPath = Path.Combine(packDirectory, manifest.SourceRegistry!);
            if (!File.Exists(regPath)) errors.Add($"Source registry not found: {manifest.SourceRegistry}");
            else
            {
                SourceRegistryDto? reg = null;
                try { var regText = await File.ReadAllTextAsync(regPath, ct); hashSb.Append(' ').Append(regText); reg = JsonSerializer.Deserialize<SourceRegistryDto>(regText, Json); }
                catch (Exception ex) { errors.Add($"{manifest.SourceRegistry} is not valid JSON: {ex.Message}"); }
                if (reg?.Sources is null || reg.Sources.Count == 0) errors.Add($"{manifest.SourceRegistry} has no sources.");
                foreach (var s in reg?.Sources ?? new List<SourceDto>())
                {
                    var w = $"source '{s.SourceUid ?? s.Title ?? "?"}'";
                    if (string.IsNullOrWhiteSpace(s.SourceUid)) errors.Add($"{w}: sourceUid is required.");
                    else if (!registeredSources.Add(s.SourceUid)) errors.Add($"{w}: duplicate sourceUid.");
                    if (string.IsNullOrWhiteSpace(s.Title)) errors.Add($"{w}: title is required.");
                    if (string.IsNullOrWhiteSpace(s.SourceType)) errors.Add($"{w}: sourceType is required.");
                    if (string.IsNullOrWhiteSpace(s.Publisher)) errors.Add($"{w}: publisher is required.");
                    if (string.IsNullOrWhiteSpace(s.AllowedUse)) errors.Add($"{w}: allowedUse is required.");
                    if (string.IsNullOrWhiteSpace(s.ReliabilityLevel)) errors.Add($"{w}: reliabilityLevel is required.");
                    if (string.IsNullOrWhiteSpace(s.LimitationNote)) errors.Add($"{w}: limitationNote is required.");
                    if (s.SummaryAllowed is null) errors.Add($"{w}: summaryAllowed is required.");
                    if (s.VerbatimCopyAllowed is null) errors.Add($"{w}: verbatimCopyAllowed is required.");
                    if (!string.IsNullOrWhiteSpace(s.SourceUid) && !string.IsNullOrWhiteSpace(s.Title))
                        sourceTitles[s.SourceUid] = $"{s.Title} — {s.Publisher}";
                }
            }
        }

        var seenUids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in manifest.Files ?? new List<string>())
        {
            var path = Path.Combine(packDirectory, file);
            if (!File.Exists(path)) { errors.Add($"Referenced file not found: {file}"); continue; }
            string text;
            PackCategoryFileDto? cat;
            try { text = await File.ReadAllTextAsync(path, ct); cat = JsonSerializer.Deserialize<PackCategoryFileDto>(text, Json); }
            catch (Exception ex) { errors.Add($"{file} is not valid JSON: {ex.Message}"); continue; }
            hashSb.Append(' ').Append(text);
            if (cat?.Items is null || cat.Items.Count == 0) { errors.Add($"{file} has no items."); continue; }

            foreach (var it in cat.Items)
            {
                var where = $"{file} item '{it.Title ?? it.Uid ?? "?"}'";
                if (string.IsNullOrWhiteSpace(it.Uid) || !Guid.TryParse(it.Uid, out _)) errors.Add($"{where}: uid missing/invalid.");
                else if (!seenUids.Add(it.Uid)) errors.Add($"{where}: duplicate uid {it.Uid}.");
                if (string.IsNullOrWhiteSpace(it.Title)) errors.Add($"{where}: title is required.");
                if (string.IsNullOrWhiteSpace(it.Description)) errors.Add($"{where}: description is required.");
                if (it.Confidence is null || it.Confidence < 0 || it.Confidence > 1) errors.Add($"{where}: confidence must be 0..1.");
                if (!string.IsNullOrWhiteSpace(it.KnowledgeType) && !Enum.TryParse<KnowledgeType>(it.KnowledgeType, true, out _)) errors.Add($"{where}: unknown knowledgeType '{it.KnowledgeType}'.");
                if (!string.IsNullOrWhiteSpace(it.Scope) && !Enum.TryParse<KnowledgeScope>(it.Scope, true, out _)) errors.Add($"{where}: unknown scope '{it.Scope}'.");
                if (!string.IsNullOrWhiteSpace(it.SourceType) && !Enum.TryParse<SourceType>(it.SourceType, true, out _)) errors.Add($"{where}: unknown sourceType '{it.SourceType}'.");
                // R2-ACC-B2: every referenced source must be registered (research-derived claims stay attributable).
                foreach (var sref in it.Sources ?? new List<string>())
                    if (!string.IsNullOrWhiteSpace(sref) && !registeredSources.Contains(sref))
                        errors.Add($"{where}: references unregistered source '{sref}'.");
                items.Add(it);
            }
        }

        return (manifest, items, sourceTitles, _hasher.Compute(hashSb.ToString()), errors);
    }

    // ---------------- write helpers ----------------

    private async Task WriteVersionAsync(KnowledgeItem item, int version, Guid? previousUid, string reason, CancellationToken ct)
    {
        _db.KnowledgeVersions.Add(new KnowledgeVersion
        {
            KnowledgeItemId = item.Id, KnowledgeItemUid = item.Uid, VersionNumber = version,
            ContentSnapshot = item.Content, ContentHash = item.ContentHash, Title = item.Title, Summary = item.Summary,
            ChangeReason = reason, Method = ProvenanceMethod.Import, Actor = "pack-installer",
            TierAtVersion = item.Tier, StatusAtVersion = item.Status, PreviousVersionUid = previousUid
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task WriteProvenanceAsync(KnowledgeItem item, string reason, string actor, Guid instanceId, Guid packUid, CancellationToken ct)
    {
        _db.ProvenanceEvents.Add(new ProvenanceEvent
        {
            KnowledgeItemId = item.Id, KnowledgeItemUid = item.Uid, Method = ProvenanceMethod.Import,
            Actor = actor, Reason = Trim(reason, 1000) ?? "", OriginInstanceId = instanceId, OriginPackUid = packUid
        });
        await _db.SaveChangesAsync(ct);
    }

    // Tags include the human tags plus a "cat:<Category>" tag that drives the Base Knowledge category filter.
    private async Task SyncTagsAsync(KnowledgeItem item, PackItemDto dto, Dictionary<string, Tag> cache, CancellationToken ct)
    {
        var names = new List<string>();
        if (!string.IsNullOrWhiteSpace(dto.Category)) names.Add("cat:" + dto.Category.Trim());
        if (!string.IsNullOrWhiteSpace(dto.Jurisdiction)) names.Add("jur:" + dto.Jurisdiction.Trim());
        foreach (var sref in dto.Sources ?? new List<string>())
            if (!string.IsNullOrWhiteSpace(sref)) names.Add("src:" + sref.Trim());
        foreach (var t in dto.Tags ?? new List<string>())
            if (!string.IsNullOrWhiteSpace(t)) names.Add(t.Trim());

        foreach (var name in names.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!cache.TryGetValue(name, out var tag))
            {
                tag = await _db.Tags.FirstOrDefaultAsync(t => t.Name == name, ct) ?? new Tag { Name = Trim(name, 150)! };
                if (tag.Id == 0) { _db.Tags.Add(tag); await _db.SaveChangesAsync(ct); }
                cache[name] = tag;
            }
            if (!await _db.KnowledgeItemTags.AnyAsync(x => x.KnowledgeItemId == item.Id && x.TagId == tag.Id, ct))
                _db.KnowledgeItemTags.Add(new KnowledgeItemTag { KnowledgeItemId = item.Id, TagId = tag.Id });
        }
        await _db.SaveChangesAsync(ct);
    }

    private async Task ChunkAndIndexAsync(List<int> itemIds, CancellationToken ct)
    {
        foreach (var id in itemIds)
        {
            try
            {
                var item = await _db.KnowledgeItems.FirstOrDefaultAsync(k => k.Id == id, ct);
                if (item is null) continue;
                var old = await _db.KnowledgeChunks.Where(c => c.KnowledgeItemId == id).ToListAsync(ct);
                if (old.Count > 0) _db.KnowledgeChunks.RemoveRange(old);
                int idx = 0;
                foreach (var chunk in _chunking.Chunk(item.Content, _rag.MaxChunkChars, _rag.ChunkOverlap))
                    _db.KnowledgeChunks.Add(new KnowledgeChunk { KnowledgeItemId = id, ChunkIndex = idx++, Content = chunk, TokenCount = _chunking.EstimateTokens(chunk) });
                await _db.SaveChangesAsync(ct);
                try { await _indexer.IndexKnowledgeItemAsync(id, ct); } catch { /* keyword fallback remains; vectors optional */ }
            }
            catch (Exception ex) { _log.LogWarning(ex, "Chunk/index failed for baseline item {Id} (non-fatal).", id); }
        }
    }

    // ---------------- rendering + parsing ----------------

    private static string Render(PackItemDto i, Dictionary<string, string> sourceTitles)
    {
        var sb = new StringBuilder();
        sb.Append((i.Description ?? "").Trim());
        if (!string.IsNullOrWhiteSpace(i.Jurisdiction)) sb.Append("\n\n**Jurisdiction:** ").Append(i.Jurisdiction!.Trim());
        if (!string.IsNullOrWhiteSpace(i.Applicability)) sb.Append("\n\n**Applicability:** ").Append(i.Applicability!.Trim());
        if (!string.IsNullOrWhiteSpace(i.Example)) sb.Append("\n\n**Example:** ").Append(i.Example!.Trim());
        if (!string.IsNullOrWhiteSpace(i.Limitation)) sb.Append("\n\n**Limitation:** ").Append(i.Limitation!.Trim());
        // Attribution: list the registered sources this item draws on (titles resolved from the registry).
        var refs = (i.Sources ?? new List<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        if (refs.Count > 0)
        {
            sb.Append("\n\n**Sources:** ");
            sb.Append(string.Join("; ", refs.Select(s => sourceTitles.TryGetValue(s, out var t) ? $"{t} [{s}]" : s)));
        }
        return sb.ToString();
    }

    private static SourceType ParseSourceType(string? s) =>
        Enum.TryParse<SourceType>(s, true, out var v) ? v : SourceType.ArchitectureNote;

    private static KnowledgeStatus ParseStatus(string? s) =>
        Enum.TryParse<KnowledgeStatus>(s, true, out var v) ? v : KnowledgeStatus.Draft;

    private static T ParseEnum<T>(string? s, T fallback) where T : struct, Enum =>
        Enum.TryParse<T>(s, true, out var v) ? v : fallback;

    private static DateTime? ParseDate(string? s) =>
        DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var d) ? d : null;

    private static string? Trim(string? s, int max) =>
        s is null ? null : (s.Length <= max ? s : s[..max]);

    // ---------------- DTOs ----------------

    private sealed class PackManifestDto
    {
        public string? PackUid { get; set; }
        public string? Name { get; set; }
        public string? Version { get; set; }
        public string? Description { get; set; }
        public string? License { get; set; }
        public string? CreatedUtc { get; set; }
        public string? LastReviewedUtc { get; set; }
        public int ItemCount { get; set; }
        public List<string>? Files { get; set; }
        public string? SourceRegistry { get; set; }   // R2-ACC-B2: optional source-registry filename
        public string? LegalLimitations { get; set; }
        public string? SourcePolicy { get; set; }
        public string? ReviewStatus { get; set; }
    }

    // R2-ACC-B2: source registry — governance/attribution for research- and standards-derived knowledge.
    private sealed class SourceRegistryDto
    {
        public string? RegistryUid { get; set; }
        public List<SourceDto>? Sources { get; set; }
    }

    private sealed class SourceDto
    {
        public string? SourceUid { get; set; }
        public string? Title { get; set; }
        public string? SourceType { get; set; }
        public string? Publisher { get; set; }
        public string? Jurisdiction { get; set; }
        public string? Url { get; set; }
        public string? RetrievedUtc { get; set; }
        public string? LicenseNote { get; set; }
        public string? AllowedUse { get; set; }
        public bool? SummaryAllowed { get; set; }
        public bool? VerbatimCopyAllowed { get; set; }
        public string? ReliabilityLevel { get; set; }
        public string? VerificationStatus { get; set; }
        public string? LimitationNote { get; set; }
    }

    private sealed class PackCategoryFileDto
    {
        public string? Category { get; set; }
        public List<PackItemDto>? Items { get; set; }
    }

    private sealed class PackItemDto
    {
        public string? Uid { get; set; }
        public string? Category { get; set; }
        public string? Title { get; set; }
        public string? KnowledgeType { get; set; }
        public string? Scope { get; set; }
        public string? Description { get; set; }
        public string? Applicability { get; set; }
        public string? Example { get; set; }
        public string? Limitation { get; set; }
        public double? Confidence { get; set; }
        public string? SourceType { get; set; }
        public string? Version { get; set; }
        public string? LastReviewedUtc { get; set; }
        public string? ReviewStatus { get; set; }
        public List<string>? Tags { get; set; }
        public List<string>? Sources { get; set; }      // R2-ACC-B2: referenced source registry uids
        public string? Jurisdiction { get; set; }        // R2-ACC-B2: e.g. "Mauritius"
    }
}
