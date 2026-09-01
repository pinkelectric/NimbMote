namespace BentleyRemote.Agent.Media;

internal sealed record MediaState(
    bool HasSession,
    string? SessionId,
    string? SourceAppId,
    string Title,
    string Artist,
    string PlaybackStatus,
    long? PositionMs,
    long? DurationMs,
    bool CanSeek,
    bool CanPrevious,
    bool CanNext,
    string? ArtworkMime,
    string? ArtworkBase64)
{
    public static readonly MediaState Empty = new(
        false, null, null, "Nothing playing", "", "closed", null, null,
        false, false, false, null, null);
}

