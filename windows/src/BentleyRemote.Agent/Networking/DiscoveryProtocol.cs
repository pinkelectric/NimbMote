using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BentleyRemote.Agent.Networking;

internal static class DiscoveryProtocol
{
    public const int Version = 1;
    public const string ProbeType = "bentley.discovery.probe";
    public const string ResponseType = "bentley.discovery.response";
    public const long ClockWindowMs = 120_000;
    private const string Domain = "bentley-remote-discovery";

    public static DiscoveryChallenge CreateProbe(
        string clientId,
        byte[] secret,
        long? timestamp = null,
        string? nonce = null)
    {
        var challengeTimestamp = timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var challengeNonce = nonce ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
        var proof = Sign(secret, ProbeBody(clientId, challengeTimestamp, challengeNonce));
        var json = JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = Version,
            type = ProbeType,
            clientId,
            timestamp = challengeTimestamp,
            nonce = challengeNonce,
            proof
        });
        return new DiscoveryChallenge(clientId, challengeTimestamp, challengeNonce, json);
    }

    public static byte[] CreateResponseForTest(
        DiscoveryChallenge challenge,
        string serverId,
        string requesterAddress,
        int requesterPort,
        int serverPort,
        byte[] secret)
    {
        var proof = Sign(secret, ResponseBody(
            challenge.ClientId,
            serverId,
            challenge.Timestamp,
            challenge.Nonce,
            requesterAddress,
            requesterPort,
            serverPort));
        return JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = Version,
            type = ResponseType,
            clientId = challenge.ClientId,
            serverId,
            timestamp = challenge.Timestamp,
            nonce = challenge.Nonce,
            requesterAddress,
            requesterPort,
            serverPort,
            proof
        });
    }

    public static bool TryValidateResponse(
        ReadOnlySpan<byte> datagram,
        DiscoveryChallenge challenge,
        string expectedServerId,
        byte[] secret,
        int localPort,
        ISet<string> localAddresses,
        long now,
        out int serverPort,
        out string reason)
    {
        serverPort = 0;
        reason = "invalid discovery response";
        if (datagram.Length is 0 or > 4096)
        {
            reason = "response size is invalid";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(datagram.ToArray());
            var root = document.RootElement;
            if (GetInt(root, "version") != Version || GetString(root, "type") != ResponseType)
            {
                reason = "unsupported discovery response";
                return false;
            }
            var clientId = GetString(root, "clientId");
            var serverId = GetString(root, "serverId");
            var timestamp = GetLong(root, "timestamp");
            var nonce = GetString(root, "nonce");
            var requesterAddress = GetString(root, "requesterAddress");
            var requesterPort = GetInt(root, "requesterPort");
            var responseServerPort = GetInt(root, "serverPort");
            if (!string.Equals(clientId, challenge.ClientId, StringComparison.Ordinal) ||
                !string.Equals(serverId, expectedServerId, StringComparison.Ordinal))
            {
                reason = "discovery identity mismatch";
                return false;
            }
            if (timestamp != challenge.Timestamp || !string.Equals(nonce, challenge.Nonce, StringComparison.Ordinal))
            {
                reason = "discovery challenge mismatch";
                return false;
            }
            if (Math.Abs(now - timestamp) > ClockWindowMs)
            {
                reason = "discovery response is stale";
                return false;
            }
            if (requesterPort != localPort || !localAddresses.Contains(requesterAddress))
            {
                reason = "discovery endpoint mismatch";
                return false;
            }
            if (responseServerPort is < 1 or > 65535)
            {
                reason = "invalid WebSocket port";
                return false;
            }

            byte[] supplied;
            try { supplied = Convert.FromBase64String(GetString(root, "proof")); }
            catch (FormatException)
            {
                reason = "malformed discovery proof";
                return false;
            }
            var expected = HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(ResponseBody(
                clientId,
                serverId,
                timestamp,
                nonce,
                requesterAddress,
                requesterPort,
                responseServerPort)));
            if (!CryptographicOperations.FixedTimeEquals(supplied, expected))
            {
                reason = "bad discovery proof";
                return false;
            }
            serverPort = responseServerPort;
            reason = "ok";
            return true;
        }
        catch (JsonException)
        {
            reason = "malformed discovery JSON";
            return false;
        }
        catch (InvalidOperationException)
        {
            reason = "malformed discovery fields";
            return false;
        }
    }

    private static string ProbeBody(string clientId, long timestamp, string nonce) =>
        string.Join('\n', Domain, Version.ToString(CultureInfo.InvariantCulture), "probe", clientId,
            timestamp.ToString(CultureInfo.InvariantCulture), nonce);

    private static string ResponseBody(
        string clientId,
        string serverId,
        long timestamp,
        string nonce,
        string requesterAddress,
        int requesterPort,
        int serverPort) =>
        string.Join('\n', Domain, Version.ToString(CultureInfo.InvariantCulture), "response", clientId, serverId,
            timestamp.ToString(CultureInfo.InvariantCulture), nonce, requesterAddress,
            requesterPort.ToString(CultureInfo.InvariantCulture), serverPort.ToString(CultureInfo.InvariantCulture));

    private static string Sign(byte[] secret, string body) =>
        Convert.ToBase64String(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(body)));

    private static string GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    private static int GetInt(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt32(out var result) ? result : 0;

    private static long GetLong(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt64(out var result) ? result : 0;
}

internal sealed record DiscoveryChallenge(string ClientId, long Timestamp, string Nonce, byte[] Datagram);
