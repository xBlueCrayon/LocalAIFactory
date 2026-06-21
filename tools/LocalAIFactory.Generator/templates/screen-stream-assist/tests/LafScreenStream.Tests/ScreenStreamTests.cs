using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using LafScreenStream.Packager;
using LafScreenStream.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LafScreenStream.Tests;

public class ScreenStreamTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ScreenStreamTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("LAFSS_NO_BROWSER", "1");
        _factory = factory;
    }

    private async Task<string> TokenAsync(HttpClient c)
    {
        var h = await c.GetFromJsonAsync<JsonElement>("/api/health");
        return h.GetProperty("token").GetString()!;
    }

    private static async Task SendText(WebSocket ws, string json) =>
        await ws.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);

    private async Task<WebSocket> OpenAsync(string token, string sessionId, string name)
    {
        var client = _factory.Server.CreateWebSocketClient();
        var url = new UriBuilder(_factory.Server.BaseAddress) { Scheme = "ws", Path = "/stream" }.Uri;
        var ws = await client.ConnectAsync(url, CancellationToken.None);
        await SendText(ws, Protocol.ToJson(new HandshakeMessage(token, name, sessionId)));
        return ws;
    }

    // ---------- unit ----------

    [Fact]
    public void Token_validation_constant_time()
    {
        var t = Tokens.New();
        Assert.True(Tokens.Validate(t, t));
        Assert.False(Tokens.Validate(t, "wrong"));
        Assert.False(Tokens.Validate(t, null));
        Assert.False(Tokens.Validate(null, t));
    }

    [Fact]
    public void Frame_message_round_trips()
    {
        var f = new FrameMessage("s1", 7, 100, 50, "image/png", "AAAA", 123);
        var e = Protocol.Parse(Protocol.ToJson(f));
        Assert.Equal(Protocol.Frame, Protocol.TypeOf(e));
        Assert.Equal(7, e.GetProperty("frameNumber").GetInt64());
        Assert.Equal("image/png", e.GetProperty("contentType").GetString());
    }

    [Fact]
    public void Fake_source_yields_valid_image_bytes()
    {
        var f = new FakeScreenSource().Capture(1);
        Assert.True(f.Data.Length > 0);
        Assert.Equal("image/png", f.ContentType);
    }

    // ---------- http ----------

    [Fact]
    public async Task Health_endpoint_ok()
    {
        var c = _factory.CreateClient();
        var r = await c.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var j = await r.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ok", j.GetProperty("status").GetString());
        Assert.False(string.IsNullOrEmpty(j.GetProperty("serverWsUrl").GetString()));
    }

    [Fact]
    public async Task Dashboard_renders_with_controls()
    {
        var c = _factory.CreateClient();
        var html = await c.GetStringAsync("/");
        Assert.Contains("ScreenStream", html);
        Assert.Contains("generate-client-btn", html);
        Assert.Contains("clients-table", html);
    }

    // ---------- streaming ----------

    [Fact]
    public async Task Invalid_token_is_rejected()
    {
        var client = _factory.Server.CreateWebSocketClient();
        var url = new UriBuilder(_factory.Server.BaseAddress) { Scheme = "ws", Path = "/stream" }.Uri;
        var ws = await client.ConnectAsync(url, CancellationToken.None);
        await SendText(ws, Protocol.ToJson(new HandshakeMessage("not-the-token", "Mallory", "bad1")));
        // server closes on invalid token
        var buf = new byte[1024];
        var r = await ws.ReceiveAsync(buf, CancellationToken.None);
        Assert.Equal(WebSocketMessageType.Close, r.MessageType);

        var c = _factory.CreateClient();
        var clients = await c.GetFromJsonAsync<List<JsonElement>>("/api/clients");
        Assert.DoesNotContain(clients!, x => x.GetProperty("id").GetString() == "bad1");
    }

    [Fact]
    public async Task Valid_token_streams_fake_frames_and_dashboard_counts_them()
    {
        var http = _factory.CreateClient();
        var token = await TokenAsync(http);
        var ws = await OpenAsync(token, "good1", "Alice");
        var src = new FakeScreenSource();
        for (long n = 1; n <= 5; n++)
        {
            var f = src.Capture(n);
            await SendText(ws, Protocol.ToJson(new FrameMessage("good1", n, f.Width, f.Height, f.ContentType,
                Convert.ToBase64String(f.Data), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())));
        }
        await Task.Delay(300);
        var clients = await http.GetFromJsonAsync<List<JsonElement>>("/api/clients");
        var me = clients!.First(x => x.GetProperty("id").GetString() == "good1");
        Assert.True(me.GetProperty("frameCount").GetInt64() >= 5, "frames should be counted");
        Assert.True(me.GetProperty("connected").GetBoolean());

        var frame = await http.GetAsync("/api/frame/good1");
        Assert.Equal(HttpStatusCode.OK, frame.StatusCode);
    }

    [Fact]
    public async Task Disconnect_stops_the_stream()
    {
        var http = _factory.CreateClient();
        var token = await TokenAsync(http);
        var ws = await OpenAsync(token, "disc1", "Bob");
        var f = new FakeScreenSource().Capture(1);
        await SendText(ws, Protocol.ToJson(new FrameMessage("disc1", 1, f.Width, f.Height, f.ContentType,
            Convert.ToBase64String(f.Data), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())));
        await Task.Delay(150);

        var kick = await http.PostAsync("/api/disconnect/disc1", null);
        Assert.Equal(HttpStatusCode.OK, kick.StatusCode);
        await Task.Delay(250);

        var clients = await http.GetFromJsonAsync<List<JsonElement>>("/api/clients");
        var me = clients!.First(x => x.GetProperty("id").GetString() == "disc1");
        Assert.False(me.GetProperty("connected").GetBoolean());
    }

    // ---------- packager ----------

    [Fact]
    public async Task Generate_client_without_publish_returns_friendly_error_not_500()
    {
        var http = _factory.CreateClient();
        var r = await http.PostAsJsonAsync("/api/generate-client", new { displayName = "TestClient" });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode); // domain failure, not a crash
        var j = await r.Content.ReadFromJsonAsync<JsonElement>();
        // default ClientTemplate dir does not exist in the test content root -> friendly error
        Assert.False(j.GetProperty("ok").GetBoolean());
        Assert.False(string.IsNullOrEmpty(j.GetProperty("error").GetString()));
    }

    [Fact]
    public void Packager_creates_a_full_client_package()
    {
        var publish = Path.Combine(Path.GetTempPath(), "ss-pub-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(publish);
        File.WriteAllBytes(Path.Combine(publish, "LAFScreenStream.Client.exe"), new byte[] { 1, 2, 3, 4 });
        File.WriteAllText(Path.Combine(publish, "LAFScreenStream.Client.dll"), "dummy");
        var outDir = Path.Combine(Path.GetTempPath(), "ss-out-" + Guid.NewGuid().ToString("N"));

        var res = ClientPackager.Generate(publish, outDir, "ws://10.0.0.5:5090/stream", "Charlie", "tok123");
        Assert.True(res.Ok, res.Error);
        Assert.True(File.Exists(Path.Combine(outDir, "LAFScreenStream.Client.exe")));
        Assert.True(File.Exists(Path.Combine(outDir, "client-config.json")));
        Assert.True(File.Exists(Path.Combine(outDir, "README-CLIENT.txt")));
        Assert.True(File.Exists(Path.Combine(outDir, "checksum.txt")));
        var cfg = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(Path.Combine(outDir, "client-config.json")));
        Assert.Equal("ws://10.0.0.5:5090/stream", cfg.GetProperty("serverWsUrl").GetString());
        Assert.Equal("tok123", cfg.GetProperty("token").GetString());
    }

    // ---------- security ----------

    [Fact]
    public void Source_contains_no_surveillance_apis()
    {
        var srcRoot = FindSrcRoot();
        var forbidden = new[]
        {
            "SetWindowsHookEx", "GetAsyncKeyState", "keybd_event", "GetKeyboardState", "RegisterHotKey",
            "Clipboard.GetText", "Clipboard.SetText", "Clipboard.GetData",
            "waveInOpen", "capCreateCaptureWindow", "MediaCapture", "VideoCapture"
        };
        var files = Directory.GetFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));
        var hits = new List<string>();
        foreach (var f in files)
        {
            var text = File.ReadAllText(f);
            foreach (var api in forbidden)
                if (text.Contains(api)) hits.Add($"{Path.GetFileName(f)}: {api}");
        }
        Assert.True(hits.Count == 0, "forbidden surveillance API(s) found: " + string.Join(", ", hits));
    }

    [Fact]
    public void Safety_manifest_lists_the_no_capture_guarantees()
    {
        Assert.Contains("No keyboard capture", SafetyManifest.DoesNotDo);
        Assert.Contains("No remote control", SafetyManifest.DoesNotDo);
        Assert.Contains("No microphone", SafetyManifest.DoesNotDo);
    }

    private static string FindSrcRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var src = Path.Combine(dir.FullName, "src", "LafScreenStream.Shared");
            if (Directory.Exists(src)) return Path.Combine(dir.FullName, "src");
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("src root not found");
    }
}
