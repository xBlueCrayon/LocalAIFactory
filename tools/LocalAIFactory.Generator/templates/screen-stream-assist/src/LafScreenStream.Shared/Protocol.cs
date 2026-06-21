using System.Security.Cryptography;
using System.Text.Json;

namespace LafScreenStream.Shared;

/// <summary>WebSocket message types. The client connects OUTBOUND to the server and only ever uploads frames.</summary>
public static class Protocol
{
    public const string Handshake = "handshake";
    public const string Frame = "frame";
    public const string Heartbeat = "heartbeat";
    public const string Disconnect = "disconnect";

    public static string ToJson(object o) => JsonSerializer.Serialize(o);
    public static JsonElement Parse(string s) => JsonDocument.Parse(s).RootElement;
    public static string TypeOf(JsonElement e) => e.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
}

public record HandshakeMessage(string token, string displayName, string sessionId)
{
    public string type => Protocol.Handshake;
}

/// <summary>A single primary-screen frame. Payload is a base64 image (PNG from the fake source, JPEG from real capture).</summary>
public record FrameMessage(string sessionId, long frameNumber, int width, int height, string contentType, string dataBase64, long tsUnixMs)
{
    public string type => Protocol.Frame;
}

public record DisconnectMessage(string sessionId, string reason)
{
    public string type => Protocol.Disconnect;
}

/// <summary>Session token helpers. Tokens are required on every stream; comparison is constant-time.</summary>
public static class Tokens
{
    public static string New() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

    public static bool Validate(string? expected, string? presented)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(presented)) return false;
        var a = System.Text.Encoding.UTF8.GetBytes(expected);
        var b = System.Text.Encoding.UTF8.GetBytes(presented);
        if (a.Length != b.Length) return false;
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}

/// <summary>
/// The product's explicit safety contract. Surfaced in the client UI + dashboard and asserted by a
/// source-scan test. This tool is CONSENT-BASED screen sharing, not surveillance.
/// </summary>
public static class SafetyManifest
{
    public const string PrimaryScreenOnly = "Shares the PRIMARY screen only.";
    public static readonly string[] DoesNotDo =
    {
        "No keyboard capture", "No clipboard capture", "No file access/exfiltration", "No webcam",
        "No microphone", "No remote control", "No hidden/stealth window", "No background service",
        "No autostart with Windows", "No persistence", "No UAC bypass"
    };
    public const string ClientVisibilityRule = "The client window is always visible and shows status + a Disconnect button + a sharing warning.";
}
