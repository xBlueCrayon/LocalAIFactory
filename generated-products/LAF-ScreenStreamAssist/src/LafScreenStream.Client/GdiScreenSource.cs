using System.Drawing.Imaging;
using LafScreenStream.Shared;

namespace LafScreenStream.Client;

/// <summary>Real primary-screen capture via GDI CopyFromScreen, encoded as JPEG. Windows-only, manual runs.</summary>
public sealed class GdiScreenSource : IScreenSource
{
    public FrameData Capture(long frameNumber)
    {
        var b = Screen.PrimaryScreen!.Bounds;
        using var bmp = new Bitmap(b.Width, b.Height);
        using (var g = Graphics.FromImage(bmp))
            g.CopyFromScreen(b.Location, Point.Empty, b.Size);

        var enc = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
        using var ep = new EncoderParameters(1);
        ep.Param[0] = new EncoderParameter(Encoder.Quality, 55L);
        using var ms = new MemoryStream();
        bmp.Save(ms, enc, ep);
        return new FrameData(ms.ToArray(), "image/jpeg", b.Width, b.Height);
    }
}
