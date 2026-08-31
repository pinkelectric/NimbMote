using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace BentleyRemote.Agent;

internal static class DeskoraTrayIcon
{
    internal static Icon Create()
    {
        using var bitmap = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.FromArgb(90, 59, 134));

            using var pen = new Pen(Color.FromArgb(255, 249, 255), 2.5f)
            {
                LineJoin = LineJoin.Round
            };
            // The phone is deliberately vertical on the left; the larger display is horizontal on the right.
            graphics.DrawRoundedRectangle(pen, 5, 6, 8, 20, 2);
            graphics.DrawRoundedRectangle(pen, 17, 11, 11, 9, 2);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private static void DrawRoundedRectangle(this Graphics graphics, Pen pen, float x, float y, float width, float height, float radius)
    {
        using var path = RoundedRectangle(x, y, width, height, radius);
        graphics.DrawPath(pen, path);
    }

    private static GraphicsPath RoundedRectangle(float x, float y, float width, float height, float radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
