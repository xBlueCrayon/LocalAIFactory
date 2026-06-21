using LocalAIFactory.Reasoning.Experience;
using Xunit;

namespace LocalAIFactory.Reasoning.Tests;

public class ExperienceTests
{
    private static ExperienceEntry Build(ExperienceType t, string title, string symptoms = "") =>
        new() { Type = t, Title = title, Symptoms = symptoms, RootCause = "rc", Fix = "fix", ReusableLesson = "lesson" };

    [Fact] public void Add_and_retrieve_all()
    {
        var m = new ExperienceMemory();
        m.Add(Build(ExperienceType.BugFix, "x"));
        Assert.Single(m.All);
    }

    [Fact] public void Records_a_failing_build()
    {
        var m = new ExperienceMemory();
        m.Add(Build(ExperienceType.BuildFailure, "CS1061 missing member", "does not contain a definition"));
        Assert.Single(m.OfType(ExperienceType.BuildFailure));
    }

    [Fact] public void FindSimilar_matches_on_keywords()
    {
        var m = new ExperienceMemory();
        m.Add(Build(ExperienceType.TestFailure, "Insufficient stock blocks issue", "insufficient material on hand requested"));
        m.Add(Build(ExperienceType.BugFix, "unrelated", "completely different words"));
        var hits = m.FindSimilar("insufficient material on hand");
        Assert.Equal("Insufficient stock blocks issue", hits.First().Title);
    }

    [Fact] public void FindSimilar_empty_query_returns_empty()
        => Assert.Empty(new ExperienceMemory().FindSimilar(""));

    [Fact] public void Promote_to_knowledge_sets_flag()
    {
        var m = new ExperienceMemory();
        var e = m.Add(Build(ExperienceType.BugFix, "x"));
        Assert.True(m.PromoteToKnowledge(e.Id, "uid-123"));
        Assert.True(e.PromotedToKnowledgePack);
        Assert.Contains("uid-123", e.RelatedKnowledgeIds);
    }

    [Fact] public void Promote_is_idempotent_no_duplicate()
    {
        var m = new ExperienceMemory();
        var e = m.Add(Build(ExperienceType.BugFix, "x"));
        Assert.True(m.PromoteToKnowledge(e.Id, "uid-1"));
        Assert.False(m.PromoteToKnowledge(e.Id, "uid-2")); // second promotion rejected
        Assert.Single(e.RelatedKnowledgeIds);
    }

    [Fact] public void Promote_unknown_id_returns_false()
        => Assert.False(new ExperienceMemory().PromoteToKnowledge("nope", "u"));

    [Fact] public void Link_code_node_is_deduplicated()
    {
        var m = new ExperienceMemory();
        var e = m.Add(Build(ExperienceType.BugFix, "x"));
        m.LinkCodeNode(e.Id, "1:Foo");
        m.LinkCodeNode(e.Id, "1:Foo");
        Assert.Single(e.RelatedCodeNodes);
    }

    [Fact] public void Json_roundtrip_preserves_entries()
    {
        var m = new ExperienceMemory();
        m.Add(Build(ExperienceType.SecurityFinding, "secret in config", "hardcoded"));
        var back = ExperienceMemory.FromJson(m.ToJson());
        Assert.Single(back.All);
        Assert.Equal("secret in config", back.All[0].Title);
    }

    [Fact] public void Corrupt_json_yields_empty_memory()
        => Assert.Empty(ExperienceMemory.FromJson("{not json").All);

    [Theory]
    [InlineData(ExperienceType.BuildFailure)]
    [InlineData(ExperienceType.TestFailure)]
    [InlineData(ExperienceType.PlaywrightFailure)]
    [InlineData(ExperienceType.SecurityFinding)]
    [InlineData(ExperienceType.GeneratorImprovement)]
    [InlineData(ExperienceType.RegressionPrevented)]
    public void OfType_filters(ExperienceType t)
    {
        var m = new ExperienceMemory();
        m.Add(Build(t, "x"));
        Assert.Single(m.OfType(t));
    }
}
