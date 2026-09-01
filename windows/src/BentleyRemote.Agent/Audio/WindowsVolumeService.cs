using NAudio.CoreAudioApi;

namespace BentleyRemote.Agent.Audio;

internal sealed class WindowsVolumeService : IAsyncDisposable
{
    private readonly MMDeviceEnumerator _enumerator = new();
    private readonly CancellationTokenSource _stop = new();
    private Task? _pollTask;
    private VolumeState _current = VolumeState.Default;

    public event Action<VolumeState>? StateChanged;
    public VolumeState Current => _current;

    public void Start() => _pollTask = Task.Run(() => PollLoopAsync(_stop.Token));

    public Task<bool> ExecuteAsync(string action, float? level, float? delta)
    {
        try
        {
            using var device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var endpoint = device.AudioEndpointVolume;
            switch (action)
            {
                case "set" when level.HasValue:
                    endpoint.MasterVolumeLevelScalar = Math.Clamp(level.Value, 0f, 1f);
                    break;
                case "change" when delta.HasValue:
                    endpoint.MasterVolumeLevelScalar = Math.Clamp(endpoint.MasterVolumeLevelScalar + delta.Value, 0f, 1f);
                    break;
                case "mute":
                    endpoint.Mute = true;
                    break;
                case "unmute":
                    endpoint.Mute = false;
                    break;
                case "toggleMute":
                    endpoint.Mute = !endpoint.Mute;
                    break;
                default:
                    return Task.FromResult(false);
            }
            Publish(new VolumeState(endpoint.MasterVolumeLevelScalar, endpoint.Mute));
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public void RefreshNow()
    {
        try
        {
            using var device = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            Publish(new VolumeState(device.AudioEndpointVolume.MasterVolumeLevelScalar, device.AudioEndpointVolume.Mute));
        }
        catch
        {
            // Audio endpoint may disappear temporarily during a device switch.
        }
    }

    private async Task PollLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        RefreshNow();
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken)) RefreshNow();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    private void Publish(VolumeState state)
    {
        if (state == _current) return;
        _current = state;
        StateChanged?.Invoke(state);
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        if (_pollTask is not null)
        {
            try { await _pollTask; } catch (OperationCanceledException) { }
        }
        _enumerator.Dispose();
        _stop.Dispose();
    }
}

