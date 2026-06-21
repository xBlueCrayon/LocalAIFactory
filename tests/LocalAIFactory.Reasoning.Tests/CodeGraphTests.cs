using LocalAIFactory.Core.Enums;
using LocalAIFactory.Reasoning.CodeGraph;
using Xunit;

namespace LocalAIFactory.Reasoning.Tests;

public class CodeGraphTests
{
    private static CodeGraphModel Build() => new CodeGraphBuilder().Build(SampleCode.Files());

    [Fact] public void Builds_nodes_for_all_types()
    {
        var g = Build();
        foreach (var t in new[] { "AccountController", "UserAuthService", "AuditService", "ErpDbContext", "AppUser", "AuditEvent", "AuthTests" })
            Assert.NotEmpty(g.FindByName(t));
    }

    [Fact] public void Tags_controllers()
        => Assert.Contains(Build().WithRole("controller"), n => n.Name == "AccountController");

    [Fact] public void Tags_services()
    {
        var svc = Build().WithRole("service").Select(n => n.Name).ToList();
        Assert.Contains("UserAuthService", svc);
        Assert.Contains("AuditService", svc);
    }

    [Fact] public void Tags_dbcontext_via_inheritance()
        => Assert.Contains(Build().WithRole("dbcontext"), n => n.Name == "ErpDbContext");

    [Fact] public void Tags_entities_via_base_type()
        => Assert.Contains(Build().WithRole("entity"), n => n.Name == "AppUser");

    [Fact] public void Tags_test_classes()
        => Assert.Contains(Build().WithRole("test"), n => n.Name == "AuthTests");

    [Fact] public void Finds_methods_inside_a_type()
    {
        var g = Build();
        Assert.Contains(g.FindByName("Login"), n => n.Kind == CodeSymbolKind.Method);
        Assert.Contains(g.FindByName("Authenticate"), n => n.Kind == CodeSymbolKind.Method);
    }

    [Fact] public void Containment_edges_link_type_to_members()
    {
        var g = Build();
        var ctrl = g.FindByName("AccountController").First(n => n.Kind == CodeSymbolKind.Class);
        var contained = g.OutgoingFrom(ctrl.Id).Where(e => e.Kind == CodeEdgeKind.Contains).ToList();
        Assert.NotEmpty(contained);
    }

    [Fact] public void Controller_references_its_service_dependency()
    {
        var g = Build();
        var ctrl = g.FindByName("AccountController").First(n => n.Kind == CodeSymbolKind.Class);
        var svc = g.FindByName("UserAuthService").First(n => n.Kind == CodeSymbolKind.Class);
        Assert.Contains(g.OutgoingFrom(ctrl.Id), e => e.ToId == svc.Id);
    }

    [Fact] public void Service_uses_dbcontext_edge()
    {
        var g = Build();
        var svc = g.FindByName("UserAuthService").First(n => n.Kind == CodeSymbolKind.Class);
        var db = g.FindByName("ErpDbContext").First(n => n.Kind == CodeSymbolKind.Class);
        Assert.Contains(g.OutgoingFrom(svc.Id), e => e.ToId == db.Id && e.Kind == CodeEdgeKind.UsesDbSet);
    }

    [Fact] public void Dbcontext_inherits_edge()
    {
        var g = Build();
        var db = g.FindByName("ErpDbContext").First(n => n.Kind == CodeSymbolKind.Class);
        Assert.Contains(g.OutgoingFrom(db.Id), e => e.Kind == CodeEdgeKind.Inherits);
    }

    [Fact] public void Entity_inherits_entitybase()
    {
        var g = Build();
        var u = g.FindByName("AppUser").First(n => n.Kind == CodeSymbolKind.Class);
        Assert.Contains(g.OutgoingFrom(u.Id), e => e.Kind == CodeEdgeKind.Inherits && e.Detail == "EntityBase");
    }

    [Fact] public void Referencers_of_service_include_controller_and_test()
    {
        var g = Build();
        var svc = g.FindByName("UserAuthService").First(n => n.Kind == CodeSymbolKind.Class);
        var names = g.ReferencersOf(svc.Id).Select(n => n.Name).ToList();
        Assert.Contains("AccountController", names);
        Assert.Contains("AuthTests", names);
    }

    [Fact] public void Impact_of_dbcontext_reaches_the_controller()
    {
        var g = Build();
        var db = g.FindByName("ErpDbContext").First(n => n.Kind == CodeSymbolKind.Class);
        var impact = g.ImpactOf(db.Id).Select(n => n.Name).ToList();
        // ErpDbContext <- UserAuthService <- AccountController
        Assert.Contains("UserAuthService", impact);
        Assert.Contains("AccountController", impact);
    }

    [Fact] public void TestCovers_edge_links_test_to_covered_type()
    {
        var g = Build();
        var test = g.FindByName("AuthTests").First(n => n.Kind == CodeSymbolKind.Class);
        Assert.Contains(g.OutgoingFrom(test.Id), e => e.Kind == CodeEdgeKind.TestCovers);
    }

    [Fact] public void Search_is_substring_and_case_insensitive()
        => Assert.Contains(Build().Search("authservice"), n => n.Name == "UserAuthService");

    [Fact] public void Empty_input_yields_empty_graph()
        => Assert.Empty(new CodeGraphBuilder().Build(Array.Empty<(string, string)>()).Nodes);

    [Fact] public void Non_cs_files_are_ignored()
    {
        var g = new CodeGraphBuilder().Build(new[] { ("a.txt", "class X {}"), ("b.md", "# hi") });
        Assert.Empty(g.Nodes);
    }

    [Fact] public void Malformed_code_still_yields_declared_symbols()
    {
        var g = new CodeGraphBuilder().Build(new[] { ("X.cs", "public class Broken { public void M( {") });
        Assert.NotEmpty(g.FindByName("Broken"));
    }

    [Fact] public void AddEdge_never_dangles()
    {
        var g = new CodeGraphModel();
        g.AddEdge(new CodeEdge("missing:a", "missing:b", CodeEdgeKind.Calls));
        Assert.Empty(g.Edges);
    }

    [Fact] public void Nodes_are_idempotent_on_id()
    {
        var g = new CodeGraphModel();
        var a = g.AddNode(new CodeNode { Id = "x", Kind = CodeSymbolKind.Class, Name = "X", FullName = "X" });
        var b = g.AddNode(new CodeNode { Id = "x", Kind = CodeSymbolKind.Class, Name = "X", FullName = "X" });
        Assert.Same(a, b);
        Assert.Single(g.Nodes);
    }
}
