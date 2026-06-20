using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

// P2 Pilot UX — read-only structural exploration over the proven engine (MSSQL only, no model/vectors).
// Screens: Repository Overview, Symbol Explorer, Dependency Explorer, Impact Analysis, Retrieval Search.
// All queries are indexed/bounded; pages always load.
public sealed class GraphController : SecuredController
{
    private readonly AppDbContext _db;
    private readonly IStructuralRetrievalService _retrieval;

    public GraphController(AppDbContext db, IStructuralRetrievalService retrieval,
        ICurrentUserService me, IAccessControlService access, IAuditTrailService audit)
        : base(me, access, audit) { _db = db; _retrieval = retrieval; }

    // Project picker — only projects the user may access are listed.
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var accessible = CurrentUser is { } u ? await Access.AccessibleProjectIdsAsync(u, ct) : new HashSet<int>();
        var projects = await _db.Projects.Where(p => accessible.Contains(p.Id)).OrderBy(p => p.Name).ToListAsync(ct);
        var rows = new List<ProjectRow>();
        foreach (var p in projects)
        {
            var symbols = await _db.CodeSymbols.CountAsync(s => s.ProjectId == p.Id, ct);
            if (symbols == 0) continue; // only structural projects
            var edges = await _db.CodeEdges.CountAsync(e => e.ProjectId == p.Id, ct);
            rows.Add(new ProjectRow(p.Id, p.Name, p.Code, symbols, edges));
        }
        return View(rows);
    }

    // Repository Overview — what the engine understood about one project.
    public async Task<IActionResult> Project(int id, CancellationToken ct)
    {
        if (await RequireProjectAsync(id, "view project overview", ct) is { } denied) return denied;
        await AuditAsync(AuditEventType.ProjectViewed, "Viewed repository overview", "Project", id.ToString(), id, ct: ct);
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null) return RedirectToAction(nameof(Index));

        var kinds = await _db.CodeSymbols.Where(s => s.ProjectId == id).Select(s => s.Kind).ToListAsync(ct);
        var rels = await _db.CodeEdges.Where(e => e.ProjectId == id).Select(e => e.RelationType).ToListAsync(ct);
        var langs = await _db.CodeSymbols.Where(s => s.ProjectId == id && s.DetectedLanguage != null)
            .Select(s => s.DetectedLanguage!).ToListAsync(ct);

        // Top-connected = most depended-upon (highest incoming edge count).
        var incoming = await _db.CodeEdges.Where(e => e.ProjectId == id).Select(e => e.ToSymbolId).ToListAsync(ct);
        var topIds = incoming.GroupBy(x => x).OrderByDescending(g => g.Count()).Take(10)
            .Select(g => new { Id = g.Key, Count = g.Count() }).ToList();
        var topSyms = await _db.CodeSymbols.Where(s => topIds.Select(t => t.Id).Contains(s.Id))
            .Select(s => new { s.Id, s.FullName, s.Kind }).ToListAsync(ct);
        var top = topIds.Select(t => {
            var s = topSyms.First(x => x.Id == t.Id);
            return new TopSymbol(t.Id, s.FullName, GraphLabels.Kind(s.Kind), t.Count);
        }).ToList();

        var vm = new OverviewVm(project, kinds.Count, rels.Count,
            await _db.CodeSymbolReferences.CountAsync(r => r.ProjectId == id, ct),
            kinds.GroupBy(k => k).ToDictionary(g => GraphLabels.Kind(g.Key), g => g.Count()),
            rels.GroupBy(r => r).ToDictionary(g => g.Key.ToString(), g => g.Count()),
            langs.GroupBy(l => l).ToDictionary(g => g.Key, g => g.Count()),
            top);
        return View(vm);
    }

    // Symbol Explorer — exact-identifier lexical search.
    public async Task<IActionResult> Symbols(int projectId, string? q, CancellationToken ct)
    {
        if (await RequireProjectAsync(projectId, "search symbols", ct) is { } denied) return denied;
        if (!string.IsNullOrWhiteSpace(q)) await AuditAsync(AuditEventType.SymbolQueried, "Symbol search", "Query", q, projectId, ct: ct);
        ViewBag.ProjectId = projectId;
        ViewBag.Query = q;
        ViewBag.Project = (await _db.Projects.FindAsync(new object?[] { projectId }, ct))?.Name;
        var hits = string.IsNullOrWhiteSpace(q)
            ? new List<SymbolHit>()
            : (await _retrieval.FindByIdentifierAsync(projectId, q!, 50, ct)).ToList();
        return View(hits);
    }

    // Dependency Explorer — a symbol's dependents (who depends on it) and dependencies (what it touches).
    public async Task<IActionResult> Symbol(int projectId, int id, CancellationToken ct)
    {
        if (await RequireProjectAsync(projectId, "view dependencies", ct) is { } denied) return denied;
        // Scope the symbol to the authorized project — a symbol id from another project must not leak via a
        // granted projectId (IDOR guard).
        var sym = await _db.CodeSymbols.FirstOrDefaultAsync(s => s.Id == id && s.ProjectId == projectId, ct);
        if (sym is null) return RedirectToAction(nameof(Project), new { id = projectId });
        await AuditAsync(AuditEventType.DependencyViewed, "Viewed dependencies", "Symbol", sym.FullName, projectId, ct: ct);
        var dependents = await _retrieval.DependentsOfAsync(projectId, sym.FullName, ct);
        var dependencies = await _retrieval.DependenciesOfAsync(projectId, sym.FullName, ct);
        return View(new SymbolVm(projectId, sym, dependents, dependencies));
    }

    // Impact Analysis — transitive blast radius of changing a target.
    public async Task<IActionResult> Impact(int projectId, string? q, CancellationToken ct)
    {
        if (await RequireProjectAsync(projectId, "impact analysis", ct) is { } denied) return denied;
        if (!string.IsNullOrWhiteSpace(q)) await AuditAsync(AuditEventType.ImpactQueried, "Impact analysis", "Query", q, projectId, ct: ct);
        ViewBag.ProjectId = projectId;
        ViewBag.Query = q;
        ViewBag.Project = (await _db.Projects.FindAsync(new object?[] { projectId }, ct))?.Name;
        ImpactResult? impact = string.IsNullOrWhiteSpace(q) ? null : await _retrieval.ImpactOfAsync(projectId, q!, 4, 250, ct);
        return View(impact);
    }

    // Deep-link by identifier (used by clickable Proof-of-Vision examples): resolve the symbol and land on
    // its Dependency Explorer; fall back to Search when ambiguous/not found.
    public async Task<IActionResult> Lookup(int projectId, string? q, CancellationToken ct)
    {
        if (await RequireProjectAsync(projectId, "lookup", ct) is { } denied) return denied;
        if (!string.IsNullOrWhiteSpace(q))
        {
            var hits = await _retrieval.FindByIdentifierAsync(projectId, q!, 1, ct);
            if (hits.Count > 0) return RedirectToAction(nameof(Symbol), new { projectId, id = hits[0].Id });
        }
        return RedirectToAction(nameof(Search), new { projectId, q });
    }

    // Retrieval Search — unified cited lookup (the Proof-of-Vision query box).
    public async Task<IActionResult> Search(int projectId, string? q, CancellationToken ct)
    {
        if (await RequireProjectAsync(projectId, "search", ct) is { } denied) return denied;
        if (!string.IsNullOrWhiteSpace(q)) await AuditAsync(AuditEventType.SymbolQueried, "Retrieval search", "Query", q, projectId, ct: ct);
        ViewBag.ProjectId = projectId;
        ViewBag.Query = q;
        ViewBag.Project = (await _db.Projects.FindAsync(new object?[] { projectId }, ct))?.Name;
        var hits = string.IsNullOrWhiteSpace(q)
            ? new List<SymbolHit>()
            : (await _retrieval.FindByIdentifierAsync(projectId, q!, 25, ct)).ToList();
        return View(hits);
    }

    public record ProjectRow(int Id, string Name, string Code, int Symbols, int Edges);
    public record TopSymbol(int Id, string FullName, string Kind, int IncomingCount);
    public record OverviewVm(Project Project, int Symbols, int Edges, int References,
        Dictionary<string, int> ByKind, Dictionary<string, int> ByRelation, Dictionary<string, int> ByLanguage,
        List<TopSymbol> Top);
    public record SymbolVm(int ProjectId, CodeSymbol Symbol,
        IReadOnlyList<GraphNeighbor> Dependents, IReadOnlyList<GraphNeighbor> Dependencies);
}

// Friendly labels/icons for symbol kinds and relations (presentation only).
public static class GraphLabels
{
    public static string Kind(CodeSymbolKind k) => k switch
    {
        CodeSymbolKind.Namespace => "Namespace", CodeSymbolKind.Class => "Class",
        CodeSymbolKind.Interface => "Interface", CodeSymbolKind.Struct => "Struct",
        CodeSymbolKind.Record => "Record", CodeSymbolKind.Enum => "Enum", CodeSymbolKind.Delegate => "Delegate",
        CodeSymbolKind.Method => "Method", CodeSymbolKind.Property => "Property", CodeSymbolKind.Field => "Field",
        CodeSymbolKind.Constructor => "Constructor", CodeSymbolKind.Event => "Event",
        CodeSymbolKind.Table => "Table", CodeSymbolKind.Column => "Column", CodeSymbolKind.View => "View",
        CodeSymbolKind.StoredProcedure => "Procedure", CodeSymbolKind.SqlFunction => "Function",
        CodeSymbolKind.Trigger => "Trigger", CodeSymbolKind.Constraint => "Constraint",
        CodeSymbolKind.ForeignKey => "Foreign Key", CodeSymbolKind.Index => "Index",
        _ => k.ToString()
    };

    public static string KindIcon(CodeSymbolKind k) => k switch
    {
        CodeSymbolKind.Interface => "bi-bezier2", CodeSymbolKind.Class => "bi-box",
        CodeSymbolKind.Table => "bi-table", CodeSymbolKind.Column => "bi-layout-three-columns",
        CodeSymbolKind.View => "bi-eye", CodeSymbolKind.StoredProcedure => "bi-gear",
        CodeSymbolKind.SqlFunction => "bi-gear", CodeSymbolKind.ForeignKey => "bi-key",
        CodeSymbolKind.Method => "bi-lightning", CodeSymbolKind.Namespace => "bi-folder2",
        _ => "bi-dot"
    };

    public static string RelationBadge(RelationType r) => r switch
    {
        RelationType.Implements => "text-bg-success", RelationType.Inherits => "text-bg-primary",
        RelationType.DependsOn => "text-bg-warning", RelationType.References => "text-bg-secondary",
        RelationType.PartOf => "text-bg-light", _ => "text-bg-light"
    };
}
