using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Ingestion.Classification;

public sealed class FileClassifier : IFileClassifier
{
    public FileClass Classify(string relativePath)
    {
        var name = Path.GetFileName(relativePath).ToLowerInvariant();
        var ext = Path.GetExtension(name);

        if (name.StartsWith("readme")) return FileClass.Readme;
        if ((name.Contains("deploy") || name.Contains("iis") || name.Contains("install")) && (ext is ".txt" or ".md"))
            return FileClass.DeploymentNote;

        return ext switch
        {
            ".cs" or ".vb" or ".fs" or ".py" or ".js" or ".ts" or ".go" or ".rb" or ".php"
                or ".cpp" or ".cc" or ".c" or ".h" or ".hpp" or ".java" or ".kt" or ".scala" => FileClass.SourceCode,
            ".sql" => FileClass.SqlScript,
            ".razor" or ".cshtml" or ".aspx" or ".ascx" or ".asmx" or ".master" or ".ashx" => FileClass.RazorView,
            ".json" => FileClass.JsonFile,
            ".xml" or ".xaml" or ".xsd" or ".wsdl" or ".resx" => FileClass.XmlFile,
            ".config" or ".props" or ".targets" or ".csproj" or ".vbproj" or ".fsproj" or ".sln"
                or ".yml" or ".yaml" or ".ini" or ".editorconfig" or ".toml" => FileClass.Configuration,
            ".md" or ".rst" or ".adoc" or ".txt" => FileClass.Documentation,
            ".ps1" or ".psm1" or ".psd1" => FileClass.PowerShell,
            ".bat" or ".cmd" => FileClass.BatchFile,
            ".log" => FileClass.BuildLog,
            ".dll" or ".exe" or ".pdb" or ".zip" or ".7z" or ".rar" or ".gz" or ".png" or ".jpg" or ".jpeg"
                or ".gif" or ".bmp" or ".ico" or ".pdf" or ".docx" or ".xlsx" or ".pptx" or ".bin"
                or ".dat" or ".nupkg" or ".snk" or ".cache" or ".woff" or ".woff2" or ".ttf" or ".eot" => FileClass.Binary,
            _ => FileClass.Unknown
        };
    }

    public bool IsTextual(FileClass fileClass, string extension) => fileClass != FileClass.Binary;

    public SourceType ToSourceType(FileClass fileClass) => fileClass switch
    {
        FileClass.SourceCode => SourceType.SourceCode,
        FileClass.SqlScript => SourceType.SqlScript,
        FileClass.RazorView => SourceType.SourceCode,
        FileClass.Configuration => SourceType.Configuration,
        FileClass.XmlFile => SourceType.Configuration,
        FileClass.JsonFile => SourceType.Configuration,
        FileClass.Documentation => SourceType.Documentation,
        FileClass.Readme => SourceType.Readme,
        FileClass.BuildLog => SourceType.BuildLog,
        FileClass.DeploymentNote => SourceType.DeploymentNote,
        FileClass.PowerShell => SourceType.SourceCode,
        FileClass.BatchFile => SourceType.SourceCode,
        _ => SourceType.Documentation
    };
}
