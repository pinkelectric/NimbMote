namespace BentleyRemote.Agent;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var context = new AgentApplicationContext();
        Application.Run(context);
    }
}

