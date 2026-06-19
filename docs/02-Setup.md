# 02 — Setup

## Prerequisites

- .NET 10 SDK
- SQL Server: LocalDB (with Visual Studio) or SQL Server Express (recommended), or any reachable MSSQL
- Optional: Ollama (local AI), Docker (Qdrant)

## Clone, build, run

```powershell
git clone <your-repo-url> LocalAIFactory
cd LocalAIFactory
dotnet restore
dotnet build LocalAIFactory.sln -c Release
dotnet run --project src/LocalAIFactory.Web
```

The app applies migrations and seeds on startup. Browse to `http://localhost:5000`.

## Configure MSSQL

Edit `ConnectionStrings:DefaultConnection` in `src/LocalAIFactory.Web/appsettings.json`, or override
with the `ConnectionStrings__DefaultConnection` environment variable (preferred for credentials).

```
# LocalDB (default)
Server=(localdb)\MSSQLLocalDB;Database=LocalAIFactory;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True

# SQL Server Express (recommended local install)
Server=.\SQLEXPRESS;Database=LocalAIFactory;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True

# SQL auth
Server=localhost,1433;Database=LocalAIFactory;User Id=sa;Password=Your_Strong_Password;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Keep `MultipleActiveResultSets=true`. Apply migrations manually if you prefer:

```powershell
dotnet ef database update --project src/LocalAIFactory.Data --startup-project src/LocalAIFactory.Web
```

## Appsettings reference (shipped safe defaults)

| Section | Key | Default | Notes |
|---|---|---|---|
| ConnectionStrings | DefaultConnection | LocalDB | Swap for SQL Express / SQL auth |
| Ollama | Enabled | `true` | Optional; degrades gracefully if absent |
| Ollama | BaseUrl / DefaultModel / EmbeddingModel | localhost:11434 / qwen2.5-coder:14b / nomic-embed-text | |
| Qdrant | Enabled | `false` | Optional vector DB |
| Qdrant | BaseUrl / Collection / VectorSize | localhost:6333 / localaifactory / 768 | |
| Rag | UseVectorSearch | `false` | Vectors only when this AND Qdrant.Enabled are true |
| Workspaces | Root | `C:\LocalAIFactory\workspaces` | Code-modification sandbox root (Phase 2) |

## Optional: local AI and vectors

```powershell
./scripts/setup-ollama.ps1        # pulls qwen2.5-coder:14b + nomic-embed-text
./scripts/start-qdrant-docker.ps1 # runs Qdrant on :6333 (then set both vector flags true)
```

## Minimal (MSSQL-only) mode

Set `Ollama.Enabled=false`, `Qdrant.Enabled=false`, `Rag.UseVectorSearch=false`. All pages still load;
AI chat and vector search are simply unavailable until you re-enable them.
