using System.Text;
using System.Text.RegularExpressions;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Ingestion.Symbols;

// R2-ACC-CAP3: deterministic, dependency-free Python structural extractor. Pure C# (no Python runtime, no
// network) — an indentation-aware line parser that recovers modules' classes, functions and methods (incl.
// async), captures FastAPI-style route decorators into the signature, and detects SQL objects named in string
// literals (emitting the same SqlObjectAccess references the C# bridge uses, so Python↔SQL impact works too).
// It is syntax-only and best-effort: malformed Python still yields the structure it can, and never throws.
public sealed class PythonSymbolExtractor : ICodeSymbolExtractor
{
    public string Language => "python";

    private static readonly Regex ClassRx = new(@"^class\s+([A-Za-z_]\w*)", RegexOptions.Compiled);
    private static readonly Regex DefRx = new(@"^(async\s+)?def\s+([A-Za-z_]\w*)\s*\((.*)$", RegexOptions.Compiled);
    private static readonly Regex RouteRx = new(@"^@\s*\w+\.(get|post|put|delete|patch|head|options)\s*\(\s*[""']([^""']+)[""']",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex StringRx = new("\"\"\"(.*?)\"\"\"|'''(.*?)'''|\"([^\"\\n]*)\"|'([^'\\n]*)'",
        RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex BranchRx = new(@"^\s*(if|elif|for|while|except|with)\b|(\band\b|\bor\b)",
        RegexOptions.Compiled);

    private sealed record Scope(int Indent, string FullName, bool IsDef);

    public CodeExtractionResult Extract(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return CodeExtractionResult.Empty;
        try { return ExtractCore(content); }
        catch { return CodeExtractionResult.Empty; } // never break ingestion on a pathological file
    }

    private static CodeExtractionResult ExtractCore(string content)
    {
        var symbols = new List<ExtractedSymbol>();
        var refs = new List<ExtractedCodeReference>();
        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        var stack = new List<Scope>();
        var bodyByDef = new Dictionary<string, StringBuilder>(StringComparer.Ordinal);
        var pendingRoute = (string?)null;
        bool pendingDecorator = false;
        int charOffset = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            var raw = lines[i];
            var lineNo = i + 1;
            var startOffset = charOffset;
            charOffset += raw.Length + 1;

            var stripped = raw.TrimStart();
            int indent = raw.Length - stripped.Length;

            if (stripped.Length == 0 || stripped.StartsWith("#")) { AppendBody(stack, bodyByDef, raw); continue; }

            if (stripped.StartsWith("@"))
            {
                var rm = RouteRx.Match(stripped);
                if (rm.Success) pendingRoute = rm.Groups[1].Value.ToUpperInvariant() + " " + rm.Groups[2].Value;
                pendingDecorator = true;
                continue;
            }

            // Dedent: leave scopes we've exited.
            while (stack.Count > 0 && stack[^1].Indent >= indent) stack.RemoveAt(stack.Count - 1);
            var parent = stack.Count > 0 ? stack[^1] : null;

            var cls = ClassRx.Match(stripped);
            var def = DefRx.Match(stripped);

            if (cls.Success)
            {
                var name = cls.Groups[1].Value;
                var full = parent is null ? name : parent.FullName + "." + name;
                symbols.Add(Make(CodeSymbolKind.Class, name, full, null, lineNo, startOffset, parent?.FullName));
                stack.Add(new Scope(indent, full, false));
                pendingRoute = null; pendingDecorator = false;
            }
            else if (def.Success)
            {
                var isAsync = def.Groups[1].Success;
                var name = def.Groups[2].Value;
                var rawParams = def.Groups[3].Value;
                var full = parent is null ? name : parent.FullName + "." + name;
                var sig = (isAsync ? "async " : "") + "(" + ParamHint(rawParams) + ")";
                if (pendingRoute is not null) sig = "[" + pendingRoute + "] " + sig;   // FastAPI route
                symbols.Add(Make(CodeSymbolKind.Method, name, full, sig, lineNo, startOffset, parent?.FullName));
                stack.Add(new Scope(indent, full, true));
                bodyByDef[full] = new StringBuilder();
                pendingRoute = null; pendingDecorator = false;
            }
            else { pendingRoute = null; pendingDecorator = false; }

            AppendBody(stack, bodyByDef, raw);
        }

        // SQL hints: scan each function body's string literals for SQL objects -> SqlObjectAccess references.
        foreach (var (defFull, body) in bodyByDef)
        {
            var found = new Dictionary<string, (double conf, string evidence)>(StringComparer.OrdinalIgnoreCase);
            foreach (Match s in StringRx.Matches(body.ToString()))
            {
                var text = FirstGroup(s);
                if (text is not null) SqlStringScan.Collect(text, found);
            }
            foreach (var (key, v) in found)
                refs.Add(new ExtractedCodeReference(CodeReferenceKind.SqlObjectAccess, defFull, key, null, v.conf, v.evidence));
        }

        return new CodeExtractionResult(symbols, refs);
    }

    private static void AppendBody(List<Scope> stack, Dictionary<string, StringBuilder> bodyByDef, string raw)
    {
        for (int j = stack.Count - 1; j >= 0; j--)
            if (stack[j].IsDef && bodyByDef.TryGetValue(stack[j].FullName, out var sb)) { sb.Append(raw).Append('\n'); return; }
    }

    private static string? FirstGroup(Match m)
    {
        for (int g = 1; g <= 4; g++) if (m.Groups[g].Success && m.Groups[g].Value.Length > 0) return m.Groups[g].Value;
        return null;
    }

    private static string ParamHint(string rawParams)
    {
        var p = rawParams.Trim();
        var close = p.IndexOf(')');
        if (close >= 0) p = p.Substring(0, close);
        return p.Length > 200 ? p.Substring(0, 200) : p;
    }

    private static ExtractedSymbol Make(CodeSymbolKind kind, string name, string fullName, string? signature,
        int line, int startOffset, string? parentFullName)
    {
        var isPublic = !name.StartsWith("_");
        var access = isPublic ? CodeAccess.Public : CodeAccess.Private;
        return new ExtractedSymbol(kind, name, fullName, signature, access, isPublic,
            startOffset, startOffset + name.Length, line, line, 0, parentFullName);
    }
}
