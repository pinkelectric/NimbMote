using System.Runtime.CompilerServices;
using Windows.ApplicationModel;
using Windows.Media.Control;

namespace BentleyRemote.Diagnostics.GsmtcSessionProbe;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        ProbeOptions options;
        try
        {
            options = ProbeOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(ex.Message);
            ProbeOptions.PrintHelp();
            return 2;
        }

        if (options.Help)
        {
            ProbeOptions.PrintHelp();
            return 0;
        }

        using var log = new ProbeLog(options.LogPath);
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        try
        {
            await using var probe = await GsmtcProbe.CreateAsync(log);
            await probe.RefreshAsync("initial snapshot");

            if (options.SessionIndex is not null && options.Action is not null)
            {
                await probe.ExecuteAsync(
                    options.SessionIndex.Value,
                    options.Action,
                    options.PositionSeconds);
            }

            if (options.WatchSeconds > 0)
            {
                await probe.WatchAsync(
                    TimeSpan.FromSeconds(options.WatchSeconds),
                    TimeSpan.FromSeconds(options.PollSeconds),
                    cancellation.Token);
            }

            return 0;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            log.Line("Probe cancelled.");
            return 0;
        }
        catch (Exception ex)
        {
            log.Line($"FATAL {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }
}

internal sealed class GsmtcProbe : IAsyncDisposable
{
    private readonly GlobalSystemMediaTransportControlsSessionManager _manager;
    private readonly ProbeLog _log;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<GlobalSystemMediaTransportControlsSession, TrackedSession> _tracked =
        new(ReferenceEqualityComparer.Instance);
    private List<TrackedSession> _ordered = [];
    private int _nextProbeId;

    private GsmtcProbe(
        GlobalSystemMediaTransportControlsSessionManager manager,
        ProbeLog log)
    {
        _manager = manager;
        _log = log;
        _manager.SessionsChanged += OnSessionsChanged;
        _manager.CurrentSessionChanged += OnCurrentSessionChanged;
    }

    public static async Task<GsmtcProbe> CreateAsync(ProbeLog log)
    {
        log.Line("Requesting GlobalSystemMediaTransportControlsSessionManager...");
        var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        log.Line("GSMTC session manager acquired.");
        return new GsmtcProbe(manager, log);
    }

    public async Task RefreshAsync(string reason)
    {
        await _gate.WaitAsync();
        try
        {
            var sessions = _manager.GetSessions().ToList();
            var sessionSet = new HashSet<GlobalSystemMediaTransportControlsSession>(
                sessions,
                ReferenceEqualityComparer.Instance);
            var now = DateTimeOffset.Now;

            foreach (var removed in _tracked.Keys.Where(session => !sessionSet.Contains(session)).ToList())
            {
                var tracker = _tracked[removed];
                tracker.Unsubscribe(this);
                _tracked.Remove(removed);
                _log.Line(
                    $"DISAPPEARED ProbeId={tracker.ProbeId} firstSeen={FormatTime(tracker.FirstSeen)} " +
                    $"lastSeen={FormatTime(now)} lifetime={(now - tracker.FirstSeen).TotalSeconds:F1}s " +
                    $"lastTitle={Quote(tracker.LastTitle)} source={Quote(tracker.Source)}");
            }

            foreach (var session in sessions)
            {
                if (_tracked.ContainsKey(session)) continue;

                var tracker = new TrackedSession(
                    ++_nextProbeId,
                    session,
                    now,
                    SafeSource(session));
                tracker.Subscribe(this);
                _tracked.Add(session, tracker);
                _log.Line(
                    $"APPEARED ProbeId={tracker.ProbeId} firstSeen={FormatTime(now)} " +
                    $"runtimeRef={tracker.RuntimeReference} source={Quote(tracker.Source)}");
            }

            _ordered = sessions.Select(session => _tracked[session]).ToList();
            var current = _manager.GetCurrentSession();
            _log.Line(
                $"SNAPSHOT reason={Quote(reason)} count={_ordered.Count} " +
                $"currentProbeId={FindProbeId(current)} " +
                $"currentRuntime={Quote(FormatRuntimeReference(current))} " +
                $"currentSource={Quote(current is null ? string.Empty : SafeSource(current))} " +
                $"at={FormatTime(now)}");

            for (var index = 0; index < _ordered.Count; index++)
            {
                await LogSessionAsync(_ordered[index], index + 1, current, reason);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task WatchAsync(
        TimeSpan duration,
        TimeSpan pollInterval,
        CancellationToken cancellationToken)
    {
        _log.Line(
            $"WATCH started duration={duration.TotalSeconds:F0}s poll={pollInterval.TotalSeconds:F0}s. " +
            "Press Ctrl+C to stop.");
        var stopAt = DateTimeOffset.UtcNow + duration;
        using var timer = new PeriodicTimer(pollInterval);

        while (DateTimeOffset.UtcNow < stopAt &&
               await timer.WaitForNextTickAsync(cancellationToken))
        {
            await RefreshAsync("poll");
        }

        _log.Line("WATCH completed.");
    }

    public async Task ExecuteAsync(int oneBasedIndex, string action, double? positionSeconds)
    {
        await RefreshAsync("before command");

        TrackedSession? tracker;
        await _gate.WaitAsync();
        try
        {
            tracker = oneBasedIndex >= 1 && oneBasedIndex <= _ordered.Count
                ? _ordered[oneBasedIndex - 1]
                : null;
        }
        finally
        {
            _gate.Release();
        }

        if (tracker is null)
        {
            throw new ArgumentOutOfRangeException(
                nameof(oneBasedIndex),
                $"Session index {oneBasedIndex} is not present in the current snapshot.");
        }

        var normalizedAction = action.ToLowerInvariant();
        _log.Line(
            $"COMMAND targetIndex={oneBasedIndex} ProbeId={tracker.ProbeId} " +
            $"action={normalizedAction} title={Quote(tracker.LastTitle)} source={Quote(tracker.Source)}");

        bool accepted = normalizedAction switch
        {
            "play" => await tracker.Session.TryPlayAsync(),
            "pause" => await tracker.Session.TryPauseAsync(),
            "next" => await tracker.Session.TrySkipNextAsync(),
            "previous" => await tracker.Session.TrySkipPreviousAsync(),
            "seek" when positionSeconds is not null =>
                await TrySeekAsync(tracker.Session, positionSeconds.Value),
            "seek" => throw new ArgumentException("The seek action requires --position-seconds."),
            _ => throw new ArgumentException(
                $"Unsupported action '{action}'. Use play, pause, next, previous, or seek."),
        };

        _log.Line($"COMMAND_RESULT ProbeId={tracker.ProbeId} accepted={accepted}");
        await Task.Delay(750);
        await RefreshAsync("after command");
    }

    private static async Task<bool> TrySeekAsync(
        GlobalSystemMediaTransportControlsSession session,
        double positionSeconds)
    {
        var timeline = session.GetTimelineProperties();
        var requested = timeline.StartTime + TimeSpan.FromSeconds(Math.Max(0, positionSeconds));
        if (timeline.EndTime > timeline.StartTime && requested > timeline.EndTime)
        {
            requested = timeline.EndTime;
        }

        return await session.TryChangePlaybackPositionAsync(requested.Ticks);
    }

    private async Task LogSessionAsync(
        TrackedSession tracker,
        int oneBasedIndex,
        GlobalSystemMediaTransportControlsSession? current,
        string reason)
    {
        try
        {
            var session = tracker.Session;
            var media = await session.TryGetMediaPropertiesAsync();
            var playback = session.GetPlaybackInfo();
            var timeline = session.GetTimelineProperties();
            var controls = playback.Controls;
            var now = DateTimeOffset.Now;
            tracker.LastSeen = now;
            tracker.LastTitle = media.Title ?? string.Empty;

            var duration = timeline.EndTime > timeline.StartTime
                ? timeline.EndTime - timeline.StartTime
                : TimeSpan.Zero;
            var position = timeline.Position >= timeline.StartTime
                ? timeline.Position - timeline.StartTime
                : timeline.Position;

            _log.Block(
                $"Session {oneBasedIndex}\n" +
                $"  ProbeId: {tracker.ProbeId}\n" +
                $"  RuntimeReference: {tracker.RuntimeReference}\n" +
                $"  SourceAppUserModelId: {tracker.Source}\n" +
                $"  AppName: {ResolveAppName(tracker.Source)}\n" +
                $"  CurrentMatchReference: {ReferenceEquals(current, session)}\n" +
                $"  CurrentMatchEquals: {Equals(current, session)}\n" +
                $"  CurrentMatchObjectHash: {current?.GetHashCode() == session.GetHashCode()}\n" +
                $"  CurrentMatchSourceOnly: {current is not null && SafeSource(current) == tracker.Source}\n" +
                $"  FirstSeen: {FormatTime(tracker.FirstSeen)}\n" +
                $"  LastSeen: {FormatTime(tracker.LastSeen)}\n" +
                $"  Title: {media.Title}\n" +
                $"  Artist: {media.Artist}\n" +
                $"  PlaybackStatus: {playback.PlaybackStatus}\n" +
                $"  PlaybackRate: {playback.PlaybackRate?.ToString("F3") ?? "n/a"}\n" +
                $"  Position: {FormatDuration(position)}\n" +
                $"  Duration: {FormatDuration(duration)}\n" +
                $"  TimelineStart: {FormatDuration(timeline.StartTime)}\n" +
                $"  TimelineEnd: {FormatDuration(timeline.EndTime)}\n" +
                $"  TimelineLastUpdated: {FormatTime(timeline.LastUpdatedTime)}\n" +
                $"  ThumbnailPresent: {media.Thumbnail is not null}\n" +
                $"  Controls: Play={controls.IsPlayEnabled}, Pause={controls.IsPauseEnabled}, " +
                $"Toggle={controls.IsPlayPauseToggleEnabled}, Previous={controls.IsPreviousEnabled}, " +
                $"Next={controls.IsNextEnabled}, Seek={controls.IsPlaybackPositionEnabled}, " +
                $"Stop={controls.IsStopEnabled}, Rewind={controls.IsRewindEnabled}, " +
                $"FastForward={controls.IsFastForwardEnabled}, Rate={controls.IsPlaybackRateEnabled}, " +
                $"Shuffle={controls.IsShuffleEnabled}, Repeat={controls.IsRepeatEnabled}\n" +
                $"  SnapshotReason: {reason}");
        }
        catch (Exception ex)
        {
            _log.Line(
                $"SESSION_READ_ERROR ProbeId={tracker.ProbeId} " +
                $"type={ex.GetType().Name} message={Quote(ex.Message)}");
        }
    }

    private void OnSessionsChanged(
        GlobalSystemMediaTransportControlsSessionManager sender,
        SessionsChangedEventArgs args)
    {
        _log.Line($"EVENT SessionsChanged at={FormatTime(DateTimeOffset.Now)}");
        _ = RefreshEventAsync("SessionsChanged");
    }

    private void OnCurrentSessionChanged(
        GlobalSystemMediaTransportControlsSessionManager sender,
        CurrentSessionChangedEventArgs args)
    {
        _log.Line($"EVENT CurrentSessionChanged at={FormatTime(DateTimeOffset.Now)}");
        _ = RefreshEventAsync("CurrentSessionChanged");
    }

    internal void OnMediaPropertiesChanged(
        GlobalSystemMediaTransportControlsSession sender,
        MediaPropertiesChangedEventArgs args)
    {
        QueueSessionEvent(sender, "MediaPropertiesChanged");
    }

    internal void OnPlaybackInfoChanged(
        GlobalSystemMediaTransportControlsSession sender,
        PlaybackInfoChangedEventArgs args)
    {
        QueueSessionEvent(sender, "PlaybackInfoChanged");
    }

    internal void OnTimelinePropertiesChanged(
        GlobalSystemMediaTransportControlsSession sender,
        TimelinePropertiesChangedEventArgs args)
    {
        QueueSessionEvent(sender, "TimelinePropertiesChanged");
    }

    private void QueueSessionEvent(
        GlobalSystemMediaTransportControlsSession session,
        string eventName)
    {
        var probeId = _tracked.TryGetValue(session, out var tracker) ? tracker.ProbeId : 0;
        _log.Line(
            $"EVENT {eventName} ProbeId={probeId} at={FormatTime(DateTimeOffset.Now)}");
        _ = RefreshEventAsync(eventName);
    }

    private async Task RefreshEventAsync(string reason)
    {
        try
        {
            await RefreshAsync(reason);
        }
        catch (Exception ex)
        {
            _log.Line(
                $"EVENT_ERROR reason={reason} type={ex.GetType().Name} message={Quote(ex.Message)}");
        }
    }

    private int FindProbeId(GlobalSystemMediaTransportControlsSession? session)
    {
        if (session is null) return 0;
        if (_tracked.TryGetValue(session, out var tracker)) return tracker.ProbeId;
        tracker = _tracked.Values.FirstOrDefault(item => Equals(item.Session, session));
        return tracker?.ProbeId ?? -1;
    }

    private static string ResolveAppName(string source)
    {
        try
        {
            var appInfo = AppInfo.GetFromAppUserModelId(source);
            return appInfo?.DisplayInfo.DisplayName ?? source;
        }
        catch
        {
            return source;
        }
    }

    private static string SafeSource(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            return session.SourceAppUserModelId ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string FormatDuration(TimeSpan value) =>
        value < TimeSpan.Zero ? $"-{FormatDuration(-value)}" : value.ToString("hh\\:mm\\:ss\\.fff");

    private static string FormatTime(DateTimeOffset value) =>
        value.ToString("yyyy-MM-dd HH:mm:ss.fff zzz");

    private static string FormatRuntimeReference(
        GlobalSystemMediaTransportControlsSession? session) =>
        session is null
            ? "none"
            : $"ref=0x{RuntimeHelpers.GetHashCode(session):X8},hash=0x{session.GetHashCode():X8}";

    private static string Quote(string? value) =>
        $"\"{(value ?? string.Empty).Replace("\"", "\\\"")}\"";

    public ValueTask DisposeAsync()
    {
        _manager.SessionsChanged -= OnSessionsChanged;
        _manager.CurrentSessionChanged -= OnCurrentSessionChanged;
        foreach (var tracker in _tracked.Values) tracker.Unsubscribe(this);
        _tracked.Clear();
        _gate.Dispose();
        return ValueTask.CompletedTask;
    }

    private sealed class TrackedSession(
        int probeId,
        GlobalSystemMediaTransportControlsSession session,
        DateTimeOffset firstSeen,
        string source)
    {
        public int ProbeId { get; } = probeId;
        public GlobalSystemMediaTransportControlsSession Session { get; } = session;
        public DateTimeOffset FirstSeen { get; } = firstSeen;
        public DateTimeOffset LastSeen { get; set; } = firstSeen;
        public string Source { get; } = source;
        public string LastTitle { get; set; } = string.Empty;
        public string RuntimeReference { get; } = FormatRuntimeReference(session);

        public void Subscribe(GsmtcProbe probe)
        {
            Session.MediaPropertiesChanged += probe.OnMediaPropertiesChanged;
            Session.PlaybackInfoChanged += probe.OnPlaybackInfoChanged;
            Session.TimelinePropertiesChanged += probe.OnTimelinePropertiesChanged;
        }

        public void Unsubscribe(GsmtcProbe probe)
        {
            Session.MediaPropertiesChanged -= probe.OnMediaPropertiesChanged;
            Session.PlaybackInfoChanged -= probe.OnPlaybackInfoChanged;
            Session.TimelinePropertiesChanged -= probe.OnTimelinePropertiesChanged;
        }
    }
}

internal sealed class ProbeLog : IDisposable
{
    private readonly object _lock = new();
    private readonly StreamWriter? _file;

    public ProbeLog(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        _file = new StreamWriter(fullPath, append: true) { AutoFlush = true };
        Line($"Log file: {fullPath}");
    }

    public void Line(string value)
    {
        lock (_lock)
        {
            Console.WriteLine(value);
            _file?.WriteLine(value);
        }
    }

    public void Block(string value)
    {
        lock (_lock)
        {
            Console.WriteLine(value);
            Console.WriteLine();
            _file?.WriteLine(value);
            _file?.WriteLine();
        }
    }

    public void Dispose() => _file?.Dispose();
}

internal sealed record ProbeOptions(
    bool Help,
    int WatchSeconds,
    int PollSeconds,
    string? LogPath,
    int? SessionIndex,
    string? Action,
    double? PositionSeconds)
{
    public static ProbeOptions Parse(string[] args)
    {
        var help = false;
        var watchSeconds = 0;
        var pollSeconds = 5;
        string? logPath = null;
        int? sessionIndex = null;
        string? action = null;
        double? positionSeconds = null;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            string NextValue()
            {
                if (++index >= args.Length)
                {
                    throw new ArgumentException($"Missing value after {argument}.");
                }

                return args[index];
            }

            switch (argument)
            {
                case "-h":
                case "--help":
                    help = true;
                    break;
                case "--watch":
                    watchSeconds = int.Parse(NextValue());
                    break;
                case "--poll":
                    pollSeconds = int.Parse(NextValue());
                    break;
                case "--log":
                    logPath = NextValue();
                    break;
                case "--session":
                    sessionIndex = int.Parse(NextValue());
                    break;
                case "--action":
                    action = NextValue();
                    break;
                case "--position-seconds":
                    positionSeconds = double.Parse(
                        NextValue(),
                        System.Globalization.CultureInfo.InvariantCulture);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {argument}");
            }
        }

        if (watchSeconds < 0) throw new ArgumentException("--watch must be non-negative.");
        if (pollSeconds < 1) throw new ArgumentException("--poll must be at least 1 second.");
        if ((sessionIndex is null) != (action is null))
        {
            throw new ArgumentException("--session and --action must be specified together.");
        }

        return new ProbeOptions(
            help,
            watchSeconds,
            pollSeconds,
            logPath,
            sessionIndex,
            action,
            positionSeconds);
    }

    public static void PrintHelp()
    {
        Console.WriteLine(
            """
            GSMTC Session Probe

            Read one snapshot:
              GsmtcSessionProbe.exe

            Watch list and session events for 10 minutes:
              GsmtcSessionProbe.exe --watch 600 --poll 5 --log <path>

            Address one session from the initial numbered snapshot:
              GsmtcSessionProbe.exe --session 2 --action play --watch 5
              GsmtcSessionProbe.exe --session 2 --action pause --watch 5
              GsmtcSessionProbe.exe --session 2 --action seek --position-seconds 30 --watch 5

            Actions: play, pause, previous, next, seek.
            The probe never sends a command unless --session and --action are both present.
            """);
    }
}
