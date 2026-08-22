using System.Security.Cryptography;
using System.Text;

namespace BentleyRemote.Agent.Security;

/// <summary>Domain-separated authenticated encryption for one paired-device desktop preview.</summary>
internal static class DesktopPreviewCrypto
{
    private static readonly byte[] Salt = new byte[32];
    private static readonly byte[] Info = Encoding.UTF8.GetBytes("bentley-remote/v1/desktop-preview/aes-256-gcm");

    public static (string Nonce, string Ciphertext) Encrypt(byte[] pairingSecret, string requestId,
        long capturedAt, string mimeType, byte[] image)
    {
        var key = PairingCrypto.Hkdf(pairingSecret, Salt, Info, 32);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[image.Length + 16];
        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, image, ciphertext.AsSpan(0, image.Length), ciphertext.AsSpan(image.Length),
                Encoding.UTF8.GetBytes(Aad(requestId, capturedAt, mimeType)));
            return (Convert.ToBase64String(nonce), Convert.ToBase64String(ciphertext));
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    public static string Aad(string requestId, long capturedAt, string mimeType) =>
        $"bentley-remote/v1/desktop-preview\n{requestId}\n{capturedAt}\n{mimeType}";
}
