using System.Text.Json;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BentleyRemote.Agent.Audio;
using BentleyRemote.Agent.Media;
using BentleyRemote.Agent.Networking;
using BentleyRemote.Agent.Protocol;
using BentleyRemote.Agent.Security;
using BentleyRemote.Agent.SystemActions;
using BentleyRemote.Agent.Desktop;
using BentleyRemote.Agent.Security;

namespace BentleyRemote.Agent.Core;

internal sealed class AgentCoordinator : IAsyncDisposable
{
    private readonly AgentConfigStore _configStore = new();
    private readonly WindowsMediaSessionService _media = new();
    private readonly WindowsVolumeService _volume = new();
    private readonly SystemActionDispatcher _systemActions = new(new WindowsSystemPowerController());
    private readonly ConnectionHub _hub;
    private readonly DesktopPreviewService _desktopPreview = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly object _statusGate = new();
    private Task? _mediaStartupTask;
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

    public Task StartAsync()
    {
        // Network pairing/reconnect must never wait for GSMTC. At Windows logon, RequestAsync()
        // can temporarily be unavailable while Wi-Fi is already usable; previously that stopped
        // ConnectionHub from starting at all until the user manually restarted the agent.
        _hub.Start();
        _volume.Start();
        _mediaStartupTask = Task.Run(() => StartMediaWhenReadyAsync(_stop.Token));
        RaiseStartupDiagnostic("Network reconnect started; media integration is initializing in background");
        return Task.CompletedTask;
    }

    private async Task StartMediaWhenReadyAsync(CancellationToken cancellationToken)
    {
        var failures = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            Task attempt;
            try
            {
                attempt = _media.StartAsync();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException or COMException)
            {
                failures++;
                RaiseStartupDiagnostic(
                    $"Windows media integration unavailable; retrying while LAN reconnect stays active (attempt {failures}): {ex.Message}");
                await Task.Delay(StartupReadinessPolicy.NextMediaRetryDelay(failures), cancellationToken);
                continue;
            }

            // Keep waiting for the same request when the media broker is merely late. Starting
            // another request here could race the broker and hide the real readiness diagnostic.
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await attempt.WaitAsync(StartupReadinessPolicy.MediaAttemptWindow, cancellationToken);
                    Debug.WriteLine("[BentleyRemote] Windows media integration ready");
                    return;
                }
                catch (TimeoutException)
                {
                    failures++;
                    RaiseStartupDiagnostic(
                        $"Windows media integration is still starting; LAN reconnect remains active (attempt {failures})");
                    await Task.Delay(StartupReadinessPolicy.NextMediaRetryDelay(failures), cancellationToken);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException or COMException)
                {
                    failures++;
                    RaiseStartupDiagnostic(
                        $"Windows media integration unavailable; retrying while LAN reconnect stays active (attempt {failures}): {ex.Message}");
                    await Task.Delay(StartupReadinessPolicy.NextMediaRetryDelay(failures), cancellationToken);
                    break;
                }
            }
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

    private void RaiseStartupDiagnostic(string status)
    {
        Debug.WriteLine($"[BentleyRemote] {status}");
        // Do not overwrite an authenticated-network status with an optional media warning.
        if (!IsConnected) SetStatus(false, status);
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
                case "desktop.preview.request":
                {
                    var requestId = RequiredString(message.Payload, "requestId");
                    if (requestId.Length is < 16 or > 80) throw new InvalidOperationException("Invalid desktop preview request.");
                    var image = _desktopPreview.CapturePrimaryDisplayJpeg();
                    var capturedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var secret = _configStore.GetSecret() ?? throw new InvalidOperationException("Pairing secret unavailable.");
                    var encrypted = DesktopPreviewCrypto.Encrypt(secret, requestId, capturedAt, "image/jpeg", image);
                    await _hub.SendAsync("desktop.preview", new
                    {
                        requestId, capturedAt, mimeType = "image/jpeg", nonce = encrypted.Nonce, ciphertext = encrypted.Ciphertext
                    }, message.Id);
                    return;
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
        _stop.Cancel();
        await _hub.DisposeAsync();
        if (_mediaStartupTask is not null)
        {
            try { await _mediaStartupTask; }
            catch (OperationCanceledException) { }
        }
        await _media.DisposeAsync();
        await _volume.DisposeAsync();
        _stop.Dispose();
    }
}
