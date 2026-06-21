using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using LafScreenStream.Server;
using LafScreenStream.Shared;

var builder = WebApplication.CreateBuilder(args);

int port = builder.Configuration.GetValue("ScreenStream:Port", 5090);
string publicServerUrl = builder.Configuration.GetValue("ScreenStream:PublicServerUrl", $"ws://localhost:{port}/stream") ?? $"ws://localhost:{port}/stream";
string clientPublishDir = builder.Configuration.GetValue("ScreenStream:ClientPublishDir", "ClientTemplate") ?? "ClientTemplate";
string generatedClientsDir = builder.Configuration.GetValue("ScreenStream:GeneratedClientsDir", "GeneratedClients") ?? "GeneratedClients";

builder.WebHost.UseUrls($"http://localhost:{port}");
builder.Services.AddSingleton<SessionHub>();

var app = builder.Build();
var hub = app.Services.GetRequiredService<SessionHub>();
var cancellations = new ConcurrentDictionary<string, CancellationTokenSource>();

// Session token: persist next to the content root so already-generated clients keep working across restarts.
string tokenPath = Path.Combine(app.Environment.ContentRootPath, "screenstream-token.txt");
string token;
if (File.Exists(tokenPath)) token = File.ReadAllText(tokenPath).Trim();
else { token = Tokens.New(); File.WriteAllText(tokenPath, token); }

string Resolve(string p) => Path.IsPathRooted(p) ? p : Path.Combine(app.Environment.ContentRootPath, p);

app.UseWebSockets();

app.MapGet("/", () => Results.Content(Dashboard.Html, "text/html"));
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", product = "LAF ScreenStream Assist", serverWsUrl = publicServerUrl, token, safety = SafetyManifest.DoesNotDo }));
app.MapGet("/api/clients", () => Results.Ok(hub.Snapshot()));

app.MapGet("/api/frame/{id}", (string id) =>
{
    var s = hub.Get(id);
    if (s?.LatestFrame is null) return Results.NotFound();
    return Results.File(s.LatestFrame, s.LatestContentType);
});

app.MapPost("/api/generate-client", (GenRequest req) =>
{
    var name = string.IsNullOrWhiteSpace(req.displayName) ? "Client" : req.displayName.Trim();
    var safe = string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
    if (safe.Length == 0) safe = "Client";
    var outDir = Path.Combine(Resolve(generatedClientsDir), safe);
    var result = LafScreenStream.Packager.ClientPackager.Generate(Resolve(clientPublishDir), outDir, publicServerUrl, name, token);
    return Results.Ok(new { ok = result.Ok, outputDir = result.OutputDir, exePath = result.ExePath, files = result.Files, error = result.Error });
});

app.MapPost("/api/disconnect/{id}", (string id) =>
{
    if (cancellations.TryGetValue(id, out var cts)) cts.Cancel();
    hub.Disconnect(id);
    return Results.Ok(new { id, action = "disconnect-requested" });
});

// Streaming endpoint: client connects OUTBOUND, authenticates with the token, then uploads primary-screen frames.
app.Map("/stream", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest) { context.Response.StatusCode = 400; return; }
    using var ws = await context.WebSockets.AcceptWebSocketAsync();

    var firstJson = await ReceiveText(ws, context.RequestAborted);
    if (firstJson is null) { await CloseSafe(ws, WebSocketCloseStatus.ProtocolError, "no handshake"); return; }

    var hs = Protocol.Parse(firstJson);
    if (Protocol.TypeOf(hs) != Protocol.Handshake ||
        !Tokens.Validate(token, hs.TryGetProperty("token", out var t) ? t.GetString() : null))
    {
        await CloseSafe(ws, WebSocketCloseStatus.PolicyViolation, "invalid token"); // no unauthenticated streaming
        return;
    }

    var sessionId = hs.TryGetProperty("sessionId", out var sid) && !string.IsNullOrWhiteSpace(sid.GetString())
        ? sid.GetString()! : Guid.NewGuid().ToString("N");
    var displayName = hs.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "Client" : "Client";

    var cts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
    cancellations[sessionId] = cts;
    hub.Register(sessionId, displayName);
    try
    {
        while (!cts.IsCancellationRequested)
        {
            var msg = await ReceiveText(ws, cts.Token);
            if (msg is null) break;
            var e = Protocol.Parse(msg);
            var type = Protocol.TypeOf(e);
            if (type == Protocol.Frame)
            {
                var data = Convert.FromBase64String(e.GetProperty("dataBase64").GetString() ?? "");
                hub.RecordFrame(sessionId,
                    data,
                    e.TryGetProperty("contentType", out var ct) ? ct.GetString() ?? "image/png" : "image/png",
                    e.TryGetProperty("width", out var w) ? w.GetInt32() : 0,
                    e.TryGetProperty("height", out var h) ? h.GetInt32() : 0);
            }
            else if (type == Protocol.Disconnect) break;
        }
    }
    catch (OperationCanceledException) { }
    catch (WebSocketException) { }
    finally
    {
        hub.Disconnect(sessionId);
        cancellations.TryRemove(sessionId, out _);
        await CloseSafe(ws, WebSocketCloseStatus.NormalClosure, "bye");
    }
});

// Open the dashboard in the default browser on startup (skipped in tests via LAFSS_NO_BROWSER=1).
app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine($"LAF ScreenStream Assist server running at http://localhost:{port}  (dashboard)");
    Console.WriteLine($"Clients connect to: {publicServerUrl}");
    if (Environment.GetEnvironmentVariable("LAFSS_NO_BROWSER") == "1") return;
    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo($"http://localhost:{port}") { UseShellExecute = true }); }
    catch { /* headless or no shell: ignore */ }
});

app.Run();

static async Task<string?> ReceiveText(WebSocket ws, CancellationToken ct)
{
    var buffer = new byte[64 * 1024];
    using var ms = new MemoryStream();
    while (true)
    {
        WebSocketReceiveResult r;
        try { r = await ws.ReceiveAsync(buffer, ct); }
        catch { return null; }
        if (r.MessageType == WebSocketMessageType.Close) return null;
        ms.Write(buffer, 0, r.Count);
        if (ms.Length > 16 * 1024 * 1024) return null; // safety cap
        if (r.EndOfMessage) break;
    }
    return Encoding.UTF8.GetString(ms.ToArray());
}

static async Task CloseSafe(WebSocket ws, WebSocketCloseStatus status, string desc)
{
    try { if (ws.State == WebSocketState.Open) await ws.CloseAsync(status, desc, CancellationToken.None); } catch { }
}

public record GenRequest(string displayName);
public partial class Program { }
