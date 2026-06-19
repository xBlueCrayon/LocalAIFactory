using System;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalAIFactory.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260101000000_InitialCreate")]
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModelConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ApiKeyEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Temperature = table.Column<double>(type: "float", nullable: false),
                    MaxTokens = table.Column<int>(type: "int", nullable: false),
                    ContextWindowHint = table.Column<int>(type: "int", nullable: false),
                    EmbeddingModel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromptTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Action = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskType = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PrimaryModelId = table.Column<int>(type: "int", nullable: true),
                    ValidationEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ValidationModelId = table.Column<int>(type: "int", nullable: true),
                    ComparisonEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ComparisonModelId = table.Column<int>(type: "int", nullable: true),
                    UseKnowledgeBase = table.Column<bool>(type: "bit", nullable: false),
                    UseProjectMemory = table.Column<bool>(type: "bit", nullable: false),
                    UseKnowledgeGraph = table.Column<bool>(type: "bit", nullable: false),
                    Temperature = table.Column<double>(type: "float", nullable: false),
                    MaxTokens = table.Column<int>(type: "int", nullable: false),
                    ContextWindowHint = table.Column<int>(type: "int", nullable: false),
                    LocalOnly = table.Column<bool>(type: "bit", nullable: false),
                    RequireApprovalBeforeCloudUse = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskProfiles", x => x.Id);
                    table.ForeignKey("FK_TaskProfiles_ModelConfigurations_PrimaryModelId", x => x.PrimaryModelId, "ModelConfigurations", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_TaskProfiles_ModelConfigurations_ValidationModelId", x => x.ValidationModelId, "ModelConfigurations", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_TaskProfiles_ModelConfigurations_ComparisonModelId", x => x.ComparisonModelId, "ModelConfigurations", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectSources", x => x.Id);
                    table.ForeignKey("FK_ProjectSources_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsDeprecated = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeItems", x => x.Id);
                    table.ForeignKey("FK_KnowledgeItems_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChatSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    ModelConfigurationId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatSessions", x => x.Id);
                    table.ForeignKey("FK_ChatSessions_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ChatSessions_ModelConfigurations_ModelConfigurationId", x => x.ModelConfigurationId, "ModelConfigurations", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovedCodeSnippets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Framework = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsReusable = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovedCodeSnippets", x => x.Id);
                    table.ForeignKey("FK_ApprovedCodeSnippets_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    ApprovedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessRules", x => x.Id);
                    table.ForeignKey("FK_BusinessRules_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    ChatSessionId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentTasks", x => x.Id);
                    table.ForeignKey("FK_AgentTasks_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportedConversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessageCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    KnowledgeItemId = table.Column<int>(type: "int", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedConversations", x => x.Id);
                    table.ForeignKey("FK_ImportedConversations_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IngestionJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    ExtractedRoot = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Phase = table.Column<int>(type: "int", nullable: false),
                    TotalFiles = table.Column<int>(type: "int", nullable: false),
                    ProcessedFiles = table.Column<int>(type: "int", nullable: false),
                    SkippedFiles = table.Column<int>(type: "int", nullable: false),
                    ChunkCount = table.Column<int>(type: "int", nullable: false),
                    EmbeddedCount = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionJobs", x => x.Id);
                    table.ForeignKey("FK_IngestionJobs_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StructuredJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedByModelConfigurationId = table.Column<int>(type: "int", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectProfiles", x => x.Id);
                    table.ForeignKey("FK_ProjectProfiles_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeEntities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    EntityType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SourceKnowledgeItemId = table.Column<int>(type: "int", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeEntities", x => x.Id);
                    table.ForeignKey("FK_KnowledgeEntities_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromptRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    ChatSessionId = table.Column<int>(type: "int", nullable: true),
                    TaskType = table.Column<int>(type: "int", nullable: false),
                    UserPrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetrievedContextJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptRuns", x => x.Id);
                    table.ForeignKey("FK_PromptRuns_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExtractedCodeBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    ImportedConversationId = table.Column<int>(type: "int", nullable: true),
                    SourceKnowledgeItemId = table.Column<int>(type: "int", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PromotedToApprovedCodeSnippetId = table.Column<int>(type: "int", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExtractedCodeBlocks", x => x.Id);
                    table.ForeignKey("FK_ExtractedCodeBlocks_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportedFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    IngestionJobId = table.Column<int>(type: "int", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Extension = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FileClass = table.Column<int>(type: "int", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RawText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StoredPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Skipped = table.Column<bool>(type: "bit", nullable: false),
                    SkipReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    KnowledgeItemId = table.Column<int>(type: "int", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedFiles", x => x.Id);
                    table.ForeignKey("FK_ImportedFiles_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ImportedFiles_IngestionJobs_IngestionJobId", x => x.IngestionJobId, "IngestionJobs", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeChunks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KnowledgeItemId = table.Column<int>(type: "int", nullable: false),
                    ChunkIndex = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokenCount = table.Column<int>(type: "int", nullable: false),
                    VectorId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmbeddingModel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeChunks", x => x.Id);
                    table.ForeignKey("FK_KnowledgeChunks_KnowledgeItems_KnowledgeItemId", x => x.KnowledgeItemId, "KnowledgeItems", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeItemTags",
                columns: table => new
                {
                    KnowledgeItemId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeItemTags", x => new { x.KnowledgeItemId, x.TagId });
                    table.ForeignKey("FK_KnowledgeItemTags_KnowledgeItems_KnowledgeItemId", x => x.KnowledgeItemId, "KnowledgeItems", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_KnowledgeItemTags_Tags_TagId", x => x.TagId, "Tags", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApprovedCodeSnippetTags",
                columns: table => new
                {
                    ApprovedCodeSnippetId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovedCodeSnippetTags", x => new { x.ApprovedCodeSnippetId, x.TagId });
                    table.ForeignKey("FK_ApprovedCodeSnippetTags_ApprovedCodeSnippets_ApprovedCodeSnippetId", x => x.ApprovedCodeSnippetId, "ApprovedCodeSnippets", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_ApprovedCodeSnippetTags_Tags_TagId", x => x.TagId, "Tags", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessRuleTags",
                columns: table => new
                {
                    BusinessRuleId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessRuleTags", x => new { x.BusinessRuleId, x.TagId });
                    table.ForeignKey("FK_BusinessRuleTags_BusinessRules_BusinessRuleId", x => x.BusinessRuleId, "BusinessRules", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_BusinessRuleTags_Tags_TagId", x => x.TagId, "Tags", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChatSessionId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokenCount = table.Column<int>(type: "int", nullable: false),
                    RetrievedContextJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModelConfigurationId = table.Column<int>(type: "int", nullable: true),
                    ModelOutputId = table.Column<int>(type: "int", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey("FK_ChatMessages_ChatSessions_ChatSessionId", x => x.ChatSessionId, "ChatSessions", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgentSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentTaskId = table.Column<int>(type: "int", nullable: false),
                    StepIndex = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Input = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Output = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentSteps", x => x.Id);
                    table.ForeignKey("FK_AgentSteps_AgentTasks_AgentTaskId", x => x.AgentTaskId, "AgentTasks", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportedConversationMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportedConversationId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedConversationMessages", x => x.Id);
                    table.ForeignKey("FK_ImportedConversationMessages_ImportedConversations_ImportedConversationId", x => x.ImportedConversationId, "ImportedConversations", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectProfileSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectProfileId = table.Column<int>(type: "int", nullable: false),
                    SectionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectProfileSections", x => x.Id);
                    table.ForeignKey("FK_ProjectProfileSections_ProjectProfiles_ProjectProfileId", x => x.ProjectProfileId, "ProjectProfiles", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeRelationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    FromEntityId = table.Column<int>(type: "int", nullable: false),
                    ToEntityId = table.Column<int>(type: "int", nullable: false),
                    RelationType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    SourceKnowledgeItemId = table.Column<int>(type: "int", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeRelationships", x => x.Id);
                    table.ForeignKey("FK_KnowledgeRelationships_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_KnowledgeRelationships_KnowledgeEntities_FromEntityId", x => x.FromEntityId, "KnowledgeEntities", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_KnowledgeRelationships_KnowledgeEntities_ToEntityId", x => x.ToEntityId, "KnowledgeEntities", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModelOutputs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromptRunId = table.Column<int>(type: "int", nullable: false),
                    ModelConfigurationId = table.Column<int>(type: "int", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PromptTokens = table.Column<int>(type: "int", nullable: true),
                    CompletionTokens = table.Column<int>(type: "int", nullable: true),
                    LatencyMs = table.Column<int>(type: "int", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelOutputs", x => x.Id);
                    table.ForeignKey("FK_ModelOutputs_PromptRuns_PromptRunId", x => x.PromptRunId, "PromptRuns", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_ModelOutputs_ModelConfigurations_ModelConfigurationId", x => x.ModelConfigurationId, "ModelConfigurations", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    IngestionJobId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RootPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsOriginalImport = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                    table.ForeignKey("FK_Workspaces_Projects_ProjectId", x => x.ProjectId, "Projects", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FileCount = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceSnapshots", x => x.Id);
                    table.ForeignKey("FK_WorkspaceSnapshots_Workspaces_WorkspaceId", x => x.WorkspaceId, "Workspaces", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<int>(type: "int", nullable: false),
                    WorkspaceSnapshotId = table.Column<int>(type: "int", nullable: true),
                    RelativePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PreviousContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Prompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModelUsed = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceChanges", x => x.Id);
                    table.ForeignKey("FK_WorkspaceChanges_Workspaces_WorkspaceId", x => x.WorkspaceId, "Workspaces", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkspaceId = table.Column<int>(type: "int", nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FileType = table.Column<int>(type: "int", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceFiles", x => x.Id);
                    table.ForeignKey("FK_WorkspaceFiles_Workspaces_WorkspaceId", x => x.WorkspaceId, "Workspaces", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_Projects_Code", "Projects", "Code", unique: true);
            migrationBuilder.CreateIndex("IX_Tags_Name", "Tags", "Name", unique: true);
            migrationBuilder.CreateIndex("IX_ModelConfigurations_Name", "ModelConfigurations", "Name", unique: true);
            migrationBuilder.CreateIndex("IX_PromptTemplates_Name", "PromptTemplates", "Name", unique: true);
            migrationBuilder.CreateIndex("IX_SystemSettings_Key", "SystemSettings", "Key", unique: true);
            migrationBuilder.CreateIndex("IX_TaskProfiles_TaskType", "TaskProfiles", "TaskType", unique: true);
            migrationBuilder.CreateIndex("IX_TaskProfiles_PrimaryModelId", "TaskProfiles", "PrimaryModelId");
            migrationBuilder.CreateIndex("IX_TaskProfiles_ValidationModelId", "TaskProfiles", "ValidationModelId");
            migrationBuilder.CreateIndex("IX_TaskProfiles_ComparisonModelId", "TaskProfiles", "ComparisonModelId");
            migrationBuilder.CreateIndex("IX_ProjectSources_ProjectId", "ProjectSources", "ProjectId");
            migrationBuilder.CreateIndex("IX_KnowledgeItems_ProjectId_Status", "KnowledgeItems", new[] { "ProjectId", "Status" });
            migrationBuilder.CreateIndex("IX_KnowledgeItems_IsApproved", "KnowledgeItems", "IsApproved");
            migrationBuilder.CreateIndex("IX_ChatSessions_ProjectId", "ChatSessions", "ProjectId");
            migrationBuilder.CreateIndex("IX_ChatSessions_ModelConfigurationId", "ChatSessions", "ModelConfigurationId");
            migrationBuilder.CreateIndex("IX_ApprovedCodeSnippets_ProjectId_IsReusable", "ApprovedCodeSnippets", new[] { "ProjectId", "IsReusable" });
            migrationBuilder.CreateIndex("IX_BusinessRules_ProjectId_IsApproved", "BusinessRules", new[] { "ProjectId", "IsApproved" });
            migrationBuilder.CreateIndex("IX_AgentTasks_ProjectId", "AgentTasks", "ProjectId");
            migrationBuilder.CreateIndex("IX_ImportedConversations_ProjectId", "ImportedConversations", "ProjectId");
            migrationBuilder.CreateIndex("IX_IngestionJobs_ProjectId", "IngestionJobs", "ProjectId");
            migrationBuilder.CreateIndex("IX_ProjectProfiles_ProjectId", "ProjectProfiles", "ProjectId");
            migrationBuilder.CreateIndex("IX_KnowledgeEntities_ProjectId_Name", "KnowledgeEntities", new[] { "ProjectId", "Name" });
            migrationBuilder.CreateIndex("IX_PromptRuns_ProjectId", "PromptRuns", "ProjectId");
            migrationBuilder.CreateIndex("IX_ExtractedCodeBlocks_ProjectId", "ExtractedCodeBlocks", "ProjectId");
            migrationBuilder.CreateIndex("IX_ImportedFiles_ProjectId", "ImportedFiles", "ProjectId");
            migrationBuilder.CreateIndex("IX_ImportedFiles_IngestionJobId", "ImportedFiles", "IngestionJobId");
            migrationBuilder.CreateIndex("IX_KnowledgeChunks_KnowledgeItemId", "KnowledgeChunks", "KnowledgeItemId");
            migrationBuilder.CreateIndex("IX_KnowledgeItemTags_TagId", "KnowledgeItemTags", "TagId");
            migrationBuilder.CreateIndex("IX_ApprovedCodeSnippetTags_TagId", "ApprovedCodeSnippetTags", "TagId");
            migrationBuilder.CreateIndex("IX_BusinessRuleTags_TagId", "BusinessRuleTags", "TagId");
            migrationBuilder.CreateIndex("IX_ChatMessages_ChatSessionId", "ChatMessages", "ChatSessionId");
            migrationBuilder.CreateIndex("IX_AgentSteps_AgentTaskId", "AgentSteps", "AgentTaskId");
            migrationBuilder.CreateIndex("IX_ImportedConversationMessages_ImportedConversationId", "ImportedConversationMessages", "ImportedConversationId");
            migrationBuilder.CreateIndex("IX_ProjectProfileSections_ProjectProfileId", "ProjectProfileSections", "ProjectProfileId");
            migrationBuilder.CreateIndex("IX_KnowledgeRelationships_ProjectId", "KnowledgeRelationships", "ProjectId");
            migrationBuilder.CreateIndex("IX_KnowledgeRelationships_FromEntityId", "KnowledgeRelationships", "FromEntityId");
            migrationBuilder.CreateIndex("IX_KnowledgeRelationships_ToEntityId", "KnowledgeRelationships", "ToEntityId");
            migrationBuilder.CreateIndex("IX_ModelOutputs_PromptRunId", "ModelOutputs", "PromptRunId");
            migrationBuilder.CreateIndex("IX_ModelOutputs_ModelConfigurationId", "ModelOutputs", "ModelConfigurationId");
            migrationBuilder.CreateIndex("IX_Workspaces_ProjectId", "Workspaces", "ProjectId");
            migrationBuilder.CreateIndex("IX_WorkspaceSnapshots_WorkspaceId", "WorkspaceSnapshots", "WorkspaceId");
            migrationBuilder.CreateIndex("IX_WorkspaceChanges_WorkspaceId", "WorkspaceChanges", "WorkspaceId");
            migrationBuilder.CreateIndex("IX_WorkspaceFiles_WorkspaceId", "WorkspaceFiles", "WorkspaceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WorkspaceFiles");
            migrationBuilder.DropTable(name: "WorkspaceChanges");
            migrationBuilder.DropTable(name: "WorkspaceSnapshots");
            migrationBuilder.DropTable(name: "Workspaces");
            migrationBuilder.DropTable(name: "ModelOutputs");
            migrationBuilder.DropTable(name: "KnowledgeRelationships");
            migrationBuilder.DropTable(name: "ProjectProfileSections");
            migrationBuilder.DropTable(name: "ImportedConversationMessages");
            migrationBuilder.DropTable(name: "AgentSteps");
            migrationBuilder.DropTable(name: "ChatMessages");
            migrationBuilder.DropTable(name: "BusinessRuleTags");
            migrationBuilder.DropTable(name: "ApprovedCodeSnippetTags");
            migrationBuilder.DropTable(name: "KnowledgeItemTags");
            migrationBuilder.DropTable(name: "KnowledgeChunks");
            migrationBuilder.DropTable(name: "ImportedFiles");
            migrationBuilder.DropTable(name: "ExtractedCodeBlocks");
            migrationBuilder.DropTable(name: "PromptRuns");
            migrationBuilder.DropTable(name: "KnowledgeEntities");
            migrationBuilder.DropTable(name: "ProjectProfiles");
            migrationBuilder.DropTable(name: "IngestionJobs");
            migrationBuilder.DropTable(name: "ImportedConversations");
            migrationBuilder.DropTable(name: "AgentTasks");
            migrationBuilder.DropTable(name: "BusinessRules");
            migrationBuilder.DropTable(name: "ApprovedCodeSnippets");
            migrationBuilder.DropTable(name: "ChatSessions");
            migrationBuilder.DropTable(name: "KnowledgeItems");
            migrationBuilder.DropTable(name: "ProjectSources");
            migrationBuilder.DropTable(name: "TaskProfiles");
            migrationBuilder.DropTable(name: "AuditLogs");
            migrationBuilder.DropTable(name: "SystemSettings");
            migrationBuilder.DropTable(name: "PromptTemplates");
            migrationBuilder.DropTable(name: "ModelConfigurations");
            migrationBuilder.DropTable(name: "Tags");
            migrationBuilder.DropTable(name: "Projects");
        }
    }
}
