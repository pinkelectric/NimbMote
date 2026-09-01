namespace BentleyRemote.Agent.Media;

/// <summary>
/// Keeps asynchronous artwork reads tied to the exact media metadata revision that requested
/// them. A late thumbnail read for a closed Edge tab must never overwrite newer metadata.
/// </summary>
internal sealed class ArtworkRevisionTracker
{
    private long _revision;

    /// <summary>Starts a new media-properties generation even when title/artist did not change.</summary>
    public long Advance() => Interlocked.Increment(ref _revision);

    public long Current => Interlocked.Read(ref _revision);

    public bool IsCurrent(long revision) => revision == Current;

    public void Clear() => Advance();

    [Obsolete("Use Advance for event-ordered GSMTC media properties.")]
    public bool Begin(string key, out long revision)
    {
        revision = Advance();
        return true;
    }
}
