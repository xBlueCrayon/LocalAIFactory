using System.Collections.Concurrent;

namespace LafScreenStream.Server;

/// <summary>In-memory registry of connected streaming clients and their latest frame. Thread-safe.</summary>
public sealed class SessionHub
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    public Session Register(string id, string displayName)
    {
        var s = new Session { Id = id, DisplayName = displayName, Connected = true, ConnectedUtc = DateTime.UtcNow };
        _sessions[id] = s;
        return s;
    }

    public void RecordFrame(string id, byte[] data, string contentType, int w, int h)
    {
        if (!_sessions.TryGetValue(id, out var s)) return;
        lock (s.Gate)
        {
            s.FrameCount++;
            s.LatestFrame = data;
            s.LatestContentType = contentType;
            s.Width = w; s.Height = h;
            s.LastFrameUtc = DateTime.UtcNow;
            s.RecentFrames.Enqueue(s.LastFrameUtc);
            while (s.RecentFrames.Count > 0 && (s.LastFrameUtc - s.RecentFrames.Peek()).TotalSeconds > 1.0)
                s.RecentFrames.Dequeue();
        }
    }

    public void Disconnect(string id)
    {
        if (_sessions.TryGetValue(id, out var s)) { s.Connected = false; s.DisconnectedUtc = DateTime.UtcNow; }
    }

    public Session? Get(string id) => _sessions.TryGetValue(id, out var s) ? s : null;

    public IEnumerable<object> Snapshot() => _sessions.Values
        .OrderByDescending(s => s.ConnectedUtc)
        .Select(s => new
        {
            id = s.Id,
            displayName = s.DisplayName,
            connected = s.Connected,
            frameCount = s.FrameCount,
            fps = s.Fps,
            lastFrameMsAgo = s.LastFrameUtc == default ? -1 : (int)(DateTime.UtcNow - s.LastFrameUtc).TotalMilliseconds,
            width = s.Width,
            height = s.Height
        }).ToList();
}

public sealed class Session
{
    public readonly object Gate = new();
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool Connected { get; set; }
    public DateTime ConnectedUtc { get; set; }
    public DateTime DisconnectedUtc { get; set; }
    public long FrameCount { get; set; }
    public DateTime LastFrameUtc { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public byte[]? LatestFrame { get; set; }
    public string LatestContentType { get; set; } = "image/png";
    public Queue<DateTime> RecentFrames { get; } = new();
    public int Fps { get { lock (Gate) return RecentFrames.Count; } }
}
