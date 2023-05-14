using LibExec.Network;

namespace Sandbox.Game;

public sealed class Entity : NetworkObject
{
    public int Value { get; private set; }

    [Server]
    public void IncrementValueServer()
    {
        IncrementValueMulticast();
    }

    [Multicast]
    private void IncrementValueMulticast()
    {
        Value++;
    }
}