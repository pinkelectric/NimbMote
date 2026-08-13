namespace BentleyRemote.Agent.Installation;

internal static class AgentInstallLayout
{
    internal const string ProductFolderName = "BentleyRemote";
    internal const string AgentFolderName = "Agent";
    internal const string ExecutableName = "BentleyRemote.Agent.exe";
    internal const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    internal const string RunValueName = "Bentley Remote";
    internal const string LegacyRunValueName = "BentleyRemote.Agent";

    internal static string InstallRoot(string localAppData) => Path.Combine(localAppData, ProductFolderName);
    internal static string AgentDirectory(string localAppData) => Path.Combine(InstallRoot(localAppData), AgentFolderName);
    internal static string ExecutablePath(string localAppData) => Path.Combine(AgentDirectory(localAppData), ExecutableName);
    internal static string StartupCommand(string localAppData) => $"\"{ExecutablePath(localAppData)}\"";
}
