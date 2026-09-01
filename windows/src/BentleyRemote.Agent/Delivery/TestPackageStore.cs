using System.Security.Cryptography;
using System.Text.Json;

namespace BentleyRemote.Agent.Delivery;

internal sealed record TestPackage(string Label, string FileName, long SizeBytes, string Sha256, string Path);

/// <summary>Reads the one APK intentionally staged by the global Codex test-delivery helper.</summary>
internal sealed class TestPackageStore
{
    public static string QueueDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexTestDelivery");
    private static string ManifestPath => Path.Combine(QueueDirectory, "latest.json");
    private static string ApkPath => Path.Combine(QueueDirectory, "latest.apk");

    public TestPackageStore() => Directory.CreateDirectory(QueueDirectory);

    public bool TryGetLatest(out TestPackage? package)
    {
        package = null;
        try
        {
            if (!File.Exists(ManifestPath) || !File.Exists(ApkPath)) return false;
            using var document = JsonDocument.Parse(File.ReadAllText(ManifestPath));
            var root = document.RootElement;
            var label = RequiredString(root, "label");
            var fileName = RequiredString(root, "fileName");
            var sizeBytes = root.GetProperty("sizeBytes").GetInt64();
            var sha256 = RequiredString(root, "sha256");
            if (label.Length is < 1 or > 96 || fileName != Path.GetFileName(fileName) ||
                !fileName.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) ||
                sizeBytes is < 1 or > 200L * 1024 * 1024 ||
                sha256.Length != 64 || !sha256.All(Uri.IsHexDigit)) return false;
            if (new FileInfo(ApkPath).Length != sizeBytes) return false;
            package = new TestPackage(label, fileName, sizeBytes, sha256.ToUpperInvariant(), ApkPath);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException)
        {
            return false;
        }
    }

    public static bool Verify(TestPackage package)
    {
        try
        {
            if (new FileInfo(package.Path).Length != package.SizeBytes) return false;
            var actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(package.Path)));
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(actual), Convert.FromHexString(package.Sha256));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FormatException) { return false; }
    }

    private static string RequiredString(JsonElement root, string name) =>
        root.GetProperty(name).GetString() ?? throw new JsonException($"{name} is required");
}
