namespace LafScreenStream.Shared;

/// <summary>Abstraction over screen capture so automated tests use a deterministic fake and manual runs use real GDI capture.</summary>
public interface IScreenSource
{
    FrameData Capture(long frameNumber);
}

public readonly record struct FrameData(byte[] Data, string ContentType, int Width, int Height);

/// <summary>
/// Deterministic fake frame source for automated tests and headless loopback proof. Returns a tiny valid
/// PNG so the dashboard can display it, without touching any real screen.
/// </summary>
public sealed class FakeScreenSource : IScreenSource
{
    // 1x1 PNG (valid image bytes) — enough to prove the frame pipeline end-to-end.
    private const string Png1x1 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
    private static readonly byte[] Bytes = Convert.FromBase64String(Png1x1);

    public FrameData Capture(long frameNumber) => new(Bytes, "image/png", 1, 1);
}
