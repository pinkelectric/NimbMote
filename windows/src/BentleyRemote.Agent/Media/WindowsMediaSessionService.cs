using Windows.Media.Control;
using Windows.Storage.Streams;

namespace BentleyRemote.Agent.Media;

internal sealed class WindowsMediaSessionService : IAsyncDisposable
{
    private const ulong MaxArtworkBytes = 512 * 1024;
    private readonly CancellationTokenSource _stop = new();
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private Task? _pollTask;
    private MediaState _current = MediaState.Empty;
    private readonly ArtworkRevisionTracker _artworkRevision = new();
    private string? _artworkMime;
    private string? _artworkBase64;
    private GlobalSystemMediaTransportControlsSession? _subscribedSession;

    public event Action<MediaState>? StateChanged;
    public MediaState Current => _current;

    public async Task StartAsync()
    {
        _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        _manager.CurrentSessionChanged += OnCurrentSessionChanged;
        await RefreshNowAsync();
        _pollTask = Task.Run(() => PollLoopAsync(_stop.Token));
    }

    public async Task RefreshNowAsync()
    {
        if (_manager is null) return;
        if (!await _refreshGate.WaitAsync(0)) return;
        try
        {
            var session = _manager.GetCurrentSession();
            SubscribeToSession(session);
            var next = session is null ? MediaState.Empty : await ReadStateAsync(session);
            if (next != _current)
            {
                _current = next;
                StateChanged?.Invoke(next);
            }
        }
        catch
        {
            if (_current != MediaState.Empty)
            {
                _current = MediaState.Empty;
                StateChanged?.Invoke(_current);
            }
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender,
        CurrentSessionChangedEventArgs args) => QueueRefresh();

    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender,
        MediaPropertiesChangedEventArgs args) => QueueRefresh();

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender,
        PlaybackInfoChangedEventArgs args) => QueueRefresh();

    private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender,
        TimelinePropertiesChangedEventArgs args) => QueueRefresh();

    private void QueueRefresh() => _ = Task.Run(RefreshNowAsync);

    private void SubscribeToSession(GlobalSystemMediaTransportControlsSession? session)
    {
        if (ReferenceEquals(_subscribedSession, session)) return;
        if (_subscribedSession is not null)
        {
            _subscribedSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
            _subscribedSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            _subscribedSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
        }
        _subscribedSession = session;
        _artworkRevision.Clear();
        _artworkMime = null;
        _artworkBase64 = null;
        if (session is not null)
        {
            session.MediaPropertiesChanged += OnMediaPropertiesChanged;
            session.PlaybackInfoChanged += OnPlaybackInfoChanged;
            session.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
        }
    }

    public async Task<bool> ExecuteAsync(string action, long? positionMs, long? offsetMs)
    {
        var session = _manager?.GetCurrentSession();
        if (session is null) return false;
        bool result;
        switch (action)
        {
            case "play":
                result = await session.TryPlayAsync();
                break;
            case "pause":
                result = await session.TryPauseAsync();
                break;
            case "toggle":
                result = await session.TryTogglePlayPauseAsync();
                break;
            case "previous":
                result = await session.TrySkipPreviousAsync();
                break;
            case "next":
                result = await session.TrySkipNextAsync();
                break;
            case "seek" when positionMs.HasValue:
            {
                var timeline = session.GetTimelineProperties();
                var requested = timeline.StartTime + TimeSpan.FromMilliseconds(Math.Max(0, positionMs.Value));
                if (timeline.EndTime > timeline.StartTime && requested > timeline.EndTime) requested = timeline.EndTime;
                result = await session.TryChangePlaybackPositionAsync(requested.Ticks);
                break;
            }
            case "seekBy" when offsetMs.HasValue:
            {
                var timeline = session.GetTimelineProperties();
                var requested = timeline.Position + TimeSpan.FromMilliseconds(offsetMs.Value);
                if (requested < timeline.StartTime) requested = timeline.StartTime;
                if (timeline.EndTime > timeline.StartTime && requested > timeline.EndTime) requested = timeline.EndTime;
                result = await session.TryChangePlaybackPositionAsync(requested.Ticks);
                break;
            }
            default:
                return false;
        }
        await Task.Delay(100);
        await RefreshNowAsync();
        return result;
    }

    private async Task PollLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken)) await RefreshNowAsync();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    private async Task<MediaState> ReadStateAsync(GlobalSystemMediaTransportControlsSession session)
    {
        var media = await session.TryGetMediaPropertiesAsync();
        var playback = session.GetPlaybackInfo();
        var timeline = session.GetTimelineProperties();
        var controls = playback.Controls;
        var durationValue = (timeline.EndTime - timeline.StartTime).TotalMilliseconds;
        long? durationMs = durationValue > 0 ? (long)durationValue : null;
        var relativePosition = timeline.Position - timeline.StartTime;
        long? positionMs = relativePosition >= TimeSpan.Zero ? (long)relativePosition.TotalMilliseconds : null;

        // Edge intentionally keeps one GSMTC object while changing tabs.  The richer fingerprint
        // detects the new metadata and clears old art before the new thumbnail is read.
        var artworkKey = $"{session.SourceAppUserModelId}\n{media.Title}\n{media.Artist}\n{media.AlbumTitle}\n{media.AlbumArtist}\n{durationMs}";
        var artworkChanged = _artworkRevision.Begin(artworkKey, out var artworkRevision);
        if (artworkChanged)
        {
            // Publish a metadata-only state first; the following thumbnail update must belong to
            // this revision and cannot retain artwork from the old Edge tab.
            _artworkMime = null;
            _artworkBase64 = null;
        }
        if (_artworkBase64 is null)
        {
            var artwork = await ReadArtworkAsync(media.Thumbnail);
            if (_artworkRevision.IsCurrent(artworkRevision))
                (_artworkMime, _artworkBase64) = artwork;
        }

        return new MediaState(
            true,
            session.SourceAppUserModelId,
            session.SourceAppUserModelId,
            string.IsNullOrWhiteSpace(media.Title) ? "Windows media" : media.Title,
            media.Artist ?? string.Empty,
            playback.PlaybackStatus.ToString().ToLowerInvariant(),
            positionMs,
            durationMs,
            controls.IsPlaybackPositionEnabled,
            controls.IsPreviousEnabled,
            controls.IsNextEnabled,
            _artworkMime,
            _artworkBase64);
    }

    private static async Task<(string? Mime, string? Base64)> ReadArtworkAsync(IRandomAccessStreamReference? reference)
    {
        if (reference is null) return (null, null);
        try
        {
            using var stream = await reference.OpenReadAsync();
            if (stream.Size == 0 || stream.Size > MaxArtworkBytes) return (null, null);
            using var reader = new DataReader(stream.GetInputStreamAt(0));
            var size = (uint)stream.Size;
            await reader.LoadAsync(size);
            var bytes = new byte[(int)size];
            reader.ReadBytes(bytes);
            var mime = bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 ? "image/png"
                : bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 ? "image/jpeg"
                : "application/octet-stream";
            return (mime, Convert.ToBase64String(bytes));
        }
        catch
        {
            return (null, null);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        if (_manager is not null) _manager.CurrentSessionChanged -= OnCurrentSessionChanged;
        SubscribeToSession(null);
        if (_pollTask is not null)
        {
            try { await _pollTask; } catch (OperationCanceledException) { }
        }
        _refreshGate.Dispose();
        _stop.Dispose();
    }
}
