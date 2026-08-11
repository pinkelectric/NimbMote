namespace BentleyRemote.Agent.Audio;

internal sealed record VolumeState(float Level, bool Muted)
{
    public static readonly VolumeState Default = new(0, false);
}

