using LibExec.Network;
using Sandbox.Components;

namespace Sandbox.Game;

[NetworkPlayer]
public sealed class Player : NetworkObject
{
    [Replicate] public string Pseudo = null!; // todo: make it a property

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

    public override void OnSpawn()
    {
        if (IsOwner)
        {
            SetPseudoServer(Setup.Pseudo);
        }
    }

    [Server]
    private void SetPseudoServer(string pseudo)
    {
        Pseudo = pseudo;
    }
}