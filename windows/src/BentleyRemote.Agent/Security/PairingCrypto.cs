using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BentleyRemote.Agent.Security;

internal static class PairingCrypto
{
    private const string Domain = "bentley-remote-pairing-v2";

    public static PairingExchange CreateRequest(string clientId, string clientName, string pairingCode)
    {
        var keyAgreement = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var publicKey = Convert.ToBase64String(keyAgreement.ExportSubjectPublicKeyInfo());
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
        var proof = SignCode(pairingCode, RequestBody(clientId, timestamp, nonce, publicKey));
        return new PairingExchange(keyAgreement, clientId, pairingCode, timestamp, nonce, publicKey, new
        {
            clientId,
            clientName,
            timestamp,
            nonce,
            publicKey,
            proof,
            crypto = "ECDH-P256-HKDF-SHA256-AES-256-GCM"
        });
    }

    public static byte[] ValidateAndDecryptAccept(
        PairingExchange exchange,
        JsonElement payload,
        string expectedServerId,
        out string serverName)
    {
        var clientId = GetString(payload, "clientId");
        var serverId = GetString(payload, "serverId");
        serverName = GetString(payload, "serverName");
        var timestamp = GetLong(payload, "timestamp");
        var nonce = GetString(payload, "nonce");
        var serverPublicKey = GetString(payload, "publicKey");
        var iv = GetString(payload, "iv");
        var ciphertext = GetString(payload, "ciphertext");
        if (clientId != exchange.ClientId || serverId != expectedServerId ||
            timestamp != exchange.Timestamp || nonce != exchange.Nonce)
            throw new InvalidDataException("Pairing transcript mismatch.");
        var responseBody = ResponseBody(clientId, serverId, timestamp, nonce, exchange.PublicKey,
            serverPublicKey, iv, ciphertext);
        var suppliedProof = Convert.FromBase64String(GetString(payload, "proof"));
        var expectedProof = Convert.FromBase64String(SignCode(exchange.PairingCode, responseBody));
        if (!CryptographicOperations.FixedTimeEquals(suppliedProof, expectedProof))
            throw new InvalidDataException("Pairing response code proof is invalid.");

        using var remote = ECDiffieHellman.Create();
        remote.ImportSubjectPublicKeyInfo(Convert.FromBase64String(serverPublicKey), out _);
        var rawSecret = exchange.KeyAgreement.DeriveRawSecretAgreement(remote.PublicKey);
        var transcript = Encoding.UTF8.GetBytes(Transcript(clientId, serverId, timestamp, nonce,
            exchange.PublicKey, serverPublicKey));
        var encryptionKey = Hkdf(rawSecret, BootstrapCodeKey(exchange.PairingCode), transcript, 32);
        try
        {
            var encrypted = Convert.FromBase64String(ciphertext);
            if (encrypted.Length < 16) throw new InvalidDataException("Encrypted pairing secret is too short.");
            var plain = new byte[encrypted.Length - 16];
            using var aes = new AesGcm(encryptionKey, 16);
            aes.Decrypt(Convert.FromBase64String(iv), encrypted[..^16], encrypted[^16..], plain, transcript);
            if (plain.Length != 32) throw new InvalidDataException("Pairing secret must be 32 bytes.");
            return plain;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(rawSecret);
            CryptographicOperations.ZeroMemory(encryptionKey);
        }
    }

    internal static string RequestBody(string clientId, long timestamp, string nonce, string publicKey) =>
        string.Join('\n', Domain, "request", clientId, timestamp.ToString(CultureInfo.InvariantCulture), nonce, publicKey);

    internal static string ResponseBody(string clientId, string serverId, long timestamp, string nonce,
        string clientPublicKey, string serverPublicKey, string iv, string ciphertext) =>
        string.Join('\n', Domain, "response", clientId, serverId, timestamp.ToString(CultureInfo.InvariantCulture),
            nonce, clientPublicKey, serverPublicKey, iv, ciphertext);

    internal static string Transcript(string clientId, string serverId, long timestamp, string nonce,
        string clientPublicKey, string serverPublicKey) =>
        string.Join('\n', Domain, "transcript", clientId, serverId,
            timestamp.ToString(CultureInfo.InvariantCulture), nonce, clientPublicKey, serverPublicKey);

    internal static string SignCode(string code, string body) =>
        Convert.ToBase64String(HMACSHA256.HashData(BootstrapCodeKey(code), Encoding.UTF8.GetBytes(body)));

    internal static byte[] BootstrapCodeKey(string code) =>
        SHA256.HashData(Encoding.UTF8.GetBytes($"bentley-remote-bootstrap\ncode\n{code}"));

    internal static byte[] Hkdf(byte[] inputKey, byte[] salt, byte[] info, int length)
    {
        var prk = HMACSHA256.HashData(salt, inputKey);
        var output = new byte[length];
        var previous = Array.Empty<byte>();
        var offset = 0;
        byte counter = 1;
        while (offset < length)
        {
            var blockInput = new byte[previous.Length + info.Length + 1];
            previous.CopyTo(blockInput, 0);
            info.CopyTo(blockInput, previous.Length);
            blockInput[^1] = counter++;
            previous = HMACSHA256.HashData(prk, blockInput);
            var take = Math.Min(previous.Length, length - offset);
            previous.AsSpan(0, take).CopyTo(output.AsSpan(offset));
            offset += take;
        }
        CryptographicOperations.ZeroMemory(prk);
        return output;
    }

    private static string GetString(JsonElement payload, string name) =>
        payload.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? throw new InvalidDataException($"{name} is required.")
            : throw new InvalidDataException($"{name} is required.");
    private static long GetLong(JsonElement payload, string name) =>
        payload.TryGetProperty(name, out var value) && value.TryGetInt64(out var result)
            ? result
            : throw new InvalidDataException($"{name} is required.");
}

internal sealed record PairingExchange(
    ECDiffieHellman KeyAgreement,
    string ClientId,
    string PairingCode,
    long Timestamp,
    string Nonce,
    string PublicKey,
    object RequestPayload) : IDisposable
{
    public void Dispose() => KeyAgreement.Dispose();
}
