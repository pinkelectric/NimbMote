using System.Text.Json;
using BentleyRemote.Agent.Audio;
using BentleyRemote.Agent.Media;
using BentleyRemote.Agent.Networking;
using BentleyRemote.Agent.Protocol;
using BentleyRemote.Agent.Security;
using BentleyRemote.Agent.SystemActions;

namespace BentleyRemote.Agent.Core;

internal sealed class AgentCoordinator : IAsyncDisposable
{
    private readonly AgentConfigStore _configStore = new();
    private readonly WindowsMediaSessionService _media = new();
    private readonly WindowsVolumeService _volume = new();
    private readonly SystemActionDispatcher _systemActions = new(new WindowsSystemPowerController());
    private readonly ConnectionHub _hub;
    private readonly object _statusGate = new();
    private string _status = "Starting…";
    private bool _connected;

    public AgentCoordinator()
    {
        _hub = new ConnectionHub(_configStore);
        _hub.ConnectionChanged += OnConnectionChanged;
        _hub.MessageReceived += message => _ = HandleMessageAsync(message);
        _media.StateChanged += state => _ = _hub.SendAsync("state.media", state);
        _volume.StateChanged += state => _ = _hub.SendAsync("state.volume", state);
    }

    public string Status { get { lock (_statusGate) return _status; } }
    public bool IsConnected { get { lock (_statusGate) return _connected; } }
    public bool IsPaired => _configStore.Current.IsPaired;
    public string? PairedPhoneName => _configStore.Current.PhoneName;

    public async Task StartAsync()
    {
        try
        {
            await _media.StartAsync();
            _volume.Start();
            _hub.Start();
        }
        catch (UnauthorizedAccessException)
        {
            SetStatus(false, "Windows denied access to global media sessions");
        }
        catch (Exception ex)
        {
            SetStatus(false, $"Startup failed: {ex.Message}");
        }
    }

    public PairingWindow BeginPairing()
    {
        SetStatus(false, "Pairing window open; enter the code on Android");
        return _hub.BeginPairing();
    }

    public async Task ForgetPhoneAsync()
    {
        await _hub.DisconnectAsync();
        _configStore.ForgetPhone();
        SetStatus(false, "Pairing removed; enter the new phone code");
    }

    private void OnConnectionChanged(bool connected, string status)
    {
        SetStatus(connected, status);
        if (connected) _ = SendSnapshotAsync();
    }

    private void SetStatus(bool connected, string status)
    {
        lock (_statusGate)
        {
            _connected = connected;
            _status = status;
        }
    }

    private Task SendSnapshotAsync() => _hub.SendAsync("state.snapshot", new
    {
        media = _media.Current,
        volume = _volume.Current
    });

    private async Task HandleMessageAsync(ProtocolMessage message)
    {
        bool ok;
        string? error = null;
        string? systemAction = null;
        try
        {
            switch (message.Type)
            {
                case "command.media":
                {
                    var action = RequiredString(message.Payload, "action");
                    ok = await _media.ExecuteAsync(
                        action,
                        OptionalLong(message.Payload, "positionMs"),
                        OptionalLong(message.Payload, "offsetMs"));
                    break;
                }
                case "command.volume":
                {
                    var action = RequiredString(message.Payload, "action");
                    ok = await _volume.ExecuteAsync(
                        action,
                        OptionalFloat(message.Payload, "level"),
                        OptionalFloat(message.Payload, "delta"));
                    break;
                }
                case "system.action":
                {
                    systemAction = RequiredString(message.Payload, "action");
                    ok = _systemActions.TrySchedule(systemAction, TimeSpan.FromMilliseconds(900), succeeded =>
                    {
                        if (!succeeded)
                            _ = _hub.SendAsync("command.result", new
                            {
                                ok = false,
                                action = systemAction,
                                error = "Windows could not complete the accepted system action"
                            }, message.Id);
                    });
                    if (!ok) error = "Unknown or disallowed system action";
                    break;
                }
                default:
                    return;
            }
            if (!ok) error ??= "Windows session rejected or does not support the command";
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentOutOfRangeException)
        {
            ok = false;
            error = ex.Message;
        }

        await _hub.SendAsync("command.result", new
        {
            ok,
            action = systemAction,
            error
        }, message.Id);
    }

    private static string RequiredString(JsonElement payload, string name) =>
        payload.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? throw new JsonException($"{name} is required")
            : throw new JsonException($"{name} is required");

    private static long? OptionalLong(JsonElement payload, string name) =>
        payload.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt64()
            : null;

    private static float? OptionalFloat(JsonElement payload, string name) =>
        payload.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetSingle()
            : null;

    public async ValueTask DisposeAsync()
    {
        await _hub.DisposeAsync();
        await _media.DisposeAsync();
        await _volume.DisposeAsync();
    }
}
