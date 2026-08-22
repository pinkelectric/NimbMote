using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace BentleyRemote.Agent.Desktop;

internal sealed class DesktopPreviewService
{
    internal const int MaxImageBytes = 1_048_576;
    private const int MaxWidth = 1280;
    private const int MaxHeight = 720;

    /// <summary>Captures only the primary interactive desktop; nothing is persisted.</summary>
    public byte[] CapturePrimaryDisplayJpeg()
    {
        var bounds = Screen.PrimaryScreen?.Bounds ?? throw new InvalidOperationException("Primary display is unavailable.");
        var scale = Math.Min(1d, Math.Min((double)MaxWidth / bounds.Width, (double)MaxHeight / bounds.Height));
        var width = Math.Max(1, (int)Math.Round(bounds.Width * scale));
        var height = Math.Max(1, (int)Math.Round(bounds.Height * scale));
        using var source = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppPArgb);
        using (var graphics = Graphics.FromImage(source)) graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size, CopyPixelOperation.SourceCopy);
        using var resized = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(resized)) graphics.DrawImage(source, new Rectangle(0, 0, width, height));
        var encoder = ImageCodecInfo.GetImageEncoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
        foreach (var quality in new long[] { 75, 60, 45, 30 })
        {
            using var stream = new MemoryStream();
            using var parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
            resized.Save(stream, encoder, parameters);
            if (stream.Length <= MaxImageBytes) return stream.ToArray();
        }
        throw new InvalidOperationException("Desktop preview exceeds the 1 MiB safety limit.");
    }
}
