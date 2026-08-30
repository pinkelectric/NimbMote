namespace BentleyRemote.Agent;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using var context = new AgentApplicationContext(
            showPairingOnStart: args.Contains("--show-pairing", StringComparer.OrdinalIgnoreCase));
        Application.Run(context);
    }
}
