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

    public IReadOnlyList<ExtractedSymbol> Extract(string content)
    {
        var outp = new List<ExtractedSymbol>();
        if (string.IsNullOrWhiteSpace(content)) return outp;

        var root = CSharpSyntaxTree.ParseText(content).GetRoot();
        foreach (var member in root.ChildNodes().OfType<MemberDeclarationSyntax>())
            Visit(member, container: null, containerIsType: false, containerIsInterface: false, outp);
        return outp;
    }

    private static void Visit(MemberDeclarationSyntax m, string? container, bool containerIsType, bool containerIsInterface, List<ExtractedSymbol> outp)
    {
        switch (m)
        {
            case BaseNamespaceDeclarationSyntax ns:
            {
                var name = ns.Name.ToString();
                var full = Combine(container, name);
                outp.Add(Make(CodeSymbolKind.Namespace, name, full, null, CodeAccess.NotApplicable, false, ns, container));
                foreach (var child in ns.Members)
                    Visit(child, full, containerIsType: false, containerIsInterface: false, outp);
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
                foreach (var child in t.Members)
                    VisitMember(child, full, isInterface, outp);
                break;
            }
        }
    }

    // Members of a type (one level): may include nested types (recurse) or leaf members.
    private static void VisitMember(MemberDeclarationSyntax m, string typeFullName, bool inInterface, List<ExtractedSymbol> outp)
    {
        switch (m)
        {
            case BaseNamespaceDeclarationSyntax:
            case EnumDeclarationSyntax:
            case DelegateDeclarationSyntax:
            case TypeDeclarationSyntax:
                Visit(m, typeFullName, containerIsType: true, containerIsInterface: inInterface, outp);
                break;

            case ConstructorDeclarationSyntax ctor:
            {
                var sig = Params(ctor.ParameterList);
                var (acc, pub) = Access(ctor.Modifiers, inInterface ? CodeAccess.Public : CodeAccess.Private);
                outp.Add(Make(CodeSymbolKind.Constructor, ".ctor", Combine(typeFullName, ".ctor"), sig, acc, pub, ctor, typeFullName, Cyclomatic(ctor)));
                break;
            }
            case MethodDeclarationSyntax method:
            {
                var meta = WithArity(method.Identifier.Text, method.TypeParameterList);
                var sig = Params(method.ParameterList) + " : " + method.ReturnType;
                var (acc, pub) = Access(method.Modifiers, inInterface ? CodeAccess.Public : CodeAccess.Private);
                outp.Add(Make(CodeSymbolKind.Method, method.Identifier.Text, Combine(typeFullName, meta), sig, acc, pub, method, typeFullName, Cyclomatic(method)));
                break;
            }
            case PropertyDeclarationSyntax prop:
            {
                var (acc, pub) = Access(prop.Modifiers, inInterface ? CodeAccess.Public : CodeAccess.Private);
                outp.Add(Make(CodeSymbolKind.Property, prop.Identifier.Text, Combine(typeFullName, prop.Identifier.Text), prop.Type.ToString(), acc, pub, prop, typeFullName, Cyclomatic(prop)));
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
