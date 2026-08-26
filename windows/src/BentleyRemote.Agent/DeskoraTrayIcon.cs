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
            graphics.Clear(Color.FromArgb(48, 40, 61));

            using var pen = new Pen(Color.FromArgb(211, 184, 255), 2.5f)
            {
                LineJoin = LineJoin.Round
            };
            graphics.DrawRoundedRectangle(pen, 4, 6, 19, 12, 2);
            graphics.DrawRoundedRectangle(pen, 22, 14, 7, 13, 2);

            using var dot = new SolidBrush(Color.FromArgb(211, 184, 255));
            graphics.FillEllipse(dot, 24.5f, 24, 2.5f, 2.5f);
            using var baseBrush = new SolidBrush(Color.FromArgb(154, 117, 216));
            graphics.FillRoundedRectangle(baseBrush, 10, 20, 8, 2.5f, 1.25f);
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

    private static void FillRoundedRectangle(this Graphics graphics, Brush brush, float x, float y, float width, float height, float radius)
    {
        using var path = RoundedRectangle(x, y, width, height, radius);
        graphics.FillPath(brush, path);
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
