namespace BentleyRemote.Agent.SystemActions;

internal interface ISystemPowerController
{
    bool Lock();
    bool Sleep();
    bool Restart();
    bool Shutdown();
}
