using LocalAIFactory.PythonBridge;
using Xunit;

namespace LocalAIFactory.PythonBridge.Tests;

public class PythonBridgeTests
{
    [Fact] public void Approved_entrypoints_are_the_fixed_safe_set()
    {
        var r = new SafePythonWorkerRunner();
        foreach (var e in new[] { "code-mine", "pattern-mine", "doc-extract", "web-scrape", "embed-text", "rerank", "build-dataset", "graph-enrich", "extract-knowledge" })
            Assert.Contains(e, r.ApprovedEntrypoints);
        Assert.Equal(9, r.ApprovedEntrypoints.Count);
    }

    [Theory]
    [InlineData("code-mine", true)]
    [InlineData("web-scrape", true)]
    [InlineData("rm-rf", false)]
    [InlineData("arbitrary-script", false)]
    [InlineData("", false)]
    public void Only_approved_entrypoints_pass_the_allowlist(string entry, bool approved)
        => Assert.Equal(approved, SafePythonWorkerRunner.IsApproved(entry));

    [Fact] public async Task Unapproved_entrypoint_is_rejected_without_throwing()
    {
        var r = new SafePythonWorkerRunner();
        var res = await r.RunAsync(new PythonRequest("delete-everything"));
        Assert.False(res.Ok);
        Assert.Contains("not approved", res.Error);
    }

    [Fact] public async Task Missing_python_degrades_gracefully_not_throws()
    {
        // Point at a non-existent interpreter to force the "Python absent" path deterministically.
        var r = new SafePythonWorkerRunner(pythonExe: "definitely-not-a-real-python-exe-xyz");
        Assert.False(r.IsAvailable);
        var res = await r.RunAsync(new PythonRequest("code-mine", new { files = System.Array.Empty<object>() }));
        Assert.False(res.Available);
        Assert.False(res.Ok); // unavailable, but no exception
    }

    [Fact] public async Task Run_is_logged()
    {
        var r = new SafePythonWorkerRunner(pythonExe: "definitely-not-a-real-python-exe-xyz");
        await r.RunAsync(new PythonRequest("code-mine"));
        await r.RunAsync(new PythonRequest("not-approved"));
        Assert.Equal(2, r.Log.Count);
    }
}
