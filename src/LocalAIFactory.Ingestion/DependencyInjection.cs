using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Ingestion.Cheque;
using LocalAIFactory.Ingestion.Classification;
using LocalAIFactory.Ingestion.Documents;
using LocalAIFactory.Ingestion.Graph;
using LocalAIFactory.Ingestion.Imports;
using LocalAIFactory.Ingestion.Pipeline;
using LocalAIFactory.Ingestion.Profiling;
using LocalAIFactory.Ingestion.Queue;
using LocalAIFactory.Ingestion.Symbols;
using Microsoft.Extensions.DependencyInjection;

namespace LocalAIFactory.Ingestion;

public static class DependencyInjection
{
    public static IServiceCollection AddLocalAIFactoryIngestion(this IServiceCollection services)
    {
        services.AddSingleton<IFileClassifier, FileClassifier>();
        services.AddSingleton<IIngestionQueue, IngestionQueue>();

        services.AddScoped<IProjectIngestionService, ProjectIngestionService>();
        services.AddScoped<IProjectProfileService, ProjectProfileService>();
        services.AddScoped<IKnowledgeGraphService, KnowledgeGraphService>();
        services.AddScoped<IFileImportService, FileImportService>();
        services.AddScoped<IChatGptImportService, ChatGptImportService>();
        // R2-ACC-20X: deterministic chat-export -> knowledge-proposal extractor (proposals only, never auto-approved).
        services.AddSingleton<IChatKnowledgeExtractor, ChatKnowledgeExtractor>();

        // KE-008: pluggable code-symbol extraction (C# + Python now; VB.NET/Razor register here later).
        services.AddSingleton<ICodeSymbolExtractor, CSharpSymbolExtractor>();
        services.AddSingleton<ICodeSymbolExtractor, PythonSymbolExtractor>(); // R2-ACC-CAP3
        services.AddSingleton<ICodeSymbolExtractorRouter, CodeSymbolExtractorRouter>();
        services.AddScoped<ICodeSymbolStore, CodeSymbolStore>();

        // KE-009: pluggable T-SQL schema extraction (T-SQL now; PL/pgSQL / PL/SQL register here later).
        services.AddSingleton<ISqlSchemaExtractor, TSqlSchemaExtractor>();
        services.AddSingleton<ISqlSchemaExtractorRouter, SqlSchemaExtractorRouter>();
        services.AddScoped<ISchemaSymbolStore, SchemaSymbolStore>();

        // KE-010: deterministic structural graph builder (resolves references into CodeEdges).
        services.AddScoped<ICodeGraphBuilder, CodeGraphBuilder>();

        // R2-ACC-CAP4/CAP5: document-intelligence + cheque-risk prototypes (deterministic, dependency-light).
        services.AddSingleton<IPdfAnalyzer, PdfAnalyzer>();
        services.AddSingleton<IExtractiveSummarizer, ExtractiveSummarizer>();
        services.AddSingleton<IChequeRiskEngine, ChequeRiskEngine>();

        // KE-012: project-scoped consolidation (re-extract from raw, prune orphans, converge the graph).
        services.AddScoped<IStructuralConsolidationService, Maintenance.StructuralConsolidationService>();

        // R2-P0A: per-import coverage / gap reporting (enterprise honesty).
        services.AddScoped<IImportCoverageService, Coverage.ImportCoverageService>();

        return services;
    }
}
