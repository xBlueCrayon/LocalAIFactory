using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (!await db.Projects.AnyAsync(ct))
        {
            db.Projects.AddRange(
                new Project { Name = "BDM", Code = "BDM", Description = "Bulk Direct Mandate / mandate management middleware (first debugging target)." },
                new Project { Name = "MCIB", Code = "MCIB", Description = "Credit information bureau integration." },
                new Project { Name = "ChequeXpert", Code = "CHEQUEXPERT", Description = "Cheque processing and clearing (Parascript)." },
                new Project { Name = "ETAMS", Code = "ETAMS", Description = "Enterprise tracking and management system." },
                new Project { Name = "SQL Library", Code = "SQL_LIB", Description = "Reusable MSSQL / PostgreSQL scripts." },
                new Project { Name = "Metabase Library", Code = "METABASE_LIB", Description = "Reusable Metabase SQL and dashboards." },
                new Project { Name = "Deployment Library", Code = "DEPLOY_LIB", Description = "IIS, Windows Service and deployment knowledge." },
                new Project { Name = "Global Engineering Rules", Code = "GLOBAL", IsGlobal = true, Description = "Cross-project engineering standards." });
            await db.SaveChangesAsync(ct);
        }

        if (!await db.PromptTemplates.AnyAsync(ct))
        {
            db.PromptTemplates.Add(new PromptTemplate
            {
                Name = "Default Chat System",
                Kind = "ChatSystem",
                IsDefault = true,
                Content =
                    "You are LocalAIFactory, a specialized senior software engineering assistant for .NET, ASP.NET MVC/Core, C#, "
                    + "MSSQL, Entity Framework Core, Python, PostgreSQL/Metabase SQL, IIS deployment and banking/financial middleware.\n"
                    + "Rules:\n"
                    + "1. PRIORITISE the APPROVED project knowledge, APPROVED code and project-specific BUSINESS RULES provided in context.\n"
                    + "2. Project-specific knowledge overrides generic knowledge. Reuse approved code instead of reinventing it.\n"
                    + "3. Prefer C#, MSSQL and EF Core unless told otherwise. Produce clean, deployable, maintainable code.\n"
                    + "4. Make minimal assumptions. If a required business rule is unknown, state the assumption explicitly.\n"
                    + "5. Be safe by default. Never propose destructive operations without a clear warning.\n"
                    + "6. When unsure, say so and point to which approved knowledge would be needed."
            });
            await db.SaveChangesAsync(ct);
        }

        ModelConfiguration model;
        if (!await db.ModelConfigurations.AnyAsync(ct))
        {
            model = new ModelConfiguration
            {
                Name = "Local Ollama (default)",
                Provider = ModelProvider.Ollama,
                ModelName = "qwen2.5-coder:14b",
                BaseUrl = "http://localhost:11434",
                EmbeddingModel = "nomic-embed-text",
                Temperature = 0.2,
                MaxTokens = 2048,
                ContextWindowHint = 8192,
                IsEnabled = true,
                IsDefault = true
            };
            db.ModelConfigurations.Add(model);
            await db.SaveChangesAsync(ct);
        }
        else
        {
            model = await db.ModelConfigurations.OrderByDescending(m => m.IsDefault).FirstAsync(ct);
        }

        if (!await db.TaskProfiles.AnyAsync(ct))
        {
            (TaskType type, string name, double temp, int max, bool rag, bool enabled)[] defs =
            {
                (TaskType.Chat, "Chat", 0.2, 2048, true, true),
                (TaskType.ProjectImport, "Project Import", 0.1, 2048, true, true),
                (TaskType.ProjectSummarization, "Project Summarization", 0.2, 3072, true, true),
                (TaskType.BusinessRuleExtraction, "Business Rule Extraction", 0.1, 2048, true, true),
                (TaskType.CodeGeneration, "Code Generation", 0.1, 4096, true, true),
                (TaskType.CodeFix, "Code Fix", 0.1, 4096, true, true),
                (TaskType.SqlAnalysis, "SQL Analysis", 0.1, 3072, true, true),
                (TaskType.MetabaseAnalysis, "Metabase Analysis", 0.1, 3072, true, true),
                (TaskType.ArchitectureAnalysis, "Architecture Analysis", 0.2, 4096, true, true),
                (TaskType.DeploymentAnalysis, "Deployment Analysis", 0.2, 3072, true, true),
                (TaskType.Embedding, "Embedding", 0.0, 512, false, true),
                // Phase 2 workspace task profiles: seeded but disabled until on-disk editing is turned on.
                (TaskType.CodeModification, "Code Modification", 0.1, 4096, true, false),
                (TaskType.BuildAnalysis, "Build Analysis", 0.1, 3072, true, false),
                (TaskType.WorkspacePlanning, "Workspace Planning", 0.2, 4096, true, false)
            };

            foreach (var d in defs)
            {
                db.TaskProfiles.Add(new TaskProfile
                {
                    TaskType = d.type,
                    Name = d.name,
                    PrimaryModelId = model.Id,            // single-model default: everything points at the one local model
                    ValidationEnabled = false,
                    ComparisonEnabled = false,
                    UseKnowledgeBase = d.rag,
                    UseProjectMemory = d.rag,
                    UseKnowledgeGraph = d.rag,
                    Temperature = d.temp,
                    MaxTokens = d.max,
                    ContextWindowHint = 8192,
                    LocalOnly = false,
                    RequireApprovalBeforeCloudUse = true,
                    IsEnabled = d.enabled
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.SystemSettings.AnyAsync(ct))
        {
            db.SystemSettings.AddRange(
                new SystemSetting { Key = "App.Initialized", Value = "true" },
                new SystemSetting { Key = "App.Version", Value = "1.0.0-phase1" });
            await db.SaveChangesAsync(ct);
        }

        // KE-005: starter domain taxonomy. Seeded additively by Code so user-added domains are preserved
        // and new starter domains are added on upgrade. The taxonomy is editable and user-managed.
        var starterDomains = new (string Code, string Name, string Description)[]
        {
            ("GENERAL", "General", "Cross-cutting or uncategorized knowledge."),
            ("BDM", "BDM", "Bulk Direct Mandate / mandate management middleware."),
            ("MCIB", "MCIB", "Credit information bureau integration."),
            ("CHEQUEXPERT", "ChequeXpert", "Cheque processing and clearing (Parascript)."),
            ("ETAMS", "ETAMS", "Enterprise tracking and management system.")
        };
        var addedDomain = false;
        foreach (var d in starterDomains)
        {
            if (!await db.KnowledgeDomains.AnyAsync(x => x.Code == d.Code, ct))
            {
                db.KnowledgeDomains.Add(new KnowledgeDomain { Code = d.Code, Name = d.Name, Description = d.Description });
                addedDomain = true;
            }
        }
        if (addedDomain) await db.SaveChangesAsync(ct);
    }
}
