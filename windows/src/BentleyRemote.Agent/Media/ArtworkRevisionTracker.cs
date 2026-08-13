namespace BentleyRemote.Agent.Media;

/// <summary>
/// Keeps asynchronous artwork reads tied to the exact media metadata revision that requested
/// them. A late thumbnail read for a closed Edge tab must never overwrite newer metadata.
/// </summary>
internal sealed class ArtworkRevisionTracker
{
    private string? _key;
    private long _revision;

    public bool Begin(string key, out long revision)
    {
        var changed = !StringComparer.Ordinal.Equals(key, _key);
        if (changed)
        {
            _key = key;
            _revision++;
        }
        revision = _revision;
        return changed;
    }

    public bool IsCurrent(long revision) => revision == _revision;

    public void Clear()
    {
        _key = null;
        _revision++;
    }
}
