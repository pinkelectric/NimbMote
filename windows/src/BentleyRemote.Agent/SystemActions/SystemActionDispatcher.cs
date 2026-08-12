namespace BentleyRemote.Agent.SystemActions;

internal sealed class SystemActionDispatcher
{
    internal static readonly IReadOnlySet<string> AllowedActions =
        new HashSet<string>(StringComparer.Ordinal) { "lock", "sleep", "restart", "shutdown" };

    private readonly ISystemPowerController _controller;

    public SystemActionDispatcher(ISystemPowerController controller) => _controller = controller;

    public bool TrySchedule(string action, TimeSpan delay, Action<bool>? completed = null)
    {
        if (!AllowedActions.Contains(action)) return false;
        _ = Task.Run(async () =>
        {
            if (delay > TimeSpan.Zero) await Task.Delay(delay);
            bool result;
            try
            {
                result = action switch
                {
                    "lock" => _controller.Lock(),
                    "sleep" => _controller.Sleep(),
                    "restart" => _controller.Restart(),
                    "shutdown" => _controller.Shutdown(),
                    _ => false
                };
            }
            catch
            {
                result = false;
            }
            completed?.Invoke(result);
        });
        return true;
    }
}
