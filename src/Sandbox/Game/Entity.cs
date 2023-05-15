using LibExec.Network;

namespace Sandbox.Game;

public sealed class Entity : NetworkObject
{
    public int Value { get; private set; }

    [Server]
    public void IncrementValueServer()
    {
        IncrementValueMulticast(5);
    }

    [Multicast]
    private void IncrementValueMulticast(int delta)
    {
        Value += delta;
    }
}