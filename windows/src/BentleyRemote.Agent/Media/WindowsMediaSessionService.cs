using Windows.Media.Control;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;

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
    private long _mediaPropertiesRevision;
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
        // Do not drop a real GSMTC event while thumbnail decoding is in progress. The
        // generation check makes queued refreshes harmless, and guarantees a new read follows.
        await _refreshGate.WaitAsync();
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
        MediaPropertiesChangedEventArgs args)
    {
        // Edge reuses a GSMTC object across videos and can emit identical/empty metadata. Event
        // order is therefore the identity: invalidate artwork before any asynchronous read ends.
        var revision = _artworkRevision.Advance();
        Interlocked.Exchange(ref _mediaPropertiesRevision, revision);
        _artworkMime = null;
        _artworkBase64 = null;
        Log($"artwork revision={revision} source=media-properties cleared");
        PublishArtworkCleared();
        QueueRefresh();
    }

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
        var revision = _artworkRevision.Advance();
        Interlocked.Exchange(ref _mediaPropertiesRevision, revision);
        _artworkMime = null;
        _artworkBase64 = null;
        if (session is not null)
        {
            session.MediaPropertiesChanged += OnMediaPropertiesChanged;
            session.PlaybackInfoChanged += OnPlaybackInfoChanged;
            session.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
        }
    }

    private void PublishArtworkCleared()
    {
        var previous = _current;
        if (!previous.HasSession || (previous.ArtworkBase64 is null && previous.ArtworkMime is null)) return;
        _current = previous with { ArtworkMime = null, ArtworkBase64 = null };
        StateChanged?.Invoke(_current);
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

        var artworkRevision = Interlocked.Read(ref _mediaPropertiesRevision);
        if (artworkRevision == 0)
        {
            artworkRevision = _artworkRevision.Advance();
            Interlocked.Exchange(ref _mediaPropertiesRevision, artworkRevision);
        }
        if (_artworkBase64 is null)
        {
            var artwork = await ReadArtworkAsync(media.Thumbnail, artworkRevision);
            if (_artworkRevision.IsCurrent(artworkRevision))
                (_artworkMime, _artworkBase64) = artwork;
            else
                Log($"artwork revision={artworkRevision} discarded-stale current={_artworkRevision.Current}");
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

    private static async Task<(string? Mime, string? Base64)> ReadArtworkAsync(IRandomAccessStreamReference? reference, long revision)
    {
        if (reference is null) return (null, null);
        try
        {
            using var stream = await reference.OpenReadAsync();
            if (stream.Size == 0 || stream.Size > MaxArtworkBytes)
            {
                Log($"artwork revision={revision} rejected bytes={stream.Size}");
                return (null, null);
            }
            var decoder = await BitmapDecoder.CreateAsync(stream);
            Log($"artwork revision={revision} decoded dimensions={decoder.PixelWidth}x{decoder.PixelHeight} bytes={stream.Size}");
            using var reader = new DataReader(stream.GetInputStreamAt(0));
            var size = (uint)stream.Size;
            await reader.LoadAsync(size);
            var bytes = new byte[(int)size];
            reader.ReadBytes(bytes);
            var mime = bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 ? "image/png"
                : bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 ? "image/jpeg"
                : "application/octet-stream";
            Log($"artwork revision={revision} decoded bytes={bytes.Length} mime={mime}");
            return (mime, Convert.ToBase64String(bytes));
        }
        catch
        {
            return (null, null);
        }
    }

    private static void Log(string message) => System.Diagnostics.Debug.WriteLine($"BentleyRemote.Media {message}");

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
