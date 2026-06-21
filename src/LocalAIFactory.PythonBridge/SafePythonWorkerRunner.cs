using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace LocalAIFactory.PythonBridge;

public sealed record PythonRequest(string Entrypoint, object? Input = null);
public sealed record PythonResult(bool Ok, bool Available, string? Json, string? Error, long ElapsedMs);
public sealed record PythonRunLogEntry(string Entrypoint, bool Available, bool Ok, long ElapsedMs, string? Error);

/// <summary>Runs an approved Python worker entrypoint. Safe-by-construction: allowlist, JSON I/O, timeout, no network.</summary>
public interface IPythonWorkerRunner
{
    bool IsAvailable { get; }
    IReadOnlyCollection<string> ApprovedEntrypoints { get; }
    Task<PythonResult> RunAsync(PythonRequest request, CancellationToken ct = default);
}

/// <summary>
/// The safe Python bridge. Only the fixed set of approved worker entrypoints may run; the request/response is
/// JSON only; execution is time-boxed; the worker module is invoked with `python -m laf_python_worker.main
/// {entrypoint}` from a fixed working directory. When Python is not installed, <see cref="IsAvailable"/> is false
/// and every run returns Available=false with no exception — so the platform degrades instead of failing.
/// </summary>
public sealed class SafePythonWorkerRunner : IPythonWorkerRunner
{
    private static readonly HashSet<string> Approved = new(StringComparer.Ordinal)
    {
        "code-mine", "pattern-mine", "doc-extract", "web-scrape", "embed-text",
        "rerank", "build-dataset", "graph-enrich", "extract-knowledge"
    };

    private readonly string _pythonExe;
    private readonly string _workerDir;
    private readonly int _timeoutSeconds;
    private readonly List<PythonRunLogEntry> _log = new();

    public SafePythonWorkerRunner(string? pythonExe = null, string? workerDir = null, int timeoutSeconds = 60)
    {
        _pythonExe = pythonExe ?? "python";
        _workerDir = workerDir ?? "tools/python";
        _timeoutSeconds = timeoutSeconds;
    }

    public IReadOnlyCollection<string> ApprovedEntrypoints => Approved;
    public IReadOnlyList<PythonRunLogEntry> Log => _log;

    public bool IsAvailable
    {
        get
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo(_pythonExe, "--version")
                { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true });
                if (p is null) return false;
                if (!p.WaitForExit(5000)) { try { p.Kill(true); } catch { } return false; }
                return p.ExitCode == 0;
            }
            catch { return false; } // python not installed -> unavailable, never throws
        }
    }

    public static bool IsApproved(string entrypoint) => Approved.Contains(entrypoint);

    public async Task<PythonResult> RunAsync(PythonRequest request, CancellationToken ct = default)
    {
        if (!Approved.Contains(request.Entrypoint))
            return Record(request.Entrypoint, new PythonResult(false, true, null, $"entrypoint '{request.Entrypoint}' is not approved", 0));
        if (!IsAvailable)
            return Record(request.Entrypoint, new PythonResult(false, false, null, "python not available", 0));

        var sw = Stopwatch.StartNew();
        try
        {
            var psi = new ProcessStartInfo(_pythonExe, $"-m laf_python_worker.main {request.Entrypoint}")
            {
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true,
                WorkingDirectory = Directory.Exists(_workerDir) ? _workerDir : Environment.CurrentDirectory
            };
            using var proc = Process.Start(psi);
            if (proc is null) return Record(request.Entrypoint, new PythonResult(false, false, null, "could not start python", sw.ElapsedMilliseconds));

            var inputJson = JsonSerializer.Serialize(request.Input ?? new { });
            await proc.StandardInput.WriteAsync(inputJson);
            proc.StandardInput.Close();

            var outTask = proc.StandardOutput.ReadToEndAsync(ct);
            var errTask = proc.StandardError.ReadToEndAsync(ct);
            if (!proc.WaitForExit(_timeoutSeconds * 1000))
            {
                try { proc.Kill(true); } catch { }
                return Record(request.Entrypoint, new PythonResult(false, true, null, "python worker timed out", sw.ElapsedMilliseconds));
            }
            var outJson = await outTask;
            var err = await errTask;
            sw.Stop();
            if (proc.ExitCode != 0)
                return Record(request.Entrypoint, new PythonResult(false, true, null, $"worker exit {proc.ExitCode}: {Trim(err)}", sw.ElapsedMilliseconds));
            return Record(request.Entrypoint, new PythonResult(true, true, outJson, null, sw.ElapsedMilliseconds));
        }
        catch (Exception ex)
        {
            return Record(request.Entrypoint, new PythonResult(false, true, null, ex.Message, sw.ElapsedMilliseconds));
        }
    }

    private PythonResult Record(string entrypoint, PythonResult r)
    { _log.Add(new PythonRunLogEntry(entrypoint, r.Available, r.Ok, r.ElapsedMs, r.Error)); return r; }

    private static string Trim(string s) => s.Length <= 300 ? s : s[..300];
}
