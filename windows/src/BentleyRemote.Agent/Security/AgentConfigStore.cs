using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BentleyRemote.Agent.Security;

internal sealed record AgentConfig(
    string AgentId,
    string? PhoneId,
    string? PhoneName,
    string? ProtectedSecret,
    string? LastKnownAddress = null)
{
    public bool IsPaired => !string.IsNullOrWhiteSpace(PhoneId) && !string.IsNullOrWhiteSpace(ProtectedSecret);
}

internal sealed class AgentConfigStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private AgentConfig _current;

    public AgentConfigStore()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BentleyRemote");
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "agent-config.json");
        _current = Load() ?? new AgentConfig(Guid.NewGuid().ToString(), null, null, null);
        Save(_current);
    }

    public AgentConfig Current
    {
        get { lock (_gate) return _current; }
    }

    public byte[]? GetSecret()
    {
        var protectedSecret = Current.ProtectedSecret;
        if (string.IsNullOrWhiteSpace(protectedSecret)) return null;
        try
        {
            return ProtectedData.Unprotect(
                Convert.FromBase64String(protectedSecret),
                Encoding.UTF8.GetBytes("BentleyRemote/v1"),
                DataProtectionScope.CurrentUser);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public void CompletePairing(string phoneId, string phoneName, byte[] secret)
    {
        var encrypted = ProtectedData.Protect(
            secret,
            Encoding.UTF8.GetBytes("BentleyRemote/v1"),
            DataProtectionScope.CurrentUser);
        lock (_gate)
        {
            _current = _current with
            {
                PhoneId = phoneId,
                PhoneName = phoneName,
                ProtectedSecret = Convert.ToBase64String(encrypted)
            };
            Save(_current);
        }
    }

    public void SetLastKnownAddress(string address)
    {
        if (!System.Net.IPAddress.TryParse(address, out var parsed) ||
            parsed.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            return;

        lock (_gate)
        {
            if (string.Equals(_current.LastKnownAddress, parsed.ToString(), StringComparison.Ordinal)) return;
            _current = _current with { LastKnownAddress = parsed.ToString() };
            Save(_current);
        }
    }

    public void ForgetPhone()
    {
        lock (_gate)
        {
            _current = _current with
            {
                PhoneId = null,
                PhoneName = null,
                ProtectedSecret = null,
                LastKnownAddress = null
            };
            Save(_current);
        }
    }

    private AgentConfig? Load()
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<AgentConfig>(File.ReadAllText(_path))
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void Save(AgentConfig config)
    {
        var tempPath = _path + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(tempPath, _path, true);
    }
}
