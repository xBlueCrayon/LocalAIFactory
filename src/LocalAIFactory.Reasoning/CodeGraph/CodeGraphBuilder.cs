using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Ingestion.Symbols;

namespace LocalAIFactory.Reasoning.CodeGraph;

/// <summary>
/// Builds an in-memory <see cref="CodeGraph"/> from C# source files using the existing deterministic Roslyn
/// extractor (<see cref="CSharpSymbolExtractor"/>). Infers semantic roles (controller/service/dbcontext/
/// entity/test) and resolves syntax-only references into typed edges. Pure and deterministic.
/// </summary>
public sealed class CodeGraphBuilder
{
    private readonly CSharpSymbolExtractor _csharp = new();

    public CodeGraphModel Build(IEnumerable<(string Path, string Content)> files)
    {
        var graph = new CodeGraphModel();
        var typeByFullName = new Dictionary<string, CodeNode>(StringComparer.Ordinal);
        var pendingRefs = new List<ExtractedCodeReference>();

        foreach (var (path, content) in files)
        {
            if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;
            var result = _csharp.Extract(content ?? "");
            var isTestFile = path.Contains("Tests", StringComparison.OrdinalIgnoreCase) || path.Contains("Test.cs", StringComparison.OrdinalIgnoreCase);

            foreach (var sym in result.Symbols)
            {
                var node = new CodeNode
                {
                    Id = NodeId(sym.Kind, sym.FullName, sym.Signature),
                    Kind = sym.Kind,
                    Name = sym.Name,
                    FullName = sym.FullName,
                    Signature = sym.Signature,
                    StartLine = sym.StartLine,
                    EndLine = sym.EndLine,
                    IsPublic = sym.IsPublic,
                    FilePath = path
                };
                InferRoles(node, isTestFile);
                var added = graph.AddNode(node);
                added.FilePath ??= path;
                if (IsType(sym.Kind) && !typeByFullName.ContainsKey(sym.FullName))
                    typeByFullName[sym.FullName] = added;

                // containment edge from parent (namespace/type) to this symbol
                if (sym.ParentFullName is { } parent)
                {
                    var parentNode = typeByFullName.GetValueOrDefault(parent)
                                     ?? graph.Nodes.FirstOrDefault(n => n.FullName == parent && IsContainer(n.Kind));
                    if (parentNode != null) graph.AddEdge(new CodeEdge(parentNode.Id, added.Id, CodeEdgeKind.Contains));
                }
            }
            pendingRefs.AddRange(result.References);
        }

        // Second pass: resolve references to edges now that all type nodes exist.
        foreach (var r in pendingRefs)
        {
            if (!typeByFullName.TryGetValue(r.FromFullName, out var from)) continue;
            var to = ResolveTarget(graph, r.ReferencedName);
            // Inheritance/implementation to an out-of-corpus type (e.g. DbContext) still matters: model it as an
            // external node so the Inherits/Implements edge — and role inference that depends on it — is preserved.
            if (to is null)
            {
                if (r.Kind is CodeReferenceKind.BaseType or CodeReferenceKind.InterfaceImplementation)
                    to = graph.AddNode(new CodeNode { Id = $"ext:{r.ReferencedName}", Kind = CodeSymbolKind.Class, Name = r.ReferencedName, FullName = r.ReferencedName });
                else continue;
            }
            var kind = MapEdge(r.Kind, to);
            graph.AddEdge(new CodeEdge(from.Id, to.Id, kind, r.ReferencedName));
            // test classes "cover" the types they reference
            if (from.Roles.Contains("test") && IsType(to.Kind))
                graph.AddEdge(new CodeEdge(from.Id, to.Id, CodeEdgeKind.TestCovers));
        }

        // Promote dbcontext/entity roles discovered via inheritance edges.
        foreach (var n in graph.Nodes.Where(n => IsType(n.Kind)).ToList())
        {
            foreach (var e in graph.OutgoingFrom(n.Id))
            {
                if (e.Kind is CodeEdgeKind.Inherits or CodeEdgeKind.Implements && e.Detail is { } baseName)
                {
                    if (baseName.Contains("DbContext", StringComparison.Ordinal)) n.Roles.Add("dbcontext");
                    if (baseName is "EntityBase" or "DocumentBase") n.Roles.Add("entity");
                }
            }
        }
        return graph;
    }

    private static CodeNode? ResolveTarget(CodeGraphModel g, string simpleName)
    {
        var matches = g.FindByName(simpleName).Where(n => IsType(n.Kind)).ToList();
        return matches.Count > 0 ? matches[0] : null;
    }

    private static CodeEdgeKind MapEdge(CodeReferenceKind k, CodeNode target) => k switch
    {
        CodeReferenceKind.BaseType => CodeEdgeKind.Inherits,
        CodeReferenceKind.InterfaceImplementation => CodeEdgeKind.Implements,
        CodeReferenceKind.SqlObjectAccess => CodeEdgeKind.UsesSqlObject,
        _ when target.Roles.Contains("dbcontext") => CodeEdgeKind.UsesDbSet,
        _ when target.Roles.Contains("entity") => CodeEdgeKind.UsesEntity,
        _ => CodeEdgeKind.References
    };

    private static void InferRoles(CodeNode n, bool isTestFile)
    {
        if (!IsType(n.Kind)) return;
        if (n.Name.EndsWith("Controller", StringComparison.Ordinal)) n.Roles.Add("controller");
        if (n.Name.EndsWith("Service", StringComparison.Ordinal)) n.Roles.Add("service");
        if (n.Name.EndsWith("DbContext", StringComparison.Ordinal)) n.Roles.Add("dbcontext");
        if (n.Name.EndsWith("Endpoints", StringComparison.Ordinal)) n.Roles.Add("apiroute");
        if (isTestFile && (n.Name.EndsWith("Tests", StringComparison.Ordinal) || n.Name.EndsWith("Test", StringComparison.Ordinal)))
            n.Roles.Add("test");
    }

    private static bool IsType(CodeSymbolKind k) =>
        k is CodeSymbolKind.Class or CodeSymbolKind.Interface or CodeSymbolKind.Struct or CodeSymbolKind.Record or CodeSymbolKind.Enum;

    private static bool IsContainer(CodeSymbolKind k) => IsType(k) || k == CodeSymbolKind.Namespace;

    private static string NodeId(CodeSymbolKind kind, string fullName, string? signature) =>
        signature is null ? $"{(int)kind}:{fullName}" : $"{(int)kind}:{fullName}:{signature}";

    /// <summary>Convenience: build a graph by reading .cs files under a root directory (skips bin/obj/.tmp/node_modules).</summary>
    public CodeGraphModel BuildFromDirectory(string root, int maxFiles = 5000)
    {
        var files = EnumerateCsFiles(root, maxFiles).Select(p => (Rel(root, p), SafeRead(p)));
        return Build(files);
    }

    public static IEnumerable<string> EnumerateCsFiles(string root, int maxFiles = 5000)
    {
        if (!Directory.Exists(root)) yield break;
        int n = 0;
        foreach (var f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var p = f.Replace('\\', '/');
            if (p.Contains("/bin/") || p.Contains("/obj/") || p.Contains("/.tmp") || p.Contains("/node_modules/")) continue;
            yield return f;
            if (++n >= maxFiles) yield break;
        }
    }

    private static string Rel(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
    private static string SafeRead(string path) { try { return File.ReadAllText(path); } catch { return ""; } }
}
