using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace BentleyRemote.Agent.Desktop;

internal sealed class DesktopPreviewService
{
    internal const int MaxImageBytes = 1_048_576;
    private const int MaxWidth = 1280;
    private const int MaxHeight = 720;

    /// <summary>Reads the current Windows wallpaper image; no screen pixels are captured or persisted.</summary>
    public byte[] CaptureDesktopWallpaperJpeg()
    {
        var wallpaperPath = GetWallpaperPath();
        if (!File.Exists(wallpaperPath)) throw new InvalidOperationException("Windows wallpaper image is unavailable.");
        using var input = File.OpenRead(wallpaperPath);
        using var image = Image.FromStream(input);
        using var source = new Bitmap(image);
        var scale = Math.Min(1d, Math.Min((double)MaxWidth / source.Width, (double)MaxHeight / source.Height));
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        using var resized = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(resized))
        {
            graphics.Clear(Color.Black);
            graphics.DrawImage(source, new Rectangle(0, 0, width, height));
        }
        var encoder = ImageCodecInfo.GetImageEncoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
        foreach (var quality in new long[] { 75, 60, 45, 30 })
        {
            using var stream = new MemoryStream();
            using var parameters = new EncoderParameters(1);
            parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            resized.Save(stream, encoder, parameters);
            if (stream.Length <= MaxImageBytes) return stream.ToArray();
        }
        throw new InvalidOperationException("Wallpaper preview exceeds the 1 MiB safety limit.");
    }

    private static string GetWallpaperPath()
    {
        var path = new StringBuilder(32_768);
        if (!SystemParametersInfo(SpiGetDesktopWallpaper, (uint)path.Capacity, path, 0) || path.Length == 0)
            throw new InvalidOperationException("Windows did not provide a wallpaper file.");
        return path.ToString();
    }

    private const uint SpiGetDesktopWallpaper = 0x0073;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, StringBuilder pvParam, uint fWinIni);
}
