using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Hosted;

// Drains the ingestion queue in the background, processing each ZIP import job in its own DI scope.
// Phase 1.1: on startup, recovers jobs left Pending/Running by a previous restart and safely requeues them.
public sealed class IngestionBackgroundService : BackgroundService
{
    private readonly IIngestionQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IngestionBackgroundService> _log;

    public IngestionBackgroundService(IIngestionQueue queue, IServiceScopeFactory scopeFactory, ILogger<IngestionBackgroundService> log)
    {
        _queue = queue; _scopeFactory = scopeFactory; _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RecoverInterruptedJobsAsync(stoppingToken);

        await foreach (var jobId in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IProjectIngestionService>();
                await svc.ProcessJobAsync(jobId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _log.LogError(ex, "Ingestion job {Id} crashed in background worker", jobId); }
        }
    }

    private async Task RecoverInterruptedJobsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Jobs that were mid-flight when the process stopped are reset so ProcessJobAsync can run them again
            // (the pipeline is idempotent: imported files are de-duplicated by content hash within the project).
            var interrupted = await db.IngestionJobs
                .Where(j => j.Status == IngestionJobStatus.Running || j.Status == IngestionJobStatus.Pending)
                .OrderBy(j => j.Id)
                .ToListAsync(ct);

            foreach (var job in interrupted)
            {
                job.Status = IngestionJobStatus.Pending;
                job.Phase = IngestionPhase.Pending;
            }
            if (interrupted.Count > 0) await db.SaveChangesAsync(ct);

            foreach (var job in interrupted)
                await _queue.EnqueueAsync(job.Id, ct);

            if (interrupted.Count > 0)
                _log.LogInformation("Recovered and requeued {Count} interrupted ingestion job(s).", interrupted.Count);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Ingestion recovery on startup failed.");
        }
    }
}
