using LibExec.Network;
using Sandbox.Components;

namespace Sandbox.Game;

[NetworkPlayer]
public sealed class Player : NetworkObject
{
    [Replicate(SkipOwner)] public string Pseudo = null!; // todo: make it a property

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
        if (IsOwner && !string.IsNullOrEmpty(Setup.Pseudo))
        {
            Pseudo = Setup.Pseudo;
            SetPseudoServer(Setup.Pseudo);
        }
    }

    [Server]
    private void SetPseudoServer(string pseudo)
    {
        Pseudo = pseudo;
    }
}