using System.Security.Cryptography;
using System.Text;

namespace BentleyRemote.Agent.Security;

/// <summary>Encrypts each APK chunk separately so local network observers cannot read or modify it.</summary>
internal static class TestPackageCrypto
{
    private static readonly byte[] Salt = new byte[32];
    private static readonly byte[] Info = Encoding.UTF8.GetBytes("bentley-remote/v1/test-package/aes-256-gcm");

    public static (string Nonce, string Ciphertext) Encrypt(byte[] pairingSecret, string transferId,
        int index, int total, string sha256, byte[] chunk)
    {
        var key = PairingCrypto.Hkdf(pairingSecret, Salt, Info, 32);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[chunk.Length + 16];
        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, chunk, ciphertext.AsSpan(0, chunk.Length), ciphertext.AsSpan(chunk.Length),
                Encoding.UTF8.GetBytes(Aad(transferId, index, total, sha256)));
            return (Convert.ToBase64String(nonce), Convert.ToBase64String(ciphertext));
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    public static string Aad(string transferId, int index, int total, string sha256) =>
        $"bentley-remote/v1/test-package\n{transferId}\n{index}\n{total}\n{sha256}";
}
