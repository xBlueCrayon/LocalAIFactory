using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using LafScreenStream.Shared;

namespace LafScreenStream.Client;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ClientForm());
    }
}

/// <summary>
/// Always-visible client window. Shows the server it shares to, the session id, the sharing status, an
/// explicit "primary screen is being shared" warning, and a Disconnect button. Streams the primary screen
/// only; stops capture and exits cleanly on disconnect.
/// </summary>
public sealed class ClientForm : Form
{
    private readonly Label _server = new() { AutoSize = true, Top = 50, Left = 20 };
    private readonly Label _session = new() { AutoSize = true, Top = 75, Left = 20 };
    private readonly Label _status = new() { AutoSize = true, Top = 100, Left = 20, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
    private readonly Label _frames = new() { AutoSize = true, Top = 125, Left = 20 };
    private readonly Button _disconnect = new() { Text = "Disconnect", Top = 160, Left = 20, Width = 140, Height = 34 };
    private readonly CancellationTokenSource _cts = new();
    private readonly string _sessionId = Guid.NewGuid().ToString("N");
    private readonly IScreenSource _capture = new GdiScreenSource();

    public ClientForm()
    {
        Text = "LAF ScreenStream Assist - Client (you are sharing your screen)";
        Width = 560; Height = 250; StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false; FormBorderStyle = FormBorderStyle.FixedSingle;

        var warn = new Label { AutoSize = true, Top = 14, Left = 20, ForeColor = Color.DarkRed,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Text = "WARNING: your PRIMARY screen is being shared with the server below." };
        Controls.AddRange(new Control[] { warn, _server, _session, _status, _frames, _disconnect });
        _session.Text = $"Session: {_sessionId}";
        _status.Text = "Status: starting...";
        _disconnect.Click += (_, _) => { _status.Text = "Status: disconnecting..."; _cts.Cancel(); };
        FormClosing += (_, _) => _cts.Cancel();

        var cfg = LoadConfig();
        _server.Text = $"Connected server: {cfg?.serverWsUrl ?? "(missing client-config.json)"}";
        if (cfg is null) { _status.Text = "Status: ERROR - client-config.json not found next to this app."; return; }
        _ = RunAsync(cfg);
    }

    private async Task RunAsync(ClientConfig cfg)
    {
        try
        {
            using var ws = new ClientWebSocket();
            SetStatus("connecting...");
            await ws.ConnectAsync(new Uri(cfg.serverWsUrl), _cts.Token);
            await SendText(ws, Protocol.ToJson(new HandshakeMessage(cfg.token, cfg.displayName, _sessionId)));
            SetStatus("SHARING your primary screen");

            long n = 0;
            var delay = TimeSpan.FromMilliseconds(333); // ~3 FPS, light on the network
            while (!_cts.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                n++;
                var f = _capture.Capture(n);
                var frame = new FrameMessage(_sessionId, n, f.Width, f.Height, f.ContentType, Convert.ToBase64String(f.Data),
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                await SendText(ws, Protocol.ToJson(frame));
                SetFrames($"Frames sent: {n}");
                await Task.Delay(delay, _cts.Token);
            }
            try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "user disconnect", CancellationToken.None); } catch { }
            SetStatus("Disconnected. You can close this window.");
        }
        catch (OperationCanceledException) { SetStatus("Disconnected. You can close this window."); }
        catch (Exception ex) { SetStatus($"Could not connect: {ex.Message}"); }
    }

    private static async Task SendText(ClientWebSocket ws, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private void SetStatus(string s) { if (IsHandleCreated) BeginInvoke(() => _status.Text = $"Status: {s}"); }
    private void SetFrames(string s) { if (IsHandleCreated) BeginInvoke(() => _frames.Text = s); }

    private static ClientConfig? LoadConfig()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "client-config.json");
        if (!File.Exists(path)) return null;
        try { return JsonSerializer.Deserialize<ClientConfig>(File.ReadAllText(path)); } catch { return null; }
    }
}

public sealed record ClientConfig(string serverWsUrl, string displayName, string token);
