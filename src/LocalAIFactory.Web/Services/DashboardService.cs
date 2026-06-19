using System.Diagnostics;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Core.ViewModels;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Services;

// Phase 1.2.3: builds the dashboard with simple, safe, SQL-Server-friendly queries.
//
//  * No GroupBy(_ => 1) and no complex aggregation — every metric is a plain COUNT(*).
//  * Independent query groups run in PARALLEL via Task.WhenAll. Because a single AppDbContext is not
//    thread-safe, each parallel branch gets its OWN context from its OWN DI scope (RunAsync), so the
//    queries never overlap on one context. This is the same scope-per-operation pattern the hosted
//    services use, and it keeps the dashboard well under the 1s budget even as tables grow.
//  * Recent-activity lists are projected to lightweight rows so large text columns are never loaded.
//  * Health is read from the cached snapshot — no external service call on the render path.
public sealed class DashboardService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IServiceHealthCache _health;
    private readonly ILogger<DashboardService> _log;

    public DashboardService(IServiceScopeFactory scopeFactory, IServiceHealthCache health, ILogger<DashboardService> log)
    {
        _scopeFactory = scopeFactory; _health = health; _log = log;
    }

    public async Task<DashboardViewModel> BuildAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _log.LogInformation("Dashboard build started.");

        var vm = new DashboardViewModel { Health = _health.Current };

        // Each task below runs on its own scope/context/connection, so they can execute concurrently.
        var knowledgeTask = RunAsync(async db => (
            total: await db.KnowledgeItems.CountAsync(ct),
            approved: await db.KnowledgeItems.CountAsync(k => k.IsApproved, ct),
            needsReview: await db.KnowledgeItems.CountAsync(k => k.Status == KnowledgeStatus.NeedsReview, ct)));

        var jobsTask = RunAsync(async db => (
            running: await db.IngestionJobs.CountAsync(j => j.Status == IngestionJobStatus.Running, ct),
            pending: await db.IngestionJobs.CountAsync(j => j.Status == IngestionJobStatus.Pending, ct),
            completed: await db.IngestionJobs.CountAsync(j => j.Status == IngestionJobStatus.Completed, ct),
            failed: await db.IngestionJobs.CountAsync(j => j.Status == IngestionJobStatus.Failed, ct)));

        var rulesTask = RunAsync(async db => (
            total: await db.BusinessRules.CountAsync(ct),
            needsReview: await db.BusinessRules.CountAsync(r => r.Status == BusinessRuleStatus.NeedsReview, ct)));

        var simpleTask = RunAsync(async db => (
            projects: await db.Projects.CountAsync(ct),
            approvedCode: await db.ApprovedCodeSnippets.CountAsync(ct),
            chats: await db.ChatSessions.CountAsync(ct),
            imports: await db.ImportedFiles.CountAsync(ct),
            models: await db.ModelConfigurations.CountAsync(ct),
            workspaces: await db.Workspaces.CountAsync(ct),
            codeCandidates: await db.ExtractedCodeBlocks.CountAsync(b => b.Status == KnowledgeStatus.NeedsReview, ct),
            profileSections: await db.ProjectProfileSections.CountAsync(s => s.Status == KnowledgeStatus.NeedsReview, ct)));

        var activeModelTask = RunAsync(db => db.ModelConfigurations.AsNoTracking()
            .Where(m => m.IsEnabled).OrderByDescending(m => m.IsDefault)
            .Select(m => m.Name).FirstOrDefaultAsync(ct));

        var recentKnowledgeTask = RunAsync(db => db.KnowledgeItems.AsNoTracking()
            .OrderByDescending(k => k.UpdatedUtc).Take(6)
            .Select(k => new RecentKnowledgeRow(k.Id, k.Title, k.SourceType, k.Status, k.UpdatedUtc))
            .ToListAsync(ct));

        var recentImportsTask = RunAsync(db => db.IngestionJobs.AsNoTracking()
            .OrderByDescending(j => j.Id).Take(6)
            .Select(j => new RecentImportRow(j.FileName, j.Status, j.TotalFiles, j.ProcessedFiles, j.CreatedUtc))
            .ToListAsync(ct));

        var recentApprovalsTask = RunAsync(db => db.AuditLogs.AsNoTracking()
            .OrderByDescending(a => a.Id).Take(8)
            .Select(a => new RecentApprovalRow(a.Action, a.EntityName, a.Details, a.CreatedUtc))
            .ToListAsync(ct));

        await Task.WhenAll(knowledgeTask, jobsTask, rulesTask, simpleTask,
            activeModelTask, recentKnowledgeTask, recentImportsTask, recentApprovalsTask);

        var k = knowledgeTask.Result;
        vm.KnowledgeItems = k.total; vm.ApprovedKnowledge = k.approved; vm.KnowledgeNeedsReview = k.needsReview;

        var j = jobsTask.Result;
        vm.JobsRunning = j.running; vm.JobsPending = j.pending; vm.JobsCompleted = j.completed; vm.JobsFailed = j.failed;

        var r = rulesTask.Result;
        vm.BusinessRules = r.total; vm.RulesNeedsReview = r.needsReview;

        var sgl = simpleTask.Result;
        vm.Projects = sgl.projects; vm.ApprovedCode = sgl.approvedCode; vm.ChatSessions = sgl.chats;
        vm.ImportedFiles = sgl.imports; vm.ModelConfigurations = sgl.models; vm.Workspaces = sgl.workspaces;
        vm.CodeCandidates = sgl.codeCandidates; vm.ProfileSectionsNeedsReview = sgl.profileSections;

        vm.ActiveModel = activeModelTask.Result;
        vm.RecentKnowledge = recentKnowledgeTask.Result;
        vm.RecentImports = recentImportsTask.Result;
        vm.RecentApprovals = recentApprovalsTask.Result;

        _log.LogInformation(
            "Dashboard build completed in {ElapsedMs} ms (knowledge={Knowledge}, jobs={JobsTotal}, rules={Rules}).",
            sw.ElapsedMilliseconds, vm.KnowledgeItems, vm.JobsRunning + vm.JobsPending + vm.JobsCompleted + vm.JobsFailed, vm.BusinessRules);

        return vm;
    }

    // Runs a query on a dedicated scope+context so parallel branches never share a DbContext.
    private async Task<T> RunAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await query(db);
    }
}
