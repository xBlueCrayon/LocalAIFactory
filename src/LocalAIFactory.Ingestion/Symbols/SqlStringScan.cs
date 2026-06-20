using System.Text.RegularExpressions;

namespace LocalAIFactory.Ingestion.Symbols;

// R2-ACC-CAP1/CAP3: shared, deterministic detection of SQL objects named inside a string literal — used by
// both the C# and Python extractors so the C#↔SQL and Python↔SQL bridges behave identically. Pure syntax: a
// canonical "schema.object" key (matched later to a SQL CodeSymbol.NormalizedKey), a confidence by detection
// kind (EXEC of a proc is more certain than a FROM/JOIN of a name), and a short evidence snippet. Names that
// do not resolve to a real SQL symbol are never fabricated — they simply produce no edge.
public static class SqlStringScan
{
    private static readonly Regex Signal = new(@"\b(SELECT|INSERT|UPDATE|DELETE|MERGE|EXEC|FROM|JOIN)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TableRx = new(@"\b(?:FROM|JOIN|INTO|UPDATE)\s+(?:(\[?\w+\]?)\s*\.\s*)?(\[?\w+\]?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ExecRx = new(@"\bEXEC(?:UTE)?\s+(?:(\[?\w+\]?)\s*\.\s*)?(\[?[\w]+\]?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly HashSet<string> Noise = new(StringComparer.OrdinalIgnoreCase)
    {
        "select","from","where","join","inner","left","right","outer","cross","on","as","set","values","into",
        "and","or","not","null","exec","execute","update","insert","delete","merge","group","order","by","having",
        "with","case","when","then","else","end","union","top","distinct","count","sum","min","max","avg"
    };

    public static bool LooksLikeSql(string? text) => text is { Length: >= 6 } && Signal.IsMatch(text);

    // Collect SQL object references found in one string into `found` (canonical key -> best confidence + evidence).
    public static void Collect(string text, Dictionary<string, (double conf, string evidence)> found)
    {
        if (!LooksLikeSql(text)) return;
        var evidence = Snippet(text);
        foreach (Match m in ExecRx.Matches(text)) Add(found, m, 0.9, evidence);
        foreach (Match m in TableRx.Matches(text)) Add(found, m, 0.75, evidence);
    }

    private static void Add(Dictionary<string, (double conf, string evidence)> found, Match m, double conf, string evidence)
    {
        var schema = Strip(m.Groups[1].Value);
        var obj = Strip(m.Groups[2].Value);
        if (obj.Length < 2 || Noise.Contains(obj)) return; // skip keywords / aliases / noise
        var key = (string.IsNullOrEmpty(schema) ? "dbo" : schema.ToLowerInvariant()) + "." + obj.ToLowerInvariant();
        if (!found.TryGetValue(key, out var cur) || conf > cur.conf) found[key] = (conf, evidence);
    }

    private static string Strip(string s) => s.Replace("[", "").Replace("]", "").Trim();

    private static string Snippet(string text)
    {
        var one = text.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Trim();
        while (one.Contains("  ")) one = one.Replace("  ", " ");
        return one.Length <= 160 ? one : one.Substring(0, 160) + "…";
    }
}
