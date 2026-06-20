using LocalAIFactory.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectSource> ProjectSources => Set<ProjectSource>();
    public DbSet<KnowledgeItem> KnowledgeItems => Set<KnowledgeItem>();
    public DbSet<KnowledgeChunk> KnowledgeChunks => Set<KnowledgeChunk>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<KnowledgeItemTag> KnowledgeItemTags => Set<KnowledgeItemTag>();
    public DbSet<ApprovedCodeSnippet> ApprovedCodeSnippets => Set<ApprovedCodeSnippet>();
    public DbSet<ApprovedCodeSnippetTag> ApprovedCodeSnippetTags => Set<ApprovedCodeSnippetTag>();
    public DbSet<BusinessRule> BusinessRules => Set<BusinessRule>();
    public DbSet<BusinessRuleTag> BusinessRuleTags => Set<BusinessRuleTag>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ModelConfiguration> ModelConfigurations => Set<ModelConfiguration>();
    public DbSet<TaskProfile> TaskProfiles => Set<TaskProfile>();
    public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();
    public DbSet<AgentTask> AgentTasks => Set<AgentTask>();
    public DbSet<AgentStep> AgentSteps => Set<AgentStep>();
    public DbSet<ImportedFile> ImportedFiles => Set<ImportedFile>();
    public DbSet<ImportCoverageReport> ImportCoverageReports => Set<ImportCoverageReport>(); // R2-P0A
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>(); // R2-P0B
    public DbSet<ProjectAccess> ProjectAccesses => Set<ProjectAccess>(); // R2-P0B
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>(); // R2-P0B
    public DbSet<ImportedConversation> ImportedConversations => Set<ImportedConversation>();
    public DbSet<ImportedConversationMessage> ImportedConversationMessages => Set<ImportedConversationMessage>();
    public DbSet<IngestionJob> IngestionJobs => Set<IngestionJob>();
    public DbSet<ProjectProfile> ProjectProfiles => Set<ProjectProfile>();
    public DbSet<ProjectProfileSection> ProjectProfileSections => Set<ProjectProfileSection>();
    public DbSet<KnowledgeEntity> KnowledgeEntities => Set<KnowledgeEntity>();
    public DbSet<KnowledgeRelationship> KnowledgeRelationships => Set<KnowledgeRelationship>();
    public DbSet<PromptRun> PromptRuns => Set<PromptRun>();
    public DbSet<ModelOutput> ModelOutputs => Set<ModelOutput>();
    public DbSet<ExtractedCodeBlock> ExtractedCodeBlocks => Set<ExtractedCodeBlock>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceSnapshot> WorkspaceSnapshots => Set<WorkspaceSnapshot>();
    public DbSet<WorkspaceChange> WorkspaceChanges => Set<WorkspaceChange>();
    public DbSet<WorkspaceFile> WorkspaceFiles => Set<WorkspaceFile>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ProposedRevision> ProposedRevisions => Set<ProposedRevision>();
    public DbSet<KnowledgeVersion> KnowledgeVersions => Set<KnowledgeVersion>();
    public DbSet<ProvenanceEvent> ProvenanceEvents => Set<ProvenanceEvent>();
    public DbSet<KnowledgeDuplicate> KnowledgeDuplicates => Set<KnowledgeDuplicate>();
    public DbSet<KnowledgeDomain> KnowledgeDomains => Set<KnowledgeDomain>();
    public DbSet<ScopeApplicability> ScopeApplicabilities => Set<ScopeApplicability>();
    public DbSet<CodeSymbol> CodeSymbols => Set<CodeSymbol>(); // KE-008
    public DbSet<CodeSymbolReference> CodeSymbolReferences => Set<CodeSymbolReference>(); // KE-009
    public DbSet<CodeEdge> CodeEdges => Set<CodeEdge>(); // KE-010
    public DbSet<RetrievalEvent> RetrievalEvents => Set<RetrievalEvent>(); // KE-011
    public DbSet<KnowledgePack> KnowledgePacks => Set<KnowledgePack>(); // R2-ACC-B1

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Project>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<ProjectSource>(e =>
        {
            e.Property(x => x.Path).HasMaxLength(1000).IsRequired();
            e.HasOne(x => x.Project).WithMany(p => p.Sources)
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<KnowledgeItem>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(400).IsRequired();
            e.HasIndex(x => new { x.ProjectId, x.Status });
            e.HasIndex(x => x.IsApproved);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<KnowledgeChunk>(e =>
        {
            e.HasIndex(x => x.KnowledgeItemId);
            e.HasOne(x => x.KnowledgeItem).WithMany(k => k.Chunks)
                .HasForeignKey(x => x.KnowledgeItemId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Tag>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<KnowledgeItemTag>(e =>
        {
            e.HasKey(x => new { x.KnowledgeItemId, x.TagId });
            e.HasOne(x => x.KnowledgeItem).WithMany(k => k.Tags)
                .HasForeignKey(x => x.KnowledgeItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ApprovedCodeSnippet>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(400).IsRequired();
            e.Property(x => x.Language).HasMaxLength(50).IsRequired();
            e.Property(x => x.Framework).HasMaxLength(100);
            e.HasIndex(x => new { x.ProjectId, x.IsReusable });
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ApprovedCodeSnippetTag>(e =>
        {
            e.HasKey(x => new { x.ApprovedCodeSnippetId, x.TagId });
            e.HasOne(x => x.ApprovedCodeSnippet).WithMany(s => s.Tags)
                .HasForeignKey(x => x.ApprovedCodeSnippetId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<BusinessRule>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(400).IsRequired();
            e.HasIndex(x => new { x.ProjectId, x.IsApproved });
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<BusinessRuleTag>(e =>
        {
            e.HasKey(x => new { x.BusinessRuleId, x.TagId });
            e.HasOne(x => x.BusinessRule).WithMany(r => r.Tags)
                .HasForeignKey(x => x.BusinessRuleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tag).WithMany().HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ChatSession>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(300).IsRequired();
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.ModelConfigurationId);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ModelConfiguration).WithMany().HasForeignKey(x => x.ModelConfigurationId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ChatMessage>(e =>
        {
            e.HasIndex(x => x.ChatSessionId);
            e.HasOne(x => x.ChatSession).WithMany(s => s.Messages)
                .HasForeignKey(x => x.ChatSessionId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ModelConfiguration>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ModelName).HasMaxLength(200).IsRequired();
            e.Property(x => x.BaseUrl).HasMaxLength(500).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<TaskProfile>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.TaskType).IsUnique();
            e.HasIndex(x => x.PrimaryModelId);
            e.HasIndex(x => x.ValidationModelId);
            e.HasIndex(x => x.ComparisonModelId);
            e.HasOne(x => x.PrimaryModel).WithMany().HasForeignKey(x => x.PrimaryModelId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ValidationModel).WithMany().HasForeignKey(x => x.ValidationModelId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ComparisonModel).WithMany().HasForeignKey(x => x.ComparisonModelId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<PromptTemplate>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Kind).HasMaxLength(100);
            e.HasIndex(x => x.Name).IsUnique();
        });

        b.Entity<AgentTask>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(400).IsRequired();
            e.HasIndex(x => x.ProjectId);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<AgentStep>(e =>
        {
            e.HasIndex(x => x.AgentTaskId);
            e.Property(x => x.Status).HasMaxLength(50).IsRequired();
            e.HasOne(x => x.AgentTask).WithMany(t => t.Steps)
                .HasForeignKey(x => x.AgentTaskId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ImportedFile>(e =>
        {
            e.Property(x => x.FileName).HasMaxLength(400).IsRequired();
            e.Property(x => x.RelativePath).HasMaxLength(1000);
            e.Property(x => x.Extension).HasMaxLength(30).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(200);
            e.Property(x => x.Sha256).HasMaxLength(64);
            e.Property(x => x.SkipReason).HasMaxLength(500);
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.IngestionJobId);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<IngestionJob>().WithMany().HasForeignKey(x => x.IngestionJobId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ImportedConversation>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(500).IsRequired();
            e.HasIndex(x => x.ProjectId);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ImportedConversationMessage>(e =>
        {
            e.HasIndex(x => x.ImportedConversationId);
            e.HasOne(x => x.ImportedConversation).WithMany(c => c.Messages)
                .HasForeignKey(x => x.ImportedConversationId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<IngestionJob>(e =>
        {
            e.Property(x => x.FileName).HasMaxLength(400).IsRequired();
            e.Property(x => x.ExtractedRoot).HasMaxLength(1000);
            e.HasIndex(x => x.ProjectId);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ProjectProfile>(e =>
        {
            e.HasIndex(x => x.ProjectId);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ProjectProfileSection>(e =>
        {
            e.Property(x => x.SectionKey).HasMaxLength(100).IsRequired();
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.ProjectProfileId);
            e.HasOne(x => x.ProjectProfile).WithMany(p => p.Sections)
                .HasForeignKey(x => x.ProjectProfileId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<KnowledgeEntity>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(300).IsRequired();
            e.HasIndex(x => new { x.ProjectId, x.Name });
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<KnowledgeRelationship>(e =>
        {
            e.HasIndex(x => x.ProjectId);
            e.HasIndex(x => x.FromEntityId);
            e.HasIndex(x => x.ToEntityId);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.FromEntity).WithMany().HasForeignKey(x => x.FromEntityId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ToEntity).WithMany().HasForeignKey(x => x.ToEntityId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<PromptRun>(e =>
        {
            e.HasIndex(x => x.ProjectId);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ModelOutput>(e =>
        {
            e.HasIndex(x => x.PromptRunId);
            e.HasIndex(x => x.ModelConfigurationId);
            e.HasOne(x => x.PromptRun).WithMany(r => r.Outputs)
                .HasForeignKey(x => x.PromptRunId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ModelConfiguration).WithMany().HasForeignKey(x => x.ModelConfigurationId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ExtractedCodeBlock>(e =>
        {
            e.Property(x => x.Language).HasMaxLength(50);
            e.HasIndex(x => x.ProjectId);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Workspace>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.RootPath).HasMaxLength(1000);
            e.HasIndex(x => x.ProjectId);
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<WorkspaceSnapshot>(e =>
        {
            e.Property(x => x.CreatedBy).HasMaxLength(200);
            e.HasIndex(x => x.WorkspaceId);
            e.HasOne(x => x.Workspace).WithMany(w => w.Snapshots)
                .HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<WorkspaceChange>(e =>
        {
            e.Property(x => x.RelativePath).HasMaxLength(1000).IsRequired();
            e.Property(x => x.ModelUsed).HasMaxLength(200);
            e.HasIndex(x => x.WorkspaceId);
            e.HasOne(x => x.Workspace).WithMany(w => w.Changes)
                .HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<WorkspaceFile>(e =>
        {
            e.Property(x => x.RelativePath).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Hash).HasMaxLength(64);
            e.HasIndex(x => x.WorkspaceId);
            e.HasOne(x => x.Workspace).WithMany(w => w.Files)
                .HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SystemSetting>(e =>
        {
            e.Property(x => x.Key).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Key).IsUnique();
        });

        b.Entity<AuditLog>(e =>
        {
            e.Property(x => x.Action).HasMaxLength(200).IsRequired();
            e.Property(x => x.EntityName).HasMaxLength(200);
            e.Property(x => x.EntityId).HasMaxLength(100);
            e.Property(x => x.UserName).HasMaxLength(200);
        });

        // Phase 2 / KE-002: proposed revisions to curated knowledge (propose-never-overwrite).
        b.Entity<ProposedRevision>(e =>
        {
            e.Property(x => x.TargetEntityType).HasMaxLength(100).IsRequired();
            e.Property(x => x.ProposedTitle).HasMaxLength(400);
            e.Property(x => x.ChangeReason).HasMaxLength(1000);
            e.HasIndex(x => new { x.TargetEntityType, x.TargetEntityId });
            e.HasIndex(x => x.Status);
        });

        // Phase 2 / KE-003: portable identity (unique Uid on every curated/graph entity), the knowledge
        // backbone fields, and the provenance + version-history tables. (EF merges these with the blocks
        // configured above for the same entity types.)
        b.Entity<KnowledgeItem>(e =>
        {
            e.Property(x => x.ContentHash).HasMaxLength(64);
            e.Property(x => x.Summary).HasMaxLength(2000);
            e.HasIndex(x => x.Uid).IsUnique();
        });
        b.Entity<BusinessRule>(e => e.HasIndex(x => x.Uid).IsUnique());
        b.Entity<ApprovedCodeSnippet>(e => e.HasIndex(x => x.Uid).IsUnique());
        b.Entity<ProjectProfile>(e => e.HasIndex(x => x.Uid).IsUnique());
        b.Entity<ProjectProfileSection>(e => e.HasIndex(x => x.Uid).IsUnique());
        b.Entity<KnowledgeEntity>(e => e.HasIndex(x => x.Uid).IsUnique());
        b.Entity<KnowledgeRelationship>(e => e.HasIndex(x => x.Uid).IsUnique());
        b.Entity<ProposedRevision>(e => e.HasIndex(x => x.Uid).IsUnique());

        b.Entity<KnowledgeVersion>(e =>
        {
            e.Property(x => x.ContentHash).HasMaxLength(64);
            e.Property(x => x.Title).HasMaxLength(400);
            e.Property(x => x.Summary).HasMaxLength(2000);
            e.Property(x => x.ChangeReason).HasMaxLength(1000);
            e.Property(x => x.Actor).HasMaxLength(200);
            e.HasIndex(x => x.Uid).IsUnique();
            e.HasIndex(x => new { x.KnowledgeItemId, x.VersionNumber }).IsUnique();
            e.HasOne(x => x.KnowledgeItem).WithMany()
                .HasForeignKey(x => x.KnowledgeItemId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ProvenanceEvent>(e =>
        {
            e.Property(x => x.ExtractorOrModelId).HasMaxLength(200);
            e.Property(x => x.Actor).HasMaxLength(200);
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.HasIndex(x => x.Uid).IsUnique();
            e.HasIndex(x => new { x.KnowledgeItemId, x.CreatedUtc });
            e.HasIndex(x => x.OriginPackUid);
            e.HasOne(x => x.KnowledgeItem).WithMany()
                .HasForeignKey(x => x.KnowledgeItemId).OnDelete(DeleteBehavior.Cascade);
        });

        // Phase 2 / KE-004: source-locus convergence key, minimal raw artifact supersession, and the
        // dedicated duplicate table. KnowledgeDuplicate uses loose references (indexed int + portable Uid,
        // no FK) like ProposedRevision — it is a capture-only ledger consumed by review/KE-030.
        b.Entity<KnowledgeItem>(e =>
        {
            e.Property(x => x.SourceLocusKey).HasMaxLength(64);
            e.HasIndex(x => x.SourceLocusKey);
        });
        b.Entity<ImportedFile>(e =>
        {
            e.HasOne<ImportedFile>().WithMany().HasForeignKey(x => x.SupersedesImportedFileId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<KnowledgeDuplicate>(e =>
        {
            e.HasIndex(x => x.Uid).IsUnique();
            e.HasIndex(x => x.KnowledgeItemId);
            e.HasIndex(x => x.DuplicateOfKnowledgeItemId);
            e.HasIndex(x => x.Status);
        });

        // Phase 2 / KE-005: scope precedence, the user-managed domain taxonomy, and applicability links.
        b.Entity<KnowledgeItem>(e =>
        {
            e.HasIndex(x => x.Scope);
            e.HasIndex(x => x.KnowledgeDomainId);
            e.HasOne<KnowledgeDomain>().WithMany().HasForeignKey(x => x.KnowledgeDomainId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<KnowledgeDomain>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(64).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Uid).IsUnique();
            e.HasIndex(x => x.Code).IsUnique();
        });
        b.Entity<ScopeApplicability>(e =>
        {
            e.HasIndex(x => x.Uid).IsUnique();
            e.HasIndex(x => x.ConstraintKnowledgeItemId);
            e.HasIndex(x => new { x.TargetKind, x.TargetId });
        });

        // Phase 2 / KE-007: raw artifact (SourceArtifact, extends ImportedFile) + import batch
        // (ImportBatch, extends IngestionJob) — portable identity and the metadata envelope.
        b.Entity<ImportedFile>(e =>
        {
            e.Property(x => x.DetectedLanguage).HasMaxLength(50);
            e.Property(x => x.ExtractionNote).HasMaxLength(1000); // R2-P0A
            e.HasIndex(x => x.Uid).IsUnique();
            e.HasIndex(x => new { x.ProjectId, x.ExtractionStatus }); // R2-P0A: fast gap aggregation
        });

        // R2-P0A: per-import coverage / gap report. Append-only honesty record; touches no curated knowledge.
        b.Entity<ImportCoverageReport>(e =>
        {
            e.HasIndex(x => x.Uid).IsUnique();
            e.HasIndex(x => new { x.ProjectId, x.CreatedUtc });
        });

        // R2-P0B: pilot security. Additive; no curated-knowledge changes.
        b.Entity<UserAccount>(e =>
        {
            e.Property(x => x.WindowsIdentity).HasMaxLength(256);
            e.Property(x => x.Sid).HasMaxLength(128);
            e.Property(x => x.DisplayName).HasMaxLength(256);
            e.HasIndex(x => x.Uid).IsUnique();
            e.HasIndex(x => x.WindowsIdentity).IsUnique();
        });
        b.Entity<ProjectAccess>(e =>
        {
            e.HasIndex(x => new { x.UserAccountId, x.ProjectId }).IsUnique();
            e.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserAccountId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<AuditEvent>(e =>
        {
            e.Property(x => x.Action).HasMaxLength(200);
            e.Property(x => x.TargetType).HasMaxLength(100);
            e.Property(x => x.TargetId).HasMaxLength(400);
            e.Property(x => x.Detail).HasMaxLength(2000);
            e.Property(x => x.WindowsIdentity).HasMaxLength(256);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.HasIndex(x => x.Uid).IsUnique();
            e.HasIndex(x => x.CreatedUtc);
            e.HasIndex(x => x.UserAccountId);
            e.HasIndex(x => x.EventType);
        });

        b.Entity<IngestionJob>(e =>
        {
            e.Property(x => x.SourceReference).HasMaxLength(1000);
            e.Property(x => x.SourceRevision).HasMaxLength(200);
            e.HasIndex(x => x.Uid).IsUnique();
        });

        // Phase 2 / KE-008: deterministic code symbols. Lean structural rows that trace back to their
        // source artifact; reconciled by SourceLocusKey on re-extraction. FileLocusKey groups a file's
        // symbols; both locus keys are fixed-length hashes that index cheaply. FullName is bounded but
        // not indexed (overloads share it; the per-symbol join uses SourceLocusKey).
        b.Entity<CodeSymbol>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(400);
            e.Property(x => x.FullName).HasMaxLength(512);
            e.Property(x => x.Signature).HasMaxLength(2000);
            e.Property(x => x.SourceLocusKey).HasMaxLength(64);
            e.Property(x => x.FileLocusKey).HasMaxLength(64);
            e.Property(x => x.DetectedLanguage).HasMaxLength(50);
            e.Property(x => x.SymbolHash).HasMaxLength(64);
            e.Property(x => x.NormalizedKey).HasMaxLength(512); // KE-010
            e.HasIndex(x => x.Uid).IsUnique();
            e.HasIndex(x => x.FileLocusKey);
            e.HasIndex(x => x.SourceLocusKey);
            e.HasIndex(x => new { x.ProjectId, x.Kind });
            e.HasIndex(x => new { x.ProjectId, x.NormalizedKey }); // KE-010 resolution / KE-011 lexical
            e.HasOne<ImportedFile>().WithMany().HasForeignKey(x => x.SourceArtifactId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<CodeSymbol>().WithMany().HasForeignKey(x => x.ParentSymbolId).OnDelete(DeleteBehavior.Restrict);
        });

        // Phase 2 / KE-009: deterministic structural references (staging for KE-010 edge resolution). Lean,
        // rebuildable rows — no Uid/version. ReferencedKey is a canonical "[db.]schema.object" join key that
        // KE-010 resolves to a target symbol. Restrict deletes; the store manages reference lifecycle in code.
        b.Entity<CodeSymbolReference>(e =>
        {
            e.Property(x => x.ReferencedDatabase).HasMaxLength(128);
            e.Property(x => x.ReferencedSchema).HasMaxLength(128);
            e.Property(x => x.ReferencedObject).HasMaxLength(256);
            e.Property(x => x.ReferencedColumn).HasMaxLength(256);
            e.Property(x => x.ReferencedKey).HasMaxLength(512);
            e.Property(x => x.FileLocusKey).HasMaxLength(64);
            e.Property(x => x.Evidence).HasMaxLength(500); // R2-ACC-CAP1
            e.HasIndex(x => x.FromSymbolId);
            e.HasIndex(x => x.SourceArtifactId);
            e.HasIndex(x => x.ReferencedKey);
            e.HasIndex(x => new { x.ProjectId, x.ReferenceKind });
            e.HasOne<CodeSymbol>().WithMany().HasForeignKey(x => x.FromSymbolId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ImportedFile>().WithMany().HasForeignKey(x => x.SourceArtifactId).OnDelete(DeleteBehavior.Restrict);
        });

        // Phase 2 / KE-010: deterministic structural edges (reference/dependency) between CodeSymbols. Sibling
        // of KnowledgeRelationship; endpoints are CodeSymbols. Convergence keyed on EdgeKey; Uid is portable
        // (Knowledge Packs). Restrict deletes — the graph builder manages edge lifecycle in code.
        b.Entity<CodeEdge>(e =>
        {
            e.Property(x => x.EdgeKey).HasMaxLength(64);
            e.Property(x => x.Evidence).HasMaxLength(500); // R2-ACC-CAP1
            e.HasIndex(x => x.Uid).IsUnique();
            e.HasIndex(x => x.EdgeKey).IsUnique();
            e.HasIndex(x => x.ToSymbolId);   // "what depends on / references X"
            e.HasIndex(x => x.FromSymbolId);
            e.HasIndex(x => x.SourceArtifactId);
            e.HasIndex(x => new { x.ProjectId, x.RelationType });
            e.HasOne<CodeSymbol>().WithMany().HasForeignKey(x => x.FromSymbolId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<CodeSymbol>().WithMany().HasForeignKey(x => x.ToSymbolId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ImportedFile>().WithMany().HasForeignKey(x => x.SourceArtifactId).OnDelete(DeleteBehavior.Restrict);
        });

        // KE-010: the unified structural graph as a read-only view — containment (from ParentSymbolId) UNION
        // reference edges (from CodeEdges). Keyless; the view itself is created by the migration. KE-011
        // traverses this single shape so code and schema form one graph.
        b.Entity<CodeGraphEdge>(e =>
        {
            e.HasNoKey();
            e.ToView("vCodeGraph");
        });

        // Phase 2 / KE-011: capture-only retrieval log. Lean, append-only; influences nothing yet.
        b.Entity<RetrievalEvent>(e =>
        {
            e.Property(x => x.Query).HasMaxLength(400);
            e.Property(x => x.Mode).HasMaxLength(40);
            e.HasIndex(x => x.Uid).IsUnique();
            e.HasIndex(x => new { x.ProjectId, x.CreatedUtc });
        });

        // R2-ACC-B1: Knowledge Pack install anchor + KnowledgeItem origin link. Additive; baseline items are
        // ordinary KnowledgeItems distinguished only by a non-null KnowledgePackId.
        b.Entity<KnowledgePack>(e =>
        {
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Version).HasMaxLength(50).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
            e.Property(x => x.License).HasMaxLength(1000);
            e.Property(x => x.SourceManifestHash).HasMaxLength(64);
            e.HasIndex(x => x.Uid).IsUnique();
            e.HasIndex(x => x.Name);
        });
        b.Entity<KnowledgeItem>(e =>
        {
            e.HasIndex(x => x.KnowledgePackId);
            e.HasOne(x => x.KnowledgePack).WithMany().HasForeignKey(x => x.KnowledgePackId)
                .OnDelete(DeleteBehavior.Restrict); // a pack cannot be deleted out from under its items
        });
    }
}
