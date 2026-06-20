namespace LocalAIFactory.Core.Enums;

public enum KnowledgeStatus { Draft = 0, Approved = 1, Deprecated = 2, NeedsReview = 3 }

public enum SourceType
{
    UserExplanation = 0,
    SourceCode = 1,
    SqlScript = 2,
    ChatGptExport = 3,
    ClaudeExport = 4,
    Documentation = 5,
    Readme = 6,
    BuildLog = 7,
    DeploymentNote = 8,
    GeneratedCode = 9,
    ApprovedCode = 10,
    BusinessRule = 11,
    DatabaseObject = 12,
    ArchitectureNote = 13,
    DebuggingFix = 14,
    ProjectProfile = 15,
    ConversationTranscript = 16,
    Configuration = 17
}

public enum ModelProvider { Ollama = 0, OpenAiCompatible = 1, OpenAi = 2, Claude = 3 }

public enum ChatRole { System = 0, User = 1, Assistant = 2 }

public enum MessageRating { None = 0, Useful = 1, Wrong = 2 }

public enum AgentTaskStatus { Pending = 0, Planning = 1, Running = 2, Completed = 3, Failed = 4, Cancelled = 5 }

public enum AgentStepKind { Plan = 0, Retrieve = 1, Generate = 2, Review = 3, Other = 4 }

public enum ImportStatus { Pending = 0, Processed = 1, NeedsReview = 2, Failed = 3 }

public enum ConversationSource { ChatGpt = 0, Claude = 1 }

public enum ProjectSourceKind { LocalFolder = 0, GitRepository = 1, Other = 2 }

public enum BusinessRuleStatus { Draft = 0, Approved = 1, Deprecated = 2, NeedsReview = 3 }

public enum TaskType
{
    Chat = 0,
    ProjectImport = 1,
    ProjectSummarization = 2,
    BusinessRuleExtraction = 3,
    CodeGeneration = 4,
    CodeFix = 5,
    SqlAnalysis = 6,
    MetabaseAnalysis = 7,
    ArchitectureAnalysis = 8,
    DeploymentAnalysis = 9,
    Embedding = 10,
    CodeModification = 11,
    BuildAnalysis = 12,
    WorkspacePlanning = 13
}

public enum FileClass
{
    SourceCode = 0,
    SqlScript = 1,
    Configuration = 2,
    Documentation = 3,
    Readme = 4,
    BuildLog = 5,
    DeploymentNote = 6,
    XmlFile = 7,
    JsonFile = 8,
    RazorView = 9,
    PowerShell = 10,
    BatchFile = 11,
    Binary = 12,
    Unknown = 13
}

public enum IngestionJobStatus { Pending = 0, Running = 1, Completed = 2, Failed = 3, Cancelled = 4 }

public enum IngestionPhase
{
    Pending = 0,
    Extracting = 1,
    Scanning = 2,
    Classifying = 3,
    Storing = 4,
    Chunking = 5,
    Embedding = 6,
    Profiling = 7,
    GraphExtraction = 8,
    CandidateExtraction = 9,
    Completed = 10,
    Failed = 11
}

public enum EntityType { Flag = 0, Workflow = 1, File = 2, Function = 3, ExternalSystem = 4, Table = 5, Job = 6, Api = 7, Module = 8, Other = 9 }

public enum RelationType { BelongsTo = 0, Uses = 1, Calls = 2, Reads = 3, Writes = 4, DependsOn = 5, DeploysTo = 6, IntegratesWith = 7 }

public enum ModelOutputKind { Primary = 0, Validation = 1, Comparison = 2 }

public enum WorkspaceStatus { Active = 0, Archived = 1 }

public enum WorkspaceChangeStatus { Proposed = 0, Applied = 1, Reverted = 2, Rejected = 3 }

// Phase 1.2: tri-state health for optional AI infrastructure (shown on the dashboard).
public enum ServiceState { Unknown = 0, Disabled = 1, Online = 2, Offline = 3 }

// Phase 1.2: the deployment tier the app is effectively operating in (auto-detected from live health).
public enum EnvironmentMode { Minimal = 0, Standard = 1, FullAi = 2 }

// Phase 2 / KE-002: permanence tier. Derived = machine-extracted, freely regenerable. Curated =
// human-authored or approved, durable, changed only by human approval. Raw = reserved for immutable
// source artifacts (formalized in KE-007). New column defaults to 0 = Derived.
public enum PermanenceTier { Derived = 0, Curated = 1, Raw = 2 }

// Phase 2 / KE-002: where a proposed revision to a curated item originated.
public enum RevisionSource { Extraction = 0, Consolidation = 1, ReEmbedding = 2 }
