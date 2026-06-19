using System.Diagnostics;

namespace LocalAIFactory.Web.Middleware;

// Phase 1.2.3: logs every top-level request with its duration. A request that hangs will log a
// "started" line with no matching "completed" line, which pinpoints exactly which endpoint stalled.
// Requests slower than the threshold are logged at Warning so they stand out.
public sealed class RequestTimingMiddleware
{
    private const long SlowThresholdMs = 1000;
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _log;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> log)
    {
        _next = next; _log = log;
    }

    public async Task Invoke(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? "/";

        // Skip static assets — only time controller/page requests.
        if (path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/lib", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/images", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase))
        {
            await _next(ctx);
            return;
        }

        var sw = Stopwatch.StartNew();
        _log.LogInformation("→ {Method} {Path} started.", ctx.Request.Method, path);
        try
        {
            await _next(ctx);
            sw.Stop();
            if (sw.ElapsedMilliseconds > SlowThresholdMs)
                _log.LogWarning("← {Method} {Path} {Status} in {ElapsedMs} ms (SLOW).", ctx.Request.Method, path, ctx.Response.StatusCode, sw.ElapsedMilliseconds);
            else
                _log.LogInformation("← {Method} {Path} {Status} in {ElapsedMs} ms.", ctx.Request.Method, path, ctx.Response.StatusCode, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _log.LogError(ex, "✗ {Method} {Path} FAILED after {ElapsedMs} ms.", ctx.Request.Method, path, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
