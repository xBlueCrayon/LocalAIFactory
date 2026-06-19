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
    }
}
