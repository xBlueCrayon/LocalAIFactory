using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.ViewModels;

public sealed class DashboardViewModel
{
    public int Projects { get; set; }
    public int KnowledgeItems { get; set; }
    public int ApprovedKnowledge { get; set; }
    public int ApprovedCode { get; set; }
    public int BusinessRules { get; set; }
    public int ChatSessions { get; set; }
    public int ImportedFiles { get; set; }
    public int ModelConfigurations { get; set; }
    public int Workspaces { get; set; }
    public string? ActiveModel { get; set; }

    // Phase 1.2: cached health snapshot — populated with no external calls during rendering.
    public ServiceHealthSnapshot Health { get; set; } = new();

    // Ingestion activity
    public int JobsRunning { get; set; }
    public int JobsPending { get; set; }
    public int JobsCompleted { get; set; }
    public int JobsFailed { get; set; }

    // Pending review (the curation backlog)
    public int KnowledgeNeedsReview { get; set; }
    public int RulesNeedsReview { get; set; }
    public int CodeCandidates { get; set; }
    public int ProfileSectionsNeedsReview { get; set; }

    // Recent activity — projected to lightweight rows (no large Content/RawText columns loaded).
    public List<RecentKnowledgeRow> RecentKnowledge { get; set; } = new();
    public List<RecentImportRow> RecentImports { get; set; } = new();
    public List<RecentApprovalRow> RecentApprovals { get; set; } = new();
}

// Phase 1.2.2: dashboard "recent activity" rows. Projected directly in SQL so the dashboard never
// materializes full entities (KnowledgeItem.Content / ImportedFile.RawText can be very large).
public sealed record RecentKnowledgeRow(int Id, string Title, SourceType SourceType, KnowledgeStatus Status, DateTime UpdatedUtc);
public sealed record RecentImportRow(string FileName, IngestionJobStatus Status, int TotalFiles, int ProcessedFiles, DateTime CreatedUtc);
public sealed record RecentApprovalRow(string Action, string? EntityName, string? Details, DateTime CreatedUtc);

// Phase 1.2.3: lightweight row for the Knowledge list. Projected in SQL so the (potentially huge)
// KnowledgeItem.Content column is NEVER selected/materialized for the list view.
public sealed record KnowledgeListRow(int Id, string Title, SourceType SourceType, KnowledgeStatus Status, DateTime UpdatedUtc, bool IsApproved);

// R2-ACC-B1: lightweight rows for the Base Knowledge screen. The large Content column is filtered on
// (server-side) but never SELECTed for the list. Category comes from the item's "cat:" tag.
public sealed record BaseKnowledgeRow(int Id, string Title, string? Category, KnowledgeType KnowledgeType,
    KnowledgeScope Scope, double Confidence, KnowledgeStatus Status, DateTime? LastReviewedUtc);
public sealed record InstalledPackRow(int Id, Guid Uid, string Name, string Version, int ItemCount,
    DateTime InstalledUtc, KnowledgePackStatus Status);

// KE-003: read-only history rows for the Knowledge details page. Projected in SQL so the large
// ContentSnapshot column is never materialized for these lists.
public sealed record KnowledgeVersionRow(int VersionNumber, string ChangeReason, ProvenanceMethod Method, string Actor, DateTime CreatedUtc);
public sealed record ProvenanceRow(ProvenanceMethod Method, string Actor, string Reason, Guid? OriginInstanceId, DateTime CreatedUtc);

public sealed class ChatPageViewModel
{
    public List<Project> Projects { get; set; } = new();
    public List<ModelConfiguration> Models { get; set; } = new();
    public List<ChatSession> Sessions { get; set; } = new();
    public ChatSession? CurrentSession { get; set; }
    public List<ChatMessage> Messages { get; set; } = new();
    public int? SelectedProjectId { get; set; }
    public int? SelectedModelId { get; set; }
}

public sealed class ChatSendViewModel
{
    public int? SessionId { get; set; }
    public int? ProjectId { get; set; }
    public int? ModelConfigurationId { get; set; }
    public string Message { get; set; } = "";
}

public sealed class SaveResponseViewModel
{
    public int MessageId { get; set; }
    public int? ProjectId { get; set; }
    public string Title { get; set; } = "";
    public string Target { get; set; } = "Knowledge"; // Knowledge | ApprovedCode | BusinessRule
    public string? Language { get; set; }
    public string? Framework { get; set; }
}
