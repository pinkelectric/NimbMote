using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace BentleyRemote.Agent.Browser;

/// <summary>Deliberately narrow browser control: an authenticated phone can reload only the foreground Edge tab.</summary>
internal sealed class BrowserTabController
{
    private const ushort VirtualKeyF5 = 0x74;
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;

    internal static bool IsSupportedAction(string action) => action is "reload" or "restoreYoutube";

    public bool TryExecute(string action, out string? error)
    {
        error = null;
        if (!IsSupportedAction(action))
        {
            error = "Unknown browser action";
            return false;
        }

        var window = GetForegroundWindow();
        if (window == IntPtr.Zero || !IsEdgeWindow(window))
        {
            error = "Open Edge in the foreground, then try again";
            return false;
        }

        if (action == "restoreYoutube" && !GetWindowTitle(window).Contains("youtube", StringComparison.OrdinalIgnoreCase))
        {
            error = "Open the YouTube tab in Edge, then try again";
            return false;
        }

        var inputs = new[]
        {
            KeyboardInput(VirtualKeyF5, 0),
            KeyboardInput(VirtualKeyF5, KeyEventKeyUp),
        };
        if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>()) != (uint)inputs.Length)
        {
            error = "Windows could not reload the Edge tab";
            return false;
        }
        return true;
    }

    private static bool IsEdgeWindow(IntPtr window)
    {
        GetWindowThreadProcessId(window, out var processId);
        if (processId == 0) return false;
        try
        {
            using var process = Process.GetProcessById((int)processId);
            return string.Equals(process.ProcessName, "msedge", StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException) { return false; }
    }

    private static string GetWindowTitle(IntPtr window)
    {
        var title = new StringBuilder(1_024);
        GetWindowText(window, title, title.Capacity);
        return title.ToString();
    }

    private static INPUT KeyboardInput(ushort key, uint flags) => new()
    {
        Type = InputKeyboard,
        Union = new InputUnion { Keyboard = new KEYBDINPUT { VirtualKey = key, Flags = flags } },
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint Type; public InputUnion Union; }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion { [FieldOffset(0)] public KEYBDINPUT Keyboard; }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort VirtualKey;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public nint ExtraInfo;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr window, StringBuilder text, int maxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int inputSize);
}
