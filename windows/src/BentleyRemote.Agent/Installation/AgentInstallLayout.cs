namespace BentleyRemote.Agent.Installation;

internal static class AgentInstallLayout
{
    internal const string ProductFolderName = "BentleyRemote";
    internal const string ProgramFilesProductFolderName = "Bentley Remote";
    internal const string ExecutableName = "BentleyRemote.Agent.exe";
    internal const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    internal const string RunValueName = "Bentley Remote";
    internal const string LegacyRunValueName = "BentleyRemote.Agent";

    internal static string PairingConfigDirectory(string localAppData) => Path.Combine(localAppData, ProductFolderName);
    internal static string InstallDirectory(string programFiles) => Path.Combine(programFiles, ProgramFilesProductFolderName);
    internal static string ExecutablePath(string programFiles) => Path.Combine(InstallDirectory(programFiles), ExecutableName);
    internal static string StartupCommand(string programFiles) => $"\"{ExecutablePath(programFiles)}\"";
}
