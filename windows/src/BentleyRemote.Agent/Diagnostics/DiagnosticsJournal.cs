using System.Diagnostics;
using System.Text;

namespace BentleyRemote.Agent.Diagnostics;

/// <summary>
/// A bounded, local-only connection journal. It is deliberately free of pairing secrets,
/// media titles, audio, desktop previews, and protocol payloads.
/// </summary>
internal static class DiagnosticsJournal
{
    private const int MaxLines = 400;
    private static readonly object Gate = new();
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NimbMote",
        "connection-diagnostics.log");

    public static void Write(string source, string message)
    {
        var safeSource = source.ReplaceLineEndings(" ").Trim();
        if (safeSource.Length > 48) safeSource = safeSource[..48];
        var safeMessage = message.ReplaceLineEndings(" ").Trim();
        if (safeMessage.Length > 320) safeMessage = safeMessage[..320];
        var line = $"{DateTimeOffset.Now:O} [{safeSource}] {safeMessage}";
        Debug.WriteLine($"[NimbMote] {line}");

        lock (Gate)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.AppendAllText(FilePath, line + Environment.NewLine, Encoding.UTF8);
                var lines = File.ReadAllLines(FilePath);
                if (lines.Length > MaxLines)
                    File.WriteAllLines(FilePath, lines[^MaxLines..], Encoding.UTF8);
            }
            catch
            {
                // Diagnostics must never interfere with reconnecting or the tray agent.
            }
        }
    }

    public static string Read()
    {
        lock (Gate)
        {
            try
            {
                return File.Exists(FilePath)
                    ? File.ReadAllText(FilePath, Encoding.UTF8)
                    : "No NimbMote connection events have been recorded yet.";
            }
            catch
            {
                return "NimbMote diagnostics could not be read.";
            }
        }
    }
}
