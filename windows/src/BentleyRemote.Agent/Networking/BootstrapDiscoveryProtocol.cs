using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BentleyRemote.Agent.Networking;

internal static class BootstrapDiscoveryProtocol
{
    public const string ProbeType = "bentley.bootstrap.probe";
    public const string ResponseType = "bentley.bootstrap.response";
    private const string Domain = "bentley-remote-bootstrap";

    public static bool TryValidateProbe(
        ReadOnlySpan<byte> datagram,
        string pairingCode,
        long now,
        ISet<string> seenNonces,
        out BootstrapProbe probe,
        out string reason)
    {
        probe = default!;
        reason = "invalid bootstrap probe";
        if (datagram.Length is 0 or > 4096) return false;
        try
        {
            using var document = JsonDocument.Parse(datagram.ToArray());
            var root = document.RootElement;
            var version = GetInt(root, "version");
            var type = GetString(root, "type");
            var phoneId = GetString(root, "phoneId");
            var timestamp = GetLong(root, "timestamp");
            var nonce = GetString(root, "nonce");
            if (version != DiscoveryProtocol.Version || type != ProbeType || phoneId.Length is < 8 or > 128)
                return false;
            if (Math.Abs(now - timestamp) > DiscoveryProtocol.ClockWindowMs)
            {
                reason = "bootstrap probe is stale";
                return false;
            }
            if (nonce.Length < 16)
            {
                reason = "bootstrap nonce replayed";
                return false;
            }
            var supplied = Convert.FromBase64String(GetString(root, "proof"));
            var expected = HMACSHA256.HashData(CodeKey(pairingCode), Encoding.UTF8.GetBytes(ProbeBody(phoneId, timestamp, nonce)));
            if (!CryptographicOperations.FixedTimeEquals(supplied, expected))
            {
                reason = "bootstrap code proof rejected";
                return false;
            }
            if (!seenNonces.Add(nonce))
            {
                reason = "bootstrap nonce replayed";
                return false;
            }
            probe = new BootstrapProbe(phoneId, timestamp, nonce);
            reason = "ok";
            return true;
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidOperationException)
        {
            reason = "malformed bootstrap probe";
            return false;
        }
    }

    public static byte[] CreateResponse(
        BootstrapProbe probe,
        string agentId,
        string requesterAddress,
        int requesterPort,
        string pairingCode)
    {
        var proof = Convert.ToBase64String(HMACSHA256.HashData(
            CodeKey(pairingCode),
            Encoding.UTF8.GetBytes(ResponseBody(agentId, probe.PhoneId, probe.Timestamp, probe.Nonce,
                requesterAddress, requesterPort))));
        return JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = DiscoveryProtocol.Version,
            type = ResponseType,
            agentId,
            phoneId = probe.PhoneId,
            timestamp = probe.Timestamp,
            nonce = probe.Nonce,
            requesterAddress,
            requesterPort,
            proof
        });
    }

    internal static byte[] CodeKey(string code) =>
        SHA256.HashData(Encoding.UTF8.GetBytes($"{Domain}\ncode\n{code}"));

    internal static string ProbeBody(string phoneId, long timestamp, string nonce) =>
        string.Join('\n', Domain, DiscoveryProtocol.Version.ToString(CultureInfo.InvariantCulture), "probe", phoneId,
            timestamp.ToString(CultureInfo.InvariantCulture), nonce);

    internal static string ResponseBody(
        string agentId, string phoneId, long timestamp, string nonce, string requesterAddress, int requesterPort) =>
        string.Join('\n', Domain, DiscoveryProtocol.Version.ToString(CultureInfo.InvariantCulture), "response", agentId,
            phoneId, timestamp.ToString(CultureInfo.InvariantCulture), nonce, requesterAddress,
            requesterPort.ToString(CultureInfo.InvariantCulture));

    private static string GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
    private static int GetInt(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.TryGetInt32(out var result) ? result : 0;
    private static long GetLong(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.TryGetInt64(out var result) ? result : 0;
}

internal sealed record BootstrapProbe(string PhoneId, long Timestamp, string Nonce);
