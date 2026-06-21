using System.Security.Cryptography;
using System.Text.Json;

namespace LafScreenStream.Packager;

/// <summary>
/// Generates a ready-to-send client package: copies the published client into an output folder and writes
/// client-config.json (server address + token), a checksum, and a plain-language README. No code is
/// compiled at run time; the client is copied from a pre-published folder.
/// </summary>
public static class ClientPackager
{
    public record Result(bool Ok, string OutputDir, string ExePath, List<string> Files, string? Error);

    public static Result Generate(string clientPublishDir, string outputDir, string serverWsUrl, string displayName, string token)
    {
        try
        {
            if (!Directory.Exists(clientPublishDir))
                return new(false, outputDir, "", new(), $"Client build not found at '{clientPublishDir}'. Publish the client first.");

            Directory.CreateDirectory(outputDir);
            foreach (var f in Directory.GetFiles(clientPublishDir))
                File.Copy(f, Path.Combine(outputDir, Path.GetFileName(f)), overwrite: true);

            var exe = Path.Combine(outputDir, "LAFScreenStream.Client.exe");

            var cfg = JsonSerializer.Serialize(new { serverWsUrl, displayName, token },
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(outputDir, "client-config.json"), cfg);

            var checksum = File.Exists(exe) ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(exe))).ToLowerInvariant() : "";
            File.WriteAllText(Path.Combine(outputDir, "checksum.txt"), $"LAFScreenStream.Client.exe SHA256: {checksum}");

            File.WriteAllText(Path.Combine(outputDir, "README-CLIENT.txt"), Readme(serverWsUrl, displayName));

            var files = Directory.GetFiles(outputDir).Select(Path.GetFileName).Where(x => x != null).Select(x => x!).ToList();
            return new(File.Exists(exe), outputDir, exe, files, File.Exists(exe) ? null : "Client EXE missing in the publish folder.");
        }
        catch (Exception ex)
        {
            return new(false, outputDir, "", new(), ex.Message);
        }
    }

    private static string Readme(string serverWsUrl, string displayName) =>
$@"LAF ScreenStream Assist - CLIENT
================================

What this is: a CONSENT-BASED screen sharing app. When you run it, it shares ONLY your primary screen
with the person who sent it to you, so they can help you.

It does NOT: capture your keyboard, clipboard, files, webcam, or microphone. It does NOT control your PC.
It does NOT run hidden or start with Windows. You can stop any time with the Disconnect button.

How to use:
  1. Double-click  LAFScreenStream.Client.exe
  2. A window opens showing you are sharing your screen to: {serverWsUrl}
  3. To STOP sharing, click the Disconnect button (or close the window).

Display name: {displayName}
If it cannot connect: the server PC must be reachable (same Wi-Fi/LAN, or a public address with the port
opened). See README-START-HERE.txt on the server side.

Only run this if YOU agreed to share your screen.
";
}
