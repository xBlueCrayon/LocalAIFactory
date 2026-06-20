using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Options;
using LocalAIFactory.Rag.Approval;
using LocalAIFactory.Rag.Chunking;
using LocalAIFactory.Rag.Context;
using LocalAIFactory.Rag.Embeddings;
using LocalAIFactory.Rag.Indexing;
using LocalAIFactory.Rag.Search;
using LocalAIFactory.Rag.Vector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LocalAIFactory.Rag;

public static class DependencyInjection
{
    public static IServiceCollection AddLocalAIFactoryRag(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<OllamaOptions>(config.GetSection("Ollama"));
        services.Configure<QdrantOptions>(config.GetSection("Qdrant"));
        services.Configure<RagOptions>(config.GetSection("Rag"));

        services.AddHttpClient();

        // Phase 1.2: background-updated health snapshot (read by the dashboard with no I/O).
        services.AddSingleton<IServiceHealthCache, Health.ServiceHealthCache>();

        services.AddScoped<IChunkingService, ChunkingService>();
        services.AddScoped<IEmbeddingProvider, OllamaEmbeddingProvider>();
        services.AddScoped<IEmbeddingProvider, OpenAiEmbeddingProvider>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddScoped<IVectorStore, QdrantVectorStore>();
        services.AddScoped<KeywordSearchService>();
        services.AddScoped<IKnowledgeSearchService, KnowledgeSearchService>();
        services.AddScoped<IKnowledgeIndexer, KnowledgeIndexer>();
        services.AddScoped<IRagContextBuilder, RagContextBuilder>();
        services.AddScoped<IApprovalService, ApprovalService>();

        // KE-011: deterministic structural retrieval (MSSQL-only; no vectors/model required).
        services.AddScoped<IStructuralRetrievalService, Retrieval.StructuralRetrievalService>();

        return services;
    }
}
