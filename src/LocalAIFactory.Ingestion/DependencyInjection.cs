using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Ingestion.Classification;
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

        // KE-008: pluggable code-symbol extraction (C# now; VB.NET/Razor register here later).
        services.AddSingleton<ICodeSymbolExtractor, CSharpSymbolExtractor>();
        services.AddSingleton<ICodeSymbolExtractorRouter, CodeSymbolExtractorRouter>();
        services.AddScoped<ICodeSymbolStore, CodeSymbolStore>();

        return services;
    }
}
