using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalAIFactory.KnowledgeGrowth;

public sealed record FetchedDocument(string Url, string Title, string Html);
public sealed record CitationMetadata(string Url, string Title, string FetchDateUtc, string SourceHash);
public sealed record GrowthProposal(string Title, IReadOnlyList<string> Facts, CitationMetadata Citation, bool Approved = false);
public sealed record GrowthOutcome(bool Accepted, string Reason, GrowthProposal? Proposal);

/// <summary>Domain allowlist for knowledge-growth scraping. Default-deny: only listed hosts (and official GitHub docs) are allowed.</summary>
public sealed class ScrapeAllowlist
{
    private static readonly HashSet<string> Hosts = new(StringComparer.OrdinalIgnoreCase)
    { "learn.microsoft.com", "docs.python.org", "docs.ollama.com", "modelcontextprotocol.io" };

    public bool IsAllowed(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return false;
        if (u.Scheme != "https") return false; // never plain http
        var host = u.Host.ToLowerInvariant();
        return Hosts.Contains(host) || host.EndsWith(".github.io") || host == "github.com";
    }
    public IReadOnlyCollection<string> Domains => Hosts;
}

/// <summary>
/// Governs learning-from-the-web safely: allowlist, cache by content hash (dedup), required citation metadata,
/// clean-room summarisation (never stores large raw third-party text), and a knowledge PROPOSAL that a human
/// must approve. Fully offline + deterministic — the caller supplies already-fetched documents.
/// </summary>
public sealed class KnowledgeGrowthService
{
    private readonly ScrapeAllowlist _allow;
    private readonly Func<string> _nowUtc;
    private readonly Dictionary<string, GrowthProposal> _cacheByHash = new(StringComparer.Ordinal);
    private const int MaxFactChars = 300, MaxFacts = 20;

    public KnowledgeGrowthService(ScrapeAllowlist? allow = null, Func<string>? nowUtc = null)
    { _allow = allow ?? new ScrapeAllowlist(); _nowUtc = nowUtc ?? (() => "2026-06-21"); }

    public IReadOnlyCollection<GrowthProposal> Cached => _cacheByHash.Values;
    public ScrapeAllowlist Allowlist => _allow;

    public GrowthOutcome Ingest(FetchedDocument doc)
    {
        if (!_allow.IsAllowed(doc.Url))
            return new GrowthOutcome(false, $"url not allowlisted: {doc.Url}", null);

        var hash = Sha256(doc.Html);
        if (_cacheByHash.TryGetValue(hash, out var existing))
            return new GrowthOutcome(false, "duplicate source (already ingested by content hash)", existing);

        var facts = Summarise(doc.Html);
        if (facts.Count == 0)
            return new GrowthOutcome(false, "no extractable technical facts", null);

        var citation = new CitationMetadata(doc.Url, doc.Title, _nowUtc(), hash);
        // Clean-room: a proposal stores SUMMARISED facts + citation, never the raw HTML/body text.
        var proposal = new GrowthProposal(doc.Title, facts, citation, Approved: false);
        _cacheByHash[hash] = proposal;
        return new GrowthOutcome(true, "knowledge proposal created (awaiting human approval)", proposal);
    }

    /// <summary>Strip tags and keep the leading sentence of each block — a summary, never the full third-party text.</summary>
    private static List<string> Summarise(string html)
    {
        var text = Regex.Replace(html ?? "", "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        var blocks = Regex.Split(text, @"\n{2,}|\.\s")
            .Select(b => Regex.Replace(b, @"\s+", " ").Trim())
            .Where(b => b.Length > 20);
        return blocks.Select(b => b.Length > MaxFactChars ? b[..MaxFactChars] : b).Take(MaxFacts).ToList();
    }

    private static string Sha256(string s) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(s ?? "")));
}
