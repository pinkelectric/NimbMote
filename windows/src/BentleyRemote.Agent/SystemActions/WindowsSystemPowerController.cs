using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BentleyRemote.Agent.SystemActions;

internal sealed class WindowsSystemPowerController : ISystemPowerController
{
    public bool Lock() => LockWorkStation();

    public bool Sleep() => SetSuspendState(hibernate: false, forceCritical: false, disableWakeEvent: false);

    public bool Restart() => StartShutdown("/r /t 0");

    public bool Shutdown() => StartShutdown("/s /t 0");

    private static bool StartShutdown(string fixedArguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "shutdown.exe"),
                Arguments = fixedArguments,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            return process is not null;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LockWorkStation();

    [DllImport("powrprof.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetSuspendState(
        [MarshalAs(UnmanagedType.Bool)] bool hibernate,
        [MarshalAs(UnmanagedType.Bool)] bool forceCritical,
        [MarshalAs(UnmanagedType.Bool)] bool disableWakeEvent);
}
