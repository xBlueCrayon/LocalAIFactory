namespace LocalAIFactory.Core.Dtos;

public enum DiffOp { Unchanged = 0, Added = 1, Removed = 2 }

public sealed class DiffLine
{
    public DiffOp Op { get; set; }
    public string Text { get; set; } = "";
    public DiffLine() { }
    public DiffLine(DiffOp op, string text) { Op = op; Text = text; }
}

public sealed class DiffResult
{
    public List<DiffLine> Lines { get; set; } = new();
    public int Added { get; set; }
    public int Removed { get; set; }
}
