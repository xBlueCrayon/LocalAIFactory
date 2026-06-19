namespace LocalAIFactory.Core.Options;

public sealed class OllamaOptions
{
    // Phase 1.2: set false on machines with no local model host (Minimal Mode). Prevents all Ollama calls.
    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string DefaultModel { get; set; } = "qwen2.5-coder:14b";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
}

public sealed class QdrantOptions
{
    // Phase 1.2 hotfix: fail-safe default is OFF. If this section is missing or mis-bound, the app makes
    // NO Qdrant calls. Set to true (and Rag.UseVectorSearch=true) only when Qdrant is actually available.
    public bool Enabled { get; set; } = false;
    public string BaseUrl { get; set; } = "http://localhost:6333";
    public string? ApiKey { get; set; }
    public string Collection { get; set; } = "localaifactory";
    public int VectorSize { get; set; } = 768;
}

public sealed class RagOptions
{
    // Phase 1.2 hotfix: fail-safe default is OFF (keyword search only). Vector search is opt-in and
    // additionally requires Qdrant.Enabled=true. Either flag off => no embedding/Qdrant calls anywhere.
    public bool UseVectorSearch { get; set; } = false;
    public string EmbeddingProvider { get; set; } = "Ollama"; // Ollama | OpenAI
    public int MaxChunkChars { get; set; } = 1200;
    public int ChunkOverlap { get; set; } = 150;
    public int TopK { get; set; } = 6;
    public int MaxContextChars { get; set; } = 8000;
    public int MaxHistoryMessages { get; set; } = 10;
}

public sealed class WorkspacesOptions
{
    public string Root { get; set; } = @"C:\LocalAIFactory\workspaces";
    public int MaxTextFileBytes { get; set; } = 1_500_000;
}
