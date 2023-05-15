using LibExec.Network;

namespace Sandbox.Game;

[NetworkPlayer]
public sealed class Player : NetworkObject
{
    public event Action? PingEvent;

    [Server]
    public void PingServer()
    {
        PingClient();
    }

    [Client]
    private void PingClient()
    {
        PingEvent?.Invoke();
    }
}