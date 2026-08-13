namespace BentleyRemote.Agent.Core;

/// <summary>
/// Startup is intentionally split: LAN reconnect is allowed to run before optional Windows
/// media APIs have finished coming online after sign-in.
/// </summary>
internal static class StartupReadinessPolicy
{
    internal static readonly TimeSpan MediaAttemptWindow = TimeSpan.FromSeconds(15);

    internal static TimeSpan NextMediaRetryDelay(int failedAttempts) =>
        TimeSpan.FromSeconds(Math.Min(5 * Math.Max(1, failedAttempts), 30));
}
