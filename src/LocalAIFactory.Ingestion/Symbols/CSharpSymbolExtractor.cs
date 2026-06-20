using System.Text.RegularExpressions;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Enums;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LocalAIFactory.Ingestion.Symbols;

// KE-008: deterministic C# symbol extractor. Syntax-only — it parses text into a syntax tree and walks
// declarations. It never builds a Compilation, adds MetadataReferences, restores packages, or asks for a
// semantic model; cross-assembly resolution is KE-010's job. Malformed code still parses (Roslyn is
// error-tolerant), so a non-compiling file still yields the symbols it does declare.
public sealed class CSharpSymbolExtractor : ICodeSymbolExtractor
{
    public string Language => "csharp";

    public CodeExtractionResult Extract(string content)
    {
        var outp = new List<ExtractedSymbol>();
        var refs = new List<ExtractedCodeReference>();
        if (string.IsNullOrWhiteSpace(content)) return CodeExtractionResult.Empty;

        var root = CSharpSyntaxTree.ParseText(content).GetRoot();
        foreach (var member in root.ChildNodes().OfType<MemberDeclarationSyntax>())
            Visit(member, container: null, containerIsType: false, containerIsInterface: false, outp, refs);
        return new CodeExtractionResult(outp, refs);
    }

    // KE-008.x: BCL/primitive type names we never emit references for (bounds reference volume at scale; these
    // are practically never in-corpus). Generic arity is stripped before this check.
    private static readonly HashSet<string> Skip = new(StringComparer.Ordinal)
    {
        "void","var","object","string","bool","byte","sbyte","char","short","ushort","int","uint","long","ulong",
        "float","double","decimal","nint","nuint","dynamic","Object","String","Boolean","Int16","Int32","Int64",
        "UInt16","UInt32","UInt64","Byte","SByte","Char","Single","Double","Decimal","DateTime","DateTimeOffset",
        "TimeSpan","Guid","Task","ValueTask","List","IList","IEnumerable","ICollection","IReadOnlyList",
        "IReadOnlyCollection","Dictionary","IDictionary","HashSet","ISet","Array","Span","ReadOnlySpan","Memory",
        "Nullable","Func","Action","Tuple","ValueTuple","CancellationToken","Exception","Type","Stream","Uri"
    };

    private static void Visit(MemberDeclarationSyntax m, string? container, bool containerIsType, bool containerIsInterface, List<ExtractedSymbol> outp, List<ExtractedCodeReference> refs)
    {
        switch (m)
        {
            case BaseNamespaceDeclarationSyntax ns:
            {
                var name = ns.Name.ToString();
                var full = Combine(container, name);
                outp.Add(Make(CodeSymbolKind.Namespace, name, full, null, CodeAccess.NotApplicable, false, ns, container));
                foreach (var child in ns.Members)
                    Visit(child, full, containerIsType: false, containerIsInterface: false, outp, refs);
                break;
            }
            case EnumDeclarationSyntax en:
            {
                var full = Combine(container, en.Identifier.Text);
                var (acc, pub) = Access(en.Modifiers, containerIsType ? CodeAccess.Private : CodeAccess.Internal);
                outp.Add(Make(CodeSymbolKind.Enum, en.Identifier.Text, full, null, acc, pub, en, container));
                foreach (var em in en.Members)
                    outp.Add(Make(CodeSymbolKind.Field, em.Identifier.Text, Combine(full, em.Identifier.Text), null, CodeAccess.Public, true, em, full));
                break;
            }
            case DelegateDeclarationSyntax del:
            {
                var meta = WithArity(del.Identifier.Text, del.TypeParameterList);
                var full = Combine(container, meta);
                var (acc, pub) = Access(del.Modifiers, containerIsType ? CodeAccess.Private : CodeAccess.Internal);
                var sig = Params(del.ParameterList) + " : " + del.ReturnType;
                outp.Add(Make(CodeSymbolKind.Delegate, del.Identifier.Text, full, sig, acc, pub, del, container));
                break;
            }
            case TypeDeclarationSyntax t: // class / struct / interface / record
            {
                var kind = t switch
                {
                    InterfaceDeclarationSyntax => CodeSymbolKind.Interface,
                    StructDeclarationSyntax => CodeSymbolKind.Struct,
                    RecordDeclarationSyntax => CodeSymbolKind.Record,
                    _ => CodeSymbolKind.Class
                };
                var meta = WithArity(t.Identifier.Text, t.TypeParameterList);
                var full = Combine(container, meta);
                var (acc, pub) = Access(t.Modifiers, containerIsType ? CodeAccess.Private : CodeAccess.Internal);
                outp.Add(Make(kind, t.Identifier.Text, full, null, acc, pub, t, container));
                var isInterface = kind == CodeSymbolKind.Interface;
                CollectTypeReferences(t, kind, full, container, refs);
                foreach (var child in t.Members)
                    VisitMember(child, full, isInterface, outp, refs);
                break;
            }
        }
    }

    // Members of a type (one level): may include nested types (recurse) or leaf members.
    private static void VisitMember(MemberDeclarationSyntax m, string typeFullName, bool inInterface, List<ExtractedSymbol> outp, List<ExtractedCodeReference> refs)
    {
        switch (m)
        {
            case BaseNamespaceDeclarationSyntax:
            case EnumDeclarationSyntax:
            case DelegateDeclarationSyntax:
            case TypeDeclarationSyntax:
                Visit(m, typeFullName, containerIsType: true, containerIsInterface: inInterface, outp, refs);
                break;

            case ConstructorDeclarationSyntax ctor:
            {
                var sig = Params(ctor.ParameterList);
                var (acc, pub) = Access(ctor.Modifiers, inInterface ? CodeAccess.Public : CodeAccess.Private);
                var full = Combine(typeFullName, ".ctor");
                outp.Add(Make(CodeSymbolKind.Constructor, ".ctor", full, sig, acc, pub, ctor, typeFullName, Cyclomatic(ctor)));
                CollectSqlReferences(ctor, full, refs);
                break;
            }
            case MethodDeclarationSyntax method:
            {
                var meta = WithArity(method.Identifier.Text, method.TypeParameterList);
                var sig = Params(method.ParameterList) + " : " + method.ReturnType;
                var (acc, pub) = Access(method.Modifiers, inInterface ? CodeAccess.Public : CodeAccess.Private);
                var full = Combine(typeFullName, meta);
                outp.Add(Make(CodeSymbolKind.Method, method.Identifier.Text, full, sig, acc, pub, method, typeFullName, Cyclomatic(method)));
                CollectSqlReferences(method, full, refs); // R2-ACC-CAP1: C#→SQL bridge
                break;
            }
            case PropertyDeclarationSyntax prop:
            {
                var (acc, pub) = Access(prop.Modifiers, inInterface ? CodeAccess.Public : CodeAccess.Private);
                var full = Combine(typeFullName, prop.Identifier.Text);
                outp.Add(Make(CodeSymbolKind.Property, prop.Identifier.Text, full, prop.Type.ToString(), acc, pub, prop, typeFullName, Cyclomatic(prop)));
                CollectSqlReferences(prop, full, refs);
                break;
            }
            case IndexerDeclarationSyntax indexer:
            {
                var (acc, pub) = Access(indexer.Modifiers, inInterface ? CodeAccess.Public : CodeAccess.Private);
                var sig = Params(indexer.ParameterList) + " : " + indexer.Type;
                outp.Add(Make(CodeSymbolKind.Property, "this[]", Combine(typeFullName, "this[]"), sig, acc, pub, indexer, typeFullName, Cyclomatic(indexer)));
                break;
            }
            case EventDeclarationSyntax ev:
            {
                var (acc, pub) = Access(ev.Modifiers, inInterface ? CodeAccess.Public : CodeAccess.Private);
                outp.Add(Make(CodeSymbolKind.Event, ev.Identifier.Text, Combine(typeFullName, ev.Identifier.Text), ev.Type.ToString(), acc, pub, ev, typeFullName));
                break;
            }
            case EventFieldDeclarationSyntax evf:
            {
                var (acc, pub) = Access(evf.Modifiers, inInterface ? CodeAccess.Public : CodeAccess.Private);
                foreach (var v in evf.Declaration.Variables)
                    outp.Add(Make(CodeSymbolKind.Event, v.Identifier.Text, Combine(typeFullName, v.Identifier.Text), evf.Declaration.Type.ToString(), acc, pub, evf, typeFullName));
                break;
            }
            case FieldDeclarationSyntax field:
            {
                var (acc, pub) = Access(field.Modifiers, inInterface ? CodeAccess.Public : CodeAccess.Private);
                foreach (var v in field.Declaration.Variables)
                    outp.Add(Make(CodeSymbolKind.Field, v.Identifier.Text, Combine(typeFullName, v.Identifier.Text), field.Declaration.Type.ToString(), acc, pub, field, typeFullName));
                break;
            }
        }
    }

    private static ExtractedSymbol Make(CodeSymbolKind kind, string name, string fullName, string? signature,
        CodeAccess access, bool isPublic, SyntaxNode node, string? parentFullName, int complexity = 0)
    {
        var span = node.Span;
        var ls = node.GetLocation().GetLineSpan();
        return new ExtractedSymbol(kind, name, fullName, signature, access, isPublic,
            span.Start, span.End, ls.StartLinePosition.Line + 1, ls.EndLinePosition.Line + 1, complexity, parentFullName);
    }

    private static string Combine(string? container, string name)
        => string.IsNullOrEmpty(container) ? name : container + "." + name;

    // KE-008.x: collect Priority-1 type references a type makes — base class, implemented interfaces, and the
    // types used by its constructor parameters, fields, properties and method parameters. ALL attributed to
    // the TYPE (the consumer) by simple name, so "what consumes IFoo" returns the class, not its members.
    private static void CollectTypeReferences(TypeDeclarationSyntax t, CodeSymbolKind kind, string typeFullName, string? nsHint, List<ExtractedCodeReference> refs)
    {
        if (t.BaseList is not null)
        {
            bool baseTaken = false;
            bool canHaveBaseClass = kind is CodeSymbolKind.Class or CodeSymbolKind.Record; // structs/interfaces: bases are interfaces
            foreach (var bt in t.BaseList.Types)
            {
                var name = OuterName(bt.Type);
                if (name is null) continue;
                // Deterministic heuristic: a class/record's first non-interface base entry is the base class.
                CodeReferenceKind rk;
                if (canHaveBaseClass && !baseTaken && !LooksLikeInterface(name)) { rk = CodeReferenceKind.BaseType; baseTaken = true; }
                else rk = CodeReferenceKind.InterfaceImplementation;
                AddRef(refs, rk, typeFullName, name, nsHint);
                foreach (var arg in TypeArgNames(bt.Type)) AddRef(refs, CodeReferenceKind.InterfaceImplementation, typeFullName, arg, nsHint);
            }
        }

        foreach (var member in t.Members)
        {
            switch (member)
            {
                case ConstructorDeclarationSyntax ctor:
                    foreach (var p in ctor.ParameterList.Parameters)
                        foreach (var n in TypeNames(p.Type)) AddRef(refs, CodeReferenceKind.ConstructorParameterType, typeFullName, n, nsHint);
                    break;
                case FieldDeclarationSyntax field:
                    foreach (var n in TypeNames(field.Declaration.Type)) AddRef(refs, CodeReferenceKind.FieldType, typeFullName, n, nsHint);
                    break;
                case PropertyDeclarationSyntax prop:
                    foreach (var n in TypeNames(prop.Type)) AddRef(refs, CodeReferenceKind.PropertyType, typeFullName, n, nsHint);
                    break;
                case MethodDeclarationSyntax method:
                    foreach (var p in method.ParameterList.Parameters)
                        foreach (var n in TypeNames(p.Type)) AddRef(refs, CodeReferenceKind.ParameterType, typeFullName, n, nsHint);
                    break;
            }
        }
    }

    private static void AddRef(List<ExtractedCodeReference> refs, CodeReferenceKind kind, string from, string name, string? nsHint)
    {
        if (!Skip.Contains(name)) refs.Add(new ExtractedCodeReference(kind, from, name, nsHint));
    }

    private static bool LooksLikeInterface(string name) => name.Length >= 2 && name[0] == 'I' && char.IsUpper(name[1]);

    // Outermost simple type name (generic arity/args stripped, qualified -> rightmost). Null for predefined types.
    private static string? OuterName(TypeSyntax? t) => t switch
    {
        IdentifierNameSyntax id => id.Identifier.Text,
        GenericNameSyntax g => g.Identifier.Text,
        QualifiedNameSyntax q => OuterName(q.Right),
        AliasQualifiedNameSyntax a => OuterName(a.Name),
        NullableTypeSyntax n => OuterName(n.ElementType),
        ArrayTypeSyntax a => OuterName(a.ElementType),
        _ => null
    };

    private static IEnumerable<string> TypeArgNames(TypeSyntax? t)
    {
        if (t is GenericNameSyntax g)
            foreach (var a in g.TypeArgumentList.Arguments)
                foreach (var n in TypeNames(a)) yield return n;
    }

    // All meaningful simple type names within a type expression (outer + nested generic args, recursively).
    private static IEnumerable<string> TypeNames(TypeSyntax? t)
    {
        switch (t)
        {
            case IdentifierNameSyntax id: yield return id.Identifier.Text; break;
            case GenericNameSyntax g:
                yield return g.Identifier.Text;
                foreach (var a in g.TypeArgumentList.Arguments)
                    foreach (var n in TypeNames(a)) yield return n;
                break;
            case QualifiedNameSyntax q:
                foreach (var n in TypeNames(q.Right)) yield return n; break;
            case AliasQualifiedNameSyntax al:
                foreach (var n in TypeNames(al.Name)) yield return n; break;
            case NullableTypeSyntax nu:
                foreach (var n in TypeNames(nu.ElementType)) yield return n; break;
            case ArrayTypeSyntax ar:
                foreach (var n in TypeNames(ar.ElementType)) yield return n; break;
            case TupleTypeSyntax tup:
                foreach (var el in tup.Elements) foreach (var n in TypeNames(el.Type)) yield return n; break;
        }
    }

    private static string WithArity(string name, TypeParameterListSyntax? tps)
    {
        var n = tps?.Parameters.Count ?? 0;
        return n > 0 ? $"{name}`{n}" : name;
    }

    private static string Params(BaseParameterListSyntax? pl)
        => pl is null ? "()" : "(" + string.Join(",", pl.Parameters.Select(p => p.Type?.ToString() ?? "?")) + ")";

    private static (CodeAccess access, bool isPublic) Access(SyntaxTokenList mods, CodeAccess dflt)
    {
        bool pub = mods.Any(SyntaxKind.PublicKeyword);
        bool prot = mods.Any(SyntaxKind.ProtectedKeyword);
        bool intern = mods.Any(SyntaxKind.InternalKeyword);
        bool priv = mods.Any(SyntaxKind.PrivateKeyword);
        if (pub) return (CodeAccess.Public, true);
        if (prot && intern) return (CodeAccess.ProtectedInternal, false);
        if (priv && prot) return (CodeAccess.PrivateProtected, false);
        if (prot) return (CodeAccess.Protected, false);
        if (intern) return (CodeAccess.Internal, false);
        if (priv) return (CodeAccess.Private, false);
        return (dflt, dflt == CodeAccess.Public);
    }

    // R2-ACC-CAP1: C#↔SQL bridge. Deterministic, syntax-only detection of SQL objects named inside SQL string
    // literals within a member (raw SQL, FromSqlRaw/ExecuteSqlRaw, Dapper, ADO.NET CommandText, EXEC). Emits a
    // SqlObjectAccess reference per distinct object with a canonical "schema.object" key (matched later to a SQL
    // CodeSymbol.NormalizedKey), a confidence by detection kind, and a short evidence snippet. Names that do not
    // resolve to a real SQL symbol are simply counted as unresolved — never fabricated.
    private static readonly Regex SqlSignal = new(@"\b(SELECT|INSERT|UPDATE|DELETE|MERGE|EXEC|FROM|JOIN)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SqlTableRx = new(@"\b(?:FROM|JOIN|INTO|UPDATE)\s+(?:(\[?\w+\]?)\s*\.\s*)?(\[?\w+\]?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SqlExecRx = new(@"\bEXEC(?:UTE)?\s+(?:(\[?\w+\]?)\s*\.\s*)?(\[?[\w]+\]?)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly HashSet<string> SqlNoise = new(StringComparer.OrdinalIgnoreCase)
    {
        "select","from","where","join","inner","left","right","outer","cross","on","as","set","values","into",
        "and","or","not","null","exec","execute","update","insert","delete","merge","group","order","by","having",
        "with","case","when","then","else","end","union","top","distinct","count","sum","min","max","avg"
    };

    private static void CollectSqlReferences(SyntaxNode member, string ownerFullName, List<ExtractedCodeReference> refs)
    {
        // Best-effort, never throws. A pathological string must not break extraction.
        try
        {
            var found = new Dictionary<string, (double conf, string evidence)>(StringComparer.OrdinalIgnoreCase);
            foreach (var lit in member.DescendantNodes())
            {
                string? text = lit switch
                {
                    LiteralExpressionSyntax l when l.IsKind(SyntaxKind.StringLiteralExpression) => l.Token.ValueText,
                    InterpolatedStringExpressionSyntax i => i.ToString(),
                    _ => null
                };
                if (text is null || text.Length < 6 || !SqlSignal.IsMatch(text)) continue;
                var evidence = Snippet(text);
                foreach (Match m in SqlExecRx.Matches(text)) Add(found, m, 0.9, evidence);
                foreach (Match m in SqlTableRx.Matches(text)) Add(found, m, 0.75, evidence);
            }
            foreach (var (key, v) in found)
                refs.Add(new ExtractedCodeReference(CodeReferenceKind.SqlObjectAccess, ownerFullName, key, null, v.conf, v.evidence));
        }
        catch { /* bridge detection is best-effort */ }
    }

    private static void Add(Dictionary<string, (double conf, string evidence)> found, Match m, double conf, string evidence)
    {
        var schema = Strip(m.Groups[1].Value);
        var obj = Strip(m.Groups[2].Value);
        if (obj.Length < 2 || SqlNoise.Contains(obj)) return;          // skip keywords / aliases / noise
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

    // Syntactic cyclomatic complexity: 1 + decision points. Deterministic; counts branch-introducing
    // syntax only (no semantic analysis).
    private static int Cyclomatic(SyntaxNode node)
    {
        int c = 1;
        foreach (var n in node.DescendantNodes())
        {
            switch (n)
            {
                case IfStatementSyntax:
                case WhileStatementSyntax:
                case ForStatementSyntax:
                case ForEachStatementSyntax:
                case DoStatementSyntax:
                case CaseSwitchLabelSyntax:
                case CasePatternSwitchLabelSyntax:
                case SwitchExpressionArmSyntax:
                case CatchClauseSyntax:
                case ConditionalExpressionSyntax:
                    c++;
                    break;
                case BinaryExpressionSyntax be when be.IsKind(SyntaxKind.LogicalAndExpression) || be.IsKind(SyntaxKind.LogicalOrExpression):
                    c++;
                    break;
            }
        }
        return c;
    }
}
